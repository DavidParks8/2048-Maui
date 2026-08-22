namespace GoodMovies.Core;

public static class YouTubeVideoKey
{
    public const int Length = 11;

    public static bool IsValid(string? key)
    {
        if (key is null || key.Length != Length)
        {
            return false;
        }

        foreach (char character in key)
        {
            if (
                !(
                    character
                    is >= 'a'
                        and <= 'z'
                        or >= 'A'
                        and <= 'Z'
                        or >= '0'
                        and <= '9'
                        or '-'
                        or '_'
                )
            )
            {
                return false;
            }
        }

        return true;
    }
}
