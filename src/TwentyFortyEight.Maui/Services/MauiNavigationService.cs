using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of INavigationService using Shell navigation.
/// </summary>
public class MauiNavigationService : INavigationService
{
    /// <inheritdoc />
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    /// <inheritdoc />
    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
