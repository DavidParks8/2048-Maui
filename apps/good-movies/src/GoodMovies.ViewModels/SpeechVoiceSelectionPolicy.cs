namespace GoodMovies.ViewModels;

internal readonly record struct SpeechVoiceCandidate(
    string Identifier,
    string Name,
    string Language,
    int Quality,
    bool IsLanguageDefault = false,
    bool IsNoveltyVoice = false,
    bool IsPersonalVoice = false
);

internal static class SpeechVoiceSelectionPolicy
{
    internal const string PreferredLanguage = "en-US";

    private static readonly string[] PreferredVoiceNames =
    [
        "Ava",
        "Samantha",
        "Zoe",
        "Allison",
        "Susan",
        "Tom",
    ];

    internal static int SelectBestIndex(IReadOnlyList<SpeechVoiceCandidate> voices)
    {
        int bestIndex = -1;
        for (int index = 0; index < voices.Count; index++)
        {
            SpeechVoiceCandidate candidate = voices[index];
            if (!IsEligible(candidate))
            {
                continue;
            }

            if (bestIndex < 0 || Compare(candidate, voices[bestIndex]) > 0)
            {
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static bool IsEligible(SpeechVoiceCandidate candidate) =>
        candidate.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
        && (candidate.Language.Length == 2 || candidate.Language[2] is '-' or '_')
        && !candidate.IsNoveltyVoice
        && !candidate.IsPersonalVoice;

    private static int Compare(SpeechVoiceCandidate left, SpeechVoiceCandidate right)
    {
        int comparison = left.Quality.CompareTo(right.Quality);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = IsPreferredLanguage(left.Language)
            .CompareTo(IsPreferredLanguage(right.Language));
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.IsLanguageDefault.CompareTo(right.IsLanguageDefault);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = GetPreferredNameRank(right.Name).CompareTo(GetPreferredNameRank(left.Name));
        if (comparison != 0)
        {
            return comparison;
        }

        return -StringComparer.Ordinal.Compare(left.Identifier, right.Identifier);
    }

    private static bool IsPreferredLanguage(string language) =>
        string.Equals(language, PreferredLanguage, StringComparison.OrdinalIgnoreCase);

    private static int GetPreferredNameRank(string name)
    {
        for (int index = 0; index < PreferredVoiceNames.Length; index++)
        {
            if (string.Equals(name, PreferredVoiceNames[index], StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return PreferredVoiceNames.Length;
    }
}
