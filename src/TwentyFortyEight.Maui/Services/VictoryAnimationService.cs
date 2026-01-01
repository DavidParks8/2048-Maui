using System.Linq;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using TwentyFortyEight.Maui.Victory;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI implementation of IVictoryAnimationService.
/// Coordinates between VictoryViewModel and the SkiaSharp-based CinematicOverlayView.
/// </summary>
public sealed partial class VictoryAnimationService : IVictoryAnimationService
{
    private readonly CinematicOverlayView _cinematicOverlay;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly VictoryViewModel _victoryViewModel;
    private readonly ILogger<VictoryAnimationService> _logger;

    private Grid? _gameBoard;
    private Func<IReadOnlyList<TileViewModel>>? _getTiles;
    private Func<IReadOnlyDictionary<TileViewModel, Border>>? _getTileBorders;

    public event EventHandler? ShowModalRequested;
    public event EventHandler? AnimationCompleted;

    public VictoryAnimationService(
        CinematicOverlayView cinematicOverlay,
        IScreenCaptureService screenCaptureService,
        VictoryViewModel victoryViewModel,
        ILogger<VictoryAnimationService> logger
    )
    {
        _cinematicOverlay = cinematicOverlay;
        _screenCaptureService = screenCaptureService;
        _victoryViewModel = victoryViewModel;
        _logger = logger;

        // Wire up cinematic overlay events
        _cinematicOverlay.ShowModalRequested += OnCinematicShowModalRequested;
        _cinematicOverlay.AnimationCompleted += OnCinematicAnimationCompleted;

        // Wire up ViewModel events
        _victoryViewModel.AnimationStartRequested += OnAnimationStartRequested;
        _victoryViewModel.AnimationStopRequested += OnAnimationStopRequested;
    }

    /// <summary>
    /// Provides references needed for screen capture.
    /// Must be called during page initialization.
    /// </summary>
    public void SetBoardReferences(
        Grid gameBoard,
        Func<IReadOnlyList<TileViewModel>> getTiles,
        Func<IReadOnlyDictionary<TileViewModel, Border>> getTileBorders
    )
    {
        _gameBoard = gameBoard;
        _getTiles = getTiles;
        _getTileBorders = getTileBorders;
    }

    public void Initialize()
    {
        // No-op for now - initialization happens via SetBoardReferences
    }

    public async Task StartAnimationAsync(int winningTileRow, int winningTileColumn, int score)
    {
        if (_gameBoard is null || _getTiles is null || _getTileBorders is null)
        {
            LogMissingReferences(_logger);
            _victoryViewModel.ShowModal();
            return;
        }

        var tiles = _getTiles();
        var tileBorders = _getTileBorders();

        // Find the winning tile's UI element
        var winningTileVm = tiles.FirstOrDefault(t =>
            t.Row == winningTileRow && t.Column == winningTileColumn
        );

        if (winningTileVm is null || !tileBorders.TryGetValue(winningTileVm, out var tileView))
        {
            LogTileNotFound(_logger, winningTileRow, winningTileColumn);
            _victoryViewModel.ShowModal();
            return;
        }

        try
        {
            // Capture snapshots
            var boardSnapshot = await CaptureBoardSnapshotAsync(_gameBoard);
            var tileSnapshot = await CaptureTileSnapshotAsync(tileView);
            var tileCenter = GetTileCenterInOverlay(tileView, _cinematicOverlay);
            SKSize tileSize = new((float)tileView.Width, (float)tileView.Height);

            if (boardSnapshot == null || tileSnapshot == null)
            {
                LogSnapshotFailed(_logger);
                _victoryViewModel.ShowModal();
                return;
            }

            // Start cinematic animation
            _cinematicOverlay.StartAnimation(boardSnapshot, tileSnapshot, tileCenter, tileSize);

            _victoryViewModel.UpdateAnimationProgress(VictoryAnimationPhase.Impact, 0f);
        }
        catch (Exception ex)
        {
            LogVictoryAnimationError(_logger, ex);
            _victoryViewModel.ShowModal();
        }
    }

    public void StopAnimation()
    {
        _cinematicOverlay.StopAnimation();
    }

    public void EnterSustainMode()
    {
        _cinematicOverlay.EnterSustainMode();
    }

    private void OnCinematicShowModalRequested(object? sender, EventArgs e)
    {
        EnterSustainMode();
        _victoryViewModel.ShowModal();
        ShowModalRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCinematicAnimationCompleted(object? sender, EventArgs e)
    {
        AnimationCompleted?.Invoke(this, EventArgs.Empty);
    }

    private async void OnAnimationStartRequested(object? sender, VictoryAnimationStartEventArgs e)
    {
        await StartAnimationAsync(e.WinningTileRow, e.WinningTileColumn, e.Score);
    }

    private void OnAnimationStopRequested(object? sender, EventArgs e)
    {
        StopAnimation();
    }

    private async Task<SKImage?> CaptureBoardSnapshotAsync(Grid gameBoard)
    {
        try
        {
            var bitmap = await _screenCaptureService.CaptureBitmapAsync(gameBoard);
            if (bitmap is null)
                return null;

            return ScreenCaptureService.ConvertToOwnedImage(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private async Task<SKImage?> CaptureTileSnapshotAsync(View tileView)
    {
        try
        {
            if (tileView is not VisualElement ve)
                return null;

            var bitmap = await _screenCaptureService.CaptureBitmapAsync(ve);
            if (bitmap is null)
                return null;

            return ScreenCaptureService.ConvertToOwnedImage(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static SKPoint GetTileCenterInOverlay(View tileView, View overlayView)
    {
        if (tileView is not VisualElement tileVe)
            return SKPoint.Empty;

        if (overlayView is not VisualElement overlayVe)
            return SKPoint.Empty;

        var tileAbs = GetAbsoluteTopLeft(tileVe);
        var overlayAbs = GetAbsoluteTopLeft(overlayVe);

        float centerX = (float)(tileAbs.X - overlayAbs.X + tileVe.Width / 2);
        float centerY = (float)(tileAbs.Y - overlayAbs.Y + tileVe.Height / 2);

        return new SKPoint(centerX, centerY);
    }

    private static Point GetAbsoluteTopLeft(VisualElement element)
    {
        double x = 0;
        double y = 0;

        VisualElement? current = element;
        while (current is not null)
        {
            x += current.X;
            y += current.Y;

            current = current.Parent as VisualElement;
        }

        return new Point(x, y);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "VictoryAnimationService: Board references not set"
    )]
    private static partial void LogMissingReferences(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "VictoryAnimationService: Winning tile not found at ({Row}, {Column})"
    )]
    private static partial void LogTileNotFound(ILogger logger, int row, int column);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "VictoryAnimationService: Failed to capture snapshot"
    )]
    private static partial void LogSnapshotFailed(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Victory animation error")]
    private static partial void LogVictoryAnimationError(ILogger logger, Exception ex);
}
