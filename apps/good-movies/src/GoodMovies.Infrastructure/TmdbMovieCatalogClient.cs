using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GoodMovies.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoodMovies.Infrastructure;

/// <summary>
/// Platform-neutral TMDB client. All provider calls go through this typed
/// client so query construction and JSON handling stay in one place.
/// </summary>
internal sealed class TmdbMovieCatalogClient : IMovieCatalogProvider, IMovieTrailerLookup
{
    private readonly HttpClient _httpClient;
    private readonly GoodMoviesInfrastructureOptions _options;
    private readonly ILogger<TmdbMovieCatalogClient> _logger;
    private readonly Uri _apiBaseAddress;
    private readonly PosterUrlBuilder _posterUrlBuilder;
    private readonly TimeProvider _timeProvider;

    [ActivatorUtilitiesConstructor]
    public TmdbMovieCatalogClient(
        HttpClient httpClient,
        GoodMoviesInfrastructureOptions options,
        TimeProvider? timeProvider = null,
        ILogger<TmdbMovieCatalogClient>? logger = null
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _apiBaseAddress = NormalizeBaseAddress(_httpClient.BaseAddress ?? _options.ApiBaseAddress);
        _posterUrlBuilder = new PosterUrlBuilder(_options);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<TmdbMovieCatalogClient>.Instance;
    }

    public async Task<CatalogFetchResult> FetchAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            List<TmdbDiscoverMovie> candidates = await LoadCandidatesAsync(today, cancellationToken)
                .ConfigureAwait(false);
            if (candidates.Count == 0)
            {
                return CatalogFetchResult.Success(Array.Empty<Movie>(), _timeProvider.GetUtcNow());
            }

            Dictionary<int, string> genres = await LoadGenresAsync(cancellationToken)
                .ConfigureAwait(false);
            List<Movie> movies = await VerifyCandidatesAsync(
                    candidates,
                    genres,
                    today,
                    cancellationToken
                )
                .ConfigureAwait(false);

