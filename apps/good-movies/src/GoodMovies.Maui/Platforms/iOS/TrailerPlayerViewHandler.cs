using AVFoundation;
using CoreGraphics;
using Foundation;
using GoodMovies.Maui.Controls;
using GoodMovies.ViewModels;
using Microsoft.Maui.Handlers;
using UIKit;
using WebKit;

namespace GoodMovies.Maui.Platforms.iOS;

public sealed class TrailerPlayerViewHandler : ViewHandler<TrailerPlayerView, WKWebView>
{
    private static readonly PropertyMapper<TrailerPlayerView, TrailerPlayerViewHandler> Mapper =
        new(ViewMapper) { [nameof(TrailerPlayerView.Source)] = MapSource };

    private static readonly CommandMapper<
        TrailerPlayerView,
        TrailerPlayerViewHandler
    > CommandMapper = new(ViewCommandMapper)
    {
        [nameof(TrailerPlayerView.Reload)] = MapReload,
        [nameof(TrailerPlayerView.StopPlayback)] = MapStopPlayback,
    };

    private TrailerNavigationDelegate? _navigationDelegate;
    private TrailerUiDelegate? _uiDelegate;
    private TrailerScriptMessageHandler? _scriptMessageHandler;
    private CancellationTokenSource? _loadTimeout;
    private long _loadVersion;
    private bool _audioSessionActive;
    private bool _presentationActive;

    public TrailerPlayerViewHandler()
        : base(Mapper, CommandMapper) { }

    protected override WKWebView CreatePlatformView()
    {
        using WKWebViewConfiguration configuration = new()
        {
            AllowsAirPlayForMediaPlayback = true,
            AllowsInlineMediaPlayback = false,
            AllowsPictureInPictureMediaPlayback = true,
            MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None,
            WebsiteDataStore = WKWebsiteDataStore.NonPersistentDataStore,
        };
        configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = false;
        _scriptMessageHandler = new TrailerScriptMessageHandler(HandlePlayerMessage);
        configuration.UserContentController.AddScriptMessageHandler(
            _scriptMessageHandler,
            TrailerScriptMessageHandler.ChannelName
        );

        WKWebView webView = new(CGRect.Empty, configuration)
        {
            AllowsLinkPreview = false,
            BackgroundColor = UIColor.FromRGB(25, 10, 58),
            ClipsToBounds = true,
            Opaque = true,
        };
        webView.ScrollView.Bounces = false;
        webView.ScrollView.ScrollEnabled = false;

        _navigationDelegate = new TrailerNavigationDelegate(InjectPlayerGuard, ReportLoadFailed);
        _uiDelegate = new TrailerUiDelegate();
        webView.NavigationDelegate = _navigationDelegate;
        webView.UIDelegate = _uiDelegate;
        return webView;
    }

    protected override void DisconnectHandler(WKWebView platformView)
    {
        _loadVersion++;
        CancelLoadTimeout();
        _presentationActive = false;
        _navigationDelegate?.ClearNavigations();
        StopAndClear(platformView);
        platformView.Configuration.UserContentController.RemoveScriptMessageHandler(
            TrailerScriptMessageHandler.ChannelName
        );
        platformView.NavigationDelegate = null!;
        platformView.UIDelegate = null!;
        _scriptMessageHandler?.Dispose();
        _navigationDelegate?.Dispose();
        _uiDelegate?.Dispose();
        _scriptMessageHandler = null;
        _navigationDelegate = null;
        _uiDelegate = null;
        base.DisconnectHandler(platformView);
    }

    private static void MapSource(
        TrailerPlayerViewHandler handler,
        TrailerPlayerView trailerPlayerView
    ) => handler.LoadSource(handler.PlatformView, trailerPlayerView.Source);

    private static void MapReload(
        TrailerPlayerViewHandler handler,
        TrailerPlayerView trailerPlayerView,
        object? args
    ) => handler.LoadSource(handler.PlatformView, trailerPlayerView.Source);

