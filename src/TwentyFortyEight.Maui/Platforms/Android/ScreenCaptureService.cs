using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Android implementation of screen capture using Bitmap.CreateBitmap and Canvas.
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
                if (element.Handler?.PlatformView is not Android.Views.View view)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                var width = view.Width;
                var height = view.Height;
                if (width <= 0 || height <= 0)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                using var bitmap = Android.Graphics.Bitmap.CreateBitmap(
                    width,
                    height,
                    Android.Graphics.Bitmap.Config.Argb8888!
                );

                using (Android.Graphics.Canvas canvas = new(bitmap))
                {
                    view.Draw(canvas);
                }

                var byteCount = bitmap.ByteCount;
                var buffer = Java.Nio.ByteBuffer.AllocateDirect(byteCount);
                bitmap.CopyPixelsToBuffer(buffer);
                buffer.Rewind();

                var bytes = new byte[byteCount];
                buffer.Get(bytes);

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
