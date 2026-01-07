using System.Numerics;
using System.Runtime.CompilerServices;

namespace TwentyFortyEight.Core;

/// <summary>
/// A lightweight, deterministic move advisor based on common 2048 heuristics.
/// </summary>
public sealed class HeuristicMoveAdvisor(IBoardSimulator simulator) : IMoveAdvisor
{
    public MoveRecommendation? Recommend(Board board, GameConfig config)
    {
        if (board.Size <= 0 || board.Length == 0)
        {
            return null;
        }

        // If there are no possible merges and no empty cells, there is no valid move.
        if (board.CountEmptyCells() == 0 && !board.HasPossibleMerges())
        {
            return null;
        }

        var baseline = ComputeFeatures(board);

        MoveRecommendation? best = null;

        foreach (var direction in s_directions)
        {
            var (previewBoard, scoreIncrease, moved, _) = simulator.SimulateMove(board, direction);
            if (!moved)
            {
                continue;
            }

            var features = ComputeFeatures(previewBoard);
            var score = ScoreMove(scoreIncrease, baseline, features);
            var reason = PickPrimaryReason(scoreIncrease, baseline, features);

            MoveRecommendation recommendation = new(direction, score, reason);

            if (best is null || recommendation.Score > best.Value.Score)
            {
                best = recommendation;
            }
        }

        // Avoid recommending a move if we somehow found none (e.g., invalid state).
        return best;
    }

    private static readonly Direction[] s_directions =
    [
        Direction.Up,
        Direction.Left,
        Direction.Right,
        Direction.Down,
    ];

    private static double ScoreMove(int scoreIncrease, Features baseline, Features after)
    {
        // Heuristic weights tuned for optimal 2048 gameplay.
        // Priority: Empty cells > Corner position > Merges > Monotonicity > Smoothness
        const double emptyWeight = 1000.0; // Heavily favor creating space
        const double scoreWeight = 2.0; // Value immediate points
        const double mergeWeight = 80.0; // Reward merge potential
        const double cornerWeight = 600.0; // Keep max tile in corner
        const double monotonicityWeight = 10.0; // Maintain ordered tiles
        const double smoothnessWeight = 3.0; // Penalize value gaps

        var emptyDelta = after.EmptyCells - baseline.EmptyCells;
        var mergesDelta = after.PotentialMerges - baseline.PotentialMerges;
        var cornerBonus = after.MaxInCorner ? 1 : 0;

        // Smoothness is a penalty (lower is better).
        var smoothnessDelta = baseline.Smoothness - after.Smoothness;

        // Monotonicity is a bonus (higher is better).
        var monotonicityDelta = after.Monotonicity - baseline.Monotonicity;

        return emptyDelta * emptyWeight
            + scoreIncrease * scoreWeight
            + mergesDelta * mergeWeight
            + cornerBonus * cornerWeight
            + monotonicityDelta * monotonicityWeight
            + smoothnessDelta * smoothnessWeight;
    }

    private static MoveCoachReason PickPrimaryReason(
        int scoreIncrease,
        Features baseline,
        Features after
    )
    {
        if (after.EmptyCells > baseline.EmptyCells)
        {
            return MoveCoachReason.CreateSpace;
        }

        if (scoreIncrease > 0)
        {
            return MoveCoachReason.MergeTiles;
        }

        if (after.MaxInCorner && !baseline.MaxInCorner)
        {
            return MoveCoachReason.KeepLargestInCorner;
        }

        if (after.Monotonicity > baseline.Monotonicity)
        {
            return MoveCoachReason.ImproveOrder;
        }

        return MoveCoachReason.AvoidDeadEnd;
    }

    private static Features ComputeFeatures(Board board)
    {
        var empty = 0;
        var max = 0;

        for (int i = 0; i < board.Length; i++)
        {
            var v = board[i];
            if (v == 0)
            {
                empty++;
            }
            else if (v > max)
            {
                max = v;
            }
        }

        var maxInCorner = max != 0 && IsMaxInCorner(board, max);
        var potentialMerges = CountPotentialMerges(board);
        var smoothness = ComputeSmoothness(board);
        var monotonicity = ComputeMonotonicity(board);

        return new Features(empty, potentialMerges, smoothness, monotonicity, maxInCorner);
    }

    private static bool IsMaxInCorner(Board board, int max)
    {
        var size = board.Size;
        return board[0, 0] == max
            || board[0, size - 1] == max
            || board[size - 1, 0] == max
            || board[size - 1, size - 1] == max;
    }

    private static int CountPotentialMerges(Board board)
    {
        var size = board.Size;
        int merges = 0;

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                var v = board[r, c];
                if (v == 0)
                {
                    continue;
                }

                if (c + 1 < size && board[r, c + 1] == v)
                {
                    merges++;
                }

                if (r + 1 < size && board[r + 1, c] == v)
                {
                    merges++;
                }
            }
        }

        return merges;
    }

    private static int ComputeSmoothness(Board board)
    {
        var size = board.Size;
        int smoothness = 0;

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                var v = board[r, c];
                if (v == 0)
                {
                    continue;
                }

                var logV = FastLog2(v);

                if (c + 1 < size)
                {
                    var right = board[r, c + 1];
                    if (right != 0)
                    {
                        smoothness += Math.Abs(logV - FastLog2(right));
                    }
                }

                if (r + 1 < size)
                {
                    var down = board[r + 1, c];
                    if (down != 0)
                    {
                        smoothness += Math.Abs(logV - FastLog2(down));
                    }
                }
            }
        }

        return smoothness;
    }

    private static int ComputeMonotonicity(Board board)
    {
        // Higher means more consistently increasing/decreasing along rows/cols.
        // This is a simplified monotonicity heuristic that rewards ordered lines.
        var size = board.Size;
        int score = 0;

        for (int r = 0; r < size; r++)
        {
            score += LineMonotonicity(board, size, r, isRow: true);
        }

        for (int c = 0; c < size; c++)
        {
            score += LineMonotonicity(board, size, c, isRow: false);
        }

        return score;
    }

    private static int LineMonotonicity(Board board, int size, int index, bool isRow)
    {
        int inc = 0;
        int dec = 0;

        int prev = 0;
        bool hasPrev = false;

        for (int i = 0; i < size; i++)
        {
            var v = isRow ? board[index, i] : board[i, index];
            if (v == 0)
            {
                continue;
            }

            if (!hasPrev)
            {
                prev = v;
                hasPrev = true;
                continue;
            }

            if (v > prev)
            {
                inc += FastLog2(v) - FastLog2(prev);
            }
            else if (v < prev)
            {
                dec += FastLog2(prev) - FastLog2(v);
            }

            prev = v;
        }

        // Reward whichever direction is more consistent.
        return -Math.Min(inc, dec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastLog2(int value)
    {
        // value is always a power of two in normal 2048 play.
        return BitOperations.Log2((uint)value);
    }

    private readonly record struct Features(
        int EmptyCells,
        int PotentialMerges,
        int Smoothness,
        int Monotonicity,
        bool MaxInCorner
    );
}
