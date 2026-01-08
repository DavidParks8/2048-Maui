using System.Collections.Frozen;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Helpers;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service responsible for animating tile movements, merges, and spawns on the game board.
/// Animations automatically respect OS accessibility settings for reduced motion.
/// </summary>
public class TileAnimationService
{
    /// <summary>
    /// Default board dimension when actual size cannot be determined.
    /// </summary>
    private const double DefaultBoardDimension = 400;

    /// <summary>
    /// Small delay to ensure UI updates before animating in milliseconds.
    /// </summary>
    private const int UiUpdateDelay = 10;

    private sealed class SwipePreviewSession
    {
        public required Grid GameBoard { get; init; }
        public required List<Border> OverlayTiles { get; init; }
        public required List<(
            Border Border,
            double FullTranslateX,
            double FullTranslateY
        )> Animations { get; init; }
        public required List<Border> HiddenSourceBorders { get; init; }
        public required List<Border> HiddenDestinationBorders { get; init; }
        public required double CellStepX { get; init; }
        public required double CellStepY { get; init; }
        public double Progress { get; set; }
    }

    private readonly List<Border> _overlayPool = [];
    private SwipePreviewSession? _preview;

    /// <summary>
    /// Animates tile updates with cancellation support.
    /// </summary>
    /// <param name="args">The tile update event arguments.</param>
    /// <param name="gameBoard">The game board Grid element.</param>
    /// <param name="boardSize">The size of the board (e.g., 4 for 4x4).</param>
    /// <param name="tileBorders">Dictionary mapping TileViewModels to their Border elements.</param>
    /// <param name="scaleFactor">The scale factor for responsive font sizing.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    public async Task AnimateAsync(
        TileUpdateEventArgs args,
        Grid gameBoard,
        int boardSize,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        double scaleFactor,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If a swipe preview is active, it should be cleaned up by the caller before
        // running the normal animation pipeline.

        // Animations will automatically respect OS accessibility settings (reduced motion)
        // If the OS has animations disabled, MAUI will skip to the finished state
        // Use the board's actual spacing so overlay translations match the real layout.
        // If spacing is changed dynamically for small screens, using a fixed spacing can
        // cause overlays to miss their destinations, making tiles appear to disappear.
        var cellStepX = CalculateCellStep(gameBoard.Width, boardSize, gameBoard.ColumnSpacing);
        var cellStepY = CalculateCellStep(gameBoard.Height, boardSize, gameBoard.RowSpacing);

        // Hide new tiles during slide animation (they will be scaled in after the move)
        HideNewTiles(args.NewTiles, tileBorders);

