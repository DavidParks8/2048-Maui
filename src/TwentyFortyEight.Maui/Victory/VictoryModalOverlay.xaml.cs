using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Victory modal overlay that displays after the cinematic animation.
/// Uses data binding to VictoryViewModel for MVVM compliance.
/// </summary>
public partial class VictoryModalOverlay : ContentView
{
    private const uint ShowFadeDurationMs = 300;
    private const uint HideFadeDurationMs = 200;

    public VictoryModalOverlay(VictoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Subscribe to state changes for animations
        viewModel.State.PropertyChanged += OnStatePropertyChanged;
    }

    private void OnStatePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(ViewModels.Models.VictoryState.IsModalVisible))
        {
            var state = (ViewModels.Models.VictoryState)sender!;
            if (state.IsModalVisible)
            {
                _ = AnimateShowAsync();
            }
            else
            {
                _ = AnimateHideAsync();
            }
        }
    }

    private async Task AnimateShowAsync()
    {
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

    private async Task AnimateHideAsync()
    {
        await Task.WhenAll(
            ModalCard.FadeToAsync(0, HideFadeDurationMs, Easing.CubicIn),
            ModalCard.ScaleToAsync(0.96, HideFadeDurationMs, Easing.CubicIn)
        );
    }
}
