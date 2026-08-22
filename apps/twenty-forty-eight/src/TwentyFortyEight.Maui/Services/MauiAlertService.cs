using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of IAlertService using MAUI Shell/Page alerts.
/// </summary>
public class MauiAlertService : IAlertService
{
    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string accept,
        string cancel
    )
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            return false;
        }

        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    /// <inheritdoc />
    public async Task ShowAlertAsync(string title, string message, string cancel)
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            return;
        }

        await page.DisplayAlertAsync(title, message, cancel);
    }

    private static Page? GetCurrentPage()
    {
        // Try Shell first
        if (Shell.Current?.CurrentPage != null)
        {
            return Shell.Current.CurrentPage;
        }

        // Fall back to first window's page
        var window = Application.Current?.Windows.FirstOrDefault();
        return window?.Page;
    }
}
