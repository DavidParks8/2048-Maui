using System.Runtime.InteropServices;
using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

public sealed partial class BoardRippleService
{
    private static partial Task<SKBitmap?> TryCaptureBitmapAsync(
        VisualElement boardContainer,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (boardContainer.Handler?.PlatformView is not Android.Views.View view)
            return Task.FromResult<SKBitmap?>(null);

        var width = view.Width;
        var height = view.Height;
        if (width <= 0 || height <= 0)
            return Task.FromResult<SKBitmap?>(null);

        using var bitmap = Android.Graphics.Bitmap.CreateBitmap(
            width,
            height,
            Android.Graphics.Bitmap.Config.Argb8888!
        );
        using (var canvas = new Android.Graphics.Canvas(bitmap))
        {
            view.Draw(canvas);
        }

        var byteCount = bitmap.ByteCount;
        var buffer = Java.Nio.ByteBuffer.AllocateDirect(byteCount);
        bitmap.CopyPixelsToBuffer(buffer);
        buffer.Rewind();

        var bytes = new byte[byteCount];
        buffer.Get(bytes);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var skBitmap = new SKBitmap(info);
        Marshal.Copy(bytes, 0, skBitmap.GetPixels(), bytes.Length);
        return Task.FromResult<SKBitmap?>(skBitmap);
    }
}
