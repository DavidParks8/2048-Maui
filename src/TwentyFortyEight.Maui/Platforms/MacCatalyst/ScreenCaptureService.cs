using CoreGraphics;
using SkiaSharp;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MacCatalyst implementation of screen capture using UIGraphicsImageRenderer.
/// </summary>
public partial class ScreenCaptureService
{
    private async partial Task<SKBitmap?> CaptureBitmapPlatformAsync(VisualElement element)
    {
        TaskCompletionSource<SKBitmap?> tcs = new();

        element.Dispatcher.Dispatch(() =>
        {
            try
            {
                if (element.Handler?.PlatformView is not UIView view)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                var bounds = view.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                UIGraphicsImageRenderer renderer = new(bounds.Size);
                using var image = renderer.CreateImage(_ =>
                {
                    view.DrawViewHierarchy(bounds, true);
                });

                using var cgImage = image.CGImage;
                if (cgImage is null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                var width = (int)cgImage.Width;
                var height = (int)cgImage.Height;
                if (width <= 0 || height <= 0)
                {
                    tcs.TrySetResult(null);
                    return;
                }

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

                var skBitmap = CreateBitmapFromBytes(bytes, width, height);
                tcs.TrySetResult(skBitmap);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return await tcs.Task.ConfigureAwait(true);
    }
}
