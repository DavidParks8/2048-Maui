namespace TwentyFortyEight.Maui.Services;

public partial class ToolbarIconService : IToolbarIconService
{
    private const string ModeIconBaseName = "ic_fluent_options_24_filled";

    private Lazy<ImageSource> _undo;
    private Lazy<ImageSource> _newGame;
    private readonly Lazy<ImageSource> _mode;
    private AppTheme _iconTheme;

    public ToolbarIconService()
    {
        _undo = new Lazy<ImageSource>(CreateUndo);
        _newGame = new Lazy<ImageSource>(CreateNewGame);
        _mode = new Lazy<ImageSource>(CreateMode);
        _iconTheme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
    }

    public ImageSource Undo
    {
        get
        {
            RefreshTheme();
            return _undo.Value;
        }
    }

    public ImageSource NewGame
    {
        get
        {
            RefreshTheme();
            return _newGame.Value;
        }
    }
    public ImageSource Mode => _mode.Value;

    private static partial ImageSource CreateUndo();

    private static partial ImageSource CreateNewGame();

    private void RefreshTheme()
    {
        var theme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
        if (_iconTheme == theme)
        {
            return;
        }

        _iconTheme = theme;
        _undo = new Lazy<ImageSource>(CreateUndo);
        _newGame = new Lazy<ImageSource>(CreateNewGame);
    }

    private static ImageSource CreateMode() => TryLoadImage(ModeIconBaseName);

    private static ImageSource TryLoadImage(string baseName)
    {
        // MAUI images are typically referenced by basename (without extension).
        // Some platforms/builds also resolve the explicit filename.
        return new FileImageSource { File = baseName };
    }
}
