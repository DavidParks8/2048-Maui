using GoodMovies.ViewModels;

namespace GoodMovies.Maui.Services;

internal sealed class MauiNoopWordLevelSpeechService
    : IWordLevelSpeechService,
        IWordSpeechService,
        ISpeechService,
        IReadAloudService,
        ITextToSpeechService,
        IWordLevelSpeech
{
    public event EventHandler<SpeechRangeEventArgs>? SpokenRange
    {
        add { }
        remove { }
    }

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task SpeakWordAsync(string word, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void Stop() { }
}
