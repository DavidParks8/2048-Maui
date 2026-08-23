using System.Net.Http.Headers;

namespace GoodMovies.Infrastructure;

/// <summary>
/// Adds the configured TMDB bearer token to each outgoing request.
/// </summary>
internal sealed class TmdbBearerTokenHandler : DelegatingHandler
{
    private readonly GoodMoviesInfrastructureOptions _options;

    public TmdbBearerTokenHandler(GoodMoviesInfrastructureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string? token = _options.Token;
        if (
            string.IsNullOrWhiteSpace(token)
            || token.Any(static character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not ('.' or '_' or '~' or '+' or '/' or '=' or '-')
            )
        )
        {
            throw new GoodMoviesConfigurationException("The TMDB bearer token format is invalid.");
        }

        try
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new GoodMoviesConfigurationException("The TMDB bearer token format is invalid.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
