namespace TwentyFortyEight.Maui.Services;

public sealed class MauiVisualFeatureInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        foreach (var feature in services.GetServices<IMauiVisualFeature>())
        {
            feature.Register();
        }
    }
}
