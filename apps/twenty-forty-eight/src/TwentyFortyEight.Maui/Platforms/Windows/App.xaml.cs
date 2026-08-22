using Microsoft.UI.Xaml;

namespace TwentyFortyEight.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => TwentyFortyEight.Maui.MauiProgram.CreateMauiApp();
}
