namespace TwentyFortyEight.ViewModels.Messages;

/// <summary>
/// Sent after the active ruleset has been applied.
/// Consumers (e.g., UI and persistence services) can switch scope and rebuild layouts.
/// </summary>
public sealed record RulesetChangedMessage(
    string OldRulesetId,
    string NewRulesetId,
    int OldBoardSize,
    int NewBoardSize
);
