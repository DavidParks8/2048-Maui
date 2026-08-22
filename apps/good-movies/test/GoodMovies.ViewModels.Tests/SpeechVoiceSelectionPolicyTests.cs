using GoodMovies.ViewModels;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class SpeechVoiceSelectionPolicyTests
{
    [TestMethod]
    public void SelectBestIndex_PrefersPremiumThenEnhancedEnglishVoice()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("default", "Samantha", "en-US", quality: 1, isLanguageDefault: true),
            Voice("enhanced", "Ava", "en-US", quality: 2),
            Voice("premium", "Zoe", "en-US", quality: 3),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(2, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_PrefersEnhancedEnglishOverDefaultUsEnglish()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("default-us", "Samantha", "en-US", quality: 1, isLanguageDefault: true),
            Voice("enhanced-gb", "Serena", "en-GB", quality: 2),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(1, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_RejectsNoveltyPersonalAndNonEnglishVoices()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("novelty", "Robot", "en-US", quality: 3, isNoveltyVoice: true),
            Voice("personal", "Personal", "en-US", quality: 3, isPersonalVoice: true),
            Voice("french", "Thomas", "fr-FR", quality: 3),
            Voice("safe", "Ava", "en-US", quality: 2),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(3, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_UsesStableFallbackWithinSameQuality()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("en-gb-premium", "Serena", "en-GB", quality: 3),
            Voice("en-us-premium-z", "Zoe", "en-US", quality: 3),
            Voice("en-us-premium-a", "Ava", "en-US", quality: 3),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(2, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_PrefersLanguageDefaultForBuiltInFallback()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("other", "Ava", "en-US", quality: 1),
            Voice("language-default", "Samantha", "en-US", quality: 1, isLanguageDefault: true),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(1, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_FinalTieBreakUsesOrdinalIdentifier()
    {
        SpeechVoiceCandidate[] voices =
        [
            Voice("voice-z", "Other", "en-US", quality: 1),
            Voice("voice-a", "Other", "en-US", quality: 1),
        ];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(1, selectedIndex);
    }

    [TestMethod]
    public void SelectBestIndex_NoEligibleEnglishVoice_ReturnsMinusOne()
    {
        SpeechVoiceCandidate[] voices = [Voice("spanish", "Monica", "es-ES", quality: 3)];

        int selectedIndex = SpeechVoiceSelectionPolicy.SelectBestIndex(voices);

        Assert.AreEqual(-1, selectedIndex);
    }

    private static SpeechVoiceCandidate Voice(
        string identifier,
        string name,
        string language,
        int quality,
        bool isLanguageDefault = false,
        bool isNoveltyVoice = false,
        bool isPersonalVoice = false
    ) =>
        new(
            identifier,
            name,
            language,
            quality,
            isLanguageDefault,
            isNoveltyVoice,
            isPersonalVoice
        );
}
