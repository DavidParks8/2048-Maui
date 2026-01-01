using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for capturing visual elements as SKBitmap images.
/// Platform-specific implementations handle native rendering APIs.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Captures a visual element as an SKBitmap.
    /// </summary>
    /// <param name="element">The visual element to capture.</param>
    /// <returns>The captured bitmap, or null if capture failed.</returns>
    Task<SKBitmap?> CaptureBitmapAsync(VisualElement element);
}
