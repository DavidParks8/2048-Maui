using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI implementation of IToastService using the GlassToast component.
/// Shows toast notifications with liquid glass styling.
/// </summary>
public class GlassToastService : IToastService
{
    private GlassToast? _currentToast;
    private CancellationTokenSource? _dismissCts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task ShowAsync(string message, int durationMs = 2000)
    {
        await _lock.WaitAsync();
        try
        {
            // Cancel any pending dismiss
            _dismissCts?.Cancel();
            _dismissCts = new CancellationTokenSource();
            var cts = _dismissCts;

            // Remove any existing toast
            if (_currentToast != null)
            {
                await RemoveCurrentToastAsync();
            }

            // Find host layout
            var hostLayout = FindHostLayout();
            if (hostLayout == null)
            {
                return;
            }

            // Create and add new toast
            var toast = new GlassToast { Message = message };
            _currentToast = toast;

            AddToLayout(hostLayout, toast);

            // Show animation
            await toast.ShowAsync();

            // Wait for duration (can be cancelled if new toast appears)
            try
            {
                await Task.Delay(durationMs, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return; // New toast is taking over
            }

            // Hide animation and remove
            if (_currentToast == toast)
            {
                await toast.HideAsync();
                RemoveFromLayout(hostLayout, toast);
                if (_currentToast == toast)
                {
                    _currentToast = null;
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RemoveCurrentToastAsync()
    {
        if (_currentToast == null)
        {
            return;
        }

        var toast = _currentToast;
        var hostLayout = FindHostLayout();

        await toast.HideAsync();

        if (hostLayout != null)
        {
            RemoveFromLayout(hostLayout, toast);
        }

        if (_currentToast == toast)
        {
            _currentToast = null;
        }
    }

    private static Layout? FindHostLayout()
    {
        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage is Shell shell)
        {
            currentPage = shell.CurrentPage;
        }

        if (currentPage is NavigationPage navPage)
        {
            currentPage = navPage.CurrentPage;
        }

        if (currentPage is ContentPage contentPage)
        {
            return contentPage.Content as Layout;
        }

        return null;
    }

    private static void AddToLayout(Layout layout, GlassToast toast)
    {
        if (layout is AbsoluteLayout absoluteLayout)
        {
            AbsoluteLayout.SetLayoutBounds(toast, new Rect(0.5, 1, -1, -1));
            AbsoluteLayout.SetLayoutFlags(
                toast,
                Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional
            );
            absoluteLayout.Children.Add(toast);
        }
        else if (layout is Grid grid)
        {
            // Span all rows/columns and let the toast's HorizontalOptions="Center" handle positioning
            Grid.SetRow(toast, 0);
            Grid.SetColumn(toast, 0);
            Grid.SetRowSpan(toast, Math.Max(1, grid.RowDefinitions.Count));
            Grid.SetColumnSpan(toast, Math.Max(1, grid.ColumnDefinitions.Count));
            toast.HorizontalOptions = LayoutOptions.Center;
            toast.VerticalOptions = LayoutOptions.End;
            toast.ZIndex = 999;
            grid.Children.Add(toast);
        }
    }

    private static void RemoveFromLayout(Layout layout, GlassToast toast)
    {
        if (layout is AbsoluteLayout absoluteLayout)
        {
            absoluteLayout.Children.Remove(toast);
        }
        else if (layout is Grid grid)
        {
            grid.Children.Remove(toast);
        }
    }
}
