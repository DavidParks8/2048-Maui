shader_type canvas_item;
render_mode unshaded, blend_mix;

// Simple separable blur-based glass shader sampled from the current screen.
// Tuned for subtle Apple-style "liquid" translucency.

// Godot 4.x requires SCREEN_TEXTURE as a uniform
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

uniform float blur_scale : hint_range(1.0, 6.0, 0.5) = 2.5;
uniform float tint_strength : hint_range(0.0, 1.0, 0.05) = 0.18;
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment()
{
    vec2 texel = blur_scale * SCREEN_PIXEL_SIZE;

    vec2 offsets[9] = vec2[](
        vec2(-1.0, -1.0),
        vec2(0.0, -1.0),
        vec2(1.0, -1.0),
        vec2(-1.0, 0.0),
        vec2(0.0, 0.0),
        vec2(1.0, 0.0),
        vec2(-1.0, 1.0),
        vec2(0.0, 1.0),
        vec2(1.0, 1.0)
    );

    float weights[9] = float[](
        1.0, 2.0, 1.0,
        2.0, 4.0, 2.0,
        1.0, 2.0, 1.0
    );

    vec4 sample_color = vec4(0.0);
    float total_weight = 0.0;

    for (int i = 0; i < 9; i++)
    {
        vec2 sample_uv = SCREEN_UV + offsets[i] * texel;
        sample_color += texture(SCREEN_TEXTURE, sample_uv) * weights[i];
        total_weight += weights[i];
    }

    sample_color /= total_weight;

    vec4 tinted = mix(sample_color, tint_color, tint_strength);
    tinted.a = clamp(sample_color.a + 0.05, 0.8, 1.0);
    COLOR = tinted;
}
