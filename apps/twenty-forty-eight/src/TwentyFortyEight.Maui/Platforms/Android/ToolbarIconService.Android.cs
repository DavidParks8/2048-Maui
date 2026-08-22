#if ANDROID
using Android.Content;
using Android.Graphics;
using Microsoft.Maui.Platform;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService
{
    private static partial ImageSource CreateUndo() =>
        FromSystemDrawable(global::Android.Resource.Drawable.IcMenuRevert);

    static ImageSource FromSystemDrawable(int drawableId)
    {
        Context? context = global::Android.App.Application.Context;
        var drawable = context.GetDrawable(drawableId)?.Mutate();
        if (drawable is null)
            return ImageSource.FromStream(static () => Stream.Null);

        try
        {
            var tintColor = ResolveToolbarTintColor();
            drawable.SetTint(tintColor.ToPlatform());
        }
        catch
        {
            // Best-effort tinting; some drawables may not support it.
        }

        const int sizePx = 96;

        using var bitmap = Bitmap.CreateBitmap(sizePx, sizePx, Bitmap.Config.Argb8888!);
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

    static MauiColor ResolveToolbarTintColor()
    {
        if (
            Application.Current?.Resources.TryGetValue("ToolbarIconTintColor", out var tint) == true
            && tint is MauiColor tintColor
        )
            return tintColor;

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return isDark ? MauiColor.FromArgb("#FFFFFFFF") : MauiColor.FromArgb("#FF000000");
    }
}
#endif
