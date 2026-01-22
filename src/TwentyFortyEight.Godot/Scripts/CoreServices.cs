using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Random source implementation for Godot.
/// </summary>
public class GodotRandomSource : IRandomSource
{
    private readonly Random _random = new();

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public double NextDouble() => _random.NextDouble();
}

/// <summary>
/// Board move simulator for Godot.
/// Simplified implementation that handles basic 2048 moves.
/// </summary>
public class GodotBoardSimulator : IBoardSimulator
{
    public (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) SimulateMove(
        BoardMoveRequest request
    )
    {
        var board = request.Playfield.Board;
        int size = board.Size;
        var newData = new int[size, size];

        // Copy current board
        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            newData[r, c] = board[r, c];

        bool moved = false;
        int scoreIncrease = 0;
        int maxMergedValue = 0;

        // Process based on direction
        switch (request.Direction)
        {
            case Direction.Left:
                (moved, scoreIncrease, maxMergedValue) = ProcessHorizontal(
                    newData,
                    size,
                    request.Playfield.Wall,
                    goLeft: true
                );
                break;
            case Direction.Right:
                (moved, scoreIncrease, maxMergedValue) = ProcessHorizontal(
                    newData,
                    size,
                    request.Playfield.Wall,
                    goLeft: false
                );
                break;
            case Direction.Up:
                (moved, scoreIncrease, maxMergedValue) = ProcessVertical(
                    newData,
                    size,
                    request.Playfield.Wall,
                    goUp: true
                );
                break;
            case Direction.Down:
                (moved, scoreIncrease, maxMergedValue) = ProcessVertical(
                    newData,
                    size,
                    request.Playfield.Wall,
                    goUp: false
                );
                break;
        }

        return (new Board(newData), scoreIncrease, moved, maxMergedValue);
    }

    public bool WouldMove(BoardMoveRequest request)
    {
        var (_, _, moved, _) = SimulateMove(request);
        return moved;
    }

    private static (bool moved, int score, int maxMerged) ProcessHorizontal(
        int[,] data,
        int size,
        WallSegment? wall,
        bool goLeft
    )
    {
        bool anyMoved = false;
        int totalScore = 0;
        int maxMerged = 0;

        for (int row = 0; row < size; row++)
        {
            // Extract row
            int[] line = new int[size];
            for (int c = 0; c < size; c++)
                line[c] = data[row, goLeft ? c : size - 1 - c];

            // Process line
            var (newLine, score, merged, lineMoved) = ProcessLine(line, size);

            if (lineMoved)
                anyMoved = true;
            totalScore += score;
            maxMerged = Math.Max(maxMerged, merged);

            // Write back
            for (int c = 0; c < size; c++)
                data[row, goLeft ? c : size - 1 - c] = newLine[c];
        }

        return (anyMoved, totalScore, maxMerged);
    }

    private static (bool moved, int score, int maxMerged) ProcessVertical(
        int[,] data,
        int size,
        WallSegment? wall,
        bool goUp
    )
    {
        bool anyMoved = false;
        int totalScore = 0;
        int maxMerged = 0;

        for (int col = 0; col < size; col++)
        {
            // Extract column
            int[] line = new int[size];
            for (int r = 0; r < size; r++)
                line[r] = data[goUp ? r : size - 1 - r, col];

            // Process line
            var (newLine, score, merged, lineMoved) = ProcessLine(line, size);

            if (lineMoved)
                anyMoved = true;
            totalScore += score;
            maxMerged = Math.Max(maxMerged, merged);

            // Write back
            for (int r = 0; r < size; r++)
                data[goUp ? r : size - 1 - r, col] = newLine[r];
        }

        return (anyMoved, totalScore, maxMerged);
    }

    private static (int[] result, int score, int maxMerged, bool moved) ProcessLine(
        int[] line,
        int size
    )
    {
        int[] result = new int[size];
        int score = 0;
        int maxMerged = 0;
        bool moved = false;

        int writePos = 0;
        bool lastWasMerge = false;

        for (int i = 0; i < size; i++)
        {
            if (line[i] == 0)
                continue;

            int val = line[i];

            // Try to merge with previous
            if (writePos > 0 && result[writePos - 1] == val && !lastWasMerge)
            {
                result[writePos - 1] = val * 2;
                score += val * 2;
                maxMerged = Math.Max(maxMerged, val * 2);
                lastWasMerge = true;
                moved = true;
            }
            else
            {
                if (writePos != i)
                    moved = true;
                result[writePos] = val;
                writePos++;
                lastWasMerge = false;
            }
        }

        return (result, score, maxMerged, moved);
    }
}

/// <summary>
/// Move analyzer for Godot - provides animation data.
/// Does not implement IMoveAnalyzer since we use our own result type.
/// </summary>
public class GodotMoveAnalyzer
{
    private readonly GodotMoveAnalysis _result = new();

