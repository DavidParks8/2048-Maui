using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Main scene that ties together all game components.
/// </summary>
public partial class MainScene : Control
{
    // Singletons (autoload)
    private GameSettings? _settings;
    private GodotStatisticsTracker? _statistics;
    private GameSaveManager? _saveManager;

    // Game controller
    private GameController? _gameController;

    // UI Components
    private BoardVisual? _boardVisual;
    private SwipeDetector? _swipeDetector;

    // Header
    private Label? _sizeLabel;
    private Label? _sizeValueLabel;
    private Label? _scoreLabel;
    private Label? _scoreValueLabel;
    private Label? _bestLabel;
    private Label? _bestValueLabel;

    // Mode indicator
    private Button? _modeButton;

    // Controls
    private Button? _newGameButton;
    private Button? _undoButton;
    private Button? _menuButton;

    // Overlays
    private Control? _gameOverOverlay;
    private Control? _victoryOverlay;
    private Control? _menuOverlay;
    private Control? _modeSelectionOverlay;
    private PanelContainer? _coachNudgeBanner;
    private PanelContainer? _coachHintPanel;
    private Label? _coachHintLabel;

    private bool _isDarkTheme;

    public override void _Ready()
    {
        // Initialize singletons
        _settings = new GameSettings();
        AddChild(_settings);

        _statistics = new GodotStatisticsTracker();
        AddChild(_statistics);

        _saveManager = new GameSaveManager();
        AddChild(_saveManager);

        // Wait a frame for singletons to initialize
        CallDeferred(MethodName.InitializeUI);
    }

    private void InitializeUI()
    {
        _isDarkTheme = _settings?.IsDarkTheme ?? false;

        // Set up main layout
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Create background
        var background = new ColorRect
        {
            Color = _isDarkTheme ? TileColors.PageBackgroundDark : TileColors.PageBackgroundLight,
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        // Main container with margin
        var mainContainer = new MarginContainer();
        mainContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        mainContainer.AddThemeConstantOverride("margin_left", 16);
        mainContainer.AddThemeConstantOverride("margin_right", 16);
        mainContainer.AddThemeConstantOverride("margin_top", 16);
        mainContainer.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(mainContainer);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 10);
        mainContainer.AddChild(vbox);

        // Header
        CreateHeader(vbox);

        // Coach nudge banner
        _coachNudgeBanner = CreateCoachNudgeBanner();
        vbox.AddChild(_coachNudgeBanner);

        // Mode button
        CreateModeButton(vbox);

        // Game board container - use a Control that expands to fill available space
        var boardContainer = new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        vbox.AddChild(boardContainer);

        // Create board with aspect ratio container that fills the board container
        var aspectRatio = new AspectRatioContainer
        {
            Ratio = 1.0f,
            StretchMode = AspectRatioContainer.StretchModeEnum.WidthControlsHeight,
        };
        aspectRatio.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        boardContainer.AddChild(aspectRatio);

        _boardVisual = new BoardVisual
        {
            BoardSize = _settings?.LastBoardSize ?? 4,
            IsDarkTheme = _isDarkTheme,
        };
        _boardVisual.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        aspectRatio.AddChild(_boardVisual);

        // Swipe detector (covers the board area)
        _swipeDetector = new SwipeDetector();
        _swipeDetector.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _swipeDetector.SwipeDetected += OnSwipeDetected;
        aspectRatio.AddChild(_swipeDetector);

        // Coach hint
        _coachHintPanel = CreateCoachHintPanel();
        vbox.AddChild(_coachHintPanel);

        // Bottom controls
        CreateBottomControls(vbox);

        // Create overlays
        CreateOverlays();

        // Initialize game controller
        _gameController = new GameController
        {
            BoardVisual = _boardVisual,
            ScoreLabel = _scoreValueLabel,
            BestScoreLabel = _bestValueLabel,
            SizeLabel = _sizeValueLabel,
            UndoButton = _undoButton,
            CoachNudge = _coachNudgeBanner,
            CoachHint = _coachHintPanel,
        };
        AddChild(_gameController);
        _gameController.CoachSuggestionChanged += OnCoachSuggestionChanged;

        // Connect board tap for adversarial mode
        _boardVisual.TileTapped += OnTileTapped;

        // Connect game events
        _gameController.GameEnded += OnGameEnded;
        _gameController.VictoryAchieved += OnVictoryAchieved;

        // Subscribe to theme changes
        if (_settings != null)
        {
            _settings.ThemeChanged += OnThemeChanged;
        }
    }

