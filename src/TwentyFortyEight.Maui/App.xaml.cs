namespace TwentyFortyEight.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            MinimumWidth = 360, // Min board (280) + padding (40) + margins (40)
            MinimumHeight = 700, // Ensures full UI visibility with adequate margins
        };

        return window;
    }
}
