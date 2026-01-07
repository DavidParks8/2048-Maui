using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service for computing move coach suggestions.
/// </summary>
public interface ICoachSuggestionService
{
    /// <summary>
    /// Gets a move recommendation if coaching is enabled and available.
    /// </summary>
    /// <param name="board">The current game board.</param>
    /// <param name="config">The game configuration.</param>
    /// <param name="isCoachEnabled">Whether coach is enabled.</param>
    /// <param name="isGameOver">Whether the game is over.</param>
    /// <returns>A move recommendation, or null if no suggestion is available.</returns>
    MoveRecommendation? GetSuggestion(
        Board board,
        GameConfig config,
        bool isCoachEnabled,
        bool isGameOver
    );
}
