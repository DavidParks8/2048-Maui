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
    /// Spacing between tiles in the grid (matches XAML ColumnSpacing/RowSpacing).
    /// </summary>
    private const double TileSpacing = 10;

    /// <summary>
    /// Default board dimension when actual size cannot be determined.
    /// </summary>
    private const double DefaultBoardDimension = 400;

    /// <summary>
    /// Small delay to ensure UI updates before animating in milliseconds.
    /// </summary>
    private const int UiUpdateDelay = 10;

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

        // Animations will automatically respect OS accessibility settings (reduced motion)
        // If the OS has animations disabled, MAUI will skip to the finished state
        var cellStepX = CalculateCellStep(gameBoard.Width, boardSize);
        var cellStepY = CalculateCellStep(gameBoard.Height, boardSize);

        // Store new tile values and hide them during slide animation
        var newTileValues = PrepareNewTiles(args.NewTiles, tileBorders);

        // Prepare destination tiles (both merged and simple moves) to look empty
        var savedDestinationValues = PrepareDestinationTiles(args.TileMovements, tileBorders);

        // Animate slide movements
        var overlayTiles = await AnimateSlideMovementsAsync(
            args.TileMovements,
            gameBoard,
            cellStepX,
            cellStepY,
            scaleFactor,
            cancellationToken
        );

        // Clean up overlay tiles
        foreach (var overlay in overlayTiles)
        {
            gameBoard.Children.Remove(overlay);
        }

        // Restore destination tiles to their actual values
        RestoreDestinationTiles(savedDestinationValues);

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
        Dictionary<TileViewModel, int> newTileValues = [];

        foreach (var tile in newTiles)
        {
            newTileValues[tile] = tile.Value;
            tile.Value = 0; // Show as empty cell during slide animation

            if (tileBorders.TryGetValue(tile, out var border))
            {
                border.Opacity = 0;
                border.Scale = 0;
            }
        }

        return newTileValues;
    }

    private static Dictionary<TileViewModel, int> PrepareDestinationTiles(
        IReadOnlyList<Core.TileMovement> movements,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders
    )
    {
        var tileMap = tileBorders.Keys.ToDictionary(t => (t.Row, t.Column));
        Dictionary<TileViewModel, int> savedValues = [];

        foreach (var movement in movements)
        {
            if (tileMap.TryGetValue((movement.To.Row, movement.To.Column), out var tile))
            {
                if (savedValues.TryAdd(tile, tile.Value))
                {
                    tile.Value = 0; // Show as empty cell during slide animation

                    if (tileBorders.TryGetValue(tile, out var border))
                    {
                        border.Opacity = 1;
                        border.Scale = 1;
                    }
                }
            }
        }
        return savedValues;
    }

    private static void RestoreDestinationTiles(Dictionary<TileViewModel, int> savedValues)
    {
        foreach (var (tile, value) in savedValues)
        {
            tile.Value = value;
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
        List<Task> slideAnimationTasks = [];

        foreach (var movement in tileMovements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var overlayBorder = CreateOverlayTile(movement.Value, scaleFactor);
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
                    AnimationConstants.BaseSlideAnimationDuration,
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

    private async Task AnimateNewTilesAsync(
        IReadOnlySet<TileViewModel> newTiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        Dictionary<TileViewModel, int> newTileValues,
        CancellationToken cancellationToken
    )
    {
        List<Task> newTileTasks = newTiles
            .Select(async tile =>
            {
                if (
                    tileBorders.TryGetValue(tile, out var border)
                    && newTileValues.TryGetValue(tile, out var actualValue)
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    tile.Value = actualValue;
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

    private static Border CreateOverlayTile(int value, double scaleFactor)
    {
        var backgroundColor = TileColorHelper.GetTileBackgroundColor(value);
        var textColor = TileColorHelper.GetTileTextColor(value);
        var baseFontSize = TileViewModel.GetTileFontSize(value);

        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = 0,
            Background = new SolidColorBrush(backgroundColor),
            ZIndex = 100,
            Content = new Label
            {
                Text = value.ToString(),
                FontSize = baseFontSize * scaleFactor,
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
