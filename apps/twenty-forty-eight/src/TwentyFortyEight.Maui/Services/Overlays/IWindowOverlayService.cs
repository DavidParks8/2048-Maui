namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for showing overlays at the app/window level.
/// On iOS/macCatalyst, uses platform APIs to cover the entire screen including nav bars.
/// On other platforms, adds the overlay to the current page's visual tree.
/// </summary>
public interface IWindowOverlayService
{
    /// <summary>
    /// Shows the bottom sheet overlay with the specified content.
    /// The sheet will automatically close when the user taps the scrim, close button, or drags it down.
    /// </summary>
    void ShowBottomSheet(string title, View content);

    /// <summary>
    /// Hides the currently visible bottom sheet.
    /// </summary>
    void HideBottomSheet();

    /// <summary>
    /// Gets whether a bottom sheet is currently visible.
    /// </summary>
    bool IsBottomSheetVisible { get; }

    /// <summary>
    /// Raised when the bottom sheet is dismissed (by user or programmatically).
    /// </summary>
    event EventHandler? BottomSheetDismissed;
}
