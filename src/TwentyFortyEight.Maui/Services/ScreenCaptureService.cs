using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Platform-specific service for capturing visual elements as SKBitmap images.
/// Uses partial methods for platform-specific implementations.
/// </summary>
public partial class ScreenCaptureService : IScreenCaptureService
{
    public async Task<SKBitmap?> CaptureBitmapAsync(VisualElement element)
    {
        if (element.Width <= 1 || element.Height <= 1)
            return null;

        return await CaptureBitmapPlatformAsync(element);
    }

    /// <summary>
    /// Platform-specific bitmap capture implementation.
    /// Implemented in Platforms/ folders.
    /// </summary>
    private partial Task<SKBitmap?> CaptureBitmapPlatformAsync(VisualElement element);

    /// <summary>
    /// Converts an SKImage to an owned SKImage via PNG encoding/decoding.
    /// This ensures the bitmap lifetime is properly managed.
    /// </summary>
    public static SKImage? ConvertToOwnedImage(SKBitmap bitmap)
    {
        try
        {
            // Convert to SKImage via PNG stream and SKBitmap.Decode (as per plan requirement).
            using var encodedImage = SKImage.FromBitmap(bitmap);
            using var encodedData = encodedImage.Encode(SKEncodedImageFormat.Png, quality: 100);
            if (encodedData is null)
                return null;

            using MemoryStream stream = new();
            encodedData.SaveTo(stream);
            stream.Position = 0;

            var decodedBitmap = SKBitmap.Decode(stream);
            if (decodedBitmap is null)
                return null;

            return CreateOwnedImage(decodedBitmap);
        }
        catch
        {
            bitmap.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Creates an SKImage that owns the bitmap's lifetime.
    /// The bitmap will be disposed when the image is disposed.
    /// </summary>
    protected static SKImage? CreateOwnedImage(SKBitmap bitmap)
    {
        var pixmap = bitmap.PeekPixels();
        if (pixmap is null)
        {
            bitmap.Dispose();
            return null;
        }

        return SKImage.FromPixels(pixmap, static (_, ctx) => ((SKBitmap)ctx!).Dispose(), bitmap);
    }

    /// <summary>
    /// Helper to convert platform-captured bytes to SKBitmap.
    /// </summary>
    protected static SKBitmap CreateBitmapFromBytes(
        byte[] bytes,
        int width,
        int height,
        SKColorType colorType = SKColorType.Bgra8888,
        SKAlphaType alphaType = SKAlphaType.Premul
    )
    {
        SKImageInfo info = new(width, height, colorType, alphaType);
        SKBitmap bitmap = new(info);
        Marshal.Copy(bytes, 0, bitmap.GetPixels(), bytes.Length);
        return bitmap;
    }
}
