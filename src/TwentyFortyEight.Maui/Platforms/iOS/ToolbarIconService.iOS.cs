#if IOS || MACCATALYST
using Foundation;
using Microsoft.Maui.Controls;
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

        // Keep as template-ish as possible; iOS often tints bar button item images automatically.
        image = image.ImageWithRenderingMode(UIImageRenderingMode.Automatic);

        var data = image.AsPNG();
        if (data is null)
            return ImageSource.FromStream(static () => Stream.Null);

        byte[] bytes = data.ToArray();
        return ImageSource.FromStream(() => new MemoryStream(bytes));
    }
}
#endif
