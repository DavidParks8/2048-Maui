namespace TwentyFortyEight.Maui;

public partial class MainPage
{
    private const string BoardPostureAnimation = "BoardPosture";
    private Rect? _pendingBoardPostureBounds;
    private bool _isBoardPostureTransitioning;
    private bool _wasBoardInputTransparent;
    private bool _wereControlsInputTransparent;
    private int _boardPostureRevision;

    partial void PreparePostureLayout();

    private Rect GetBoardLayoutBounds() =>
        new(
            BoardViewport.X + BoardContainer.X,
            BoardViewport.Y + BoardContainer.Y,
            BoardContainer.Width,
            BoardContainer.Height
        );

    private void BeginBoardPostureTransition()
    {
        var previous = GetBoardLayoutBounds();
        double scale = BoardContainer.Scale;
        previous = new Rect(
            previous.Center.X + BoardContainer.TranslationX - previous.Width * scale / 2,
            previous.Center.Y + BoardContainer.TranslationY - previous.Height * scale / 2,
            previous.Width * scale,
            previous.Height * scale
        );

        EndBoardPostureTransition();
        _gestureRecognizerService.CancelCurrentGesture();
        _swipePreviewInteractionService.Reset();

        if (previous.IsEmpty || _accessibilitySettingsService.ShouldReduceMotion())
        {
            UpdateSwipeRecognizersForMode();
            return;
        }

        _pendingBoardPostureBounds = previous;
        _isBoardPostureTransitioning = true;
        _wasBoardInputTransparent = BoardViewport.InputTransparent;
        _wereControlsInputTransparent = ControlsGrid.InputTransparent;
        BoardViewport.InputTransparent = true;
        ControlsGrid.InputTransparent = true;
        int revision = _boardPostureRevision;
        Dispatcher.Dispatch(() => StartBoardPostureTransition(revision));
    }

    private void StartBoardPostureTransition(int revision)
    {
        if (revision != _boardPostureRevision || _pendingBoardPostureBounds is not Rect previous)
        {
            return;
        }

        PreparePostureLayout();
        if (revision != _boardPostureRevision || !_isBoardPostureTransitioning)
        {
            return;
        }

        var target = GetBoardLayoutBounds();
        _pendingBoardPostureBounds = null;
        if (
            target.IsEmpty
            || previous == target
            || _accessibilitySettingsService.ShouldReduceMotion()
        )
        {
            EndBoardPostureTransition();
            return;
        }

        double offsetX = previous.Center.X - target.Center.X;
        double offsetY = previous.Center.Y - target.Center.Y;
        double initialScale = previous.Width / target.Width;
        SetProgress(0);
        var animation = new Animation(SetProgress);
        animation.Commit(
            BoardContainer,
            BoardPostureAnimation,
            length: 220,
            easing: Easing.CubicOut,
            finished: (_, _) =>
            {
                if (revision == _boardPostureRevision)
                {
                    EndBoardPostureTransition();
                }
            }
        );

        void SetProgress(double progress)
        {
            BoardContainer.TranslationX = offsetX * (1 - progress);
            BoardContainer.TranslationY = offsetY * (1 - progress);
            BoardContainer.Scale = 1 + (initialScale - 1) * (1 - progress);
        }
    }

    private void EndBoardPostureTransition()
    {
        _boardPostureRevision++;
        _pendingBoardPostureBounds = null;
        BoardContainer.AbortAnimation(BoardPostureAnimation);
        BoardContainer.TranslationX = 0;
        BoardContainer.TranslationY = 0;
        BoardContainer.Scale = 1;
        if (_isBoardPostureTransitioning)
        {
            _isBoardPostureTransitioning = false;
            BoardViewport.InputTransparent = _wasBoardInputTransparent;
            ControlsGrid.InputTransparent = _wereControlsInputTransparent;
            UpdateSwipeRecognizersForMode();
        }
    }
}
