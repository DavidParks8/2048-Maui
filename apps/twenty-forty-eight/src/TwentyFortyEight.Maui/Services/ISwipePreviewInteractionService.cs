using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.Maui.Services;

public interface ISwipePreviewInteractionService
{
    Task HandleSwipePanUpdatedAsync(SwipePanEventArgs e, SwipePreviewUiContext context);

    Task HandleTilesUpdatedAsync(TileUpdateEventArgs e, SwipePreviewUiContext context);

    void Reset();
}
