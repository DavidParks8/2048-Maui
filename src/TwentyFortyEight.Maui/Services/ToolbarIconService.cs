using Microsoft.Maui.Controls;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService : IToolbarIconService
{
    private const string ModeIconBaseName = "ic_fluent_options_24_filled";

    private readonly Lazy<ImageSource> _undo;
    private readonly Lazy<ImageSource> _mode;

    public ToolbarIconService()
    {
        _undo = new Lazy<ImageSource>(CreateUndo);
        _mode = new Lazy<ImageSource>(CreateMode);
    }

    public ImageSource Undo => _undo.Value;
    public ImageSource Mode => _mode.Value;

    private static partial ImageSource CreateUndo();

    private static ImageSource CreateMode() => TryLoadImage(ModeIconBaseName);

    private static ImageSource TryLoadImage(string baseName)
    {
        // MAUI images are typically referenced by basename (without extension).
        // Some platforms/builds also resolve the explicit filename.
        return new FileImageSource { File = baseName };
    }
}
