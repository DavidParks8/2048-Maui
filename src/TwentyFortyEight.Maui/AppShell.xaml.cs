namespace TwentyFortyEight.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute(nameof(StatsPage), typeof(StatsPage));
    }
}
