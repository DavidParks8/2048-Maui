using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]

namespace TwentyFortyEight.Core.Tests;

/// <summary>
/// Helper methods for creating test fixtures.
/// </summary>
internal static class TestHelpers
{
    private sealed class TestSpawnStrategyFactory(IRandomSource random) : ISpawnStrategyFactory
    {
        private readonly ClassicSpawnStrategy _classic = new(random);
        private readonly ModernSpawnStrategy _modern = new(random);

        public ISpawnStrategy Create(GameConfig config)
        {
            return config.Mode switch
            {
                GameMode.Classic => _classic,
                _ => _modern,
            };
        }
    }

    public static ISpawnStrategyFactory CreateSpawnStrategyFactory(IRandomSource random) =>
        new TestSpawnStrategyFactory(random);

    /// <summary>
    /// Creates a GameState from a flat board array for testing.
    /// </summary>
    public static GameState CreateGameState(
        int[] boardData,
        int size = 4,
        int score = 0,
        int moveCount = 0,
        bool isWon = false,
        bool isGameOver = false
    )
    {
        Board board = new(boardData, size);
        var maxTileValue = boardData.Length > 0 ? boardData.Max() : 0;
        return new GameState(board, score, moveCount, isWon, isGameOver, maxTileValue);
    }

    public static Game2048Engine CreateEngine(
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker? statisticsTracker = null,
        IBoardSimulator? boardSimulator = null
    )
    {
        statisticsTracker ??= NullStatisticsTracker.Instance;
        boardSimulator ??= new BoardMoveSimulator();

        return new Game2048Engine(
            config,
            random,
            statisticsTracker,
            boardSimulator,
            CreateSpawnStrategyFactory(random)
        );
    }

    public static Game2048Engine CreateEngine(
        GameState state,
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker? statisticsTracker = null,
        IBoardSimulator? boardSimulator = null
    )
    {
        statisticsTracker ??= NullStatisticsTracker.Instance;
        boardSimulator ??= new BoardMoveSimulator();

        return new Game2048Engine(
            state,
            config,
            random,
            statisticsTracker,
            boardSimulator,
            CreateSpawnStrategyFactory(random)
        );
    }

    public static Game2048Engine CreateEngine(
        GameSave save,
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker? statisticsTracker = null,
        IBoardSimulator? boardSimulator = null
    )
    {
        statisticsTracker ??= NullStatisticsTracker.Instance;
        boardSimulator ??= new BoardMoveSimulator();

        return new Game2048Engine(
            save,
            config,
            random,
            statisticsTracker,
            boardSimulator,
            CreateSpawnStrategyFactory(random)
        );
    }
}
