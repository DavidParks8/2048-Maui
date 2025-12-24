using TwentyFortyEight.Maui.Helpers;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service responsible for animating tile movements, merges, and spawns on the game board.
/// </summary>
public class TileAnimationService(ISettingsService settingsService)
{
    /// <summary>
    /// Spacing between tiles in the grid (matches XAML ColumnSpacing/RowSpacing).
    /// </summary>
    private const double TileSpacing = 10;

    /// <summary>
    /// Default board dimension when actual size cannot be determined.
    /// </summary>
    private const double DefaultBoardDimension = 400;

    /// <summary>
    /// Base duration of the slide animation in milliseconds.
    /// </summary>
    private const uint BaseSlideAnimationDuration = AnimationConstants.BaseSlideAnimationDuration;

    /// <summary>
    /// Base duration of the scale-up animation for merged tiles in milliseconds.
    /// </summary>
    private const uint BaseMergePulseUpDuration = AnimationConstants.BaseMergePulseUpDuration;

    /// <summary>
    /// Base duration of the scale-down animation for merged tiles in milliseconds.
    /// </summary>
    private const uint BaseMergePulseDownDuration = AnimationConstants.BaseMergePulseDownDuration;

    /// <summary>
    /// Base duration of the scale animation for new tiles in milliseconds.
    /// </summary>
    private const uint BaseNewTileScaleDuration = AnimationConstants.BaseNewTileScaleDuration;

    /// <summary>
    /// Default animation speed when the setting is invalid.
    /// </summary>
    private const double DefaultAnimationSpeed = 1.0;

    /// <summary>
    /// Minimum duration for an animation to be perceptible (in milliseconds).
    /// </summary>
    private const uint MinimumAnimationDuration = 16;

    /// <summary>
    /// Small delay to ensure UI updates before animating in milliseconds.
    /// </summary>
    private const int UiUpdateDelay = 10;

    /// <summary>
    /// Gets the adjusted animation duration based on the animation speed setting.
    /// </summary>
    private uint GetAdjustedDuration(uint baseDuration)
    {
        var speed = settingsService.AnimationSpeed;
        if (!double.IsFinite(speed) || speed <= 0)
        {
            speed = DefaultAnimationSpeed;
        }

        var adjusted = baseDuration / speed;
        if (!double.IsFinite(adjusted) || adjusted <= 0)
        {
            return MinimumAnimationDuration;
        }

        // Round rather than truncate to avoid 0ms due to casting.
        var adjustedMs = (uint)Math.Round(adjusted, MidpointRounding.AwayFromZero);
        return adjustedMs < MinimumAnimationDuration ? MinimumAnimationDuration : adjustedMs;
    }

    /// <summary>
    /// Animates tile updates with cancellation support.
    /// </summary>
    /// <param name="args">The tile update event arguments.</param>
    /// <param name="gameBoard">The game board Grid element.</param>
    /// <param name="boardSize">The size of the board (e.g., 4 for 4x4).</param>
    /// <param name="tileBorders">Dictionary mapping TileViewModels to their Border elements.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    public async Task AnimateAsync(
        TileUpdateEventArgs args,
        Grid gameBoard,
        int boardSize,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Skip animations if disabled
        if (!settingsService.AnimationsEnabled)
        {
            // Just update the tiles without animation
            foreach (var tile in args.MergedTiles)
            {
                if (tileBorders.TryGetValue(tile, out var border))
                {
                    border.Opacity = 1;
                    border.Scale = 1;
                }
            }

            foreach (var tile in args.NewTiles)
            {
                if (tileBorders.TryGetValue(tile, out var border))
                {
                    border.Opacity = 1;
                    border.Scale = 1;
                }
            }

            return;
        }

        var cellStepX = CalculateCellStep(gameBoard.Width, boardSize);
        var cellStepY = CalculateCellStep(gameBoard.Height, boardSize);

        // Store new tile values and hide them during slide animation
        var newTileValues = PrepareNewTiles(args.NewTiles, tileBorders);

        // Hide merged tiles initially
        PrepareMergedTiles(args.MergedTiles, tileBorders);

        // Animate slide movements
        var overlayTiles = await AnimateSlideMovementsAsync(
            args.TileMovements,
            gameBoard,
            cellStepX,
            cellStepY,
            cancellationToken
        );

