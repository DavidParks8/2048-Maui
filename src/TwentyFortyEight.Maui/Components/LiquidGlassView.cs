using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

public partial class LiquidGlassView : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    [AutoBindable(DefaultValue = "LiquidGlassEffectType.Regular")]
    private readonly LiquidGlassEffectType _effectType;

    [AutoBindable(DefaultValue = "8d")]
    private readonly double _cornerRadius;

    [AutoBindable(DefaultValue = "false")]
    private readonly bool _enableShadowEffect;

#pragma warning restore CS0169
}

public enum LiquidGlassEffectType
{
    Regular,
    Clear,
}
