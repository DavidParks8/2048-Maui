#if WINDOWS
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService
{
    private static partial ImageSource CreateUndo() => CreatePngGlyph("undo", "\uE7A7");

    static ImageSource CreatePngGlyph(string key, string glyph)
    {
        var themeSuffix = Application.Current?.RequestedTheme == AppTheme.Dark ? "dark" : "light";
        var fileName = $"toolbar_{key}_{themeSuffix}.png";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);

        if (!File.Exists(path))
        {
            var color = ResolveThemeColor(
                "NativeTextPrimaryLight",
                "NativeTextPrimaryDark",
                fallback: Colors.Black
            );

            byte[] png = RenderGlyphPng(
                glyph,
                fontFamily: "Segoe MDL2 Assets",
                color,
                pixelSize: 64
            );
            File.WriteAllBytes(path, png);
        }

        return ImageSource.FromFile(path);
    }

    static byte[] RenderGlyphPng(string glyph, string fontFamily, Color color, int pixelSize)
    {
        using SKBitmap bitmap = new(pixelSize, pixelSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using SKPaint paint = new()
        {
            IsAntialias = true,
            Color = new SKColor(
                (byte)(color.Red * 255),
                (byte)(color.Green * 255),
                (byte)(color.Blue * 255),
                (byte)(color.Alpha * 255)
            ),
            TextAlign = SKTextAlign.Left,
        };

        using var typeface = SKTypeface.FromFamilyName(fontFamily);
        paint.Typeface = typeface;
        paint.TextSize = pixelSize * 0.62f;

        // Center glyph
        var text = glyph;
        SKRect bounds = new();
        paint.MeasureText(text, ref bounds);
        float x = (pixelSize - bounds.Width) / 2f - bounds.Left;
        float y = (pixelSize - bounds.Height) / 2f - bounds.Top;

        canvas.DrawText(text, x, y, paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    static Color ResolveThemeColor(string lightKey, string darkKey, Color fallback)
    {
        if (Application.Current is null)
            return fallback;

        var key = Application.Current.RequestedTheme == AppTheme.Dark ? darkKey : lightKey;

        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Color color)
            return color;

        return fallback;
    }
}
#endif
