# Read-aloud voice

Good Movies uses `AVSpeechSynthesizer` on-device so synopsis speech does not require a
network service or subscription. The synopsis remains one unmodified utterance. This lets
iOS use its punctuation for phrasing while preserving the exact character offsets supplied
by `WillSpeakRangeOfSpeechString` for word highlighting.

## Voice selection policy

On the first speech request after launch, Good Movies ranks the English voices currently
installed on the device:

1. Premium quality, then Enhanced quality, then Default quality.
2. U.S. English (`en-US`) within the same quality tier.
3. Apple's language-default voice within the same quality and locale.
4. Ava, Samantha, Zoe, Allison, Susan, then Tom when otherwise tied.
5. Voice identifier in ordinal order as a final deterministic tie-breaker.

Novelty and Personal Voice entries are excluded. If no eligible installed English voice is
reported, the app asks iOS for its built-in `en-US` voice.

Read-aloud uses a slightly slower-than-default rate, the voice's natural pitch, and short
start and finish pauses. A tapped word is spoken a little more slowly and without an added
delay.

Good Movies sets `PrefersAssistiveTechnologySettings` on every utterance. If an assistive
technology such as VoiceOver is active, the user's selected assistive voice, rate, and other
speech settings take precedence. If no assistive technology is active, iOS uses the
Premium/Enhanced voice and child-friendly delivery selected by Good Movies.

## Siri limitation

Third-party apps cannot select the private voice Siri uses. `AVSpeechSynthesizer` can use
only voices exposed by the public `AVSpeechSynthesisVoice.GetSpeechVoices()` API. Apple
defines Premium as its highest public quality tier, but the user must download a Premium or
Enhanced voice before an app can select it.

`PrefersAssistiveTechnologySettings` is not a general way to import an inactive Spoken
Content voice. Apple's SDK header states that the override applies only while assistive
technology is on and that querying the utterance does not reveal the effective user
settings. Good Movies enables the flag to respect active accessibility preferences, not as
a substitute for installing a public Premium or Enhanced voice.

The iOS 26 simulator's clean voice inventory confirms the practical fallback: it exposes
Samantha as `super-compact` Default quality, plus legacy and novelty voices, with no
Enhanced or Premium English package. That configuration will sound synthetic until a
higher-quality voice is downloaded or selected in accessibility settings.

## Installing a more natural voice

Audio quality cannot be measured reliably by automated tests. For the most natural
on-device result, download an Enhanced Quality or Premium English voice:

1. Open **Settings > Accessibility > Spoken Content > Voices**.
2. Choose **English**, then an English voice, preferably U.S. English.
3. Download and select its Enhanced Quality or Premium variant.
4. Wait for the download to finish.
5. Quit and reopen Good Movies so it discovers the installed voice.

If VoiceOver is used, select the downloaded voice in
**Settings > Accessibility > VoiceOver > Speech** as well.

Apple notes that enhanced voices can be 100 MB or larger and may require Wi-Fi.

Apple references:

- [AVSpeechSynthesisVoice](https://developer.apple.com/documentation/avfaudio/avspeechsynthesisvoice)
- [Premium voice quality](https://developer.apple.com/documentation/avfaudio/avspeechsynthesisvoicequality/premium)
- [Assistive technology precedence](https://developer.apple.com/documentation/avfaudio/avspeechutterance/prefersassistivetechnologysettings)
- [Download voices for Spoken Content](https://support.apple.com/en-us/111798)
