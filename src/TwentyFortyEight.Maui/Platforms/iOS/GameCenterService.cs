using Foundation;
using GameKit;
using Microsoft.Extensions.Logging;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// iOS implementation of Game Center service using GameKit framework.
/// </summary>
public class GameCenterService : IGameCenterService
{
    private readonly ILogger<GameCenterService>? _logger;
    private bool _isAuthenticated;
    private readonly HashSet<string> _reportedAchievements = new();

    public GameCenterService(ILogger<GameCenterService>? logger = null)
    {
        _logger = logger;
    }

    public bool IsAvailable => _isAuthenticated;

    public async Task AuthenticateAsync()
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();

            GKLocalPlayer.Local.AuthenticateHandler = (viewController, error) =>
            {
                if (viewController != null)
                {
                    // Present the Game Center login view controller
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var window =
                            UIApplication.SharedApplication?.KeyWindow
                            ?? UIApplication.SharedApplication?.Windows?.FirstOrDefault();
                        var rootViewController = window?.RootViewController;

                        if (rootViewController != null)
                        {
                            rootViewController.PresentViewController(
                                viewController,
                                true,
                                () =>
                                {
                                    _logger?.LogInformation(
                                        "Game Center authentication view presented"
                                    );
                                }
                            );
                        }
                        else
                        {
                            _logger?.LogWarning(
                                "No root view controller available for Game Center authentication"
                            );
                            tcs.TrySetResult(false);
                        }
                    });
                }
                else if (error != null)
                {
                    _logger?.LogError(
                        $"Game Center authentication error: {error.LocalizedDescription}"
                    );
                    _isAuthenticated = false;
                    tcs.TrySetResult(false);
                }
                else
                {
                    // Successfully authenticated
                    _isAuthenticated = GKLocalPlayer.Local.IsAuthenticated;
                    _logger?.LogInformation(
                        $"Game Center authentication: {(_isAuthenticated ? "success" : "not authenticated")}"
                    );
                    tcs.TrySetResult(_isAuthenticated);
                }
            };

            await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to authenticate with Game Center");
            _isAuthenticated = false;
        }
    }

    public async Task SubmitScoreAsync(long score)
    {
        if (!IsAvailable)
        {
            _logger?.LogDebug("Game Center not available, skipping score submission");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var scoreReporter = new GKScore(GameCenterConstants.LeaderboardId)
                {
                    Value = score,
                };

                var scores = new[] { scoreReporter };
                await GKScore.ReportScoresAsync(scores);
                _logger?.LogInformation($"Score submitted to Game Center: {score}");
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to submit score to Game Center: {score}");
        }
    }

    public async Task ReportAchievementAsync(string achievementId, double percentComplete)
    {
        if (!IsAvailable)
        {
            _logger?.LogDebug("Game Center not available, skipping achievement report");
            return;
        }

        // Only report 100% achievements once
        if (percentComplete >= 100.0 && _reportedAchievements.Contains(achievementId))
        {
            _logger?.LogDebug($"Achievement {achievementId} already reported");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var achievement = new GKAchievement(achievementId)
                {
                    PercentComplete = percentComplete,
                    ShowsCompletionBanner = percentComplete >= 100.0,
                };

                var achievements = new[] { achievement };
                await GKAchievement.ReportAchievementsAsync(achievements);

                if (percentComplete >= 100.0)
                {
                    _reportedAchievements.Add(achievementId);
                }

                _logger?.LogInformation(
                    $"Achievement reported to Game Center: {achievementId} ({percentComplete}%)"
                );
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to report achievement to Game Center: {achievementId}");
        }
    }

    public async Task ShowLeaderboardAsync()
    {
        if (!IsAvailable)
        {
            _logger?.LogDebug("Game Center not available, cannot show leaderboard");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var viewController = new GKGameCenterViewController();
                viewController.ViewState = GKGameCenterViewControllerState.Leaderboards;
                viewController.LeaderboardIdentifier = GameCenterConstants.LeaderboardId;
                viewController.Finished += (sender, e) =>
                {
                    viewController.DismissViewController(true, null);
                };

                var window =
                    UIApplication.SharedApplication?.KeyWindow
                    ?? UIApplication.SharedApplication?.Windows?.FirstOrDefault();
                var rootViewController = window?.RootViewController;

                if (rootViewController != null)
                {
                    rootViewController.PresentViewController(viewController, true, null);
                    _logger?.LogInformation("Game Center leaderboard presented");
                }
                else
                {
                    _logger?.LogWarning("No root view controller available to show leaderboard");
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show Game Center leaderboard");
        }
    }

    public async Task ShowAchievementsAsync()
    {
        if (!IsAvailable)
        {
            _logger?.LogDebug("Game Center not available, cannot show achievements");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var viewController = new GKGameCenterViewController();
                viewController.ViewState = GKGameCenterViewControllerState.Achievements;
                viewController.Finished += (sender, e) =>
                {
                    viewController.DismissViewController(true, null);
                };

                var window =
                    UIApplication.SharedApplication?.KeyWindow
                    ?? UIApplication.SharedApplication?.Windows?.FirstOrDefault();
                var rootViewController = window?.RootViewController;

                if (rootViewController != null)
                {
                    rootViewController.PresentViewController(viewController, true, null);
                    _logger?.LogInformation("Game Center achievements presented");
                }
                else
                {
                    _logger?.LogWarning("No root view controller available to show achievements");
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show Game Center achievements");
        }
    }
}
