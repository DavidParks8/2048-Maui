using System.Globalization;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of ILocalizationService using resource strings.
/// </summary>
public class MauiLocalizationService : ILocalizationService
{
    /// <inheritdoc />
    public string RestartConfirmTitle => AppStrings.RestartConfirmTitle;

    /// <inheritdoc />
    public string RestartConfirmMessage => AppStrings.RestartConfirmMessage;

    /// <inheritdoc />
    public string StartNew => AppStrings.StartNew;

    /// <inheritdoc />
    public string Cancel => AppStrings.Cancel;

    /// <inheritdoc />
    public string GameOver => AppStrings.GameOver;

    /// <inheritdoc />
    public string YourScore => AppStrings.YourScore;

    /// <inheritdoc />
    public string BestFormat => AppStrings.BestFormat;

    /// <inheritdoc />
    public string TryAgain => AppStrings.TryAgain;

    /// <inheritdoc />
    public string HowToPlayTitle => AppStrings.HowToPlayTitle;

    /// <inheritdoc />
    public string HowToPlayContent => AppStrings.HowToPlayContent;

    /// <inheritdoc />
    public string GotIt => AppStrings.GotIt;

    /// <inheritdoc />
    public string YouWin => AppStrings.YouWin;

    /// <inheritdoc />
    public string ResetStatisticsTitle => AppStrings.ResetStatisticsTitle;

    /// <inheritdoc />
    public string ResetStatisticsMessage => AppStrings.ResetStatisticsMessage;

    /// <inheritdoc />
    public string Reset => AppStrings.Reset;

    /// <inheritdoc />
    public string ScreenReaderScoreAnnouncement(int score) =>
        string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.ScreenReaderScoreAnnouncementFormat,
            score
        );

    /// <inheritdoc />
    public string ScreenReaderGameOverFinalScore(int finalScore) =>
        string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.ScreenReaderGameOverFinalScoreFormat,
            finalScore
        );

    /// <inheritdoc />
    public string FormatScore(int score) =>
        string.Format(CultureInfo.CurrentCulture, AppStrings.ScoreFormat, score);
}