    private static void MapStopPlayback(
        TrailerPlayerViewHandler handler,
        TrailerPlayerView trailerPlayerView,
        object? args
    ) => handler.StopAndClear();

    private void LoadSource(WKWebView webView, Uri? uri)
    {
        long loadVersion = ++_loadVersion;
        CancelLoadTimeout();
        webView.StopLoading();
        if (!YouTubeTrailerUri.TryGetTrustedVideoKey(uri, out string videoKey))
        {
            ReportLoadFailed(loadVersion);
            return;
        }

        if (!ActivatePlaybackAudioSession())
        {
            ReportLoadFailed(loadVersion);
            return;
        }

        _navigationDelegate?.SetAllowedVideoKey(videoKey);
        _presentationActive = false;
        StartLoadTimeout(loadVersion);
        using NSUrl source = new(uri!.AbsoluteUri);
        using NSMutableUrlRequest request = new(source)
        {
            CachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalCacheData,
            TimeoutInterval = 20,
        };
        request["Referer"] = $"https://{YouTubeTrailerUri.Host}/";
        WKNavigation? navigation = webView.LoadRequest(request);
        if (navigation is null)
        {
            ReportLoadFailed(loadVersion);
            return;
        }

        _navigationDelegate?.Track(navigation, videoKey, loadVersion);
    }

    private void StopAndClear()
    {
        _loadVersion++;
        CancelLoadTimeout();
        _presentationActive = false;
        _navigationDelegate?.ClearNavigations();
        StopAndClear(PlatformView);
    }

    private void CancelLoadTimeout()
    {
        _loadTimeout?.Cancel();
        _loadTimeout?.Dispose();
        _loadTimeout = null;
    }

    private void StopAndClear(WKWebView webView)
    {
        webView.StopLoading();
        webView.EvaluateJavaScript(
            "if (window.goodMoviesPlayerGuardTimer) { "
                + "clearInterval(window.goodMoviesPlayerGuardTimer); "
                + "window.goodMoviesPlayerGuardTimer = null; }"
                + "if (window.goodMoviesPlayerGuardObserver) { "
                + "window.goodMoviesPlayerGuardObserver.disconnect(); "
                + "window.goodMoviesPlayerGuardObserver = null; }"
                + "window.goodMoviesPlayerGuardInstalled = false;"
                + "var player = document.getElementById('movie_player');"
                + "if (player && player.stopVideo) { player.stopVideo(); }",
            null!
        );
        DeactivatePlaybackAudioSession();
    }

    private void HandlePlayerMessage(string message)
    {
        int separator = message.IndexOf(":", StringComparison.Ordinal);
        if (
            separator <= 0
            || !long.TryParse(message.AsSpan(0, separator), out long loadVersion)
            || loadVersion != _loadVersion
        )
        {
            return;
        }

        ReadOnlySpan<char> playerEvent = message.AsSpan(separator + 1);
        if (playerEvent.SequenceEqual("playing"))
        {
            CancelLoadTimeout();
            VirtualView.ReportLoadSucceeded();
            return;
        }

        if (
            playerEvent.SequenceEqual("presentation:fullscreen")
            || playerEvent.SequenceEqual("presentation:picture-in-picture")
        )
        {
            _presentationActive = true;
            CancelLoadTimeout();
            VirtualView.ReportLoadSucceeded();
            return;
        }

        if (playerEvent.SequenceEqual("presentation:inline") && _presentationActive)
        {
            _presentationActive = false;
            VirtualView.ReportPresentationEnded();
            return;
        }

        if (playerEvent.SequenceEqual("ended"))
        {
            _presentationActive = false;
            VirtualView.ReportPresentationEnded();
            return;
        }

        if (playerEvent.SequenceEqual("error"))
        {
            ReportLoadFailed(loadVersion);
        }
    }

    private void ReportLoadFailed(long loadVersion)
    {
        if (loadVersion != _loadVersion)
        {
            return;
        }

        CancelLoadTimeout();
        _presentationActive = false;
        DeactivatePlaybackAudioSession();
        VirtualView.ReportLoadFailed();
    }

