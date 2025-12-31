using Microsoft.Maui.Controls;

namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService : IToolbarIconService
{
    private readonly Lazy<ImageSource> _undo;

    public ToolbarIconService()
    {
        _undo = new Lazy<ImageSource>(CreateUndo);
    }

    public ImageSource Undo => _undo.Value;

    private static partial ImageSource CreateUndo();
}
