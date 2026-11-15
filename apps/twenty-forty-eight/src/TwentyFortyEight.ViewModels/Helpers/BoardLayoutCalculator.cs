using Microsoft.Maui.Graphics;

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
    /// Maximum allowed board size in device-independent units.
    /// </summary>
    public const double MaxBoardSize = 800;

    /// <summary>
    /// Default grid size (4x4) used as reference for scale factor calculations.
    /// </summary>
    private const int DefaultGridSize = 4;

    /// <summary>
    /// Calculates the square board size from its allocated viewport, not the screen.
    /// </summary>
    /// <param name="pageWidth">The available viewport width.</param>
    /// <param name="pageHeight">The available viewport height.</param>
    /// <param name="horizontalReserved">Horizontal space reserved for padding, margins, etc.</param>
    /// <param name="verticalReserved">Vertical space reserved for header, controls, etc.</param>
    /// <returns>A size that fits the viewport, capped at MaxBoardSize.</returns>
    public static double CalculateBoardSize(
        double pageWidth,
        double pageHeight,
        double horizontalReserved = 0,
        double verticalReserved = 0
    )
    {
        double availableWidth = pageWidth - horizontalReserved;
        double availableHeight = pageHeight - verticalReserved;

        // Take the smaller dimension to maintain square aspect ratio
        double targetSize = Math.Min(availableWidth, availableHeight);

        return Math.Clamp(targetSize, 0, MaxBoardSize);
    }

    /// <summary>Gap between the board and its surrounding groups.</summary>
    public const double GroupGap = 16;

    /// <summary>Gap between cards that belong to the same group.</summary>
    public const double CardGap = 8;

    /// <summary>
    /// Composes the board, mode card, score cards and actions inside <paramref name="bounds"/>.
    /// Closed or portrait windows stack the groups; wide windows flank a centered board
    /// with symmetric wings; an active fold gives the board one pane and the rest the other.
    /// </summary>
    public static GameLayout Arrange(
        Rect bounds,
        Rect? fold,
        Size mode,
        Size scoreCard,
        Size actions,
        GameLayoutKind? previous = null
    )
    {
        if (fold is Rect division && Split(bounds, division, mode, scoreCard, actions) is { } split)
        {
            return split;
        }

        var stacked = Stacked(bounds, mode, scoreCard, actions);
        var wings = Wings(bounds, mode, scoreCard, actions);
        if (wings is not { } flanked)
        {
            return stacked;
        }

        // A dead band keeps resizing near the crossover from flipping layouts back and forth.
        double margin = previous == GameLayoutKind.Wings ? -12 : 12;
        return flanked.Board.Width > stacked.Board.Width + margin ? flanked : stacked;
    }

    private static GameLayout Stacked(Rect bounds, Size mode, Size card, Size actions)
    {
        var status = StatusBlock(bounds.Width, mode, card);
        double actionsHeight = actions.Height > 0 ? actions.Height + GroupGap : 0;
        double board = Math.Clamp(
            Math.Min(bounds.Width, bounds.Height - status.Height - GroupGap - actionsHeight),
            0,
            MaxBoardSize + 20
        );
        double strip = Math.Min(
            bounds.Width,
            Math.Max(board, Math.Max(status.Width, actions.Width))
        );
        double top =
            bounds.Top
            + Math.Max(0, (bounds.Height - status.Height - GroupGap - board - actionsHeight) / 2);
        double left = bounds.Center.X - strip / 2;

        var (modeRect, scoresRect) = PlaceStatus(left, top, strip, status.Wrapped, mode, card);
        Rect boardRect = new(
            bounds.Center.X - board / 2,
            top + status.Height + GroupGap,
            board,
            board
        );
        Rect actionsRect = new(left, boardRect.Bottom + GroupGap, strip, actions.Height);
        return new(GameLayoutKind.Stacked, boardRect, modeRect, scoresRect, false, actionsRect);
    }

    private static GameLayout? Wings(Rect bounds, Size mode, Size card, Size actions)
    {
        double wing = Math.Max(card.Width, Math.Max(mode.Width, actions.Width));
        double board = Math.Min(bounds.Width - 2 * (wing + GroupGap), bounds.Height);
        board = Math.Min(board, MaxBoardSize + 20);
        double scoresHeight = 2 * card.Height + CardGap;
        double rightHeight = mode.Height + (actions.Height > 0 ? GroupGap + actions.Height : 0);
        if (board <= 0 || board < scoresHeight || board < rightHeight)
        {
            return null;
        }

        Rect boardRect = new(
            bounds.Center.X - board / 2,
            bounds.Center.Y - board / 2,
            board,
            board
        );
        Rect scores = new(boardRect.Left - GroupGap - wing, boardRect.Top, wing, scoresHeight);
        Rect modeRect = new(boardRect.Right + GroupGap, boardRect.Top, wing, mode.Height);
        Rect actionsRect = new(
            boardRect.Right + GroupGap,
            boardRect.Bottom - actions.Height,
            actions.Width,
            actions.Height
        );
        return new(GameLayoutKind.Wings, boardRect, modeRect, scores, true, actionsRect);
    }

    private static GameLayout? Split(Rect bounds, Rect fold, Size mode, Size card, Size actions)
    {
        var division = bounds.Intersect(fold);
        if (division.IsEmpty)
        {
            return null;
        }

        bool horizontal = division.Width >= division.Height;
        Rect boardPane = horizontal
            ? new(bounds.Left, bounds.Top, bounds.Width, division.Top - bounds.Top)
            : new(bounds.Left, bounds.Top, division.Left - bounds.Left, bounds.Height);
        Rect consolePane = horizontal
            ? new(bounds.Left, division.Bottom, bounds.Width, bounds.Bottom - division.Bottom)
            : new(division.Right, bounds.Top, bounds.Right - division.Right, bounds.Height);

        var status = StatusBlock(consolePane.Width, mode, card);
        double blockWidth = Math.Max(status.Width, actions.Width);
        double blockHeight = status.Height + (actions.Height > 0 ? GroupGap + actions.Height : 0);
        double board = Math.Min(Math.Min(boardPane.Width, boardPane.Height), MaxBoardSize + 20);
        if (board <= 0 || blockWidth > consolePane.Width || blockHeight > consolePane.Height)
        {
            return null;
        }

        Rect boardRect = new(
            boardPane.Center.X - board / 2,
            boardPane.Center.Y - board / 2,
            board,
            board
        );
        double left = consolePane.Center.X - blockWidth / 2;
        double top = consolePane.Center.Y - blockHeight / 2;
        var (modeRect, scoresRect) = PlaceStatus(left, top, blockWidth, status.Wrapped, mode, card);
        Rect actionsRect = new(left, top + status.Height + GroupGap, blockWidth, actions.Height);
        return new(
            horizontal ? GameLayoutKind.SplitHorizontal : GameLayoutKind.SplitVertical,
            boardRect,
            modeRect,
            scoresRect,
            false,
            actionsRect,
            boardPane,
            consolePane
        );
    }

    private static (double Width, double Height, bool Wrapped) StatusBlock(
        double available,
        Size mode,
        Size card
    )
    {
        double scoresWidth = 2 * card.Width + CardGap;
        double rowWidth = mode.Width + CardGap + scoresWidth;
        if (rowWidth <= available)
        {
            return (rowWidth, Math.Max(mode.Height, card.Height), false);
        }

        return (Math.Max(mode.Width, scoresWidth), mode.Height + CardGap + card.Height, true);
    }

    private static (Rect Mode, Rect Scores) PlaceStatus(
        double left,
        double top,
        double width,
        bool wrapped,
        Size mode,
        Size card
    )
    {
        double scoresWidth = 2 * card.Width + CardGap;
        if (wrapped)
        {
            return (
                new(left, top, width, mode.Height),
                new(left, top + mode.Height + CardGap, width, card.Height)
            );
        }

        double height = Math.Max(mode.Height, card.Height);
        return (
            new(left, top, width - scoresWidth - CardGap, height),
            new(left + width - scoresWidth, top, scoresWidth, height)
        );
    }

    /// <summary>
    /// Keeps content in the largest clear rectangle beside a fold or camera occlusion.
    /// </summary>
    public static Rect ExcludeReservedRegion(Rect bounds, Rect region)
    {
        var intersection = bounds.Intersect(region);
        if (intersection.IsEmpty)
        {
            return bounds;
        }

        Rect largest = new(bounds.Left, bounds.Top, bounds.Width, intersection.Top - bounds.Top);
        Consider(
            new Rect(
                bounds.Left,
                intersection.Bottom,
                bounds.Width,
                bounds.Bottom - intersection.Bottom
            )
        );
        Consider(new Rect(bounds.Left, bounds.Top, intersection.Left - bounds.Left, bounds.Height));
        Consider(
            new Rect(
                intersection.Right,
                bounds.Top,
                bounds.Right - intersection.Right,
                bounds.Height
            )
        );
        return largest;

        void Consider(Rect candidate)
        {
            if (candidate.Width * candidate.Height > largest.Width * largest.Height)
            {
                largest = candidate;
            }
        }
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

public enum GameLayoutKind
{
    Stacked,
    Wings,
    SplitVertical,
    SplitHorizontal,
}

/// <summary>Absolute rectangles for every group on the game page.</summary>
public readonly record struct GameLayout(
    GameLayoutKind Kind,
    Rect Board,
    Rect Mode,
    Rect Scores,
    bool ScoresVertical,
    Rect Actions,
    Rect? BoardPane = null,
    Rect? ConsolePane = null
);
