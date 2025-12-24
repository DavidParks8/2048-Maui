using Android.App;
using Android.Runtime;

[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

namespace TwentyFortyEight.Maui;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
