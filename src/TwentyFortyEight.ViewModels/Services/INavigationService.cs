namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstraction for navigation between pages/views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    /// <param name="route">The route to navigate to.</param>
    Task NavigateToAsync(string route);

    /// <summary>
    /// Navigates back to the previous page.
    /// </summary>
    Task GoBackAsync();
}
