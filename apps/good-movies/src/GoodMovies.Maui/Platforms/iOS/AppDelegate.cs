using Foundation;
using UIKit;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => GoodMovies.Maui.MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        UINavigationBarAppearance appearance = new();
        appearance.ConfigureWithOpaqueBackground();
        appearance.BackgroundColor = UIColor.FromRGB(25, 10, 58);
        appearance.TitleTextAttributes = new UIStringAttributes { ForegroundColor = UIColor.White };
        appearance.LargeTitleTextAttributes = appearance.TitleTextAttributes;

        UINavigationBar.Appearance.StandardAppearance = appearance;
        UINavigationBar.Appearance.ScrollEdgeAppearance = appearance;
        UINavigationBar.Appearance.CompactAppearance = appearance;
        UINavigationBar.Appearance.TintColor = UIColor.White;

        return base.FinishedLaunching(application, launchOptions);
    }

    public override void DidEnterBackground(UIApplication application)
    {
        base.DidEnterBackground(application);
        (Microsoft.Maui.Controls.Application.Current as GoodMovies.Maui.App)?.StopSpeech();
    }
}
