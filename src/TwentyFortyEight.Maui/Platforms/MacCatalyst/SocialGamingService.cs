#if __MACCATALYST__
using Foundation;
using GameKit;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.ViewModels.Services;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MacCatalyst implementation of social gaming service using Game Center.
/// </summary>
public partial class SocialGamingService(ILogger<SocialGamingService> logger) : ISocialGamingService
{
    private bool _isAuthenticated;
    private readonly HashSet<string> _reportedAchievements = new();

    public bool IsAvailable => _isAuthenticated;

    private static UIWindow? GetKeyWindow()
    {
        var connectedScenes = UIApplication.SharedApplication?.ConnectedScenes;
        if (connectedScenes == null)
            return null;

        foreach (var scene in connectedScenes)
        {
            if (
                scene is UIWindowScene windowScene
                && windowScene.ActivationState == UISceneActivationState.ForegroundActive
            )
            {
                foreach (var window in windowScene.Windows)
                {
                    if (window.IsKeyWindow)
                        return window;
                }
                return windowScene.Windows.FirstOrDefault();
            }
        }

        foreach (var scene in connectedScenes)
        {
            if (scene is UIWindowScene windowScene)
            {
                return windowScene.Windows.FirstOrDefault();
            }
        }

        return null;
    }

    public async Task AuthenticateAsync()
    {
        try
        {
            TaskCompletionSource<bool> tcs = new();

            GKLocalPlayer.Local.AuthenticateHandler = (viewController, error) =>
            {
                if (viewController != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var window = GetKeyWindow();
                        var rootViewController = window?.RootViewController;

                        if (rootViewController != null)
                        {
                            rootViewController.PresentViewController(
                                viewController,
                                true,
                                () =>
                                {
                                    LogAuthenticationViewPresented();
                                }
                            );
                        }
                        else
                        {
                            LogNoRootViewController();
                            tcs.TrySetResult(false);
                        }
                    });
                }
                else if (error != null)
                {
                    LogAuthenticationError(error.LocalizedDescription);
                    _isAuthenticated = false;
                    tcs.TrySetResult(false);
                }
                else
                {
                    _isAuthenticated = GKLocalPlayer.Local.Authenticated;
                    LogAuthenticationResult(_isAuthenticated);
                    tcs.TrySetResult(_isAuthenticated);
                }
            };

            await tcs.Task;
        }
        catch (Exception ex)
        {
            LogAuthenticationFailed(ex);
            _isAuthenticated = false;
        }
    }

    public async Task SubmitScoreAsync(long score)
    {
        if (!IsAvailable)
        {
            LogServiceNotAvailable("score submission");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await GKLeaderboard.SubmitScoreAsync(
                    (nint)score,
                    0,
                    GKLocalPlayer.Local,
                    [PlatformAchievementIds.iOS.LeaderboardId]
                );
                LogScoreSubmitted(score);
            });
        }
        catch (Exception ex)
        {
            LogScoreSubmissionFailed(score, ex);
        }
    }

    public async Task ReportAchievementAsync(string achievementId, double percentComplete)
    {
        if (!IsAvailable)
        {
            LogServiceNotAvailable("achievement report");
            return;
        }

        if (percentComplete >= 100.0 && _reportedAchievements.Contains(achievementId))
        {
            LogAchievementAlreadyReported(achievementId);
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                GKAchievement achievement = new(achievementId)
                {
                    PercentComplete = percentComplete,
                    ShowsCompletionBanner = percentComplete >= 100.0,
                };

                await GKAchievement.ReportAchievementsAsync(new[] { achievement });

                if (percentComplete >= 100.0)
                {
                    _reportedAchievements.Add(achievementId);
                }

                LogAchievementReported(achievementId, percentComplete);
            });
        }
        catch (Exception ex)
        {
            LogAchievementReportFailed(achievementId, ex);
        }
    }

    public async Task ShowLeaderboardAsync()
    {
        if (!IsAvailable)
        {
            LogServiceNotAvailable("show leaderboard");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
#pragma warning disable CA1422
                GKGameCenterViewController viewController = new(
                    GKGameCenterViewControllerState.Leaderboards
                );
                viewController.Finished += (sender, e) =>
                {
                    viewController.DismissViewController(true, null);
                };
#pragma warning restore CA1422

                var window = GetKeyWindow();
                var rootViewController = window?.RootViewController;

                if (rootViewController != null)
                {
                    rootViewController.PresentViewController(viewController, true, null);
                    LogLeaderboardPresented();
                }
                else
                {
                    LogNoRootViewControllerForLeaderboard();
                }
            });
        }
        catch (Exception ex)
        {
            LogShowLeaderboardFailed(ex);
        }
    }

    public async Task ShowAchievementsAsync()
    {
        if (!IsAvailable)
        {
            LogServiceNotAvailable("show achievements");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
#pragma warning disable CA1422
                GKGameCenterViewController viewController = new(
                    GKGameCenterViewControllerState.Achievements
                );
                viewController.Finished += (sender, e) =>
                {
                    viewController.DismissViewController(true, null);
                };
#pragma warning restore CA1422

                var window = GetKeyWindow();
                var rootViewController = window?.RootViewController;

                if (rootViewController != null)
                {
                    rootViewController.PresentViewController(viewController, true, null);
                    LogAchievementsPresented();
                }
                else
                {
                    LogNoRootViewControllerForAchievements();
                }
            });
        }
        catch (Exception ex)
        {
            LogShowAchievementsFailed(ex);
        }
    }

    // LoggerMessage source-generated logging methods
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Game Center authentication view presented"
    )]
    partial void LogAuthenticationViewPresented();

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "No root view controller available for Game Center authentication"
    )]
    partial void LogNoRootViewController();

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Game Center authentication error: {ErrorMessage}"
    )]
    partial void LogAuthenticationError(string errorMessage);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Game Center authentication: {IsAuthenticated}"
    )]
    partial void LogAuthenticationResult(bool isAuthenticated);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Failed to authenticate with Game Center"
    )]
    partial void LogAuthenticationFailed(Exception ex);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Game Center not available, skipping {Operation}"
    )]
    partial void LogServiceNotAvailable(string operation);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Score submitted: {Score}")]
    partial void LogScoreSubmitted(long score);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Failed to submit score: {Score}"
    )]
    partial void LogScoreSubmissionFailed(long score, Exception ex);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Achievement {AchievementId} already reported"
    )]
    partial void LogAchievementAlreadyReported(string achievementId);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Achievement reported: {AchievementId} ({PercentComplete}%)"
    )]
    partial void LogAchievementReported(string achievementId, double percentComplete);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "Failed to report achievement: {AchievementId}"
    )]
    partial void LogAchievementReportFailed(string achievementId, Exception ex);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Game Center leaderboard presented"
    )]
    partial void LogLeaderboardPresented();

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Warning,
        Message = "No root view controller available to show leaderboard"
    )]
    partial void LogNoRootViewControllerForLeaderboard();

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Error,
        Message = "Failed to show Game Center leaderboard"
    )]
    partial void LogShowLeaderboardFailed(Exception ex);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Information,
        Message = "Game Center achievements presented"
    )]
    partial void LogAchievementsPresented();

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Warning,
        Message = "No root view controller available to show achievements"
    )]
    partial void LogNoRootViewControllerForAchievements();

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Error,
        Message = "Failed to show Game Center achievements"
    )]
    partial void LogShowAchievementsFailed(Exception ex);
}
#endif
