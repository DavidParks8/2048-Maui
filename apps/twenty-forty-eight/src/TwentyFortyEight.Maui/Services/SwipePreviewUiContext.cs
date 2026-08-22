using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.Maui.Services;

public readonly record struct SwipePreviewUiContext(
    Grid GameBoard,
    int BoardSize,
    IReadOnlyDictionary<TileViewModel, Border> TileBorders,
    double ScaleFactor,
    bool IsInputBlocked,
    bool IsModeSheetVisible,
    bool IsTileAnimationRunning
);
