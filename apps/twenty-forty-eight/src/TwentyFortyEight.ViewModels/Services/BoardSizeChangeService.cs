using CommunityToolkit.Mvvm.Messaging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Messages;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Default implementation that uses IMessenger to request size changes.
/// </summary>
internal sealed class BoardSizeChangeService(IMessenger messenger) : IBoardSizeChangeService
{
    public void RequestBoardSizeChange(int newSize)
    {
        if (newSize <= 0 || newSize > GameConfig.MaxReasonableBoardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize));
        }

        messenger.Send(new BoardSizeChangeRequestedMessage(newSize));
    }
}
