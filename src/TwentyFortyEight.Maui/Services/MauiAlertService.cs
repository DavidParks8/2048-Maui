using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of IAlertService using MAUI Sheet-style confirmations.
/// </summary>
public class MauiAlertService : IAlertService
{
    private const int MaxSearchDepth = 10; // Limit visual tree search depth
    private WeakReference<ConfirmationSheet>? _cachedSheetRef;

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

    private ConfirmationSheet? FindConfirmationSheet()
    {
        // Try cached reference first
        if (_cachedSheetRef?.TryGetTarget(out var cachedSheet) == true)
        {
            return cachedSheet;
        }

        var page = GetCurrentPage();
        if (page == null)
        {
            return null;
        }

        // Search for ConfirmationSheet in the visual tree with depth limit
        var sheet = FindConfirmationSheetRecursive(page, 0);
        if (sheet != null)
        {
            _cachedSheetRef = new WeakReference<ConfirmationSheet>(sheet);
        }

        return sheet;
    }

    private static ConfirmationSheet? FindConfirmationSheetRecursive(Element element, int depth)
    {
        // Limit search depth to prevent excessive recursion
        if (depth > MaxSearchDepth)
        {
            return null;
        }

        if (element is ConfirmationSheet sheet)
        {
            return sheet;
        }

        if (element is ContentPage page && page.Content != null)
        {
            return FindConfirmationSheetRecursive(page.Content, depth + 1);
        }

        if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is Element childElement)
                {
                    var result = FindConfirmationSheetRecursive(childElement, depth + 1);
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
