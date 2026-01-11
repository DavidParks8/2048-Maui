using Microsoft.Maui.Controls.Shapes;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Converters;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels;
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
    private bool _isViewModelSubscribed;

    public event EventHandler? PlayRequested;

    public ModeSelectionView(
        GameViewModel viewModel,
        int originalBoardSize,
        GameMode originalGameMode
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        _originalBoardSize = originalBoardSize;
        _originalGameMode = originalGameMode;
        BindingContext = _viewModel;

        CreateSizePicker();
        UpdateHelperText();
        UpdateModeTabVisualState();
        UpdatePlayButtonState();

        // Listen for picker changes to update button state
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _isViewModelSubscribed = true;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_isViewModelSubscribed)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _isViewModelSubscribed = false;
        }

        Unloaded -= OnUnloaded;
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
        if (
            e.PropertyName == nameof(GameViewModel.PendingBoardSize)
            || e.PropertyName == nameof(GameViewModel.PendingGameMode)
        )
        {
            UpdateHelperText();
            UpdateModeTabVisualState();
            UpdatePlayButtonState();
        }
    }

    private void UpdatePlayButtonState()
    {
        PlayButton.IsEnabled =
            _viewModel.PendingBoardSize != _originalBoardSize
            || _viewModel.PendingGameMode != _originalGameMode;
    }

    private void OnModernModeClicked(object? sender, EventArgs e)
    {
        _viewModel.PendingGameMode = GameMode.Modern;
    }

    private void OnClassicModeClicked(object? sender, EventArgs e)
    {
        _viewModel.PendingGameMode = GameMode.Classic;
    }

    private void OnWalltastrophyModeClicked(object? sender, EventArgs e)
    {
        _viewModel.PendingGameMode = GameMode.Walltastrophy;
    }

    private void OnAdversarialModeClicked(object? sender, EventArgs e)
    {
        _viewModel.PendingGameMode = GameMode.Adversarial;
    }

    private void UpdateHelperText()
    {
        ModeDescriptionLabel.Text = _viewModel.PendingGameMode switch
        {
            GameMode.Modern => AppStrings.ModernModeDescription,
            GameMode.Classic => AppStrings.ClassicModeDescription,
            GameMode.Walltastrophy => AppStrings.WalltastrophyModeDescription,
            GameMode.Adversarial => AppStrings.AdversarialModeDescription,
            _ => AppStrings.ModernModeDescription,
        };
    }

    private void UpdateModeTabVisualState()
    {
        var selectedMode = _viewModel.PendingGameMode;
        bool isModern = selectedMode == GameMode.Modern;
        bool isClassic = selectedMode == GameMode.Classic;
        bool isWalltastrophy = selectedMode == GameMode.Walltastrophy;
        bool isAdversarial = selectedMode == GameMode.Adversarial;

        var selectedBackground = GetThemeColor("Gray200", "Gray600");
        var selectedTextColor = GetThemeColor("NativeTextPrimaryLight", "NativeTextPrimaryDark");
        var unselectedTextColor = GetThemeColor(
            "NativeTextSecondaryLight",
            "NativeTextSecondaryDark"
        );

        ModernTab.Background = isModern ? selectedBackground : Colors.Transparent;
        ClassicTab.Background = isClassic ? selectedBackground : Colors.Transparent;
        WalltastrophyTab.Background = isWalltastrophy ? selectedBackground : Colors.Transparent;
        AdversarialTab.Background = isAdversarial ? selectedBackground : Colors.Transparent;

        ModernTabButton.TextColor = isModern ? selectedTextColor : unselectedTextColor;
        ClassicTabButton.TextColor = isClassic ? selectedTextColor : unselectedTextColor;
        WalltastrophyTabButton.TextColor = isWalltastrophy
            ? selectedTextColor
            : unselectedTextColor;
        AdversarialTabButton.TextColor = isAdversarial ? selectedTextColor : unselectedTextColor;
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
