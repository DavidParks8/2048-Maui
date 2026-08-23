namespace GoodMovies.ViewModels;

internal sealed class TrailerPlaybackStateCoordinator
{
    private readonly object _sync = new();
    private string? _activeYoutubeKey;
    private long _version;

    public event Action<string?>? ActiveYoutubeKeyChanged;

    public string? ActiveYoutubeKey
    {
        get
        {
            lock (_sync)
            {
                return _activeYoutubeKey;
            }
        }
    }

    public LaunchOperation BeginLaunch(string youtubeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(youtubeKey);

        bool cleared;
        LaunchOperation operation;
        lock (_sync)
        {
            if (string.Equals(_activeYoutubeKey, youtubeKey, StringComparison.Ordinal))
            {
                return new LaunchOperation(_version, youtubeKey, WasAlreadyPlaying: true);
            }

            operation = new LaunchOperation(++_version, youtubeKey, WasAlreadyPlaying: false);
            cleared = _activeYoutubeKey is not null;
            _activeYoutubeKey = null;
        }

        if (cleared)
        {
            ActiveYoutubeKeyChanged?.Invoke(null);
        }

        return operation;
    }

    public bool CompleteLaunch(LaunchOperation operation, bool succeeded)
    {
        bool activated = false;
        lock (_sync)
        {
            if (operation.WasAlreadyPlaying)
            {
                return string.Equals(
                    _activeYoutubeKey,
                    operation.YoutubeKey,
                    StringComparison.Ordinal
                );
            }

            if (operation.Version != _version)
            {
                return false;
            }

            if (!succeeded)
            {
                _version++;
                return false;
            }

            _activeYoutubeKey = operation.YoutubeKey;
            activated = true;
        }

        if (activated)
        {
            ActiveYoutubeKeyChanged?.Invoke(operation.YoutubeKey);
        }

        return true;
    }

    public bool CancelLaunch(LaunchOperation operation)
    {
        lock (_sync)
        {
            if (operation.WasAlreadyPlaying || operation.Version != _version)
            {
                return false;
            }

            _version++;
            return true;
        }
    }

    public void Reset()
    {
        bool cleared;
        lock (_sync)
        {
            _version++;
            cleared = _activeYoutubeKey is not null;
            _activeYoutubeKey = null;
        }

        if (cleared)
        {
            ActiveYoutubeKeyChanged?.Invoke(null);
        }
    }

    internal readonly record struct LaunchOperation(
        long Version,
        string YoutubeKey,
        bool WasAlreadyPlaying
    );
}
