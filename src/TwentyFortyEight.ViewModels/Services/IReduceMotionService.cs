namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service to check if the user has enabled reduce motion accessibility settings.
/// </summary>
public interface IReduceMotionService
{
    /// <summary>
    /// Returns true if the user prefers reduced motion animations.
    /// </summary>
    bool ShouldReduceMotion();
}
