namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Default implementation of coach nudge tracking logic.
/// </summary>
public sealed class CoachNudgeService(ISettingsService settingsService) : ICoachNudgeService
{
    private const int InvalidMoveThreshold = 3;

    private int _consecutiveInvalidMoves;
    private bool _hasShownThisSession;

    /// <inheritdoc />
    public bool IsNudgeVisible { get; private set; }

    /// <inheritdoc />
    public void TrackInvalidMove()
    {
        _consecutiveInvalidMoves++;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _consecutiveInvalidMoves = 0;
        IsNudgeVisible = false;
    }

    /// <inheritdoc />
    public bool ShouldShowNudge()
    {
        var isCoachEnabled = settingsService.CoachEnabled;
        var coachNudgesEnabled = settingsService.CoachNudgesEnabled;

        // Don't show if already shown this session
        if (_hasShownThisSession)
        {
            return false;
        }

        // Don't show if coach is already on
        if (isCoachEnabled)
        {
            return false;
        }

        // Don't show if nudges are disabled
        if (!coachNudgesEnabled)
        {
            return false;
        }

        // Show if threshold reached
        if (_consecutiveInvalidMoves >= InvalidMoveThreshold)
        {
            IsNudgeVisible = true;
            _hasShownThisSession = true;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Dismiss()
    {
        IsNudgeVisible = false;
        _consecutiveInvalidMoves = 0;
        _hasShownThisSession = true;
    }
}
