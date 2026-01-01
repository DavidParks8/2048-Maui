using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.Maui.Victory;
using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.Maui.Helpers;

/// <summary>
/// Orchestrates victory animation by coordinating snapshot capture,
/// overlay animations, and modal display.
/// </summary>
public partial class VictoryAnimationOrchestrator(
    IScreenCaptureService screenCaptureService,
    IReduceMotionService reduceMotionService,
    ILogger<VictoryAnimationOrchestrator> logger
    )
{
    private readonly ILogger _logger = logger;
    private CinematicOverlayView? _cinematicOverlay;
    private VictoryModalOverlay? _victoryModal;
    private int _pendingVictoryScore;

    /// <summary>
    /// Initialize the orchestrator with overlay instances from MainPage's XAML.
    /// Must be called before HandleVictoryAsync.
    /// </summary>
    public void Initialize(CinematicOverlayView cinematicOverlay, VictoryModalOverlay victoryModal)
    {
        _cinematicOverlay = cinematicOverlay;
        _victoryModal = victoryModal;

        _victoryModal.Initialize(_cinematicOverlay);

        // Wire up overlay to modal transition
        _cinematicOverlay.ShowModalRequested += OnCinematicShowModalRequested;
    }

    /// <summary>
    /// Handles victory animation request from the game engine.
    /// </summary>
    public async Task HandleVictoryAsync(
        VictoryEventArgs victoryArgs,
        int score,
        IReadOnlyList<TileViewModel> tiles,
        IReadOnlyDictionary<TileViewModel, Border> tileBorders,
        Grid gameBoard
    )
    {
        if (_cinematicOverlay is null || _victoryModal is null)
            throw new InvalidOperationException(
                "Orchestrator not initialized. Call Initialize() first."
            );

        // Check reduce motion preference
        if (reduceMotionService.ShouldReduceMotion())
        {
            // Skip animation, show modal immediately
            await _victoryModal.ShowAsync(score);
            return;
        }

        // Cache score for when the overlay transitions to the modal.
        _pendingVictoryScore = score;

        // Get the winning tile's UI element using row/column from the event.
        // Do NOT assume `tiles` is ordered row-major.
        var winningTileVm = tiles.FirstOrDefault(t =>
            t.Row == victoryArgs.WinningTileRow && t.Column == victoryArgs.WinningTileColumn
        );
        if (winningTileVm is null)
        {
            await _victoryModal.ShowAsync(score);
            return;
        }
        if (!tileBorders.TryGetValue(winningTileVm, out var tileView))
        {
            // Fallback: show modal without animation
            await _victoryModal.ShowAsync(score);
            return;
        }

        try
        {
            // Capture snapshots
            var boardSnapshot = await CaptureBoardSnapshotAsync(gameBoard);
            var tileSnapshot = await CaptureTileSnapshotAsync(tileView);
            var tileCenter = GetTileCenterInOverlay(tileView, _cinematicOverlay);
            SKSize tileSize = new((float)tileView.Width, (float)tileView.Height);

            var tileBg = winningTileVm.BackgroundColor;

            if (boardSnapshot == null || tileSnapshot == null)
            {
                // Fallback: show modal without animation
                await _victoryModal.ShowAsync(score);
                return;
            }

            // Start cinematic animation
            _cinematicOverlay.StartAnimation(
                boardSnapshot,
                tileSnapshot,
                tileCenter,
                tileSize
            );
        }
        catch (Exception ex)
        {
            LogVictoryAnimationError(_logger, ex);
            await _victoryModal.ShowAsync(score);
        }
    }

    private async Task<SKImage?> CaptureBoardSnapshotAsync(Grid gameBoard)
    {
        try
        {
            var bitmap = await screenCaptureService.CaptureBitmapAsync(gameBoard);
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

            var bitmap = await screenCaptureService.CaptureBitmapAsync(ve);
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
        // tileView is a child of GameBoard, while the overlay is a sibling of GameBoard.
        // Convert to overlay coordinates by computing absolute top-left positions.
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

    private async void OnCinematicShowModalRequested(object? sender, EventArgs e)
    {
        if (_cinematicOverlay is null || _victoryModal is null)
            return;

        _cinematicOverlay.EnterSustainMode();

        // Use the score provided by the HandleVictoryAsync call that started this animation.
        await _victoryModal.ShowAsync(_pendingVictoryScore);
    }

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Victory animation error")]
    private static partial void LogVictoryAnimationError(ILogger logger, Exception ex);
}
