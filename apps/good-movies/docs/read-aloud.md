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

Read-aloud uses a slightly slower-than-default rate, a near-natural pitch, and short start
and finish pauses. A tapped word is spoken a little more slowly and without an added delay.
Rate and pitch are set by the app rather than inherited from VoiceOver or Speak Screen so
reading practice remains predictable.

## Installing a more natural voice

Audio quality cannot be measured reliably by automated tests. For the most natural
on-device result, download an Enhanced Quality or Premium English voice:

1. Open **Settings > Accessibility > Spoken Content > Voices**.
2. Choose **English** and an English voice, preferably U.S. English.
3. Download its Enhanced Quality or Premium variant.
4. Quit and reopen Good Movies so it refreshes the installed voice list.

Apple notes that enhanced voices can be 100 MB or larger and may require Wi-Fi.