        if (!args.SkipSlideAnimation)
        {
            // Hide destination tiles during slide animation to avoid showing the final state under the overlay
            var destinationTiles = HideDestinationTiles(args.TileMovements, tileBorders);

            // Animate slide movements
            var overlayTiles = await AnimateSlideMovementsAsync(
                args.TileMovements,
                gameBoard,
                cellStepX,
                cellStepY,
                scaleFactor,
                cancellationToken
            );

            // Clean up overlay tiles (return to pool)
            ReturnOverlayTiles(gameBoard, overlayTiles);

            // Show destination tiles once the slide completes
            ShowTiles(destinationTiles);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Animate merged tiles (pulse effect)
        await AnimateMergedTilesAsync(args.MergedTiles, tileBorders, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Animate new tiles appearing
        await AnimateNewTilesAsync(args.NewTiles, tileBorders, cancellationToken);
    }

    /// <summary>
    /// Begins a scrubbable swipe preview by creating overlay tiles and hiding destinations.
    /// Caller should follow up with <see cref="UpdateSwipePreviewProgress"/> and either
    /// <see cref="CancelSwipePreviewAsync"/> or <see cref="CompleteSwipePreviewAsync"/>.
    /// </summary>
    public async Task BeginSwipePreviewAsync(
        IReadOnlyList<Core.TileMovement> tileMovements,
        Grid gameBoard,
        int boardSize,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        double scaleFactor,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        EndSwipePreviewImmediate();

        var cellStepX = CalculateCellStep(gameBoard.Width, boardSize, gameBoard.ColumnSpacing);
        var cellStepY = CalculateCellStep(gameBoard.Height, boardSize, gameBoard.RowSpacing);

        // For previews, the underlying board is still in the "before" state.
        // Hide sources so tiles don't appear to duplicate while the overlay moves.
        var hiddenSourceTiles = HideSourceTiles(tileMovements, tileBorders);

        List<Border> overlayTiles = [];
        List<(Border border, double translateX, double translateY)> animations = [];

        foreach (var movement in tileMovements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var overlayBorder = RentOverlayTile(movement.Value, scaleFactor);
            overlayTiles.Add(overlayBorder);

            Grid.SetRow(overlayBorder, movement.From.Row);
            Grid.SetColumn(overlayBorder, movement.From.Column);
            gameBoard.Children.Add(overlayBorder);

            var translateX = (movement.To.Column - movement.From.Column) * cellStepX;
            var translateY = (movement.To.Row - movement.From.Row) * cellStepY;
            animations.Add((overlayBorder, translateX, translateY));
        }

        _preview = new SwipePreviewSession
        {
            GameBoard = gameBoard,
            OverlayTiles = overlayTiles,
            Animations = animations.Select(a => (a.border, a.translateX, a.translateY)).ToList(),
            HiddenSourceBorders = hiddenSourceTiles,
            HiddenDestinationBorders = new List<Border>(),
            CellStepX = cellStepX,
            CellStepY = cellStepY,
            Progress = 0,
        };

        if (overlayTiles.Count > 0)
        {
            await Task.Yield();
        }
    }

    /// <summary>
    /// Updates the swipe preview translation linearly with the provided progress.
    /// </summary>
    /// <param name="progress">Normalized progress from 0 to 1.</param>
    public void UpdateSwipePreviewProgress(double progress)
    {
        if (_preview is null)
            return;

        var clamped = Math.Clamp(progress, 0, 1);
        _preview.Progress = clamped;

        foreach (var animation in _preview.Animations)
        {
            animation.Border.TranslationX = animation.FullTranslateX * clamped;
            animation.Border.TranslationY = animation.FullTranslateY * clamped;
        }
    }

    /// <summary>
    /// Animates the preview back to 0 and cleans up overlays.
    /// </summary>
    public async Task CancelSwipePreviewAsync(CancellationToken cancellationToken)
    {
        if (_preview is null)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        // Animate back to start.
        var tasks = _preview
            .Animations.Select(static animation =>
                (Task)animation.Border.TranslateToAsync(0, 0, 90, Easing.Linear)
            )
            .ToList();

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        EndSwipePreviewImmediate();
    }

    /// <summary>
    /// Animates the preview to completion (progress 1) and keeps overlays until cleaned up.
    /// Call <see cref="EndSwipePreviewImmediate"/> after the underlying tiles are updated.
    /// </summary>
    public async Task CompleteSwipePreviewAsync(CancellationToken cancellationToken)
    {
        if (_preview is null)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var duration = SwipePreviewAnimationTiming.GetCompletionDuration(
            AnimationConstants.BaseSlideAnimationDuration,
            _preview.Progress
        );

        if (duration == 0)
        {
            _preview.Progress = 1;
            return;
        }

        var tasks = _preview
            .Animations.Select(animation =>
                (Task)
                    animation.Border.TranslateToAsync(
                        animation.FullTranslateX,
                        animation.FullTranslateY,
                        duration,
                        Easing.CubicOut
                    )
            )
            .ToList();

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }

        _preview.Progress = 1;
    }

    /// <summary>
    /// Immediately removes any preview overlays and restores hidden source tiles.
    /// </summary>
    public void EndSwipePreviewImmediate()
    {
        if (_preview is null)
            return;

        try
        {
            ReturnOverlayTiles(_preview.GameBoard, _preview.OverlayTiles);
        }
        catch
        {
            // Defensive: grid may be rebuilding.
        }

        try
        {
            ShowTiles(_preview.HiddenSourceBorders);
        }
        catch
        {
            // Defensive.
        }

        try
        {
            ShowTiles(_preview.HiddenDestinationBorders);
        }
        catch
        {
            // Defensive.
        }

        _preview = null;
    }