    private void StartLoadTimeout(long loadVersion)
    {
        _loadTimeout?.Cancel();
        _loadTimeout?.Dispose();
        CancellationTokenSource timeout = new();
        _loadTimeout = timeout;
        _ = ReportTimeoutAsync(timeout, loadVersion);
    }

    private async Task ReportTimeoutAsync(CancellationTokenSource timeout, long loadVersion)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), timeout.Token);
            if (ReferenceEquals(_loadTimeout, timeout))
            {
                ReportLoadFailed(loadVersion);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
    }

    private void InjectPlayerGuard(WKWebView webView, string videoKey, long loadVersion)
    {
        if (loadVersion != _loadVersion)
        {
            return;
        }

        webView.EvaluateJavaScript(
            $$"""
            (function() {
              if (window.goodMoviesPlayerGuardInstalled) { return; }
              window.goodMoviesPlayerGuardInstalled = true;
              var selectedKey = '{{videoKey}}';
              var loadVersion = {{loadVersion}};
              var restrictedUiStyle = document.createElement('style');
              restrictedUiStyle.textContent =
                '.ytp-ce-element,.ytp-endscreen-content,.ytp-impression-link,'
                + '.ytp-copylink-button,.ytp-more-videos-view,.ytp-pause-overlay,'
                + '.ytp-suggestion-set,.ytp-title-channel,.ytp-title-link,'
                + '.ytp-videowall-still,.ytp-youtube-button'
                + '{display:none!important}';
              document.head.appendChild(restrictedUiStyle);
              var notifyNative = function(message) {
                window.webkit.messageHandlers.goodMoviesPlayer.postMessage(
                  loadVersion + ":" + message
                );
              };
              var reportPresentationMode = function(video) {
                notifyNative('presentation:' + (video.webkitPresentationMode || 'inline'));
              };
              var attachVideoEvents = function(video) {
                if (!video || video.goodMoviesEventsInstalled) { return; }
                video.goodMoviesEventsInstalled = true;
                video.addEventListener('webkitbeginfullscreen', function() {
                  notifyNative('presentation:fullscreen');
                });
                video.addEventListener('webkitendfullscreen', function() {
                  setTimeout(function() { reportPresentationMode(video); }, 0);
                });
                video.addEventListener('webkitpresentationmodechanged', function() {
                  reportPresentationMode(video);
                });
                reportPresentationMode(video);
              };
              var checkPlayer = function() {
                var error = document.querySelector('.ytp-error');
                if (error && error.offsetParent !== null) {
                  notifyNative('error');
                  return;
                }
                var player = document.getElementById('movie_player');
                if (!player || !player.getVideoData) { return; }
                attachVideoEvents(player.querySelector('video'));
                var data = player.getVideoData();
                if (data && data.video_id && data.video_id !== selectedKey) {
                  player.stopVideo();
                  player.cueVideoById(selectedKey);
                  return;
                }
                notifyNative('ready');
              };
              var player = document.getElementById('movie_player');
              if (player && player.addEventListener) {
                player.addEventListener('onError', function() { notifyNative('error'); });
                player.addEventListener('onStateChange', function(state) {
                  attachVideoEvents(player.querySelector('video'));
                  var data = player.getVideoData ? player.getVideoData() : null;
                  if (data && data.video_id && data.video_id !== selectedKey) {
                    player.stopVideo();
                    player.cueVideoById(selectedKey);
                    return;
                  }
                  if (state === 0) {
                    player.stopVideo();
                    notifyNative('ended');
                    return;
                  }
                  if (state === 1) { notifyNative('playing'); }
                });
              }
              window.goodMoviesPlayerGuardObserver = new MutationObserver(checkPlayer);
              window.goodMoviesPlayerGuardObserver.observe(document.documentElement, {
                childList: true,
                subtree: true,
                attributes: true
              });
              window.goodMoviesPlayerGuardTimer = setInterval(checkPlayer, 1000);
              checkPlayer();
            })();
            """,
            null!
        );
    }

    private bool ActivatePlaybackAudioSession()
    {
        if (_audioSessionActive)
        {
            return true;
        }

        AVAudioSession audioSession = AVAudioSession.SharedInstance();
        // Playback supports AirPlay by default. AllowAirPlay is invalid for this category.
        if (audioSession.SetCategory(AVAudioSessionCategory.Playback) is not null)
        {
            TrailerPlayerDiagnostics.LogAudioCategoryFailure();
            return false;
        }

        if (audioSession.SetActive(true) is not null)
        {
            TrailerPlayerDiagnostics.LogAudioActivationFailure();
            return false;
        }

        _audioSessionActive = true;
        return true;
    }

    private void DeactivatePlaybackAudioSession()
    {
        if (!_audioSessionActive)
        {
            return;
        }

        _audioSessionActive = false;
        if (
            AVAudioSession
                .SharedInstance()
                .SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation)
            is not null
        )
        {
            TrailerPlayerDiagnostics.LogAudioDeactivationFailure();
        }
    }

    private sealed class TrailerScriptMessageHandler(Action<string> onMessage)
        : NSObject,
            IWKScriptMessageHandler
    {
        public const string ChannelName = "goodMoviesPlayer";

        private readonly Action<string> _onMessage = onMessage;

        public void DidReceiveScriptMessage(
            WKUserContentController userContentController,
            WKScriptMessage message
        )
        {
            _onMessage(message.Body?.ToString() ?? string.Empty);
        }
    }

    private sealed class TrailerNavigationDelegate(
        Action<WKWebView, string, long> navigationFinished,
        Action<long> reportLoadFailed
    ) : WKNavigationDelegate
    {
        private readonly Dictionary<WKNavigation, NavigationRequest> _navigations = new();
        private readonly Action<WKWebView, string, long> _navigationFinished = navigationFinished;
        private readonly Action<long> _reportLoadFailed = reportLoadFailed;
        private string? _allowedVideoKey;

        public void SetAllowedVideoKey(string videoKey)
        {
            _allowedVideoKey = videoKey;
        }

        public void Track(WKNavigation navigation, string videoKey, long loadVersion)
        {
            _navigations[navigation] = new NavigationRequest(videoKey, loadVersion);
        }

        public void ClearNavigations()
        {
            _allowedVideoKey = null;
            _navigations.Clear();
        }

        public override void DecidePolicy(
            WKWebView webView,
            WKNavigationAction navigationAction,
            Action<WKNavigationActionPolicy> decisionHandler
        )
        {
            NSUrl? requestUrl = navigationAction.Request.Url;
            bool isAllowed =
                requestUrl is not null
                && Uri.TryCreate(requestUrl.AbsoluteString, UriKind.Absolute, out Uri? uri)
                && YouTubeTrailerUri.TryGetTrustedVideoKey(uri, out string videoKey)
                && string.Equals(videoKey, _allowedVideoKey, StringComparison.Ordinal);
            decisionHandler(
                isAllowed ? WKNavigationActionPolicy.Allow : WKNavigationActionPolicy.Cancel
            );
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            if (_navigations.Remove(navigation, out NavigationRequest request))
            {
                _navigationFinished(webView, request.VideoKey, request.LoadVersion);
            }
        }

        public override void DidFailNavigation(
            WKWebView webView,
            WKNavigation navigation,
            NSError error
        ) => ReportFailure(navigation);

        public override void DidFailProvisionalNavigation(
            WKWebView webView,
            WKNavigation navigation,
            NSError error
        ) => ReportFailure(navigation);

        private void ReportFailure(WKNavigation navigation)
        {
            if (_navigations.Remove(navigation, out NavigationRequest request))
            {
                _reportLoadFailed(request.LoadVersion);
            }
        }

        private readonly record struct NavigationRequest(string VideoKey, long LoadVersion);
    }

    private sealed class TrailerUiDelegate : WKUIDelegate
    {
        public override WKWebView? CreateWebView(
            WKWebView webView,
            WKWebViewConfiguration configuration,
            WKNavigationAction navigationAction,
            WKWindowFeatures windowFeatures
        ) => null;
    }
}
