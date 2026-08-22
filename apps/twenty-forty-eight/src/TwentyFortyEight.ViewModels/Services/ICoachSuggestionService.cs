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
    /// <param name="request">Coach suggestion request.</param>
    /// <returns>A move recommendation, or null if no suggestion is available.</returns>
    MoveRecommendation? GetSuggestion(CoachSuggestionRequest request);
}
