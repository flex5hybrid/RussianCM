## TTS / radio / announcements

- `Content.Server/_RuMC14/TTS/TTSSystem.cs` already sends radio `PlayTTSEvent` per recipient session during `HandleRadioTTS`; the client-side `isRadio` playback is local to that session.
- The main leak was announcement TTS: `RMCAnnouncementMadeEvent` previously carried only source + raw text, and `OnAnnouncementMade` rebuilt a broad marine/ghost filter, so radio-style announcements and faction-limited announcements were over-broadcast.
- `MarineAnnounceSystem.AnnounceRadio()` was also raising `RMCAnnouncementMadeEvent` in addition to `_radio.SendRadioMessage(...)`, which duplicated radio TTS through the broad announcement path.
- `Content.Server/_RuMC14/TTS/TTSManager.cs` had a cache race: parallel requests for identical SSML could both miss `_cache.TryGetValue()` and then both call `_cache.Add(...)` after `await`, throwing `ArgumentException` for duplicate keys.
- Ghost TTS is delivered from `Content.Server/_RuMC14/TTS/TTSSystem.cs`: local say/whisper, radio, and sourced announcements send only to ghosts near the speaker/source. Radio reuses existing `RadioReceiveEvent` delivery with a short `ChatMsg`-based dedupe so ghosts hear one copy instead of one copy per radio receiver.
- `dotnet build SpaceStation14.sln -c Debug -v minimal` currently may exit with code `1` after printing no project errors because `Microsoft.DotNet.Installer.Windows.TimestampedFileLogger` throws `ObjectDisposedException` on process exit.
