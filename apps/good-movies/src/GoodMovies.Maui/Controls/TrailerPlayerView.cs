namespace GoodMovies.Maui.Controls;

public sealed class TrailerPlayerView : View
{
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source),
        typeof(Uri),
        typeof(TrailerPlayerView)
    );

    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public event EventHandler? LoadStarted;

    public event EventHandler? LoadSucceeded;

    public event EventHandler? LoadFailed;

    public void Reload() => Handler?.Invoke(nameof(Reload));

    public void StopPlayback() => Handler?.Invoke(nameof(StopPlayback));

    internal void ReportLoadStarted() => LoadStarted?.Invoke(this, EventArgs.Empty);

    internal void ReportLoadSucceeded() => LoadSucceeded?.Invoke(this, EventArgs.Empty);

    internal void ReportLoadFailed() => LoadFailed?.Invoke(this, EventArgs.Empty);
}
