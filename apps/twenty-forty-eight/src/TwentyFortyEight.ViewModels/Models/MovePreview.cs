using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Models;

/// <summary>
/// Represents a non-committing move preview used to drive scrubbable swipe animations.
/// </summary>
public sealed class MovePreview
{
    public required Direction Direction { get; init; }
    public required IReadOnlyList<TileMovement> TileMovements { get; init; }
}
