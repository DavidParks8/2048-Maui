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
    private bool _presentationActive;

    public TrailerPlayerViewHandler()
        : base(Mapper, CommandMapper) { }

    protected override WKWebView CreatePlatformView()
    {
        WKWebViewConfiguration configuration = new()
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
        CancelLoadTimeout();
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
        platformView.Dispose();
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
        webView.StopLoading();
        if (
            !YouTubeTrailerUri.IsTrustedEmbedUri(uri)
            || !YouTubeTrailerUri.TryGetVideoKey(uri, out string videoKey)
        )
        {
            ReportLoadFailed();
            return;
        }

        if (!ActivatePlaybackAudioSession())
        {
            ReportLoadFailed();
            return;
        }

        _navigationDelegate?.SetAllowedVideoKey(videoKey);
        _presentationActive = false;
        StartLoadTimeout();
        VirtualView.ReportLoadStarted();
        NSMutableUrlRequest request = new(new NSUrl(uri!.AbsoluteUri))
        {
            CachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalCacheData,
            TimeoutInterval = 20,
        };
        request["Referer"] = $"https://{YouTubeTrailerUri.Host}/";
        webView.LoadRequest(request);
    }

    private void StopAndClear()
    {
        CancelLoadTimeout();
        StopAndClear(PlatformView);
    }

    private void CancelLoadTimeout()
    {
        _loadTimeout?.Cancel();
        _loadTimeout?.Dispose();
        _loadTimeout = null;
    }

    private static void StopAndClear(WKWebView webView)
    {
        webView.StopLoading();
        webView.EvaluateJavaScript(
            "var player = document.getElementById('movie_player');"
                + "if (player && player.stopVideo) { player.stopVideo(); }",
            null!
        );
        DeactivatePlaybackAudioSession();
    }

    private void HandlePlayerMessage(string message)
    {
        if (message == "playing")
        {
            _loadTimeout?.Cancel();
            VirtualView.ReportLoadSucceeded();
            return;
        }

        if (message is "presentation:fullscreen" or "presentation:picture-in-picture")
        {
            _presentationActive = true;
            _loadTimeout?.Cancel();
            VirtualView.ReportLoadSucceeded();
            return;
        }

        if (message == "presentation:inline" && _presentationActive)
        {
            _presentationActive = false;
            VirtualView.ReportPresentationEnded();
            return;
        }

        if (message == "ended")
        {
            _presentationActive = false;
            VirtualView.ReportPresentationEnded();
            return;
        }

        if (message == "error")
        {
            ReportLoadFailed();
        }
    }

    private void ReportLoadFailed()
    {
        _loadTimeout?.Cancel();
        DeactivatePlaybackAudioSession();
        VirtualView.ReportLoadFailed();
    }

    private void StartLoadTimeout()
    {
        _loadTimeout?.Cancel();
        _loadTimeout?.Dispose();
        CancellationTokenSource timeout = new();
        _loadTimeout = timeout;
        _ = ReportTimeoutAsync(timeout);
    }

    private async Task ReportTimeoutAsync(CancellationTokenSource timeout)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), timeout.Token);
            if (ReferenceEquals(_loadTimeout, timeout))
            {
                ReportLoadFailed();
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
    }

    private void InjectPlayerGuard(WKWebView webView, string videoKey)
    {
        webView.EvaluateJavaScript(
            $$"""
            (function() {
              if (window.goodMoviesPlayerGuardInstalled) { return; }
              window.goodMoviesPlayerGuardInstalled = true;
              var selectedKey = '{{videoKey}}';
              var restrictedUiStyle = document.createElement('style');
              restrictedUiStyle.textContent =
                '.ytp-ce-element,.ytp-endscreen-content,.ytp-impression-link,'
                + '.ytp-copylink-button,.ytp-more-videos-view,.ytp-pause-overlay,'
                + '.ytp-suggestion-set,.ytp-title-channel,.ytp-title-link,'
                + '.ytp-videowall-still,.ytp-youtube-button'
                + '{display:none!important}';
              document.head.appendChild(restrictedUiStyle);
              var notifyNative = function(message) {
                window.webkit.messageHandlers.goodMoviesPlayer.postMessage(message);
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
              new MutationObserver(checkPlayer).observe(document.documentElement, {
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

    private static bool ActivatePlaybackAudioSession()
    {
        AVAudioSession audioSession = AVAudioSession.SharedInstance();
        NSError? categoryError = audioSession.SetCategory(
            AVAudioSessionCategory.Playback,
            AVAudioSessionCategoryOptions.AllowAirPlay
        );
        if (categoryError is not null)
        {
            Console.WriteLine(
                $"Could not configure trailer audio: {categoryError.LocalizedDescription}"
            );
            return false;
        }

        NSError? activationError = audioSession.SetActive(true);
        if (activationError is null)
        {
            return true;
        }

        Console.WriteLine(
            $"Could not activate trailer audio: {activationError.LocalizedDescription}"
        );
        return false;
    }

    private static void DeactivatePlaybackAudioSession()
    {
        NSError? error = AVAudioSession
            .SharedInstance()
            .SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);
        if (error is not null)
        {
            Console.WriteLine($"Could not deactivate trailer audio: {error.LocalizedDescription}");
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
        Action<WKWebView, string> navigationFinished,
        Action reportLoadFailed
    ) : WKNavigationDelegate
    {
        private readonly Action<WKWebView, string> _navigationFinished = navigationFinished;
        private readonly Action _reportLoadFailed = reportLoadFailed;
        private string? _allowedVideoKey;

        public void SetAllowedVideoKey(string videoKey)
        {
            _allowedVideoKey = videoKey;
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
                && YouTubeTrailerUri.IsTrustedEmbedUri(uri)
                && YouTubeTrailerUri.TryGetVideoKey(uri, out string videoKey)
                && string.Equals(videoKey, _allowedVideoKey, StringComparison.Ordinal);
            decisionHandler(
                isAllowed ? WKNavigationActionPolicy.Allow : WKNavigationActionPolicy.Cancel
            );
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            if (_allowedVideoKey is not null)
            {
                _navigationFinished(webView, _allowedVideoKey);
            }
        }

        public override void DidFailNavigation(
            WKWebView webView,
            WKNavigation navigation,
            NSError error
        )
        {
            _reportLoadFailed();
        }

        public override void DidFailProvisionalNavigation(
            WKWebView webView,
            WKNavigation navigation,
            NSError error
        )
        {
            _reportLoadFailed();
        }
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
