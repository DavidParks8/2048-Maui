using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Default implementation of coach suggestion service.
/// </summary>
public sealed class CoachSuggestionService(IMoveAdvisor moveAdvisor) : ICoachSuggestionService
{
    /// <inheritdoc />
    public MoveRecommendation? GetSuggestion(CoachSuggestionRequest request)
    {
        if (!request.IsCoachEnabled)
        {
            return null;
        }

        if (request.IsGameOver)
        {
            return null;
        }

        return moveAdvisor.Recommend(
            new MoveAdvisorRequest(
                new PlayfieldSnapshot(request.Board, request.Wall),
                request.Config
            )
        );
    }
}
