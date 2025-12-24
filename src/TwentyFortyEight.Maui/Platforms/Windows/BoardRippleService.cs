using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

public sealed partial class BoardRippleService
{
    private static async partial Task<SKBitmap?> TryCaptureBitmapAsync(
        VisualElement boardContainer,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (boardContainer.Handler?.PlatformView is not FrameworkElement fe)
            return null;

        // RenderTargetBitmap must be created and used on the UI thread (STA).
        // Marshal the entire capture operation to the dispatcher to avoid COMException.
        var tcs = new TaskCompletionSource<(int Width, int Height, byte[] Bytes)?>();

        await boardContainer.Dispatcher.DispatchAsync(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rtb = new RenderTargetBitmap();
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
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(cancellationToken);
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
        var info = new SKImageInfo(
            capturedWidth,
            capturedHeight,
            SKColorType.Bgra8888,
            SKAlphaType.Premul
        );
        var bitmap = new SKBitmap(info);
        Marshal.Copy(capturedBytes, 0, bitmap.GetPixels(), capturedBytes.Length);
        return bitmap;
    }
}