        // Clean up overlay tiles
        foreach (var overlay in overlayTiles)
        {
            gameBoard.Children.Remove(overlay);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Animate merged tiles (pulse effect)
        await AnimateMergedTilesAsync(args.MergedTiles, tileBorders, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Animate new tiles appearing
        await AnimateNewTilesAsync(args.NewTiles, tileBorders, newTileValues, cancellationToken);
    }

    private static double CalculateCellStep(double dimension, int boardSize)
    {
        var step = (dimension + TileSpacing) / boardSize;
        return step > 0 ? step : (DefaultBoardDimension + TileSpacing) / boardSize;
    }

    private static Dictionary<TileViewModel, int> PrepareNewTiles(
        IReadOnlySet<TileViewModel> newTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        var newTileValues = new Dictionary<TileViewModel, int>();

        foreach (var tile in newTiles)
        {
            newTileValues[tile] = tile.Value;
            tile.UpdateValue(0); // Show as empty cell during slide animation

            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 0;
            }
        }

        return newTileValues;
    }

    private static void PrepareMergedTiles(
        IReadOnlySet<TileViewModel> mergedTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        foreach (var tile in mergedTiles)
        {
            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 1;
            }
        }
    }

    private async Task<List<Border>> AnimateSlideMovementsAsync(
        IReadOnlyList<Core.TileMovement> tileMovements,
        Grid gameBoard,
        double cellStepX,
        double cellStepY,
        CancellationToken cancellationToken
    )
    {
        var overlayTiles = new List<Border>();
        var slideAnimationTasks = new List<Task>();

        foreach (var movement in tileMovements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var overlayBorder = CreateOverlayTile(movement.Value);
            overlayTiles.Add(overlayBorder);

            Grid.SetRow(overlayBorder, movement.From.Row);
            Grid.SetColumn(overlayBorder, movement.From.Column);
            gameBoard.Children.Add(overlayBorder);

            var translateX = (movement.To.Column - movement.From.Column) * cellStepX;
            var translateY = (movement.To.Row - movement.From.Row) * cellStepY;

            slideAnimationTasks.Add(
                overlayBorder.TranslateToAsync(
                    translateX,
                    translateY,
                    GetAdjustedDuration(BaseSlideAnimationDuration),
                    Easing.CubicOut
                )
            );
        }

        if (slideAnimationTasks.Count > 0)
        {
            await Task.WhenAll(slideAnimationTasks);
        }

        return overlayTiles;
    }

    private async Task AnimateMergedTilesAsync(
        IReadOnlySet<TileViewModel> mergedTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        CancellationToken cancellationToken
    )
    {
        var mergedTileTasks = mergedTiles
            .Select(async tile =>
            {
                if (tileBorders.TryGetValue(tile, out var border))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    border.Opacity = 1;
                    border.Scale = 0.8;
                    await border.ScaleToAsync(
                        1.2,
                        GetAdjustedDuration(BaseMergePulseUpDuration),
                        Easing.CubicOut
                    );
                    await border.ScaleToAsync(
                        1.0,
                        GetAdjustedDuration(BaseMergePulseDownDuration),
                        Easing.CubicIn
                    );
                }
            })
            .ToList();

        await Task.WhenAll(mergedTileTasks);
    }

    private async Task AnimateNewTilesAsync(
        IReadOnlySet<TileViewModel> newTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        Dictionary<TileViewModel, int> newTileValues,
        CancellationToken cancellationToken
    )
    {
        var newTileTasks = newTiles
            .Select(async tile =>
            {
                if (
                    tileBorders.TryGetValue(tile, out var border)
                    && newTileValues.TryGetValue(tile, out var actualValue)
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    tile.UpdateValue(actualValue);
                    border.Scale = 0;
                    border.Opacity = 1;

                    await Task.Delay(UiUpdateDelay, cancellationToken);
                    await border.ScaleToAsync(
                        1.0,
                        GetAdjustedDuration(BaseNewTileScaleDuration),
                        Easing.CubicOut
                    );
                }
            })
            .ToList();

        await Task.WhenAll(newTileTasks);
    }

    private static Border CreateOverlayTile(int value)
    {
        var backgroundColor = TileColorHelper.GetTileBackgroundColor(value);
        var textColor = TileColorHelper.GetTileTextColor(value);

        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = 0,
            BackgroundColor = backgroundColor,
            ZIndex = 100,
            Content = new Label
            {
                Text = value.ToString(),
                FontSize = 32,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 5 },
        };
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
        var overlaysToRemove = gameBoard.Children.OfType<Border>().Where(b => b.ZIndex == 100);

        foreach (var overlay in overlaysToRemove)
        {
            gameBoard.Children.Remove(overlay);
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
