using Godot;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Visual representation of a single tile on the game board.
/// </summary>
public partial class TileVisual : Control
{
    private ColorRect? _background;
    private Label? _label;
    private int _value;
    private bool _isDarkTheme;

    public int Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                UpdateVisual();
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                UpdateVisual();
            }
        }
    }

    public override void _Ready()
    {
        _background = new ColorRect { AnchorRight = 1, AnchorBottom = 1 };
        AddChild(_background);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorRight = 1,
            AnchorBottom = 1,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        AddChild(_label);

        UpdateVisual();
        Resized += OnResized;
    }

    private void OnResized()
    {
        UpdateFontSize();
    }

    private void UpdateVisual()
    {
        if (_background == null || _label == null)
            return;

        _background.Color = TileColors.GetTileBackgroundColor(_value, _isDarkTheme);

        if (_value == 0)
        {
            _label.Text = "";
        }
        else
        {
            _label.Text = _value.ToString();
            _label.AddThemeColorOverride(
                "font_color",
                TileColors.GetTileTextColor(_value, _isDarkTheme)
            );
        }

        UpdateFontSize();
    }

    private void UpdateFontSize()
    {
        if (_label == null)
            return;

        float tileSize = Mathf.Min(Size.X, Size.Y);
        int fontSize = TileColors.GetTileFontSize(_value, tileSize);
        _label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    /// <summary>
    /// Plays a spawn animation (scale from 0 to 1).
    /// </summary>
    public async void PlaySpawnAnimation()
    {
        Scale = Vector2.Zero;
        PivotOffset = Size / 2;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", Vector2.One, 0.15);

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>
    /// Plays a merge animation (pop effect).
    /// </summary>
    public async void PlayMergeAnimation()
    {
        PivotOffset = Size / 2;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.1);
        tween.TweenProperty(this, "scale", Vector2.One, 0.1);

        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
