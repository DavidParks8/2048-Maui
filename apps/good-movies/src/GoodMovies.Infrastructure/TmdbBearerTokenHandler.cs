using System.Net.Http.Headers;

namespace GoodMovies.Infrastructure;

/// <summary>
/// Adds the current TMDB bearer token to each outgoing request.
/// </summary>
public sealed class TmdbBearerTokenHandler : DelegatingHandler
{
    private readonly IGoodMoviesTokenProvider _tokenProvider;

    public TmdbBearerTokenHandler(IGoodMoviesTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        string? token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (
            string.IsNullOrWhiteSpace(token)
            || token.Any(static character =>
                char.IsWhiteSpace(character) || char.IsControl(character)
            )
        )
        {
            throw new GoodMoviesConfigurationException("The TMDB bearer token format is invalid.");
        }

        try
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (ArgumentException)
        {
            throw new GoodMoviesConfigurationException("The TMDB bearer token format is invalid.");
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
