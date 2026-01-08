using CommunityToolkit.Mvvm.Messaging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Messages;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Default implementation that uses WeakReferenceMessenger to request size changes.
/// </summary>
internal sealed class BoardSizeChangeService : IBoardSizeChangeService
{
    public void RequestBoardSizeChange(int newSize)
    {
        if (newSize <= 0 || newSize > GameConfig.MaxReasonableBoardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize));
        }

        WeakReferenceMessenger.Default.Send(new BoardSizeChangeRequestedMessage(newSize));
    }
}
