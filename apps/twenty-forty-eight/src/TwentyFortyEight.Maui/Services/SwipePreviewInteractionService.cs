using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

public sealed class SwipePreviewInteractionService(
    GameViewModel viewModel,
    TileAnimationService animationService,
    IUserFeedbackService userFeedbackService
) : ISwipePreviewInteractionService
{
    private const double DefaultBoardDimension = 400;
    private CancellationTokenSource? _swipePreviewCts;
    private bool _isSwipePreviewActive;
    private bool _isSwipePreviewHapticFired;
    private bool _isSwipePreviewCommitRequested;
    private Direction? _swipePreviewDirection;
    private MovePreview? _swipePreview;
    private Task? _swipePreviewCompletionTask;

    public void Reset()
    {
        EndSwipePreviewImmediate();
    }

    public async Task HandleSwipePanUpdatedAsync(SwipePanEventArgs e, SwipePreviewUiContext context)
    {
        // Respect global input blocking (modals, victory, etc.) and avoid fighting active animations.
        if (context.IsInputBlocked || context.IsTileAnimationRunning)
        {
            if (_isSwipePreviewActive)
            {
                EndSwipePreviewImmediate();
            }
            return;
        }

        // Do not allow swipe input while the mode sheet is visible.
        if (context.IsModeSheetVisible)
        {
            if (_isSwipePreviewActive)
            {
                EndSwipePreviewImmediate();
            }
            return;
        }

        switch (e.Status)
        {
            case GestureStatus.Started:
                EndSwipePreviewImmediate();
                break;

            case GestureStatus.Running:
            {
                // If we've already requested a commit, keep the preview frozen until the
                // TilesUpdated handler completes it.
                if (_isSwipePreviewCommitRequested)
                {
                    return;
                }

                var direction = _swipePreviewDirection ?? e.PreviewDirection;
                if (direction is null)
                {
                    if (_isSwipePreviewActive)
                    {
                        // Lost direction; just cancel the preview.
                        EndSwipePreviewImmediate();
                    }
                    return;
                }

                // If direction flips very early, restart the preview.
                if (
                    _isSwipePreviewActive
                    && _swipePreviewDirection.HasValue
                    && direction.Value != _swipePreviewDirection.Value
                )
                {
                    // Only allow direction switch while near the origin.
                    if (_swipePreview is not null)
                    {
                        var step = direction.Value is Direction.Left or Direction.Right
                            ? CalculateCellStep(
                                context.GameBoard.Width,
                                context.BoardSize,
                                context.GameBoard.ColumnSpacing
                            )
                            : CalculateCellStep(
                                context.GameBoard.Height,
                                context.BoardSize,
                                context.GameBoard.RowSpacing
                            );

                        var delta = direction.Value is Direction.Left or Direction.Right
                            ? e.TotalX
                            : e.TotalY;

                        var directionSignCandidate = direction.Value
                            is Direction.Right
                                or Direction.Down
                            ? 1
                            : -1;

                        var progress = Math.Clamp((delta * directionSignCandidate) / step, 0, 1);
                        if (progress > 0.15)
                        {
                            return;
                        }
                    }

                    EndSwipePreviewImmediate();
                }

                // Enter preview mode only for slow drags.
                if (!_isSwipePreviewActive)
                {
                    if (e.IsFast)
                    {
                        return;
                    }

                    // Require a deliberate delay so we don't enter preview on normal swipes.
                    // Increased from 80ms to 150ms to make preview less easily triggered.
                    if (e.Elapsed.TotalMilliseconds < 150)
                    {
                        return;
                    }

                    if (!viewModel.TryCreateMovePreview(direction.Value, out var preview))
                    {
                        return;
                    }

                    _swipePreviewCts?.Cancel();
                    _swipePreviewCts?.Dispose();
                    _swipePreviewCts = new CancellationTokenSource();

                    _swipePreviewDirection = direction.Value;
                    _swipePreview = preview;
                    _isSwipePreviewActive = true;

                    if (!_isSwipePreviewHapticFired)
                    {
                        userFeedbackService.PerformSwipePreviewHaptic();
                        _isSwipePreviewHapticFired = true;
                    }

                    try
                    {
                        await animationService.BeginSwipePreviewAsync(
                            preview.TileMovements,
                            context.GameBoard,
                            context.BoardSize,
                            context.TileBorders,
                            context.ScaleFactor,
                            _swipePreviewCts.Token
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        EndSwipePreviewImmediate();
                        return;
                    }
                }

                if (_swipePreviewDirection is null)
                {
                    return;
                }

                var cellStep = _swipePreviewDirection.Value is Direction.Left or Direction.Right
                    ? CalculateCellStep(
                        context.GameBoard.Width,
                        context.BoardSize,
                        context.GameBoard.ColumnSpacing
                    )
                    : CalculateCellStep(
                        context.GameBoard.Height,
                        context.BoardSize,
                        context.GameBoard.RowSpacing
                    );

                var directionalDelta = _swipePreviewDirection.Value
                    is Direction.Left
                        or Direction.Right
                    ? e.TotalX
                    : e.TotalY;

                var directionSign = _swipePreviewDirection.Value
                    is Direction.Right
                        or Direction.Down
                    ? 1
                    : -1;

                var previewProgress = Math.Clamp(
                    (directionalDelta * directionSign) / cellStep,
                    0,
                    1
                );
                animationService.UpdateSwipePreviewProgress(previewProgress);
                break;
            }

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
            {
                if (_isSwipePreviewActive && _swipePreviewDirection.HasValue)
                {
                    var cellStep = _swipePreviewDirection.Value is Direction.Left or Direction.Right
                        ? CalculateCellStep(
                            context.GameBoard.Width,
                            context.BoardSize,
                            context.GameBoard.ColumnSpacing
                        )
                        : CalculateCellStep(
                            context.GameBoard.Height,
                            context.BoardSize,
                            context.GameBoard.RowSpacing
                        );

                    var directionalDelta = _swipePreviewDirection.Value
                        is Direction.Left
                            or Direction.Right
                        ? e.TotalX
                        : e.TotalY;

                    var directionSign = _swipePreviewDirection.Value
                        is Direction.Right
                            or Direction.Down
                        ? 1
                        : -1;

                    var previewProgress = Math.Clamp(
                        (directionalDelta * directionSign) / cellStep,
                        0,
                        1
                    );

                    if (e.Status == GestureStatus.Canceled || previewProgress < 0.5)
                    {
                        try
                        {
                            _swipePreviewCts?.Cancel();
                            _swipePreviewCts?.Dispose();
                            _swipePreviewCts = new CancellationTokenSource();
                            await animationService.CancelSwipePreviewAsync(_swipePreviewCts.Token);
                        }
                        catch
                        {
                            // Best effort cleanup.
                            EndSwipePreviewImmediate();
                        }
                        finally
                        {
                            EndSwipePreviewImmediate();
                        }

                        return;
                    }

                    // Commit: leave overlays in-place; TilesUpdated handler will complete to 1.
                    _isSwipePreviewCommitRequested = true;

                    // Hide destination tiles immediately so the underlying board update doesn't
                    // visually "jump" to the final state while the overlays finish sliding.
                    if (_swipePreview is not null)
                    {
                        animationService.HideSwipePreviewDestinationsForCommit(
                            _swipePreview.TileMovements,
                            context.TileBorders
                        );
                    }

                    // Start the remaining slide immediately on lift for a smooth finish.
                    // (If reduced motion is enabled, MAUI will complete instantly.)
                    _swipePreviewCts?.Cancel();
                    _swipePreviewCts?.Dispose();
                    _swipePreviewCts = new CancellationTokenSource();
                    _swipePreviewCompletionTask = animationService.CompleteSwipePreviewAsync(
                        _swipePreviewCts.Token
                    );

                    _ = viewModel.CommitSwipePreviewMoveAsync(_swipePreviewDirection.Value);
                    return;
                }

                // No preview: behave as the existing swipe detector.
                if (e.Status == GestureStatus.Completed && e.SwipeDirection.HasValue)
                {
                    viewModel.MoveCommand.Execute(e.SwipeDirection.Value);
                }

                EndSwipePreviewImmediate();
                break;
            }
        }
    }

    public async Task HandleTilesUpdatedAsync(TileUpdateEventArgs e, SwipePreviewUiContext context)
    {
        // If this move was committed from a swipe preview, finish the overlay slide first
        // so tiles don't snap back before merge/new-tile effects.
        if (e.SkipSlideAnimation && _isSwipePreviewActive && _isSwipePreviewCommitRequested)
        {
            try
            {
                animationService.HideSwipePreviewDestinationsForCommit(
                    e.TileMovements,
                    context.TileBorders
                );

                if (_swipePreviewCompletionTask is not null)
                {
                    await _swipePreviewCompletionTask;
                }
                else
                {
                    _swipePreviewCts?.Cancel();
                    _swipePreviewCts?.Dispose();
                    _swipePreviewCts = new CancellationTokenSource();
                    await animationService.CompleteSwipePreviewAsync(_swipePreviewCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
            finally
            {
                EndSwipePreviewImmediate();
            }
        }
    }

    private void EndSwipePreviewImmediate()
    {
        _swipePreviewCts?.Cancel();
        _swipePreviewCts?.Dispose();
        _swipePreviewCts = null;

        animationService.EndSwipePreviewImmediate();
        _isSwipePreviewActive = false;
        _isSwipePreviewHapticFired = false;
        _isSwipePreviewCommitRequested = false;
        _swipePreviewDirection = null;
        _swipePreview = null;
        _swipePreviewCompletionTask = null;
    }

    private static double CalculateCellStep(double dimension, int boardSize, double spacing)
    {
        var step = (dimension + spacing) / boardSize;
        return step > 0 ? step : (DefaultBoardDimension + spacing) / boardSize;
    }
}
