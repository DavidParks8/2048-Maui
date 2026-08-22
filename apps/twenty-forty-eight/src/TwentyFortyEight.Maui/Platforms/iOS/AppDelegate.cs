using Foundation;
using UIKit;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => TwentyFortyEight.Maui.MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        ConfigureLiquidGlassNavigationBar();
        return base.FinishedLaunching(application, launchOptions);
    }

    static void ConfigureLiquidGlassNavigationBar()
    {
        if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            return;

        UINavigationBarAppearance appearance = new();
        appearance.ConfigureWithTransparentBackground();
        appearance.BackgroundEffect = UIBlurEffect.FromStyle(
            UIBlurEffectStyle.SystemChromeMaterial
        );
        appearance.ShadowColor = UIColor.Clear;

        UINavigationBar.Appearance.StandardAppearance = appearance;
        UINavigationBar.Appearance.ScrollEdgeAppearance = appearance;
        UINavigationBar.Appearance.CompactAppearance = appearance;
    }
}
