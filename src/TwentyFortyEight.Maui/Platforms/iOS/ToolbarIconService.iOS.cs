#if IOS || MACCATALYST
using System.IO;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService
{
    private static partial ImageSource CreateUndo() => FromSystemImage("arrow.uturn.backward");

    static ImageSource FromSystemImage(string symbolName)
    {
        UIImage? image = UIImage.GetSystemImage(symbolName);
        if (image is null)
            return ImageSource.FromStream(static () => Stream.Null);

        image = image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

        UIColor tintColor = ResolveToolbarTintColor();

        UIImage? tinted = RenderTinted(image, tintColor);
        if (tinted is null)
            return ImageSource.FromStream(static () => Stream.Null);

        var data = tinted.AsPNG();
        if (data is null)
            return ImageSource.FromStream(static () => Stream.Null);

        byte[] bytes = data.ToArray();
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    static UIImage? RenderTinted(UIImage templateImage, UIColor tintColor)
    {
        var size = templateImage.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return null;

        var format = UIGraphicsImageRendererFormat.DefaultFormat;
        format.Opaque = false;

        UIGraphicsImageRenderer renderer = new(size, format);
        return renderer.CreateImage(_ =>
        {
            tintColor.SetColor();
            templateImage.Draw(new CoreGraphics.CGRect(0, 0, size.Width, size.Height));
        });
    }

    static UIColor ResolveToolbarTintColor()
    {
        // If the app defines a dedicated tint resource, prefer it.
        if (
            Application.Current?.Resources.TryGetValue("ToolbarIconTintColor", out var tint) == true
            && tint is Color tintColor
        )
            return tintColor.ToPlatform();

        // Use iOS dynamic system colors when available.
        // (Some bindings don't expose UIColor.LabelColor directly.)
        var label = UIColor.FromName("labelColor");
        if (label is not null)
            return label;

        var secondaryLabel = UIColor.FromName("secondaryLabelColor");
        if (secondaryLabel is not null)
            return secondaryLabel;

        // Fallback to a theme-aware value.
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return (isDark ? Colors.White : Colors.Black).ToPlatform();
    }
}
#endif
