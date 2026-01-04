using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of IAlertService using MAUI Sheet-style confirmations.
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
        var confirmationSheet = FindConfirmationSheet();
        if (confirmationSheet != null)
        {
            return await confirmationSheet.ShowAsync(title, message, accept, cancel);
        }

        // Fallback to standard alert if sheet is not found
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

    private static ConfirmationSheet? FindConfirmationSheet()
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            return null;
        }

        // Search for ConfirmationSheet in the visual tree
        return FindConfirmationSheetRecursive(page);
    }

    private static ConfirmationSheet? FindConfirmationSheetRecursive(Element element)
    {
        if (element is ConfirmationSheet sheet)
        {
            return sheet;
        }

        if (element is ContentPage page && page.Content != null)
        {
            return FindConfirmationSheetRecursive(page.Content);
        }

        if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is Element childElement)
                {
                    var result = FindConfirmationSheetRecursive(childElement);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
        }

        return null;
    }
}
