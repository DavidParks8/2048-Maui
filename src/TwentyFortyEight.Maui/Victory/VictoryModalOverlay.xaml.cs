using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels;

namespace TwentyFortyEight.Maui.Victory;

public partial class VictoryModalOverlay : ContentView
{
    private const uint ShowFadeDurationMs = 300;
    private const uint HideFadeDurationMs = 200;

    private readonly GameViewModel _viewModel;
    private CinematicOverlayView? _cinematicOverlay;

    public VictoryModalOverlay(GameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    public void Initialize(CinematicOverlayView cinematicOverlay)
    {
        _cinematicOverlay = cinematicOverlay;
    }

    /// <summary>
    /// Show the modal with animation.
    /// </summary>
    /// <param name="score">Current score to display.</param>
    public async Task ShowAsync(int score)
    {
        ScoreLabel.Text = string.Format(AppStrings.ScoreFormat, score);
        IsVisible = true;

        // Ensure consistent initial state for repeat shows.
        ModalCard.Opacity = 0;
        ModalCard.Scale = 0.96;

        await Task.WhenAll(
            ModalCard.FadeToAsync(1, ShowFadeDurationMs, Easing.CubicOut),
            ModalCard.ScaleToAsync(1, ShowFadeDurationMs, Easing.CubicOut)
        );

        // Announce for screen readers.
        SemanticScreenReader.Announce(AppStrings.VictoryAnnouncement);
    }

    private async Task HideAsync()
    {
        await Task.WhenAll(
            ModalCard.FadeToAsync(0, HideFadeDurationMs, Easing.CubicIn),
            ModalCard.ScaleToAsync(0.96, HideFadeDurationMs, Easing.CubicIn)
        );

        IsVisible = false;
    }

    private async void OnKeepPlayingClicked(object? sender, EventArgs e)
    {
        await HideAsync();
        _cinematicOverlay?.StopAnimation();
    }

    private async void OnNewGameClicked(object? sender, EventArgs e)
    {
        await HideAsync();
        _cinematicOverlay?.StopAnimation();
        _viewModel.NewGameCommand.Execute(null);
    }
}
