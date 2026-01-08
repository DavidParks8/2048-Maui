using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Request data for computing a coach suggestion.
/// </summary>
public readonly record struct CoachSuggestionRequest(
    Board Board,
    GameConfig Config,
    bool IsCoachEnabled,
    bool IsGameOver,
    WallSegment? Wall = null
);
