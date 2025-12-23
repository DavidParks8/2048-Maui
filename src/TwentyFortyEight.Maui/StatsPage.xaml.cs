using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class StatsPage : ContentPage
{
    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh statistics when page appears
        if (BindingContext is StatsViewModel viewModel)
        {
            viewModel.RefreshStatistics();
        }
    }
}