            MovieCatalogSnapshot snapshot = new MovieCatalogSnapshot(movies, today);
            return CatalogFetchResult.Success(snapshot.Movies, _timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GoodMoviesConfigurationException exception)
        {
            _logger.CatalogRefreshMissingConfiguration();
            return CatalogFetchResult.MissingConfiguration(exception);
        }
        catch (Exception exception)
        {
            CatalogRefreshException failure =
                exception as CatalogRefreshException
                ?? new CatalogRefreshException("The movie catalog refresh failed.", exception);
            _logger.CatalogRefreshFailed();
            return CatalogFetchResult.Failure(failure);
        }
    }

    public async Task<TrailerLookupResult> GetTrailerAsync(
        int movieId,
        CancellationToken cancellationToken = default
    )
    {
        if (movieId <= 0)
        {
            return TrailerLookupResult.Failure(
                new ArgumentOutOfRangeException(nameof(movieId), "A movie ID must be positive.")
            );
        }

        try
        {
            TmdbVideosResponse response = await GetJsonAsync(
                    BuildVideosUri(movieId),
                    GoodMoviesJsonContext.Default.TmdbVideosResponse,
                    cancellationToken
                )
                .ConfigureAwait(false);

            MovieTrailer? selected = TrailerSelectionPolicy.Select(
                response
                    .Results.Where(static video => video is not null)
                    .Select(static video => new MovieTrailer(
                        video.Key ?? string.Empty,
                        video.Site ?? string.Empty,
                        video.Type ?? string.Empty,
                        video.Official,
                        video.LanguageCode
                    ))
            );
            return selected is null
                ? TrailerLookupResult.NotFound()
                : TrailerLookupResult.Found(selected);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GoodMoviesConfigurationException exception)
        {
            _logger.TrailerLookupMissingConfiguration();
            return TrailerLookupResult.MissingConfiguration(exception);
        }
        catch (Exception exception)
        {
            _logger.TrailerLookupFailed();
            return TrailerLookupResult.Failure(exception);
        }
    }

    private async Task<Dictionary<int, string>> LoadGenresAsync(CancellationToken cancellationToken)
    {
        TmdbGenreListResponse response = await GetJsonAsync(
                BuildGenresUri(),
                GoodMoviesJsonContext.Default.TmdbGenreListResponse,
                cancellationToken
            )
            .ConfigureAwait(false);

        Dictionary<int, string> genres = new();
        foreach (TmdbGenre genre in response.Genres)
        {
            if (genre.Id > 0 && !string.IsNullOrWhiteSpace(genre.Name))
            {
                genres.TryAdd(genre.Id, genre.Name.Trim());
            }
        }

        return genres;
    }

    private async Task<List<TmdbDiscoverMovie>> LoadCandidatesAsync(
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        DateOnly earliestDate = ReleaseWindowPolicy.EarliestVisibleDate(today);
        DateOnly latestDate = ReleaseWindowPolicy.LatestVisibleDate(today);
        // The API can repeat a movie across pages and across both passes. Keep
        // the first complete candidate in deterministic sequence and verify each
        // ID once.
        Dictionary<int, TmdbDiscoverMovie> unique = new();
        for (int pass = 0; pass < 2; pass++)
        {
            List<TmdbDiscoverMovie> candidates = await LoadCandidatePagesAsync(
                    earliestDate,
                    latestDate,
                    familyPass: pass == 1,
                    cancellationToken
                )
                .ConfigureAwait(false);
            foreach (TmdbDiscoverMovie candidate in candidates)
            {
                if (candidate.Id > 0)
                {
                    unique.TryAdd(candidate.Id, candidate);
                }
            }
        }

        return unique.Values.OrderBy(static candidate => candidate.Id).ToList();
    }

    private async Task<List<TmdbDiscoverMovie>> LoadCandidatePagesAsync(
        DateOnly earliestDate,
        DateOnly latestDate,
        bool familyPass,
        CancellationToken cancellationToken
    )
    {
        TmdbDiscoverResponse firstPage = await GetJsonAsync(
                BuildDiscoverUri(1, earliestDate, latestDate, familyPass),
                GoodMoviesJsonContext.Default.TmdbDiscoverResponse,
                cancellationToken
            )
            .ConfigureAwait(false);

        int totalPages = firstPage.TotalPages;
        if (totalPages < 0)
        {
            throw new TmdbProtocolException("TMDB returned a negative page count.");
        }

        if (totalPages == 0)
        {
            totalPages = 1;
        }

        // Reading fewer of the furthest-out pages is far better than failing the
        // whole refresh, so the cap clamps instead of throwing.
        if (totalPages > _options.MaxPages)
        {
            totalPages = _options.MaxPages;
        }

        List<TmdbDiscoverMovie> candidates = new(firstPage.Results);
        for (int page = 2; page <= totalPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TmdbDiscoverResponse response = await GetJsonAsync(
                    BuildDiscoverUri(page, earliestDate, latestDate, familyPass),
                    GoodMoviesJsonContext.Default.TmdbDiscoverResponse,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (response.TotalPages < 0)
            {
                throw new TmdbProtocolException("TMDB returned a negative page count.");
            }

            if (
                response.TotalPages > 0
                && response.TotalPages < totalPages
                && response.TotalPages < _options.MaxPages
            )
            {
                throw new TmdbProtocolException("TMDB changed the page count during a refresh.");
            }

            candidates.AddRange(response.Results);
        }

        return candidates;
    }

    private async Task<List<Movie>> VerifyCandidatesAsync(
        List<TmdbDiscoverMovie> candidates,
        IReadOnlyDictionary<int, string> genres,
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        Movie?[] verified = new Movie?[candidates.Count];
        await Parallel
            .ForEachAsync(
                Enumerable.Range(0, candidates.Count),
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = _options.MaxConcurrentRequests,
                },
                async (index, token) =>
                {
                    verified[index] = await VerifyAndMapAsync(
                            candidates[index],
                            genres,
                            today,
                            token
                        )
                        .ConfigureAwait(false);
                }
            )
            .ConfigureAwait(false);

        return verified.Where(static movie => movie is not null).Cast<Movie>().ToList();
    }

    private async Task<Movie?> VerifyAndMapAsync(
        TmdbDiscoverMovie candidate,
        IReadOnlyDictionary<int, string> genres,
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        TmdbReleaseDatesResponse response = await GetJsonAsync(
                BuildReleaseDatesUri(candidate.Id),
                GoodMoviesJsonContext.Default.TmdbReleaseDatesResponse,
                cancellationToken
            )
            .ConfigureAwait(false);

        VerifiedRelease? selected = null;
        bool hasDisallowedCertification = false;
        foreach (TmdbReleaseCountry country in response.Results)
        {
            if (
                !string.Equals(
                    country.CountryCode?.Trim(),
                    "US",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            foreach (TmdbReleaseDate release in country.ReleaseDates)
            {
                string reported = release.Certification?.Trim() ?? string.Empty;
                bool isRated = reported.Length > 0;
                MovieCertification? certification = null;
                if (isRated && !MovieCertification.TryCreate(reported, out certification))
                {
                    // A US certification we do not allow disqualifies the whole
                    // movie, even if another entry for it is still unrated.
                    hasDisallowedCertification = true;
                    continue;
                }

                if (
                    !TheatricalRelease.IsAllowedTheatricalType(release.Type)
                    || !TryParseReleaseDate(release.ReleaseDate, out DateOnly releaseDate)
                    || !ReleaseWindowPolicy.IsVisible(releaseDate, today)
                )
                {
                    continue;
                }

                VerifiedRelease value = new(releaseDate, release.Type, certification);
                if (
                    selected is null
                    || value.ReleaseDate < selected.ReleaseDate
                    || (
                        value.ReleaseDate == selected.ReleaseDate
                        && value.ReleaseType < selected.ReleaseType
                    )
                )
                {
                    selected = value;
                }
            }
        }

        if (selected is null || hasDisallowedCertification)
        {
            return null;
        }

        MovieGenre[] mappedGenres = candidate
            .GenreIds.Where(static id => id > 0)
            .Distinct()
            .Where(genres.ContainsKey)
            .Select(id => new MovieGenre(id, genres[id]))
            .ToArray();

        if (
            selected.Certification is null
            && (
                !candidate.GenreIds.Any(MovieGenre.IsFamilyAudienceGenre)
                || !string.Equals(
                    candidate.OriginalLanguage?.Trim(),
                    "en",
                    StringComparison.OrdinalIgnoreCase
                )
                || candidate.Popularity < _options.MinimumUnratedPopularity
            )
        )
        {
            // Not rated yet, so we only vouch for animation and family titles
            // that are prominent enough to be a real theatrical release.
            return null;
        }

        Uri? posterUri = _posterUrlBuilder.Build(candidate.PosterPath);
        string? safePosterPath = posterUri is null ? null : candidate.PosterPath;

        return new Movie(
            candidate.Id,
            candidate.Title ?? string.Empty,
            selected.Certification?.Code,
            new[] { new TheatricalRelease(selected.ReleaseDate, "US", selected.ReleaseType) },
            mappedGenres,
            candidate.Overview,
            safePosterPath,
            posterUri,
            candidate.OriginalLanguage,
            candidate.GenreIds
        );
    }

    private async Task<T> GetJsonAsync<T>(
        Uri endpoint,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);

        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new TmdbRequestException(
                $"TMDB returned HTTP {(int)response.StatusCode}.",
                response.StatusCode
            );
        }

        try
        {
            await using Stream content = await response
                .Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            T? value = await JsonSerializer
                .DeserializeAsync(content, jsonTypeInfo, cancellationToken)
                .ConfigureAwait(false);
            return value
                ?? throw new TmdbProtocolException("TMDB returned an empty JSON document.");
        }
        catch (JsonException exception)
        {
            throw new TmdbProtocolException("TMDB returned malformed JSON.", exception);
        }
    }

    /// <summary>
    /// The rated pass filters on the US certification so only G/PG titles come
    /// back. TMDB only carries a certification once the MPAA has rated a movie,
    /// which hides most releases more than a few months out, so the family pass
    /// drops that filter and asks for animation or family titles instead. Every
    /// candidate is still verified per movie before it reaches the catalog.
    /// </summary>
    private Uri BuildDiscoverUri(
        int page,
        DateOnly earliestDate,
        DateOnly latestDate,
        bool familyPass
    ) =>
        BuildUri(
            "3/discover/movie",
            $"region=US"
                + $"&include_adult=false"
                + $"&language={Escape("en-US")}"
                + $"&sort_by={Escape("primary_release_date.asc")}"
                + $"&primary_release_date.gte={earliestDate:yyyy-MM-dd}"
                + $"&primary_release_date.lte={latestDate:yyyy-MM-dd}"
                + $"&with_release_type={Escape("2|3")}"
                + (
                    familyPass
                        ? $"&with_genres={Escape($"{MovieGenre.AnimationId}|{MovieGenre.FamilyId}")}"
                            + $"&with_original_language=en"
                        : $"&certification_country=US&certification.lte=PG"
                )
                + $"&page={page}"
        );

    private Uri BuildGenresUri() => BuildUri("3/genre/movie/list", "language=en-US");

    private Uri BuildReleaseDatesUri(int movieId) =>
        BuildUri($"3/movie/{movieId}/release_dates", string.Empty);

    private Uri BuildVideosUri(int movieId) =>
        BuildUri($"3/movie/{movieId}/videos", "language=en-US");

    private Uri BuildUri(string path, string query)
    {
        UriBuilder builder = new(_apiBaseAddress)
        {
            Path = $"{_apiBaseAddress.AbsolutePath.TrimEnd('/')}/{path.TrimStart('/')}",
            Query = query,
        };
        return builder.Uri;
    }

    private static Uri NormalizeBaseAddress(Uri address)
    {
        if (
            !address.IsAbsoluteUri
            || !string.Equals(
                address.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new GoodMoviesConfigurationException(
                "The TMDB API base address must be an absolute HTTPS URI."
            );
        }

        string value = address.ToString();
        return new Uri(value.EndsWith('/') ? value : $"{value}/");
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static bool TryParseReleaseDate(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (
            trimmed.Length >= 10
            && DateOnly.TryParseExact(
                trimmed.AsSpan(0, 10),
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out date
            )
        )
        {
            return true;
        }

        return DateTimeOffset.TryParse(
                trimmed,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTimeOffset dateTime
            )
            && (date = DateOnly.FromDateTime(dateTime.UtcDateTime)) != default;
    }

    private sealed record VerifiedRelease(
        DateOnly ReleaseDate,
        int ReleaseType,
        MovieCertification? Certification
    );
}
