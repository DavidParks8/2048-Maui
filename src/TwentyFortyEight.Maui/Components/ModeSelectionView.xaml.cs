using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Converters;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Helpers;
#if IOS || MACCATALYST
using UIKit;
#endif

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// Component for mode selection (board size picker and action buttons).
/// Displays board size picker with platform-specific styling and action buttons.
/// </summary>
public partial class ModeSelectionView : ContentView
{
    private readonly GameViewModel _viewModel;
    private readonly int _originalBoardSize;
    private readonly GameMode _originalGameMode;
    private Picker? _sizePicker;
    private Picker? _modePicker;

    public event EventHandler? PlayRequested;

    public ModeSelectionView(GameViewModel viewModel, int originalBoardSize)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _originalBoardSize = originalBoardSize;
        _originalGameMode = viewModel.PendingGameMode;
        BindingContext = _viewModel;

        CreateModePicker();
        CreateSizePicker();
        HelperTextLabel.Text = AppStrings.ModeHelperText;
        UpdatePlayButtonState();

        // Listen for picker changes to update button state
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void CreateModePicker()
    {
        var modes = new List<string> { AppStrings.Classic, AppStrings.Walltastrophy };

        _modePicker = new Picker
        {
            ItemsSource = modes,
            Title = AppStrings.GameMode,
            BindingContext = _viewModel,
        };

        View modePickerView = _modePicker;

        if (
            DeviceInfo.Current.Platform == DevicePlatform.iOS
            || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst
        )
        {
            modePickerView = CreateiOSStyledPicker(_modePicker);
        }

        _modePicker.SetBinding(
            Picker.SelectedIndexProperty,
            static (GameViewModel vm) => vm.PendingGameMode,
            mode: BindingMode.TwoWay,
            converter: GameModeToSelectedIndexConverter.Instance
        );

        // Ensure the picker reflects the current pending selection.
        _modePicker.SelectedIndex = (int)_viewModel.PendingGameMode;

        ModePickerContainer.Content = modePickerView;
    }

    private void CreateSizePicker()
    {
        var sizes = Enumerable
            .Range(
                BoardSizePickerConstants.MinSize,
                BoardSizePickerConstants.MaxSize - BoardSizePickerConstants.MinSize + 1
            )
            .ToList();
        var sizeLabels = sizes.Select(static size => $"{size}x{size}").ToList();

        _sizePicker = new Picker
        {
            ItemsSource = sizeLabels,
            Title = AppStrings.BoardSize,
            BindingContext = _viewModel,
        };

        View sizePickerView = _sizePicker;

        if (
            DeviceInfo.Current.Platform == DevicePlatform.iOS
            || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst
        )
        {
            sizePickerView = CreateiOSStyledPicker(_sizePicker);
        }

        _sizePicker.SetBinding(
            Picker.SelectedIndexProperty,
            static (GameViewModel vm) => vm.PendingBoardSize,
            mode: BindingMode.TwoWay,
            converter: BoardSizeToSelectedIndexConverter.Instance
        );

        // Ensure the picker reflects the current pending selection.
        _sizePicker.SelectedIndex = _viewModel.PendingBoardSize - BoardSizePickerConstants.MinSize;

        SizePickerContainer.Content = sizePickerView;
    }

    private View CreateiOSStyledPicker(Picker sizePicker)
    {
        const double iOSFieldHeight = 44;

        double cornerRadius = 20;
        if (
            Application.Current?.Resources.TryGetValue(
                "NativeCardCornerRadius",
                out var radiusValue
            ) == true
            && radiusValue is double radius
        )
        {
            cornerRadius = radius;
        }

        sizePicker.Background = Colors.Transparent;
        sizePicker.FontSize = 17;
        sizePicker.HeightRequest = iOSFieldHeight;
        sizePicker.MinimumHeightRequest = iOSFieldHeight;
        sizePicker.HorizontalOptions = LayoutOptions.Fill;
        sizePicker.VerticalOptions = LayoutOptions.Center;

#if IOS || MACCATALYST
        EventHandler? handlerChanged = null;
        handlerChanged = (_, _) =>
        {
            if (sizePicker.Handler?.PlatformView is UITextField textField)
            {
                // MAUI's Picker uses a UITextField which applies its own rounded-rect background/border.
                // That native chrome creates the subtle vertical edge lines in the field.
                textField.BorderStyle = UITextBorderStyle.None;
                textField.Background = null;
                textField.BackgroundColor = UIColor.Clear;
                textField.Layer.BorderWidth = 0;
                textField.Layer.CornerRadius = 0;
                textField.ClipsToBounds = true;

                sizePicker.HandlerChanged -= handlerChanged;
            }
        };
        sizePicker.HandlerChanged += handlerChanged;
#endif

        return new Border
        {
            HeightRequest = iOSFieldHeight,
            Background = GetThemeColor(
                "NativeSettingsCellBackgroundLight",
                "NativeSettingsCellBackgroundDark"
            ),
            Stroke = GetThemeColor("Gray200", "Gray600"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(cornerRadius) },
            Padding = new Thickness(12, 0),
            Content = sizePicker,
        };
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(GameViewModel.PendingBoardSize) 
            || e.PropertyName == nameof(GameViewModel.PendingGameMode))
        {
            UpdatePlayButtonState();
        }
    }

    private void UpdatePlayButtonState()
    {
        // Only enable the button if the board size or game mode has changed
        PlayButton.IsEnabled = _viewModel.PendingBoardSize != _originalBoardSize
            || _viewModel.PendingGameMode != _originalGameMode;
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        PlayRequested?.Invoke(this, EventArgs.Empty);
    }

    private static Color GetThemeColor(string lightKey, string darkKey)
    {
        var app = Application.Current;
        if (app == null)
        {
            return Colors.Gray;
        }

        var key = app.RequestedTheme == AppTheme.Dark ? darkKey : lightKey;
        if (app.Resources.TryGetValue(key, out var value) && value is Color color)
        {
            return color;
        }

        return Colors.Gray;
    }
}
