using Microsoft.Maui.Controls;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

public interface IWallOverlayRenderer
{
    void Update(
        Grid gameBoard,
        AbsoluteLayout overlayLayer,
        int boardSize,
        WallSegment? wall,
        VisualElement animationHost
    );

    void Reset(VisualElement animationHost);
}
