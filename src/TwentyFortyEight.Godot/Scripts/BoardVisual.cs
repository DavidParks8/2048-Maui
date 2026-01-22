using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Visual representation of the game board grid.
/// </summary>
public partial class BoardVisual : Control
{
    private int _boardSize = 4;
    private float _tileSpacing = 10;
    private float _cornerRadius = 5;
    private bool _isDarkTheme;
    private readonly List<TileVisual> _tiles = [];
    private ColorRect? _backgroundPanel;
    private Control? _tilesContainer;
    private Control? _wallOverlay;
    private WallSegment? _currentWall;

    public int BoardSize
    {
        get => _boardSize;
        set
        {
            if (_boardSize != value)
            {
                _boardSize = value;
                RebuildBoard();
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
                UpdateTheme();
            }
        }
    }

    public event Action<int>? TileTapped;

    public override void _Ready()
    {
        // Background panel - needs full rect preset for proper sizing
        _backgroundPanel = new ColorRect();
        _backgroundPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _backgroundPanel.Color = _isDarkTheme
            ? TileColors.PanelBackgroundDark
            : TileColors.PanelBackgroundLight;
        AddChild(_backgroundPanel);

        // Tiles container
        _tilesContainer = new Control();
        _tilesContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_tilesContainer);

        // Wall overlay (for Walltastrophy mode)
        _wallOverlay = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _wallOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_wallOverlay);

        RebuildBoard();
        Resized += OnResized;
    }

    private void OnResized()
    {
        UpdateTilePositions();
    }

    private void RebuildBoard()
    {
        if (_tilesContainer == null)
            return;

        // Clear existing tiles
        foreach (var tile in _tiles)
        {
            tile.QueueFree();
        }
        _tiles.Clear();

        // Create new tiles
        int totalTiles = _boardSize * _boardSize;
        for (int i = 0; i < totalTiles; i++)
        {
            var tile = new TileVisual { Value = 0, IsDarkTheme = _isDarkTheme };

            // Handle tap for adversarial mode
            int tileIndex = i;
            tile.GuiInput += (inputEvent) => OnTileGuiInput(inputEvent, tileIndex);

            _tilesContainer.AddChild(tile);
            _tiles.Add(tile);
        }

        UpdateTilePositions();
        UpdateTheme();
    }

    private void OnTileGuiInput(InputEvent inputEvent, int tileIndex)
    {
        if (
            inputEvent is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
        {
            TileTapped?.Invoke(tileIndex);
        }
    }

    private void UpdateTilePositions()
    {
        if (_tiles.Count == 0)
            return;

        float boardWidth = Size.X;
        float boardHeight = Size.Y;
        float availableSize = Mathf.Min(boardWidth, boardHeight) - _tileSpacing * 2;
        float tileSize = (availableSize - _tileSpacing * (_boardSize - 1)) / _boardSize;

        float startX = (boardWidth - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;
        float startY =
            (boardHeight - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;

        for (int i = 0; i < _tiles.Count; i++)
        {
            int row = i / _boardSize;
            int col = i % _boardSize;

            float x = startX + col * (tileSize + _tileSpacing);
            float y = startY + row * (tileSize + _tileSpacing);

            _tiles[i].Position = new Vector2(x, y);
            _tiles[i].Size = new Vector2(tileSize, tileSize);
        }
    }

    private void UpdateTheme()
    {
        if (_backgroundPanel != null)
        {
            _backgroundPanel.Color = _isDarkTheme
                ? TileColors.PanelBackgroundDark
                : TileColors.PanelBackgroundLight;
        }

        foreach (var tile in _tiles)
        {
            tile.IsDarkTheme = _isDarkTheme;
        }
    }

    /// <summary>
    /// Updates the board with the current game state.
    /// </summary>
    public void UpdateFromBoard(
        Board board,
        IReadOnlySet<int>? newTileIndices = null,
        IReadOnlySet<int>? mergedIndices = null
    )
    {
        for (int i = 0; i < _tiles.Count && i < board.Length; i++)
        {
            int newValue = board[i];
            var tile = _tiles[i];

            bool isNew = newTileIndices?.Contains(i) == true;
            bool isMerged = mergedIndices?.Contains(i) == true;

            tile.Value = newValue;

            if (isNew && newValue > 0)
            {
                tile.PlaySpawnAnimation();
            }
            else if (isMerged)
            {
                tile.PlayMergeAnimation();
            }
        }
    }

    /// <summary>
    /// Animates tiles moving in a direction using temporary visual nodes.
    /// </summary>
    public async Task AnimateMoveAsync(IReadOnlyList<TileMovement> movements, float duration = 0.1f)
    {
        if (movements.Count == 0)
            return;

        // Filter to only movements where the tile actually changes position
        var actualMovements = movements
            .Where(m => m.From.Row != m.To.Row || m.From.Column != m.To.Column)
            .ToList();

        if (actualMovements.Count == 0)
            return;

        // Calculate tile positions for animation
        float boardWidth = Size.X;
        float boardHeight = Size.Y;
        float availableSize = Mathf.Min(boardWidth, boardHeight) - _tileSpacing * 2;
        float tileSize = (availableSize - _tileSpacing * (_boardSize - 1)) / _boardSize;
        float startX = (boardWidth - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;
        float startY =
            (boardHeight - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;

        Vector2 GetTilePosition(int row, int col)
        {
            float x = startX + col * (tileSize + _tileSpacing);
            float y = startY + row * (tileSize + _tileSpacing);
            return new Vector2(x, y);
        }

        // Hide source tiles and create temporary moving tiles
        var tempTiles = new List<TileVisual>();
        var hiddenTiles = new List<(TileVisual tile, int originalValue)>();
        var tweens = new List<Tween>();

        foreach (var movement in actualMovements)
        {
            int fromIndex = movement.From.Row * _boardSize + movement.From.Column;

            if (fromIndex < 0 || fromIndex >= _tiles.Count)
                continue;

            var sourceTile = _tiles[fromIndex];
            int movingValue = sourceTile.Value;

            if (movingValue == 0)
                continue;

            // Hide the source tile during animation
            hiddenTiles.Add((sourceTile, movingValue));
            sourceTile.Value = 0;

            // Create temporary tile for animation
            var tempTile = new TileVisual
            {
                Value = movingValue,
                IsDarkTheme = _isDarkTheme,
                Position = GetTilePosition(movement.From.Row, movement.From.Column),
                Size = new Vector2(tileSize, tileSize),
                ZIndex = 100, // Render on top
            };
            _tilesContainer?.AddChild(tempTile);
            tempTiles.Add(tempTile);

            // Animate to destination
            var destPos = GetTilePosition(movement.To.Row, movement.To.Column);
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Quad);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(tempTile, "position", destPos, duration);
            tweens.Add(tween);
        }

        // Wait for animations to complete
        if (tweens.Count > 0)
        {
            await ToSignal(tweens[0], Tween.SignalName.Finished);
        }

        // Clean up temporary tiles
        foreach (var tempTile in tempTiles)
        {
            tempTile.QueueFree();
        }
    }

    /// <summary>
    /// Updates the wall overlay for Walltastrophy mode.
    /// </summary>
    public void UpdateWall(WallSegment? wall)
    {
        _currentWall = wall;
        UpdateWallOverlay();
    }

    private void UpdateWallOverlay()
    {
        if (_wallOverlay == null)
            return;

        // Clear existing walls
        foreach (var child in _wallOverlay.GetChildren())
        {
            child.QueueFree();
        }

        if (_currentWall == null)
            return;

        // Calculate wall position
        float boardWidth = Size.X;
        float boardHeight = Size.Y;
        float availableSize = Mathf.Min(boardWidth, boardHeight) - _tileSpacing * 2;
        float tileSize = (availableSize - _tileSpacing * (_boardSize - 1)) / _boardSize;

        float startX = (boardWidth - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;
        float startY =
            (boardHeight - (tileSize * _boardSize + _tileSpacing * (_boardSize - 1))) / 2;

        var wallColor = _isDarkTheme ? TileColors.WallColorDark : TileColors.WallColorLight;
        float wallThickness = 4;

        if (_currentWall.Orientation == WallOrientation.Horizontal)
        {
            // Horizontal wall between rows
            float y =
                startY
                + (_currentWall.Divider + 1) * tileSize
                + _currentWall.Divider * _tileSpacing
                + _tileSpacing / 2;
            float x = startX + _currentWall.Start * (tileSize + _tileSpacing);
            float width = _currentWall.Length * tileSize + (_currentWall.Length - 1) * _tileSpacing;

            var wallRect = new ColorRect
            {
                Color = wallColor,
                Position = new Vector2(x, y - wallThickness / 2),
                Size = new Vector2(width, wallThickness),
            };
            _wallOverlay.AddChild(wallRect);
        }
        else
        {
            // Vertical wall between columns
            float x =
                startX
                + (_currentWall.Divider + 1) * tileSize
                + _currentWall.Divider * _tileSpacing
                + _tileSpacing / 2;
            float y = startY + _currentWall.Start * (tileSize + _tileSpacing);
            float height =
                _currentWall.Length * tileSize + (_currentWall.Length - 1) * _tileSpacing;

            var wallRect = new ColorRect
            {
                Color = wallColor,
                Position = new Vector2(x - wallThickness / 2, y),
                Size = new Vector2(wallThickness, height),
            };
            _wallOverlay.AddChild(wallRect);
        }
    }
}
