using AVFoundation;
using Foundation;
using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Platforms.iOS;

/// <summary>
/// AVFoundation speech adapter that reports the exact character range selected
/// by AVSpeechSynthesizer for each spoken segment.
/// </summary>
public sealed class IosWordLevelSpeechService
    : IWordLevelSpeechService,
        IWordSpeechService,
        ISpeechService,
        IReadAloudService,
        ITextToSpeechService,
        IWordLevelSpeech,
        IDisposable
{
    private const float ReadAloudRate = 0.44f;
    private const float SingleWordRate = 0.4f;
    private const float FriendlyPitch = 1.02f;
    private const double ReadAloudStartDelay = 0.06;
    private const double ReadAloudEndDelay = 0.04;

    private readonly object _sync = new();
    private AVSpeechSynthesizer? _synthesizer;
    private SpeechSynthesizerDelegate? _synthesizerDelegate;
    private AVSpeechSynthesisVoice? _voice;
    private SpeechOperation? _activeOperation;
    private bool _disposed;

    public event EventHandler<SpeechRangeEventArgs>? SpokenRange;

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default) =>
        SpeakCoreAsync(text, reportRanges: true, cancellationToken);

    public Task SpeakWordAsync(string word, CancellationToken cancellationToken = default) =>
        SpeakCoreAsync(word, reportRanges: false, cancellationToken);

    public void Stop()
    {
        SpeechOperation? operation;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            operation = _activeOperation;
            if (operation is null)
            {
                return;
            }
        }

        if (MainThread.IsMainThread)
        {
            StopNative(operation);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => StopNative(operation));
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _activeOperation?.Completion.TrySetResult(null);
        }

        if (MainThread.IsMainThread)
        {
            DisposeNative();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(DisposeNative);
        }
    }

    private async Task SpeakCoreAsync(
        string text,
        bool reportRanges,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Speech text is required.", nameof(text));
        }

        cancellationToken.ThrowIfCancellationRequested();
        SpeechOperation operation = await MainThread
            .InvokeOnMainThreadAsync(() => StartNative(text, reportRanges, cancellationToken))
            .ConfigureAwait(false);

        await operation.Completion.Task.ConfigureAwait(false);
    }

    private SpeechOperation StartNative(
        string text,
        bool reportRanges,
        CancellationToken cancellationToken
    )
    {
        if (!MainThread.IsMainThread)
        {
            throw new InvalidOperationException("Speech must be started on the main thread.");
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
        StopActiveNative();

        AVSpeechSynthesizer synthesizer;
        SpeechOperation operation;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            synthesizer = _synthesizer ??= CreateSynthesizer();
            operation = new(
                this,
                CreateUtterance(text, reportRanges),
                reportRanges,
                cancellationToken
            );
            _activeOperation = operation;
            operation.CancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    if (state is SpeechOperation canceledOperation)
                    {
                        canceledOperation.Owner.CancelFromToken(canceledOperation);
                    }
                },
                operation
            );
        }

        if (operation.Completion.Task.IsCompleted || cancellationToken.IsCancellationRequested)
        {
            StopNative(operation);
            return operation;
        }

        try
        {
            synthesizer.SpeakUtterance(operation.Utterance);
        }
        catch (Exception exception)
        {
            CompleteFailed(operation, exception);
        }

        return operation;
    }

    private AVSpeechSynthesizer CreateSynthesizer()
    {
        AVSpeechSynthesizer synthesizer = new();
        _synthesizerDelegate = new SpeechSynthesizerDelegate(this);
        synthesizer.Delegate = _synthesizerDelegate;
        _voice = SelectVoice();
        return synthesizer;
    }

    private AVSpeechUtterance CreateUtterance(string text, bool reportRanges) =>
        new(text)
        {
            Voice = _voice,
            Rate = reportRanges ? ReadAloudRate : SingleWordRate,
            PitchMultiplier = FriendlyPitch,
            PreUtteranceDelay = reportRanges ? ReadAloudStartDelay : 0,
            PostUtteranceDelay = reportRanges ? ReadAloudEndDelay : 0,
            PrefersAssistiveTechnologySettings = false,
        };

    private static AVSpeechSynthesisVoice? SelectVoice()
    {
        AVSpeechSynthesisVoice? languageDefault = AVSpeechSynthesisVoice.FromLanguage(
            SpeechVoiceSelectionPolicy.PreferredLanguage
        );
        AVSpeechSynthesisVoice[] voices = AVSpeechSynthesisVoice.GetSpeechVoices();
        SpeechVoiceCandidate[] candidates = new SpeechVoiceCandidate[voices.Length];

        for (int index = 0; index < voices.Length; index++)
        {
            AVSpeechSynthesisVoice voice = voices[index];
            AVSpeechSynthesisVoiceTraits traits = voice.VoiceTraits;
            candidates[index] = new SpeechVoiceCandidate(
                voice.Identifier,
                voice.Name,
                voice.Language,
                checked((int)voice.Quality),
                string.Equals(
                    voice.Identifier,
                    languageDefault?.Identifier,
                    StringComparison.Ordinal
                ),
                (traits & AVSpeechSynthesisVoiceTraits.IsNoveltyVoice) != 0,
                (traits & AVSpeechSynthesisVoiceTraits.IsPersonalVoice) != 0
            );
        }

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(candidates);
        return selectedIndex >= 0 ? voices[selectedIndex] : languageDefault;
    }

    private void CancelFromToken(SpeechOperation operation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_activeOperation, operation))
            {
                return;
            }

            operation.Completion.TrySetCanceled(operation.CancellationToken);
        }

        MainThread.BeginInvokeOnMainThread(() => StopNative(operation));
    }

    private void StopNative(SpeechOperation operation)
    {
        if (!MainThread.IsMainThread)
        {
            throw new InvalidOperationException("Speech must be stopped on the main thread.");
        }

        lock (_sync)
        {
            if (!ReferenceEquals(_activeOperation, operation))
            {
                return;
            }
        }

        StopActiveNative();
    }

    private void StopActiveNative()
    {
        SpeechOperation? operation;
        lock (_sync)
        {
            operation = _activeOperation;
            _activeOperation = null;
        }

        if (operation is null)
        {
            return;
        }

        operation.Completion.TrySetResult(null);
        operation.CancellationRegistration.Dispose();
        _synthesizer?.StopSpeaking(AVSpeechBoundary.Immediate);
    }

    private void DisposeNative()
    {
        if (!MainThread.IsMainThread)
        {
            throw new InvalidOperationException("Speech must be disposed on the main thread.");
        }

        StopActiveNative();
        lock (_sync)
        {
            _synthesizer?.Dispose();
            _synthesizer = null;
            _synthesizerDelegate = null;
            _voice = null;
        }
    }

    private void CompleteFailed(SpeechOperation operation, Exception exception)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_activeOperation, operation))
            {
                return;
            }

            _activeOperation = null;
        }

        operation.CancellationRegistration.Dispose();
        operation.Completion.TrySetException(exception);
    }

    private void CompleteSuccessfully(SpeechOperation operation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_activeOperation, operation))
            {
                return;
            }

            _activeOperation = null;
        }

        operation.CancellationRegistration.Dispose();
        operation.Completion.TrySetResult(null);
    }

    private void CompleteCanceled(SpeechOperation operation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_activeOperation, operation))
            {
                return;
            }

            _activeOperation = null;
        }

        operation.Completion.TrySetCanceled();
        operation.CancellationRegistration.Dispose();
    }

    private void ReportRange(
        AVSpeechSynthesizer synthesizer,
        AVSpeechUtterance utterance,
        NSRange characterRange
    )
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ReportRange(synthesizer, utterance, characterRange)
            );
            return;
        }

        EventHandler<SpeechRangeEventArgs>? handler;
        SpeechOperation? operation;
        lock (_sync)
        {
            operation = _activeOperation;
            if (
                operation is null
                || !operation.ReportRanges
                || !IsSameNativeObject(operation.Utterance, utterance)
                || !IsSameNativeObject(_synthesizer, synthesizer)
                || operation.Completion.Task.IsCompleted
            )
            {
                return;
            }

            handler = SpokenRange;
        }

        handler?.Invoke(
            this,
            new SpeechRangeEventArgs(
                checked((int)characterRange.Location),
                checked((int)characterRange.Length)
            )
        );
    }

    private void HandleFinished(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => HandleFinished(synthesizer, utterance));
            return;
        }

        SpeechOperation? operation;
        lock (_sync)
        {
            operation = _activeOperation;
            if (
                operation is null
                || !IsSameNativeObject(operation.Utterance, utterance)
                || !IsSameNativeObject(_synthesizer, synthesizer)
            )
            {
                return;
            }
        }

        CompleteSuccessfully(operation);
    }

    private void HandleCanceled(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => HandleCanceled(synthesizer, utterance));
            return;
        }

        SpeechOperation? operation;
        lock (_sync)
        {
            operation = _activeOperation;
            if (
                operation is null
                || !IsSameNativeObject(operation.Utterance, utterance)
                || !IsSameNativeObject(_synthesizer, synthesizer)
            )
            {
                return;
            }
        }

        CompleteCanceled(operation);
    }

    private static bool IsSameNativeObject(NSObject? left, NSObject? right) =>
        left is not null && right is not null && left.Handle == right.Handle;

    private sealed class SpeechOperation
    {
        public SpeechOperation(
            IosWordLevelSpeechService owner,
            AVSpeechUtterance utterance,
            bool reportRanges,
            CancellationToken cancellationToken
        )
        {
            Owner = owner;
            Utterance = utterance;
            ReportRanges = reportRanges;
            CancellationToken = cancellationToken;
            Completion = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

        public IosWordLevelSpeechService Owner { get; }

        public AVSpeechUtterance Utterance { get; }

        public bool ReportRanges { get; }

        public TaskCompletionSource<object?> Completion { get; }

        public CancellationToken CancellationToken { get; }

        public CancellationTokenRegistration CancellationRegistration { get; set; }
    }

    private sealed class SpeechSynthesizerDelegate : AVSpeechSynthesizerDelegate
    {
        private readonly IosWordLevelSpeechService _owner;

        public SpeechSynthesizerDelegate(IosWordLevelSpeechService owner)
        {
            _owner = owner;
        }

        public override void WillSpeakRangeOfSpeechString(
            AVSpeechSynthesizer synthesizer,
            NSRange characterRange,
            AVSpeechUtterance utterance
        )
        {
            _owner.ReportRange(synthesizer, utterance, characterRange);
        }

        public override void DidFinishSpeechUtterance(
            AVSpeechSynthesizer synthesizer,
            AVSpeechUtterance utterance
        ) => _owner.HandleFinished(synthesizer, utterance);

        public override void DidCancelSpeechUtterance(
            AVSpeechSynthesizer synthesizer,
            AVSpeechUtterance utterance
        ) => _owner.HandleCanceled(synthesizer, utterance);
    }
}
