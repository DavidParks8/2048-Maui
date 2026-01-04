using Microsoft.Maui.Handlers;

namespace TwentyFortyEight.Maui.Services;

public sealed class LiquidGlassFeature(ILiquidGlassApplier applier) : IMauiVisualFeature
{
    public void Register()
    {
#if IOS || MACCATALYST
        BorderHandler.Mapper.AppendToMapping(
            Components.LiquidGlass.MappingName,
            (handler, view) => applier.Apply(handler, view)
        );
        ContentViewHandler.Mapper.AppendToMapping(
            Components.LiquidGlass.MappingName,
            (handler, view) => applier.Apply(handler, view)
        );
#endif
    }
}
