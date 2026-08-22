#if ANDROID
using Android.Content;
using Android.Views;
using AndroidX.Core.View;
using Google.Android.Material.Card;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.Android.Handlers;

public class BottomBarHandler : ContentViewHandler
{
    MaterialCardView? _card;

    public static new PropertyMapper<BottomBar, BottomBarHandler> Mapper = new(
        ContentViewHandler.Mapper
    )
    {
        [nameof(BottomBar.Elevation)] = MapElevation,
        [nameof(BottomBar.BarHeight)] = MapBarHeight,
    };

    public BottomBarHandler()
        : base(Mapper) { }

    protected override ContentViewGroup CreatePlatformView()
    {
        // This is the MAUI container for the content.
        var contentHost = base.CreatePlatformView();

        var context = contentHost.Context!;

        _card = new MaterialCardView(context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent
            ),
        };

        // Make it look like a bottom app surface (no rounded corners by default).
        _card.Radius = 0;
        _card.CardElevation = Dp(context, (float)((VirtualView as BottomBar)?.Elevation ?? 8d));
        _card.UseCompatPadding = false;
        _card.PreventCornerOverlap = false;

        // Use Material surface color (if available) rather than hardcoding.
        // Falls back gracefully if theme attr isn't set.
        TryApplyMaterialSurfaceColor(_card, context);

        // Optional top divider (subtle)
        _card.StrokeWidth = (int)Dp(context, 1);
        _card.StrokeColor = global::Android.Graphics.Color.Argb(30, 255, 255, 255); // slight; tweak if needed

        // Respect system insets (gesture nav) so buttons aren't too low.
        ViewCompat.SetOnApplyWindowInsetsListener(_card, new InsetsListener());

        // Put MAUI content inside the material surface.
        _card.AddView(contentHost);

        // Return a wrapper that hosts the card.
        var wrapper = new ContentViewGroup(context);
        wrapper.AddView(_card);
        return wrapper;
    }

    protected override void ConnectHandler(ContentViewGroup platformView)
    {
        base.ConnectHandler(platformView);
        UpdateElevation();
    }

    static void MapElevation(BottomBarHandler handler, BottomBar view) => handler.UpdateElevation();

    static void MapBarHeight(
        BottomBarHandler handler,
        BottomBar view
    ) { /* layout controlled by MAUI */
    }

    void UpdateElevation()
    {
        if (_card == null)
            return;
        var context = _card.Context!;
        _card.CardElevation = Dp(context, (float)((VirtualView as BottomBar)?.Elevation ?? 8d));
    }

    static float Dp(Context context, float dp) => dp * context.Resources!.DisplayMetrics!.Density;

    static void TryApplyMaterialSurfaceColor(MaterialCardView card, Context context)
    {
        // Tries to resolve ?attr/colorSurface from current theme
        var tv = new global::Android.Util.TypedValue();
        if (
            context.Theme?.ResolveAttribute(
                global::Android.Resource.Attribute.ColorBackground,
                tv,
                true
            ) == true
        )
        {
            // This isn't perfect; many apps will want Material's colorSurface.
            // But it keeps things "native" without hardcoding.
            card.SetCardBackgroundColor(tv.Data);
        }
    }

    class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(
            global::Android.Views.View? v,
            WindowInsetsCompat? insets
        )
        {
            if (v == null || insets == null)
                return insets;

            var view = v!;

            var sysBars = insets.GetInsets(
                WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout()
            );

            var bottomInset = sysBars?.Bottom ?? 0;
            view.SetPadding(view.PaddingLeft, view.PaddingTop, view.PaddingRight, bottomInset);
            return insets;
        }
    }
}
#endif