    private void OnCoachSuggestionChanged(Direction direction)
    {
        if (_coachHintLabel == null)
        {
            return;
        }

        var glyph = direction switch
        {
            Direction.Up => "↑",
            Direction.Down => "↓",
            Direction.Left => "←",
            Direction.Right => "→",
            _ => "?",
        };

        _coachHintLabel.Text = string.Format(Strings.CoachSuggestionFormat, glyph);
        _coachHintPanel?.Show();
    }

    private void CreateHeader(VBoxContainer parent)
    {
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        parent.AddChild(header);

        // Size card
        var sizeCard = CreateStatCard(Strings.Size, "4x4", out _sizeValueLabel);
        header.AddChild(sizeCard);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(spacer);

        // Score card
        var scoreCard = CreateStatCard(Strings.Score, "0", out _scoreValueLabel);
        header.AddChild(scoreCard);

        // Best card
        var bestCard = CreateStatCard(Strings.Best, "0", out _bestValueLabel);
        header.AddChild(bestCard);
    }

    private Control CreateStatCard(string title, string value, out Label valueLabel)
    {
        var panel = new PanelContainer();
        var styleBox = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("3d3d5c") : new Color("bbada0"),
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        var titleLabel = new Label
        {
            Text = title.ToUpper(),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleLabel.AddThemeColorOverride(
            "font_color",
            _isDarkTheme ? new Color("aaaaaa") : new Color("eee4da")
        );
        vbox.AddChild(titleLabel);

        valueLabel = new Label
        {
            Name = "Value",
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 22);
        valueLabel.AddThemeColorOverride("font_color", _isDarkTheme ? Colors.White : Colors.White);
        vbox.AddChild(valueLabel);

        return panel;
    }

    private PanelContainer CreateCoachNudgeBanner()
    {
        var banner = new PanelContainer { Visible = false };
        var style = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("2a2a4a") : new Color(1f, 1f, 1f, 0.95f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        };
        banner.AddThemeStyleboxOverride("panel", style);
        LiquidGlassHelper.Apply(
            banner,
            _isDarkTheme ? new Color(0.25f, 0.25f, 0.4f, 0.95f) : new Color(1f, 1f, 1f, 0.95f)
        );

        var hbox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hbox.AddThemeConstantOverride("separation", 10);
        banner.AddChild(hbox);

        var text = new Label
        {
            Text = Strings.CoachNudgeMessage,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hbox.AddChild(text);

        var enableButton = new Button { Text = Strings.EnableCoachButton };
        enableButton.Pressed += () =>
        {
            _gameController?.EnableCoachFromNudge();
            banner.Hide();
        };
        hbox.AddChild(enableButton);

        var dismissButton = new Button { Text = Strings.DismissButton, Flat = true };
        dismissButton.Pressed += () =>
        {
            _gameController?.DismissCoachNudge();
            banner.Hide();
        };
        hbox.AddChild(dismissButton);

        return banner;
    }

    private PanelContainer CreateCoachHintPanel()
    {
        var panel = new PanelContainer { Visible = false };
        var style = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("3d3d5c") : new Color(1f, 1f, 1f, 0.9f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        };
        panel.AddThemeStyleboxOverride("panel", style);
        LiquidGlassHelper.Apply(
            panel,
            _isDarkTheme ? new Color(0.3f, 0.3f, 0.45f, 0.95f) : new Color(1f, 1f, 1f, 0.95f)
        );

        var hbox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(hbox);

        var emoji = new Label { Text = "🤖", VerticalAlignment = VerticalAlignment.Center };
        hbox.AddChild(emoji);

        _coachHintLabel = new Label
        {
            Text = Strings.CoachHintPlaceholder,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hbox.AddChild(_coachHintLabel);

        return panel;
    }

    private void CreateModeButton(VBoxContainer parent)
    {
        _modeButton = new Button
        {
            Text = GameController.GetModeDisplayName(_settings?.LastGameMode ?? GameMode.Classic),
            Flat = true,
        };
        _modeButton.AddThemeFontSizeOverride("font_size", 14);
        _modeButton.Pressed += OnModeButtonPressed;
        parent.AddChild(_modeButton);
    }

    private void CreateBottomControls(VBoxContainer parent)
    {
        var controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", 8);
        parent.AddChild(controls);

        // New Game button
        _newGameButton = CreateIconButton("New", OnNewGamePressed);
        controls.AddChild(_newGameButton);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        controls.AddChild(spacer);

        // Undo button
        _undoButton = CreateIconButton("Undo", OnUndoPressed);
        _undoButton.Visible = _settings?.UndoButtonVisible ?? true;
        controls.AddChild(_undoButton);

        // Menu button
        _menuButton = CreateIconButton("Menu", OnMenuPressed);
        controls.AddChild(_menuButton);
    }

    private Button CreateIconButton(string text, Action pressedHandler)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(56, 56) };

        var styleBox = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("3d3d5c") : new Color("8f7a66"),
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
        };
        button.AddThemeStyleboxOverride("normal", styleBox);

        var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
        hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.1f);
        button.AddThemeStyleboxOverride("hover", hoverStyle);

        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 16);

        button.Pressed += pressedHandler;

        return button;
    }

    private void CreateOverlays()
    {
        // Game Over overlay
        _gameOverOverlay = CreateOverlay(
            Strings.GameOver,
            Strings.TryAgain,
            OnTryAgainPressed,
            isVictory: false
        );
        AddChild(_gameOverOverlay);

        // Victory overlay
        _victoryOverlay = CreateOverlay(
            Strings.Victory,
            Strings.KeepPlaying,
            OnKeepPlayingPressed,
            isVictory: true,
            secondaryButton: (Strings.NewGame, OnVictoryNewGamePressed)
        );
        AddChild(_victoryOverlay);

        // Menu overlay
        _menuOverlay = CreateMenuOverlay();
        AddChild(_menuOverlay);

        // Mode selection overlay
        _modeSelectionOverlay = CreateModeSelectionOverlay();
        AddChild(_modeSelectionOverlay);
    }

    private Control CreateOverlay(
        string title,
        string buttonText,
        Action buttonHandler,
        bool isVictory,
        (string text, Action handler)? secondaryButton = null
    )
    {
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.AddChild(center);

        var panel = new PanelContainer();
        var styleBox = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("2a2a4a") : new Color("faf8ef"),
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ContentMarginLeft = 40,
            ContentMarginRight = 40,
            ContentMarginTop = 32,
            ContentMarginBottom = 32,
        };
        panel.AddThemeStyleboxOverride("panel", styleBox);
        LiquidGlassHelper.Apply(
            panel,
            _isDarkTheme ? new Color(0.2f, 0.2f, 0.35f, 0.95f) : new Color(1f, 1f, 1f, 0.95f)
        );
        center.AddChild(panel);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        titleLabel.AddThemeColorOverride(
            "font_color",
            isVictory ? new Color("edc22e") : new Color("776e65")
        );
        vbox.AddChild(titleLabel);

        if (isVictory)
        {
            var subtitle = new Label
            {
                Text = Strings.VictorySubtitle,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            subtitle.AddThemeFontSizeOverride("font_size", 18);
            subtitle.AddThemeColorOverride("font_color", new Color("776e65"));
            vbox.AddChild(subtitle);
        }

        var button = new Button { Text = buttonText };
        button.Pressed += buttonHandler;
        vbox.AddChild(button);

        if (secondaryButton.HasValue)
        {
            var secButton = new Button { Text = secondaryButton.Value.text, Flat = true };
            secButton.Pressed += secondaryButton.Value.handler;
            vbox.AddChild(secButton);
        }

        return overlay;
    }

    private Control CreateMenuOverlay()
    {
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Close on background click
        overlay.GuiInput += (inputEvent) =>
        {
            if (inputEvent is InputEventMouseButton mb && mb.Pressed)
                overlay.Hide();
        };

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 64);
        margin.AddThemeConstantOverride("margin_bottom", 64);
        overlay.AddChild(margin);

        var panel = new PanelContainer();
        var styleBox = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("2a2a4a") : new Color("faf8ef"),
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
        };
        panel.AddThemeStyleboxOverride("panel", styleBox);
        LiquidGlassHelper.Apply(
            panel,
            _isDarkTheme ? new Color(0.2f, 0.2f, 0.35f, 0.95f) : new Color(1f, 1f, 1f, 0.95f)
        );
        margin.AddChild(panel);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        panel.AddChild(scroll);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(vbox);

        // Menu items
        var menuItems = new[]
        {
            (Strings.NewGame, (Action)OnMenuNewGame),
            (Strings.HowToPlay, (Action)OnMenuHowToPlay),
            (Strings.Statistics, (Action)OnMenuStatistics),
            (Strings.Settings, (Action)OnMenuSettings),
            (Strings.About, (Action)OnMenuAbout),
        };

        foreach (var (text, handler) in menuItems)
        {
            var btn = new Button { Text = text, Alignment = HorizontalAlignment.Left };
            btn.AddThemeFontSizeOverride("font_size", 18);
            btn.Pressed += () =>
            {
                overlay.Hide();
                handler();
            };
            vbox.AddChild(btn);
        }

        return overlay;
    }

    private Control CreateModeSelectionOverlay()
    {
        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        overlay.GuiInput += (inputEvent) =>
        {
            if (inputEvent is InputEventMouseButton mb && mb.Pressed)
                overlay.Hide();
        };

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.AddChild(center);

        var panel = new PanelContainer();
        var styleBox = new StyleBoxFlat
        {
            BgColor = _isDarkTheme ? new Color("2a2a4a") : new Color("faf8ef"),
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        panel.AddThemeStyleboxOverride("panel", styleBox);
        LiquidGlassHelper.Apply(
            panel,
            _isDarkTheme ? new Color(0.2f, 0.2f, 0.35f, 0.95f) : new Color(1f, 1f, 1f, 0.95f)
        );
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = Strings.Mode,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(title);

        // Board size selection
        var sizeLabel = new Label { Text = "Board Size" };
        vbox.AddChild(sizeLabel);

        var sizeContainer = new HBoxContainer();
        vbox.AddChild(sizeContainer);

        var sizes = new[] { 3, 4, 5, 6, 8 };
        foreach (var size in sizes)
        {
            var btn = new Button { Text = $"{size}x{size}" };
            btn.Pressed += () => OnBoardSizeSelected(size);
            sizeContainer.AddChild(btn);
        }

        // Mode selection
        var modeLabel = new Label { Text = "Game Mode" };
        vbox.AddChild(modeLabel);

        var modes = new[]
        {
            (GameMode.Classic, Strings.ClassicMode),
            (GameMode.Modern, Strings.ModernMode),
            (GameMode.Walltastrophy, Strings.WalltastrophyMode),
            (GameMode.Adversarial, Strings.AdversarialMode),
        };

        foreach (var (mode, name) in modes)
        {
            var btn = new Button { Text = name };
            btn.Pressed += () =>
            {
                var boardSize = _settings?.LastBoardSize ?? 4;
                _gameController?.ChangeMode(boardSize, mode);
                _modeButton!.Text = GameController.GetModeDisplayName(mode);
                overlay.Hide();
            };
            vbox.AddChild(btn);
        }

        return overlay;
    }

    // Event handlers
    private void OnSwipeDetected(Direction direction)
    {
        if (_gameController != null)
        {
            _ = _gameController.MoveAsync(direction);
        }
    }

    private void OnTileTapped(int tileIndex)
    {
        if (_gameController != null && _gameController.IsAdversarialMode)
        {
            _ = _gameController.TapEmptyCellAsync(tileIndex);
        }
    }

    private void OnNewGamePressed()
    {
        // TODO: Show confirmation dialog
        _gameController?.NewGame();
    }

    private void OnUndoPressed()
    {
        _gameController?.Undo();
    }

    private void OnMenuPressed()
    {
        _menuOverlay?.Show();
    }

    private void OnModeButtonPressed()
    {
        _modeSelectionOverlay?.Show();
    }

    private void OnBoardSizeSelected(int size)
    {
        var mode = _settings?.LastGameMode ?? GameMode.Classic;
        _gameController?.ChangeMode(size, mode);
        _boardVisual!.BoardSize = size;
        _modeSelectionOverlay?.Hide();
    }

    private void OnGameEnded()
    {
        _gameOverOverlay?.Show();
    }

    private void OnVictoryAchieved()
    {
        _victoryOverlay?.Show();
    }

    private void OnTryAgainPressed()
    {
        _gameOverOverlay?.Hide();
        _gameController?.NewGame();
    }

    private void OnKeepPlayingPressed()
    {
        _victoryOverlay?.Hide();
    }

    private void OnVictoryNewGamePressed()
    {
        _victoryOverlay?.Hide();
        _gameController?.NewGame();
    }

    // Menu handlers
    private void OnMenuNewGame()
    {
        _gameController?.NewGame();
    }

    private void OnMenuHowToPlay()
    {
        // Show how to play dialog
        var dialog = new AcceptDialog
        {
            Title = Strings.HowToPlayTitle,
            DialogText = Strings.HowToPlayInstructions,
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnMenuStatistics()
    {
        // Show statistics dialog
        var tracker = GodotStatisticsTracker.Instance;
        if (tracker == null)
            return;

        var stats = tracker.GetStatistics();
        var text = $"""
            {Strings.GamesPlayed}: {stats.GamesPlayed}
            {Strings.GamesWon}: {stats.GamesWon}
            {Strings.WinRate}: {stats.WinRate:F1}%
            {Strings.BestScore}: {stats.BestScore}
            {Strings.AverageScore}: {stats.AverageScore}
            {Strings.HighestTile}: {stats.HighestTile}
            {Strings.TotalMoves}: {stats.TotalMoves}
            {Strings.CurrentStreak}: {stats.CurrentStreak}
            {Strings.BestStreak}: {stats.BestStreak}
            """;

        var dialog = new AcceptDialog { Title = Strings.Statistics, DialogText = text };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnMenuSettings()
    {
        // Show settings dialog
        var dialog = new Window
        {
            Title = Strings.Settings,
            Size = new Vector2I(300, 400),
            Transient = true,
            Exclusive = true,
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        dialog.AddChild(vbox);

        // Coach toggle
        var coachCheck = new CheckBox
        {
            Text = Strings.EnableCoach,
            ButtonPressed = _settings?.CoachEnabled ?? false,
        };
        coachCheck.Toggled += (pressed) =>
        {
            if (_settings != null)
                _settings.CoachEnabled = pressed;
        };
        vbox.AddChild(coachCheck);

        // Undo button toggle
        var undoCheck = new CheckBox
        {
            Text = Strings.ShowUndoButton,
            ButtonPressed = _settings?.UndoButtonVisible ?? true,
        };
        undoCheck.Toggled += (pressed) =>
        {
            if (_settings != null)
                _settings.UndoButtonVisible = pressed;
            if (_undoButton != null)
                _undoButton.Visible = pressed;
        };
        vbox.AddChild(undoCheck);

        // Dark theme toggle
        var themeCheck = new CheckBox { Text = "Dark Theme", ButtonPressed = _isDarkTheme };
        themeCheck.Toggled += (pressed) =>
        {
            if (_settings != null)
                _settings.IsDarkTheme = pressed;
        };
        vbox.AddChild(themeCheck);

        var closeBtn = new Button { Text = "Close" };
        closeBtn.Pressed += () => dialog.Hide();
        vbox.AddChild(closeBtn);

        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnMenuAbout()
    {
        var dialog = new AcceptDialog
        {
            Title = Strings.AboutTitle,
            DialogText = $"{Strings.ForTalia}\n\n{Strings.AboutMessage}\n\n{Strings.MadeWithLove}",
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OnThemeChanged()
    {
        _isDarkTheme = _settings?.IsDarkTheme ?? false;

        if (_boardVisual != null)
        {
            _boardVisual.IsDarkTheme = _isDarkTheme;
        }

        // Would need to rebuild UI for full theme change
        // For now, just update the board
    }
}
