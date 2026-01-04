namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// Attached properties that enable the iOS/macCatalyst "liquid glass" material effect on any MAUI view.
/// </summary>
/// <remarks>
/// This API is platform-agnostic. The rendering implementation is registered via DI and applied by
/// platform handlers (iOS/macCatalyst only). On other platforms, these properties are ignored.
/// </remarks>
public static class LiquidGlass
{
    /// <summary>
    /// Mapper key used by the platform feature to trigger updates via <c>handler.UpdateValue</c>.
    /// </summary>
    internal const string MappingName = "LiquidGlass";

    /// <summary>
    /// Enables or disables the glass effect on the target view.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="LiquidGlassEffectType.None"/>.
    /// </remarks>
    public static readonly BindableProperty EffectTypeProperty = BindableProperty.CreateAttached(
        "EffectType",
        typeof(LiquidGlassEffectType),
        typeof(LiquidGlass),
        LiquidGlassEffectType.None,
        propertyChanged: OnLiquidGlassPropertyChanged
    );

    /// <summary>
    /// Corner radius, in device-independent units, applied to the glass material.
    /// </summary>
    /// <remarks>
    /// The platform implementation may clamp this value based on the view's measured size.
    /// Default is <c>8</c>.
    /// </remarks>
    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.CreateAttached(
        "CornerRadius",
        typeof(double),
        typeof(LiquidGlass),
        8d,
        propertyChanged: OnLiquidGlassPropertyChanged
    );

    /// <summary>
    /// Enables or disables the subtle shadow applied to the hosting view.
    /// </summary>
    /// <remarks>
    /// The shadow is applied by the platform implementation and is only meaningful on platforms
    /// that support the glass effect.
    /// </remarks>
    public static readonly BindableProperty EnableShadowEffectProperty =
        BindableProperty.CreateAttached(
            "EnableShadowEffect",
            typeof(bool),
            typeof(LiquidGlass),
            true,
            propertyChanged: OnLiquidGlassPropertyChanged
        );

    /// <summary>
    /// Gets the configured <see cref="LiquidGlassEffectType"/> for the given view.
    /// </summary>
    public static LiquidGlassEffectType GetEffectType(BindableObject view) =>
        (LiquidGlassEffectType)view.GetValue(EffectTypeProperty);

    /// <summary>
    /// Sets the configured <see cref="LiquidGlassEffectType"/> for the given view.
    /// </summary>
    public static void SetEffectType(BindableObject view, LiquidGlassEffectType value) =>
        view.SetValue(EffectTypeProperty, value);

    /// <summary>
    /// Gets the configured corner radius for the given view.
    /// </summary>
    public static double GetCornerRadius(BindableObject view) =>
        (double)view.GetValue(CornerRadiusProperty);

    /// <summary>
    /// Sets the configured corner radius for the given view.
    /// </summary>
    public static void SetCornerRadius(BindableObject view, double value) =>
        view.SetValue(CornerRadiusProperty, value);

    /// <summary>
    /// Gets whether the shadow effect is enabled for the given view.
    /// </summary>
    public static bool GetEnableShadowEffect(BindableObject view) =>
        (bool)view.GetValue(EnableShadowEffectProperty);

    /// <summary>
    /// Sets whether the shadow effect is enabled for the given view.
    /// </summary>
    public static void SetEnableShadowEffect(BindableObject view, bool value) =>
        view.SetValue(EnableShadowEffectProperty, value);

    /// <summary>
    /// Triggers a platform handler refresh when any attached property changes.
    /// </summary>
    private static void OnLiquidGlassPropertyChanged(
        BindableObject bindable,
        object oldValue,
        object newValue
    )
    {
        if (bindable is VisualElement element && element.Handler != null)
        {
            element.Handler.UpdateValue(MappingName);
        }
    }
}
