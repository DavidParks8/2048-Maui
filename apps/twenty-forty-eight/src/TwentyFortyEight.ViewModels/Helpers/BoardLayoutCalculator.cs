namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Provides calculations for responsive board layout sizing.
/// All methods are pure and testable without MAUI dependencies.
/// </summary>
public static class BoardLayoutCalculator
{
    /// <summary>
    /// Default board dimension used as the reference for scaling calculations.
    /// </summary>
    public const double DefaultBoardSize = 400;

    /// <summary>
    /// Minimum allowed board size in pixels.
    /// </summary>
    public const double MinBoardSize = 280;

    /// <summary>
    /// Maximum allowed board size in pixels.
    /// </summary>
    public const double MaxBoardSize = 800;

    /// <summary>
    /// Default grid size (4x4) used as reference for scale factor calculations.
    /// </summary>
    private const int DefaultGridSize = 4;

    /// <summary>
    /// Calculates the optimal board size based on available screen space.
    /// </summary>
    /// <param name="pageWidth">The available page width in pixels.</param>
    /// <param name="pageHeight">The available page height in pixels.</param>
    /// <param name="horizontalReserved">Horizontal space reserved for padding, margins, etc.</param>
    /// <param name="verticalReserved">Vertical space reserved for header, controls, etc.</param>
    /// <returns>The calculated board size, clamped between MinBoardSize and MaxBoardSize.</returns>
    public static double CalculateBoardSize(
        double pageWidth,
        double pageHeight,
        double horizontalReserved = 50,
        double verticalReserved = 260
    )
    {
        double availableWidth = pageWidth - horizontalReserved;
        double availableHeight = pageHeight - verticalReserved;

        // Take the smaller dimension to maintain square aspect ratio
        double targetSize = Math.Min(availableWidth, availableHeight);

        // Apply min/max constraints
        return Math.Clamp(targetSize, MinBoardSize, MaxBoardSize);
    }

    /// <summary>
    /// Calculates the scale factor for responsive font sizing on tiles.
    /// </summary>
    /// <param name="boardSize">The actual board size in pixels.</param>
    /// <param name="gridSize">The grid dimension (e.g., 4 for a 4x4 board).</param>
    /// <returns>A scale factor relative to the default 4x4 board at 400px.</returns>
    public static double CalculateScaleFactor(double boardSize, int gridSize)
    {
        if (gridSize <= 0)
        {
            return 1.0;
        }

        return (boardSize / DefaultBoardSize) * (DefaultGridSize / (double)gridSize);
    }

    /// <summary>
    /// Calculates the tile spacing based on board size.
    /// Smaller boards get smaller spacing to maximize tile area.
    /// </summary>
    /// <param name="boardSize">The board size in pixels.</param>
    /// <param name="minSpacing">Minimum spacing to apply.</param>
    /// <returns>The calculated tile spacing in pixels.</returns>
    public static double CalculateTileSpacing(double boardSize, double minSpacing = 5)
    {
        return Math.Max(minSpacing, boardSize / 40);
    }
}
