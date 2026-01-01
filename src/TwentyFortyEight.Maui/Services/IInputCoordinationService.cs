using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for coordinating input from multiple sources (keyboard, gamepad, scroll).
/// Centralizes input blocking logic for overlays and modals.
/// </summary>
public interface IInputCoordinationService
{
    /// <summary>
    /// Gets or sets whether gameplay input should be blocked.
    /// Set to true when overlays or modals are visible.
    /// </summary>
    bool IsInputBlocked { get; set; }

    /// <summary>
    /// Raised when a direction input is received from any source (keyboard, gamepad, scroll).
    /// </summary>
    event EventHandler<Direction>? DirectionInputReceived;

    /// <summary>
    /// Registers input behaviors with the page and subscribes to their events.
    /// </summary>
    /// <param name="page">The page to attach behaviors to.</param>
    void RegisterBehaviors(ContentPage page);

    /// <summary>
    /// Unregisters input behaviors from the page and unsubscribes from their events.
    /// </summary>
    /// <param name="page">The page to detach behaviors from.</param>
    void UnregisterBehaviors(ContentPage page);
}
