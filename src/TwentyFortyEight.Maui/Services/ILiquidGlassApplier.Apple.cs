#if IOS || __MACCATALYST__
namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Applies the platform-specific Liquid Glass effect to a native view created by a MAUI handler.
/// </summary>
public interface ILiquidGlassApplier
{
    /// <summary>
    /// Applies or removes the effect for the given handler/view pair.
    /// </summary>
    void Apply(IViewHandler handler, IView view);
}
#endif
