using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
