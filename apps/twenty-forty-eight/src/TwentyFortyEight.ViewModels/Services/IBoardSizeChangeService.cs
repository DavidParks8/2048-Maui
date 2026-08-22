namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service abstraction for requesting a runtime board size change without directly
/// referencing the messenger from UI code.
/// </summary>
public interface IBoardSizeChangeService
{
    /// <summary>
    /// Requests that the active game switch to the specified board size.
    /// The request is broadcast via messaging; the game view model applies it.
    /// </summary>
    /// <param name="newSize">New board size (e.g., 4-8).</param>
    void RequestBoardSizeChange(int newSize);
}
