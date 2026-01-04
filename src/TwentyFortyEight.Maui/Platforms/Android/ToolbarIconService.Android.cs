#if ANDROID
using Android.Content;
using Android.Graphics;
using System.IO;
using Microsoft.Maui.Controls;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService
{
    private static partial ImageSource CreateUndo() =>
        FromSystemDrawable(global::Android.Resource.Drawable.IcMenuRevert);

    static ImageSource FromSystemDrawable(int drawableId)
    {
        Context? context = global::Android.App.Application.Context;
        var drawable = context.GetDrawable(drawableId);
        if (drawable is null)
            return ImageSource.FromStream(static () => Stream.Null);

        var width = drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : 96;
        var height = drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : 96;

        using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
        using (var canvas = new Canvas(bitmap))
        {
            drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);
        }

        using var ms = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, ms);

        byte[] bytes = ms.ToArray();
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }
}
#endif
