using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Windows implementation of screen capture using RenderTargetBitmap.
/// </summary>
public partial class ScreenCaptureService
{
    private async partial Task<SKBitmap?> CaptureBitmapPlatformAsync(VisualElement element)
    {
        if (element.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement fe)
            return null;

        // RenderTargetBitmap must be created and used on the UI thread (STA).
        TaskCompletionSource<(int Width, int Height, byte[] Bytes)?> tcs = new();

        await element.Dispatcher.DispatchAsync(async () =>
        {
            try
            {
                RenderTargetBitmap rtb = new();
                await rtb.RenderAsync(fe);

                var width = rtb.PixelWidth;
                var height = rtb.PixelHeight;
                if (width <= 0 || height <= 0)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                var pixelBuffer = await rtb.GetPixelsAsync();
                var bytes = pixelBuffer.ToArray();

                tcs.TrySetResult((width, height, bytes));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        var result = await tcs.Task.ConfigureAwait(true);
        if (result is null)
            return null;

        var (capturedWidth, capturedHeight, capturedBytes) = result.Value;
        return CreateBitmapFromBytes(capturedBytes, capturedWidth, capturedHeight);
    }
}
