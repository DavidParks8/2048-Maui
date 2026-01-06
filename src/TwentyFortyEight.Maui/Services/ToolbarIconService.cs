using Microsoft.Maui.Controls;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService : IToolbarIconService
{
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

    private static partial ImageSource CreateMode();
}
