using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Default implementation of coach suggestion service.
/// </summary>
public sealed class CoachSuggestionService(IMoveAdvisor moveAdvisor) : ICoachSuggestionService
{
    /// <inheritdoc />
    public MoveRecommendation? GetSuggestion(
        Board board,
        GameConfig config,
        bool isCoachEnabled,
        bool isGameOver
    )
    {
        if (!isCoachEnabled)
        {
            return null;
        }

        if (isGameOver)
        {
            return null;
        }

        return moveAdvisor.Recommend(board, config);
    }
}
