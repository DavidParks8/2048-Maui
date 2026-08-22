using CommunityToolkit.Mvvm.Messaging;
using TwentyFortyEight.ViewModels.Messages;

namespace TwentyFortyEight.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation - Shell uses DI automatically
        Routing.RegisterRoute("stats", typeof(StatsPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("about", typeof(AboutPage));

        // Register navigation message handlers
        RegisterNavigationHandlers();
    }

    private void RegisterNavigationHandlers()
    {
        var messenger = StrongReferenceMessenger.Default;

        messenger.Register<AppShell, NavigateToStatsMessage>(
            this,
            static (shell, _) =>
            {
                shell.Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync("stats");
                });
            }
        );

        messenger.Register<AppShell, NavigateToSettingsMessage>(
            this,
            static (shell, _) =>
            {
                shell.Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync("settings");
                });
            }
        );

        messenger.Register<AppShell, NavigateToAboutMessage>(
            this,
            static (shell, _) =>
            {
                shell.Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync("about");
                });
            }
        );
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Clean up to avoid memory leaks (StrongReferenceMessenger requirement)
        StrongReferenceMessenger.Default.UnregisterAll(this);
    }
}
