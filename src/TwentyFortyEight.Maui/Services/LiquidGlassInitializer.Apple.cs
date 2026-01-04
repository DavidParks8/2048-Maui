#if IOS || __MACCATALYST__
using Microsoft.Maui.Handlers;

namespace TwentyFortyEight.Maui.Services;

public sealed class LiquidGlassInitializer(ILiquidGlassApplier applier) : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        BorderHandler.Mapper.AppendToMapping(
            Components.LiquidGlass.MappingName,
            (handler, view) => applier.Apply(handler, view)
        );
        ContentViewHandler.Mapper.AppendToMapping(
            Components.LiquidGlass.MappingName,
            (handler, view) => applier.Apply(handler, view)
        );
    }
}
#endif
