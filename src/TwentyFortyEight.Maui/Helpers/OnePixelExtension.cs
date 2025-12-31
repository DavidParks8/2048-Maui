using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Devices;

namespace TwentyFortyEight.Maui.Helpers;

[ContentProperty(nameof(Multiplier))]
[AcceptEmptyServiceProvider]
public sealed class OnePixelExtension : IMarkupExtension<double>
{
    public double Multiplier { get; set; } = 1;

    public double ProvideValue(IServiceProvider serviceProvider)
    {
        var density = DeviceDisplay.MainDisplayInfo.Density;
        if (density <= 0)
        {
            density = 1;
        }

        return Multiplier / density;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
