namespace TwentyFortyEight.Godot;

/// <summary>
/// Localization strings for the game.
/// These can be expanded with a proper localization system later.
/// </summary>
public static class Strings
{
    // Game
    public const string NewGame = "New Game";
    public const string Size = "Size";
    public const string Score = "Score";
    public const string Best = "Best";
    public const string Mode = "Mode";
    public const string Undo = "Undo";
    public const string Settings = "Settings";
    public const string Statistics = "Statistics";
    public const string About = "About";
    public const string HowToPlay = "How to Play";

    // Game Modes
    public const string ClassicMode = "Classic";
    public const string ModernMode = "Modern";
    public const string WalltastrophyMode = "Walltastrophy";
    public const string AdversarialMode = "Adversarial";

    // Game Over / Victory
    public const string GameOver = "Game Over!";
    public const string Victory = "Victory!";
    public const string VictorySubtitle = "You reached 2048!";
    public const string VictorySubtitleAdversarial = "You blocked 2048!";
    public const string TryAgain = "Try Again";
    public const string KeepPlaying = "Keep Playing";
    public const string FinalScore = "Final Score: {0}";

    // Settings
    public const string GameplaySection = "Gameplay";
    public const string EnableCoach = "Enable Coach";
    public const string EnableCoachDescription = "Shows suggested moves";
    public const string EnableCoachNudges = "Enable Coach Nudges";
    public const string CoachNudgesDescription = "Prompts to enable coach when struggling";
    public const string ShowUndoButton = "Show Undo Button";
    public const string HapticsSection = "Haptics";
    public const string EnableHaptics = "Enable Haptics";

    // Statistics
    public const string GamesSection = "Games";
    public const string GamesPlayed = "Games Played";
    public const string GamesWon = "Games Won";
    public const string WinRate = "Win Rate";
    public const string ScoresSection = "Scores";
    public const string BestScore = "Best Score";
    public const string AverageScore = "Average Score";
    public const string HighestTile = "Highest Tile";
    public const string MovesSection = "Moves";
    public const string TotalMoves = "Total Moves";
    public const string StreaksSection = "Streaks";
    public const string CurrentStreak = "Current";
    public const string BestStreak = "Best";
    public const string ResetStatistics = "Reset Statistics";
    public const string ResetStatisticsConfirmTitle = "Reset Statistics?";
    public const string ResetStatisticsConfirmMessage =
        "This will permanently delete all your game statistics.";
    public const string Reset = "Reset";
    public const string Cancel = "Cancel";

    // About
    public const string AboutTitle = "About";
    public const string ForTalia = "For Talia";
    public const string AboutMessage = "This game was made with love as a gift. Enjoy!";
    public const string MadeWithLove = "Made with ❤️";

    // How to Play
    public const string HowToPlayTitle = "How to Play";
    public const string HowToPlayInstructions =
        @"Use arrow keys or swipe to move tiles.

When two tiles with the same number touch, they merge into one!

Try to reach 2048!

Game Modes:
• Classic - Original 2048 rules
• Modern - Adaptive tile spawning
• Walltastrophy - Random walls appear
• Adversarial - You place tiles, AI moves";

    // Dialogs
    public const string ConfirmNewGameTitle = "Start New Game?";
    public const string ConfirmNewGameMessage = "Your current progress will be lost.";
    public const string Yes = "Yes";
    public const string No = "No";

    // Coach
    public const string CoachNudgeTitle = "Need Help?";
    public const string CoachNudgeMessage = "Enable Coach for move suggestions";
    public const string EnableCoachButton = "Enable Coach";
    public const string DismissButton = "Dismiss";
    public const string CoachSuggestionFormat = "Coach suggests: {0}";
    public const string CoachHintPlaceholder = "Enable Coach to see suggestions.";
}
