using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.CCCVars;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Content.Shared.Chat;
using System.Linq;
using Robust.Shared.Audio.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Client.Corvax.TTS;

// RuCM TTS
// CMU14 class: TTS voice selection, ordered delivery and playback.
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly MemoryContentRoot ContentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;
    private int _fileIndex;
    private readonly Dictionary<EntityUid, PlayingSound> _playing = new();
    private readonly Dictionary<PlaybackLane, EntityUid> _activeLanes = new();
    private readonly List<EntityUid> _finished = new();
    private readonly List<PlaybackLane> _queuedLanes = new();
    private CMUTTSPlaybackQueue<PlaybackLane, PlayTTSEvent> _queue = default!;

    private enum PlaybackChannel : byte { Local, Radio, Announcement, Preview }
    private readonly record struct PlaybackLane(NetEntity? Speaker, PlaybackChannel Channel);
    private sealed record PlayingSound(AudioStream Stream, bool Whisper, PlaybackLane Lane, uint PlaybackId, NetEntity? Source);

    public override void Initialize()
    {
        base.Initialize();
        _queue = new CMUTTSPlaybackQueue<PlaybackLane, PlayTTSEvent>(() => _timing.RealTime);

        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, ContentRoot);
        }

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<AddReferenceVoiceResponse>(OnReferenceVoiceResult);
        SubscribeNetworkEvent<ReferenceVoiceCatalogResponse>(OnReferenceVoiceCatalog);
        SubscribeNetworkEvent<ReferenceVoiceAccessResponse>(OnReferenceVoiceAccess);
        SubscribeNetworkEvent<DeleteReferenceVoiceResponse>(OnReferenceVoiceDeleteResult);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => StopTTS());
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnVolumeChanged);
        _cfg.OnValueChanged(CCCVars.TTSEnabled, OnEnabledChanged);
    }

    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSEnabled, OnEnabledChanged);
        StopTTS();
        base.Shutdown();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _finished.Clear();
        foreach (var (uid, _) in _playing)
        {
            if (TryComp<AudioComponent>(uid, out var audio) &&
                audio.State != AudioState.Stopped && (!audio.Started || audio.Playing))
                continue;
            _finished.Add(uid);
        }
        foreach (var uid in _finished)
            FinishPlayback(uid, true);
        _queue.CopyKeys(_queuedLanes);
        foreach (var lane in _queuedLanes)
            StartQueuedPlayback(lane);
    }

    private void StopTTS()
    {
        foreach (var uid in _playing.Keys.ToArray())
            FinishPlayback(uid, false);
        _queue.Clear();
        _activeLanes.Clear();

        CleanupRadioEffect();
    }

    private void OnEnabledChanged(bool enabled)
    {
        if (!enabled)
            StopTTS();
    }

    private void FinishPlayback(EntityUid uid, bool played)
    {
        if (!_playing.Remove(uid, out var sound))
            return;
        _audio.Stop(uid);
        sound.Stream.Dispose();
        _activeLanes.Remove(sound.Lane);
        if (sound.PlaybackId != 0)
            RaiseNetworkEvent(new TTSPlaybackFinishedEvent(sound.PlaybackId, sound.Source, played));
    }

    private void OnVolumeChanged(float volume)
    {
        if (!float.IsFinite(volume) || volume <= 0)
        {
            StopTTS();
            return;
        }
        foreach (var (uid, sound) in _playing)
            _audio.SetVolume(uid, SharedAudioSystem.GainToVolume(Math.Clamp(volume, 0f, 1f)) - (sound.Whisper ? 6f : 0f));
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = _cfg.GetCVar(CCCVars.TTSVolume);
        if (!_cfg.GetCVar(CCCVars.TTSEnabled) || !float.IsFinite(volume) || volume <= 0f ||
            ev.Data.Length is < 12 or > 8388608)
            return;

        // Remote radios share a lane; local speech remains independent for each speaker.
        var channel = ev.IsAnnouncement ? PlaybackChannel.Announcement : ev.IsRadio ? PlaybackChannel.Radio :
            ev.SpeakerUid == null ? PlaybackChannel.Preview : PlaybackChannel.Local;
        var lane = new PlaybackLane(channel == PlaybackChannel.Local ? ev.SpeakerUid : null, channel);
        if (channel == PlaybackChannel.Preview)
        {
            _queue.Clear(lane);
            if (_activeLanes.TryGetValue(lane, out var previous))
                FinishPlayback(previous, false);
        }
        if (_queue.Enqueue(lane, ev, ev.Data.Length))
            StartQueuedPlayback(lane);
    }

    private void StartQueuedPlayback(PlaybackLane lane)
    {
        if (_activeLanes.ContainsKey(lane) || _playing.Count >= 32)
            return;
        while (_queue.TryDequeue(lane, out var ev))
        {
            if (PlayTTS(ev, lane))
                return;
        }
    }

    private bool PlayTTS(PlayTTSEvent ev, PlaybackLane lane)
    {
        var volume = _cfg.GetCVar(CCCVars.TTSVolume);
        if (!_cfg.GetCVar(CCCVars.TTSEnabled) || !float.IsFinite(volume) || volume <= 0f)
            return false;

        volume = Math.Clamp(volume, 0f, 1f);
        EntityUid? source = null;
        // Resolve spatial sources before WAV decoding and audio-buffer allocation.
        if (ev.SourceUid is { } netSource && !ev.IsRadio &&
            (!TryGetEntity(netSource, out source) || TerminatingOrDeleted(source)))
            return false;

        var filePath = new ResPath($"{_fileIndex++}.wav");

        ContentRoot.AddOrUpdateFile(filePath, ev.Data);
        AudioStream? stream = null;

        try
        {
            var audioResource = new AudioResource();
            audioResource.Load(
                IoCManager.Instance!,
                Prefix / filePath);
            stream = audioResource.AudioStream;

            var soundSpecifier =
                new ResolvedPathSpecifier(Prefix / filePath);

            var audioParams = AudioParams.Default
                .WithVolume(SharedAudioSystem.GainToVolume(volume) - (ev.IsWhisper ? 6f : 0f))
                .WithMaxDistance(ev.IsWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange);

            if (source is { } sourceUid)
            {
                var playback = _audio.PlayEntity(
                    audioResource.AudioStream,
                    sourceUid,
                    soundSpecifier,
                    audioParams);

                if (playback is { } played)
                {
                    TrackPlayback(played.Entity, stream, ev, lane);
                    stream = null;
                    return true;
                }

                return false;
            }

            var globalPlayback = _audio.PlayGlobal(
                audioResource.AudioStream,
                soundSpecifier,
                ev.IsRadio
                    ? audioParams.WithPitchScale(0.99f)
                    : audioParams);

            if (globalPlayback is { } global)
            {
                if (ev.IsRadio)
                    ApplyRadioEffect(global);

                TrackPlayback(global.Entity, stream, ev, lane);
                stream = null;
                return true;
            }
        }
        catch (Exception e)
        {
            Logger.Warning($"Could not play TTS audio: {e.Message}");
        }
        finally
        {
            stream?.Dispose();
            ContentRoot.RemoveFile(filePath);
        }
        return false;
    }

    private void TrackPlayback(EntityUid uid, AudioStream stream, PlayTTSEvent ev, PlaybackLane lane)
    {
        _playing.Add(uid, new PlayingSound(stream, ev.IsWhisper, lane, ev.PlaybackId, ev.SourceUid));
        _activeLanes.Add(lane, uid);
    }
}
