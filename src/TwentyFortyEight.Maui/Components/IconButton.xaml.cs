using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A button component with an icon centered above text, styled for iOS glass effect.
/// </summary>
public partial class IconButton : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the icon text (Unicode symbol or emoji).
    /// </summary>
    [AutoBindable]
    private readonly string _icon = string.Empty;

    /// <summary>
    /// Gets or sets the button text displayed below the icon.
    /// </summary>
    [AutoBindable]
    private readonly string _buttonText = string.Empty;

    /// <summary>
    /// Gets or sets the command to execute when the button is tapped.
    /// </summary>
    [AutoBindable]
    private readonly ICommand? _command;

    /// <summary>
    /// Gets or sets whether the button is enabled.
    /// </summary>
    [AutoBindable(DefaultValue = "true")]
    private readonly bool _isButtonEnabled;

#pragma warning restore CS0169

    public static readonly BindableProperty IconImageSourceProperty = BindableProperty.Create(
        nameof(IconImageSource),
        typeof(ImageSource),
        typeof(IconButton),
        defaultValue: null,
        propertyChanged: static (bindable, _, _) =>
        {
            var control = (IconButton)bindable;
            control.OnPropertyChanged(nameof(HasIconImageSource));
            control.OnPropertyChanged(nameof(HasIconText));
        }
    );

    public ImageSource? IconImageSource
    {
        get => (ImageSource?)GetValue(IconImageSourceProperty);
        set => SetValue(IconImageSourceProperty, value);
    }

    public static readonly BindableProperty ButtonSizeProperty = BindableProperty.Create(
        nameof(ButtonSize),
        typeof(double),
        typeof(IconButton),
        defaultValue: 70d,
        propertyChanged: static (bindable, _, _) =>
        {
            var control = (IconButton)bindable;
            control.OnPropertyChanged(nameof(ButtonStrokeShape));
        }
    );

    public double ButtonSize
    {
        get => (double)GetValue(ButtonSizeProperty);
        set => SetValue(ButtonSizeProperty, value);
    }

    public bool HasIconImageSource => IconImageSource is not null;

    public bool HasIconText => !HasIconImageSource;

    public RoundRectangle ButtonStrokeShape => new() { CornerRadius = (float)(ButtonSize / 2d) };

    public IconButton()
    {
        InitializeComponent();
    }
}
