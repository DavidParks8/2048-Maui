using TwentyFortyEight.ViewModels;

namespace TwentyFortyEight.Maui;

/// <summary>
/// Page displaying gameplay statistics.
/// </summary>
public partial class StatsPage : ContentPage
{
    private readonly StatsViewModel _viewModel;

    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshStatistics();
    }
}
