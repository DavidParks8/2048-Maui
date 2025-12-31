using Maui.BindableProperty.Generator.Core;
using Microsoft.Maui.Controls;

namespace TwentyFortyEight.Maui.Controls;

public partial class BottomBar : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    [AutoBindable(DefaultValue = "72d")]
    private readonly double _barHeight;

    /// <summary>
    /// Android: maps to Material elevation (dp-ish).
    /// Other platforms may ignore.
    /// </summary>
    [AutoBindable(DefaultValue = "8d")]
    private readonly double _elevation;

    [AutoBindable(DefaultValue = "IosMaterialStyle.SystemChromeMaterial")]
    private readonly IosMaterialStyle _iosMaterial;

    [AutoBindable(DefaultValue = "true")]
    private readonly bool _useWindowsMica;

#pragma warning restore CS0169
}

public enum IosMaterialStyle
{
    SystemUltraThinMaterial,
    SystemThinMaterial,
    SystemMaterial,
    SystemThickMaterial,
    SystemChromeMaterial,
}
