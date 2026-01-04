namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// Confirmation sheet that slides up from the bottom similar to SwiftUI sheets.
/// </summary>
public partial class ConfirmationSheet : ContentView
{
    private const uint ShowAnimationDurationMs = 300;
    private const uint HideAnimationDurationMs = 200;
    private const double BackdropMaxOpacity = 0.5;
    private const double OffScreenTranslationY = 1000;

    private TaskCompletionSource<bool>? _taskCompletionSource;

    public ConfirmationSheet()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the confirmation sheet and waits for user response.
    /// </summary>
    /// <param name="title">The title text.</param>
    /// <param name="message">The message text.</param>
    /// <param name="accept">The accept button text.</param>
    /// <param name="cancel">The cancel button text.</param>
    /// <returns>True if user accepted, false if cancelled.</returns>
    public Task<bool> ShowAsync(string title, string message, string accept, string cancel)
    {
        // If already showing, return current task
        if (_taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
        {
            return _taskCompletionSource.Task;
        }

        _taskCompletionSource = new TaskCompletionSource<bool>();

        // Set content
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        AcceptButton.Text = accept;
        CancelButton.Text = cancel;

        // Show the sheet - fire and forget
        _ = AnimateShowAsync();

        return _taskCompletionSource.Task;
    }

    private async Task AnimateShowAsync()
    {
        IsVisible = true;

        // Ensure consistent initial state
        Backdrop.Opacity = 0;
        SheetContainer.TranslationY = OffScreenTranslationY;

        // Animate backdrop and sheet simultaneously
        await Task.WhenAll(
            Backdrop.FadeTo(BackdropMaxOpacity, ShowAnimationDurationMs, Easing.CubicOut),
            SheetContainer.TranslateTo(0, 0, ShowAnimationDurationMs, Easing.CubicOut)
        );
    }

    private async Task AnimateHideAsync()
    {
        // Animate backdrop and sheet simultaneously
        await Task.WhenAll(
            Backdrop.FadeTo(0, HideAnimationDurationMs, Easing.CubicIn),
            SheetContainer.TranslateTo(0, OffScreenTranslationY, HideAnimationDurationMs, Easing.CubicIn)
        );

        IsVisible = false;
    }

    private async void OnAcceptButtonClicked(object? sender, EventArgs e)
    {
        if (_taskCompletionSource == null || _taskCompletionSource.Task.IsCompleted)
        {
            return;
        }

        try
        {
            await AnimateHideAsync();
            _taskCompletionSource.TrySetResult(true);
        }
        catch
        {
            // If animation fails, still complete the task to avoid hanging
            _taskCompletionSource.TrySetResult(true);
        }
    }

    private async void OnCancelButtonClicked(object? sender, EventArgs e)
    {
        if (_taskCompletionSource == null || _taskCompletionSource.Task.IsCompleted)
        {
            return;
        }

        try
        {
            await AnimateHideAsync();
            _taskCompletionSource.TrySetResult(false);
        }
        catch
        {
            // If animation fails, still complete the task to avoid hanging
            _taskCompletionSource.TrySetResult(false);
        }
    }

    private async void OnBackdropTapped(object? sender, EventArgs e)
    {
        // Tapping backdrop acts as cancel
        if (_taskCompletionSource == null || _taskCompletionSource.Task.IsCompleted)
        {
            return;
        }

        try
        {
            await AnimateHideAsync();
            _taskCompletionSource.TrySetResult(false);
        }
        catch
        {
            // If animation fails, still complete the task to avoid hanging
            _taskCompletionSource.TrySetResult(false);
        }
    }
}
