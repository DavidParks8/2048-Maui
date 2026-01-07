using Maui.BindableProperty.Generator.Core;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// Victory modal overlay that displays after the victory animation begins.
/// Uses data binding to VictoryViewModel for MVVM compliance.
/// </summary>
public partial class VictoryModalOverlay : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// iOS material style for the modal background blur effect.
    /// </summary>
    [AutoBindable(DefaultValue = "IosMaterialStyle.SystemThickMaterial")]
    private readonly IosMaterialStyle _iosMaterial;

#pragma warning restore CS0169
    private const uint ShowFadeDurationMs = 300;
    private const uint HideFadeDurationMs = 200;

    private readonly IScreenReaderService _screenReaderService;

    public VictoryModalOverlay()
        : this(ResolveViewModel(), ResolveScreenReaderService()) { }

    public VictoryModalOverlay(VictoryViewModel viewModel)
        : this(viewModel, ResolveScreenReaderService()) { }

    public VictoryModalOverlay(VictoryViewModel viewModel, IScreenReaderService screenReaderService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _screenReaderService = screenReaderService;

        // Subscribe to state changes for animations
        viewModel.State.PropertyChanged += OnStatePropertyChanged;
    }

    private static VictoryViewModel ResolveViewModel()
    {
        if (Application.Current is App app)
        {
            return app.Services.GetRequiredService<VictoryViewModel>();
        }

        throw new InvalidOperationException(
            "VictoryModalOverlay requires an App with a configured Services container."
        );
    }

    private static IScreenReaderService ResolveScreenReaderService()
    {
        if (Application.Current is App app)
        {
            return app.Services.GetRequiredService<IScreenReaderService>();
        }

        throw new InvalidOperationException(
            "VictoryModalOverlay requires an App with a configured Services container."
        );
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
        _screenReaderService.Announce(AppStrings.VictoryAnnouncement);
    }

    private async Task AnimateHideAsync()
    {
        await Task.WhenAll(
            ModalCard.FadeToAsync(0, HideFadeDurationMs, Easing.CubicIn),
            ModalCard.ScaleToAsync(0.96, HideFadeDurationMs, Easing.CubicIn)
        );
    }
}
