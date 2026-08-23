using GoodMovies.ViewModels;
using Microsoft.Extensions.Logging;

namespace GoodMovies.Maui;

public partial class App : Application
{
    private readonly Func<AppShell> _appShellFactory;
    private readonly CatalogViewModel _catalogViewModel;
    private readonly IWordLevelSpeechService _speechService;
    private readonly ILogger<App> _logger;
    private readonly object _dateBoundarySync = new();
    private readonly SemaphoreSlim _catalogUpdateGate = new(1, 1);
    private readonly Dictionary<Window, AppShell> _windowShells = new();
    private CancellationTokenSource? _dateBoundaryCancellation;

    public App(
        Func<AppShell> appShellFactory,
        CatalogViewModel catalogViewModel,
        ILogger<App> logger,
        IWordLevelSpeechService speechService
    )
    {
        InitializeComponent();
        _appShellFactory =
            appShellFactory ?? throw new ArgumentNullException(nameof(appShellFactory));
        _catalogViewModel =
            catalogViewModel ?? throw new ArgumentNullException(nameof(catalogViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        AppShell shell = _appShellFactory();
        Window window = new(shell);
        _windowShells.Add(window, shell);
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        window.Resumed += OnWindowResumed;
        window.Stopped += OnWindowStopped;
        window.Destroying += OnWindowDestroying;
        return window;
    }

    protected override void OnSleep()
    {
        StopSpeech();
        StopDateBoundaryWatch();
    }

    protected override void OnResume()
    {
        StartForegroundWork(Windows.Count == 0 ? null : Windows[0]);
    }

    private void OnWindowActivated(object? sender, EventArgs e) => StartForegroundWork(sender);

    private void OnWindowResumed(object? sender, EventArgs e) => StartForegroundWork(sender);

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        StopSpeech();
        StopDateBoundaryWatch();
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        StopSpeech();
        StopDateBoundaryWatch();
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Activated -= OnWindowActivated;
            window.Deactivated -= OnWindowDeactivated;
            window.Resumed -= OnWindowResumed;
            window.Stopped -= OnWindowStopped;
            window.Destroying -= OnWindowDestroying;
            if (_windowShells.Remove(window, out AppShell? shell))
            {
                shell.TearDown();
            }
        }

        StopSpeech();
        StopDateBoundaryWatch();
    }

    internal void StopSpeech() => _speechService.StopSpeaking();

    private void StartForegroundWork(object? sender)
    {
        if (sender is not Window)
        {
            return;
        }

        CancellationToken lifecycleToken = StartDateBoundaryWatch();
        _ = RunCatalogUpdateAsync(lifecycleToken);
    }

    private async Task RunCatalogUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _catalogUpdateGate.WaitAsync(cancellationToken);
            try
            {
                await _catalogViewModel.CheckForUpdatesAndReapplyDateAsync(cancellationToken);
            }
            finally
            {
                _catalogUpdateGate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A window can be stopped while an update check is in flight.
        }
        catch (Exception exception)
        {
            LogCatalogUpdateFailed(_logger, exception);
        }
    }

    private CancellationToken StartDateBoundaryWatch()
    {
        lock (_dateBoundarySync)
        {
            if (_dateBoundaryCancellation is { IsCancellationRequested: false })
            {
                return _dateBoundaryCancellation.Token;
            }

            _dateBoundaryCancellation = new CancellationTokenSource();
            _ = WatchDateBoundaryAsync(_dateBoundaryCancellation.Token);
            return _dateBoundaryCancellation.Token;
        }
    }

    private void StopDateBoundaryWatch()
    {
        lock (_dateBoundarySync)
        {
            _dateBoundaryCancellation?.Cancel();
            _dateBoundaryCancellation?.Dispose();
            _dateBoundaryCancellation = null;
        }
    }

    private async Task WatchDateBoundaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTimeOffset now = DateTimeOffset.Now;
                DateTime nextLocalDate = now.LocalDateTime.Date.AddDays(1);
                TimeSpan nextOffset = TimeZoneInfo.Local.GetUtcOffset(nextLocalDate);
                DateTimeOffset nextMidnight = new(nextLocalDate, nextOffset);
                TimeSpan untilTomorrow = nextMidnight - now + TimeSpan.FromSeconds(1);
                await Task.Delay(untilTomorrow, cancellationToken);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await RunCatalogUpdateAsync(cancellationToken);
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            LogDateBoundaryWatchFailed(_logger, exception);
        }
        finally
        {
            lock (_dateBoundarySync)
            {
                if (_dateBoundaryCancellation is { } active && active.Token == cancellationToken)
                {
                    active.Dispose();
                    _dateBoundaryCancellation = null;
                }
            }
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "The movie catalog update check failed."
    )]
    private static partial void LogCatalogUpdateFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "The local date-boundary watcher stopped unexpectedly."
    )]
    private static partial void LogDateBoundaryWatchFailed(ILogger logger, Exception exception);
}
