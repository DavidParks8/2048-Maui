using System.Runtime.InteropServices;
using CoreGraphics;
using SkiaSharp;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

public sealed partial class BoardRippleService
{
    private static partial Task<SKBitmap?> TryCaptureBitmapAsync(
        VisualElement boardContainer,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (boardContainer.Handler?.PlatformView is not UIView view)
            return Task.FromResult<SKBitmap?>(null);

        var bounds = view.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return Task.FromResult<SKBitmap?>(null);

        UIGraphicsImageRenderer renderer = new(bounds.Size);
        using var image = renderer.CreateImage(_ =>
        {
            // drawViewHierarchy gives best fidelity for composed UI
            view.DrawViewHierarchy(bounds, true);
        });

        using var cgImage = image.CGImage;
        if (cgImage is null)
            return Task.FromResult<SKBitmap?>(null);

        var width = (int)cgImage.Width;
        var height = (int)cgImage.Height;
        if (width <= 0 || height <= 0)
            return Task.FromResult<SKBitmap?>(null);

        var bytesPerRow = width * 4;
        var bytes = new byte[bytesPerRow * height];

        using var colorSpace = CGColorSpace.CreateDeviceRGB();
        using CGBitmapContext ctx = new(
            bytes,
            width,
            height,
            8,
            bytesPerRow,
            colorSpace,
            CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst
        );

        ctx.DrawImage(new CGRect(0, 0, width, height), cgImage);

        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKBitmap skBitmap = new(info);
        Marshal.Copy(bytes, 0, skBitmap.GetPixels(), bytes.Length);
        return Task.FromResult<SKBitmap?>(skBitmap);
    }
}
