namespace TwentyFortyEight.Godot;

/// <summary>
/// Tracks invalid move streaks to determine when to surface a coach nudge banner.
/// Mirrors the logic from the MAUI implementation so that UX stays consistent.
/// </summary>
public sealed class CoachNudgeService
{
    private const int InvalidMoveThreshold = 3;

    private int _consecutiveInvalidMoves;
    private bool _hasShownThisSession;

    public void TrackInvalidMove()
    {
        _consecutiveInvalidMoves++;
    }

    public void Reset()
    {
        _consecutiveInvalidMoves = 0;
    }

    public void RestartSession()
    {
        _hasShownThisSession = false;
        _consecutiveInvalidMoves = 0;
    }

    public bool ShouldShowNudge(GameSettings? settings)
    {
        if (settings == null)
            return false;

        if (_hasShownThisSession)
            return false;

        if (settings.CoachEnabled)
            return false;

        if (!settings.CoachNudgesEnabled)
            return false;

        if (_consecutiveInvalidMoves >= InvalidMoveThreshold)
        {
            _hasShownThisSession = true;
            _consecutiveInvalidMoves = 0;
            return true;
        }

        return false;
    }

    public void Dismiss()
    {
        _hasShownThisSession = true;
        _consecutiveInvalidMoves = 0;
    }
}
