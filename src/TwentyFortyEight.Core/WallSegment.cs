using System.Text.Json.Serialization;

namespace TwentyFortyEight.Core;

/// <summary>
/// Orientation of a between-cell wall segment.
/// </summary>
public enum WallOrientation
{
    Horizontal = 0,
    Vertical = 1,
}

/// <summary>
/// Represents a contiguous wall segment that exists between adjacent cells.
/// </summary>
/// <remarks>
/// A wall does not occupy cells; it blocks adjacency across a divider.
///
/// For <see cref="WallOrientation.Vertical"/>, <see cref="Divider"/> is the column divider between
/// <c>col = Divider</c> and <c>col = Divider + 1</c>, and <see cref="Start"/> / <see cref="Length"/>
/// are measured in rows.
///
/// For <see cref="WallOrientation.Horizontal"/>, <see cref="Divider"/> is the row divider between
/// <c>row = Divider</c> and <c>row = Divider + 1</c>, and <see cref="Start"/> / <see cref="Length"/>
/// are measured in columns.
/// </remarks>
public sealed record WallSegment
{
    [JsonConstructor]
    public WallSegment(WallOrientation orientation, int divider, int start, int length)
    {
        if (!Enum.IsDefined(typeof(WallOrientation), orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(orientation));
        }

        if (divider < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divider));
        }

        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        Orientation = orientation;
        Divider = divider;
        Start = start;
        Length = length;
    }

    public WallOrientation Orientation { get; init; }

    public int Divider { get; init; }

    public int Start { get; init; }

    public int Length { get; init; }

    public bool IsValidForSize(int size)
    {
        if (size < 2)
        {
            return false;
        }

        if (Divider < 0 || Divider >= size - 1)
        {
            return false;
        }

        if (Start < 0 || Start >= size)
        {
            return false;
        }

        if (Length <= 0 || Start + Length > size)
        {
            return false;
        }

        return true;
    }
}
