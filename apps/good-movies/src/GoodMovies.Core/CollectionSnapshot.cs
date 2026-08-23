namespace GoodMovies.Core;

internal static class CollectionSnapshot
{
    public static IReadOnlyList<T> Create<T>(IEnumerable<T>? items)
    {
        T[] copy = items?.ToArray() ?? Array.Empty<T>();
        return copy.Length == 0 ? Array.Empty<T>() : Array.AsReadOnly(copy);
    }
}