    private static double CalculateCellStep(double dimension, int boardSize, double spacing)
    {
        var step = (dimension + spacing) / boardSize;
        return step > 0 ? step : (DefaultBoardDimension + spacing) / boardSize;
    }

    /// <summary>
    /// After the underlying tiles have been updated (move committed), hide destination tiles so the
    /// final state doesn't appear underneath the preview overlays while they finish sliding.
    /// </summary>
    public void HideSwipePreviewDestinationsForCommit(
        IReadOnlyList<Core.TileMovement> tileMovements,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        if (_preview is null)
            return;

        if (_preview.HiddenDestinationBorders.Count > 0)
            return;

        _preview.HiddenDestinationBorders.AddRange(
            HideDestinationTiles(tileMovements, tileBorders)
        );
    }

    private static void HideNewTiles(
        IReadOnlySet<TileViewModel> newTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        foreach (var tile in newTiles)
        {
            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 0;
            }

            // Release any XAML trigger-based hiding once we've applied the initial hidden state.
            // This prevents the trigger from overriding the subsequent spawn animation.
            tile.IsNewTile = false;
        }
    }

    private static List<Border> HideDestinationTiles(
        IReadOnlyList<Core.TileMovement> movements,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        var tileMap = tileBorders.Keys.ToDictionary(t => (t.Row, t.Column));
        HashSet<TileViewModel> destinationTiles = [];
        List<Border> destinationBorders = [];

        foreach (var movement in movements)
        {
            if (!tileMap.TryGetValue((movement.To.Row, movement.To.Column), out var tile))
                continue;

            if (!destinationTiles.Add(tile))
                continue;

            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 1;
                destinationBorders.Add(border);
            }
        }

        return destinationBorders;
    }

    private static List<Border> HideSourceTiles(
        IReadOnlyList<Core.TileMovement> movements,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        var tileMap = tileBorders.Keys.ToDictionary(t => (t.Row, t.Column));
        HashSet<TileViewModel> sourceTiles = [];
        List<Border> sourceBorders = [];

        foreach (var movement in movements)
        {
            if (!tileMap.TryGetValue((movement.From.Row, movement.From.Column), out var tile))
                continue;

            if (!sourceTiles.Add(tile))
                continue;

            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 1;
                sourceBorders.Add(border);
            }
        }

        return sourceBorders;
    }

    private static void ShowTiles(IReadOnlyList<Border> borders)
    {
        foreach (var border in borders)
        {
            border.Opacity = 1;
            border.Scale = 1;
        }
    }

    private async Task<List<Border>> AnimateSlideMovementsAsync(
        IReadOnlyList<Core.TileMovement> tileMovements,
        Grid gameBoard,
        double cellStepX,
        double cellStepY,
        double scaleFactor,
        CancellationToken cancellationToken
    )
    {
        List<Border> overlayTiles = [];
        List<(Border border, double translateX, double translateY)> animations = [];

        foreach (var movement in tileMovements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var overlayBorder = RentOverlayTile(movement.Value, scaleFactor);
            overlayTiles.Add(overlayBorder);

            Grid.SetRow(overlayBorder, movement.From.Row);
            Grid.SetColumn(overlayBorder, movement.From.Column);
            gameBoard.Children.Add(overlayBorder);

            var translateX = (movement.To.Column - movement.From.Column) * cellStepX;
            var translateY = (movement.To.Row - movement.From.Row) * cellStepY;
            animations.Add((overlayBorder, translateX, translateY));
        }

        // Yield once so the UI can apply the added overlays before the first animation frame.
        if (animations.Count > 0)
        {
            await Task.Yield();

            List<Task> slideAnimationTasks = animations
                .Select(static animation =>
                    (Task)
                        animation.border.TranslateToAsync(
                            animation.translateX,
                            animation.translateY,
                            AnimationConstants.BaseSlideAnimationDuration,
                            Easing.CubicOut
                        )
                )
                .ToList();

            await Task.WhenAll(slideAnimationTasks);
        }

        return overlayTiles;
    }

    private static async Task AnimateMergedTilesAsync(
        IReadOnlySet<TileViewModel> mergedTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        CancellationToken cancellationToken
    )
    {
        List<Task> mergedTileTasks = mergedTiles
            .Select(async tile =>
            {
                if (tileBorders.TryGetValue(tile, out var border))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    border.Opacity = 1;
                    border.Scale = 0.8;
                    await border.ScaleToAsync(
                        1.2,
                        AnimationConstants.BaseMergePulseUpDuration,
                        Easing.CubicOut
                    );
                    await border.ScaleToAsync(
                        1.0,
                        AnimationConstants.BaseMergePulseDownDuration,
                        Easing.CubicIn
                    );
                }
            })
            .ToList();

        await Task.WhenAll(mergedTileTasks);
    }

    private static async Task AnimateNewTilesAsync(
        IReadOnlySet<TileViewModel> newTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        CancellationToken cancellationToken
    )
    {
        List<Task> newTileTasks = newTiles
            .Select(async tile =>
            {
                if (tileBorders.TryGetValue(tile, out var border))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    border.Scale = 0;
                    border.Opacity = 1;

                    await Task.Delay(UiUpdateDelay, cancellationToken);
                    await border.ScaleToAsync(
                        1.0,
                        AnimationConstants.BaseNewTileScaleDuration,
                        Easing.CubicOut
                    );
                }
            })
            .ToList();

        await Task.WhenAll(newTileTasks);
    }

    private Border RentOverlayTile(int value, double scaleFactor)
    {
        Border border;
        if (_overlayPool.Count > 0)
        {
            int lastIndex = _overlayPool.Count - 1;
            border = _overlayPool[lastIndex];
            _overlayPool.RemoveAt(lastIndex);
        }
        else
        {
            border = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                ZIndex = 100,
                Content = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    // Keep tile numbers stable even when OS text size is increased.
                    FontAutoScalingEnabled = false,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.NoWrap,
                    MaxLines = 1,
                },
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 5,
                },
            };
        }

        var backgroundColor = TileColorHelper.GetTileBackgroundColor(value);
        var textColor = TileColorHelper.GetTileTextColor(value);
        var baseFontSize = TileViewModel.GetTileFontSize(value);

        border.Background = new SolidColorBrush(backgroundColor);
        border.Opacity = 1;
        border.Scale = 1;
        border.TranslationX = 0;
        border.TranslationY = 0;

        if (border.Content is Label label)
        {
            label.Text = value.ToString();
            label.FontSize = baseFontSize * scaleFactor;
            label.TextColor = textColor;
        }

        return border;
    }

    private void ReturnOverlayTiles(Grid gameBoard, IReadOnlyList<Border> overlays)
    {
        foreach (var overlay in overlays)
        {
            gameBoard.Children.Remove(overlay);
            _overlayPool.Add(overlay);
        }
    }

    /// <summary>
    /// Resets all tile borders to their normal visual state and removes overlay tiles.
    /// Call this when animations are cancelled to ensure consistent UI state.
    /// </summary>
    /// <param name="gameBoard">The game board Grid element.</param>
    /// <param name="tileBorders">Dictionary mapping TileViewModels to their Border elements.</param>
    public static void ResetTileStates(
        Grid gameBoard,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        // Remove any overlay tiles (ZIndex = 100)
        for (int i = gameBoard.Children.Count - 1; i >= 0; i--)
        {
            if (gameBoard.Children[i] is Border border && border.ZIndex == 100)
            {
                gameBoard.Children.RemoveAt(i);
            }
        }

        // Reset all tile borders to normal state
        foreach (var (_, border) in tileBorders)
        {
            border.Opacity = 1;
            border.Scale = 1;
            border.TranslationX = 0;
            border.TranslationY = 0;
        }
    }
}
