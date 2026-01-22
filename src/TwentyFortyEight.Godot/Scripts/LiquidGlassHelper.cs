using System;
using Godot;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Applies the liquid glass shader to supported platforms (iOS + macOS/Mac Catalyst).
/// Falls back to a translucent fill elsewhere.
/// </summary>
public static class LiquidGlassHelper
{
    private static ShaderMaterial? _sharedMaterial;

    private static bool SupportsGlass =>
        OS.GetName() is "iOS" or "macOS" || OS.HasFeature("macos") || OS.HasFeature("ios");

    /// <summary>
    /// Applies the liquid glass shader to the provided canvas item when supported.
    /// </summary>
    public static void Apply(CanvasItem? canvas, Color? tintOverride = null)
    {
        if (canvas is null)
        {
            return;
        }

        if (!SupportsGlass)
        {
            // Provide a translucent fallback
            var modulate = canvas.Modulate;
            modulate.A = MathF.Max(modulate.A, 0.9f);
            canvas.Modulate = modulate;
            return;
        }

        var shader = GetSharedMaterial();
        if (shader == null)
        {
            return;
        }

        if (canvas.Material is ShaderMaterial existing && existing.Shader == shader.Shader)
        {
            // Already configured
            return;
        }

        var material = (ShaderMaterial)shader.Duplicate();
        var tint = tintOverride ?? new Color(1f, 1f, 1f, 0.95f);
        material.SetShaderParameter("tint_color", tint);
        canvas.Material = material;
    }

    private static ShaderMaterial? GetSharedMaterial()
    {
        if (_sharedMaterial != null)
        {
            return _sharedMaterial;
        }

        var shader = GD.Load<Shader>("res://Resources/Shaders/liquid_glass.gdshader");
        if (shader == null)
        {
            GD.PrintErr("Liquid glass shader not found.");
            return null;
        }

        _sharedMaterial = new ShaderMaterial { Shader = shader };
        return _sharedMaterial;
    }
}
