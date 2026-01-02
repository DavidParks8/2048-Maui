#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.Windows.Handlers;

public class BottomBarHandler : ContentViewHandler
{
    public static new PropertyMapper<BottomBar, BottomBarHandler> Mapper = new(
        ContentViewHandler.Mapper
    )
    {
        [nameof(BottomBar.BarHeight)] = MapBarHeight,
    };

    public BottomBarHandler()
        : base(Mapper) { }

    static void MapBarHeight(BottomBarHandler handler, BottomBar view)
    {
        // Height is controlled by MAUI layout
    }
}
#endif
