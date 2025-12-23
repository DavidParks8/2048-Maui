namespace TwentyFortyEight.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation - Shell uses DI automatically
        Routing.RegisterRoute("stats", typeof(StatsPage));
    }
}