    public GodotMoveAnalysis Analyze(Board previousBoard, Board newBoard, Direction direction)
    {
        _result.Clear();

        int size = previousBoard.Size;

        // Find what moved where
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                int oldVal = previousBoard[r, c];
                int newVal = newBoard[r, c];

                if (oldVal != 0)
                {
                    // Track tile movements based on direction
                    var movement = TrackMovement(previousBoard, newBoard, r, c, direction, size);
                    if (movement.HasValue)
                    {
                        _result.AddMovement(movement.Value);
                        _result.AddMovedToIndex(
                            movement.Value.To.Row * size + movement.Value.To.Column
                        );
                    }
                }

                if (newVal != 0 && oldVal == 0)
                {
                    // New tile spawned here
                    _result.AddSpawnedIndex(r * size + c);
                }

                if (newVal > oldVal && oldVal != 0)
                {
                    // This tile was merged
                    _result.AddMergedIndex(r * size + c);
                }
            }
        }

        return _result;
    }

    private static TileMovement? TrackMovement(
        Board prev,
        Board next,
        int fromRow,
        int fromCol,
        Direction dir,
        int size
    )
    {
        int value = prev[fromRow, fromCol];
        if (value == 0)
            return null;

        // Find where this tile ended up
        int toRow = fromRow;
        int toCol = fromCol;

        switch (dir)
        {
            case Direction.Left:
                for (int c = 0; c <= fromCol; c++)
                {
                    if (next[fromRow, c] == value || next[fromRow, c] == value * 2)
                    {
                        toCol = c;
                        break;
                    }
                }
                break;
            case Direction.Right:
                for (int c = size - 1; c >= fromCol; c--)
                {
                    if (next[fromRow, c] == value || next[fromRow, c] == value * 2)
                    {
                        toCol = c;
                        break;
                    }
                }
                break;
            case Direction.Up:
                for (int r = 0; r <= fromRow; r++)
                {
                    if (next[r, fromCol] == value || next[r, fromCol] == value * 2)
                    {
                        toRow = r;
                        break;
                    }
                }
                break;
            case Direction.Down:
                for (int r = size - 1; r >= fromRow; r--)
                {
                    if (next[r, fromCol] == value || next[r, fromCol] == value * 2)
                    {
                        toRow = r;
                        break;
                    }
                }
                break;
        }

        if (toRow != fromRow || toCol != fromCol)
        {
            return new TileMovement(
                new Position(fromRow, fromCol),
                new Position(toRow, toCol),
                value,
                next[toRow, toCol] > value
            );
        }

        return null;
    }
}

/// <summary>
/// Standalone move analysis result for Godot.
/// </summary>
public sealed class GodotMoveAnalysis
{
    private readonly List<TileMovement> _movements = [];
    private readonly HashSet<int> _spawnedIndices = [];
    private readonly HashSet<int> _mergedIndices = [];
    private readonly HashSet<int> _movedToIndices = [];

    public IReadOnlyList<TileMovement> Movements => _movements;
    public IReadOnlySet<int> SpawnedIndices => _spawnedIndices;
    public IReadOnlySet<int> MergedIndices => _mergedIndices;
    public IReadOnlySet<int> MovedToIndices => _movedToIndices;

    public void Clear()
    {
        _movements.Clear();
        _spawnedIndices.Clear();
        _mergedIndices.Clear();
        _movedToIndices.Clear();
    }

    public void AddMovement(TileMovement m) => _movements.Add(m);

    public void AddSpawnedIndex(int i) => _spawnedIndices.Add(i);

    public void AddMergedIndex(int i) => _mergedIndices.Add(i);

    public void AddMovedToIndex(int i) => _movedToIndices.Add(i);
}

/// <summary>
/// Simple heuristic move advisor for Godot.
/// </summary>
public class GodotMoveAdvisor : IMoveAdvisor
{
    private readonly IBoardSimulator _simulator;

    public GodotMoveAdvisor(IBoardSimulator simulator)
    {
        _simulator = simulator;
    }

    public MoveRecommendation? Recommend(MoveAdvisorRequest request)
    {
        var directions = new[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };
        MoveRecommendation? best = null;
        double bestScore = -1;

        foreach (var dir in directions)
        {
            var (_, scoreIncrease, moved, _) = _simulator.SimulateMove(
                new BoardMoveRequest(request.Playfield, dir)
            );

            if (moved)
            {
                double heuristicScore = scoreIncrease * 10;

                if (dir == Direction.Down)
                    heuristicScore += 50;
                if (dir == Direction.Left)
                    heuristicScore += 30;

                if (heuristicScore > bestScore)
                {
                    bestScore = heuristicScore;
                    best = new MoveRecommendation(dir, heuristicScore, MoveCoachReason.MergeTiles);
                }
            }
        }

        return best;
    }
}

/// <summary>
/// Spawn strategy factory for Godot.
/// </summary>
public class GodotSpawnStrategyFactory : ISpawnStrategyFactory
{
    public ISpawnStrategy Create(GameConfig config)
    {
        return config.Mode switch
        {
            GameMode.Classic => new GodotClassicSpawnStrategy(),
            _ => new GodotModernSpawnStrategy(),
        };
    }
}

/// <summary>
/// Classic spawn strategy (90% spawn 2, 10% spawn 4).
/// </summary>
public class GodotClassicSpawnStrategy : ISpawnStrategy
{
    public int GetSpawnValue(GameState state, GameConfig config)
    {
        return 2;
    }
}

/// <summary>
/// Modern spawn strategy (adaptive based on max tile).
/// </summary>
public class GodotModernSpawnStrategy : ISpawnStrategy
{
    public int GetSpawnValue(GameState state, GameConfig config)
    {
        int maxTile = state.MaxTileValue;

        if (maxTile >= 1024)
            return 4;
        return 2;
    }
}

/// <summary>
/// Null implementation of statistics tracker for fallback.
/// </summary>
public class NullStatisticsTracker : IStatisticsTracker
{
    private readonly Core.GameStatistics _stats = new();

    public void OnGameStarted() { }

    public void OnGameEnded(int score, bool isWon) { }

    public void OnGameWon() { }

    public void OnMoveMade() { }

    public void UpdateHighestTile(int value) { }

    public void UpdateBestScore(GameMode mode, int score) { }

    public Core.GameStatistics GetStatistics() => _stats;

    public void Reset() { }
}
