using Content.Shared.Chat;
using System.Linq;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Preferences;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking;

namespace Content.Server.Corvax.TTS;

// RuCM TTS
// CMU14 class: TTS voice selection, ordered delivery and playback.
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    private uint _roundGeneration;
    private static readonly TimeSpan MaxSpeechAge = TimeSpan.FromSeconds(15);

    private bool _isEnabled;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ICommonSession, PreviewLimit> _previewLimits = new();

    private sealed class PreviewLimit
    {
        public TimeSpan ResetAt;
        public int Count;
        public bool Pending;
    }

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged,
            true);

        SubscribeLocalEvent<EntitySpokeEvent>(OnSpoke);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        InitializeVoices();
        InitializeReferenceVoices();
        InitializeChannels();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            _roundGeneration++;
            _speechQueue.Reset();
            ResetDeliveryOrder();
            _radioDeliveries.Clear();
            foreach (var user in _referenceVoiceCooldowns.Keys.ToArray())
            {
                if (_referenceVoiceCooldowns[user] <= _timing.CurTime)
                    _referenceVoiceCooldowns.Remove(user);
            }
        });
    }

    public override void Shutdown()
    {
        _roundGeneration++;
        _isEnabled = false;
        _speechQueue.Reset();
        ResetDeliveryOrder();
        ShutdownReferenceVoices();
        _cfg.UnsubValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged);

        base.Shutdown();
    }

    private void OnTtsEnabledChanged(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled)
        {
            _roundGeneration++;
            _speechQueue?.Reset();
            ResetDeliveryOrder();
        }
    }

    private void ResetDeliveryOrder()
    {
        _deliveryOrder.Reset();
        _radioOrder.Reset();
        _announcementOrder.Reset();
    }

    private void OnSpoke(EntitySpokeEvent args)
    {
        if (TryComp<TTSComponent>(args.VoiceSource ?? args.Source, out var tts))
        {
            EnsureVoiceAssigned((args.VoiceSource ?? args.Source, tts));
            OnEntitySpoke(args.Source, tts, args);
        }
    }

    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        _ = CMUTTSRequestRunner.Run(() => SendLocalTTS(uid, component, args),
            e => Logger.Warning($"TTS local speech failed: {e}"));
    }

    private async System.Threading.Tasks.Task SendLocalTTS(
        EntityUid uid,
        TTSComponent component,
        EntitySpokeEvent args)
    {
        if (!_isEnabled)
            return;

        if (args.Channel != null)
            return;

        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (args.Message.Length > 4000)
            return;

        var roundGeneration = _roundGeneration;
        var requestedAt = _timing.RealTime;
        using var delivery = _deliveryOrder.Reserve(args.VoiceSource ?? uid);

        var voiceId = component.VoicePrototypeId;

        if (string.IsNullOrWhiteSpace(voiceId))
            return;

        var voiceEvent = new TransformSpeakerVoiceEvent(args.VoiceSource ?? uid, voiceId);
        RaiseLocalEvent(args.VoiceSource ?? uid, voiceEvent);
        voiceId = voiceEvent.VoiceId;

        if (CustomTTSVoice.TryGetSpeaker(voiceId, out _))
            await EnsureReferenceVoiceCatalogLoaded();
        if (!_isEnabled || roundGeneration != _roundGeneration || TerminatingOrDeleted(uid))
            return;

        if (!TryResolveSpeaker(voiceId, out var speaker, GetSpeakerSex(args.VoiceSource ?? uid)))
        {
            Logger.Warning(
                $"TTS voice prototype '{voiceId}' was not found.");

            return;
        }

        var text = Sanitize(args.Message);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (TerminatingOrDeleted(uid))
            return;
        var whisper = args.ObfuscatedMessage != null;
        var recipients = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, whisper, ignoreXenos: args.IgnoreXenos);
        var muffledRecipients = whisper
            ? _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, true, true, args.IgnoreXenos)
            : new Dictionary<ICommonSession, EntityUid>();
        if (recipients.Count == 0 && muffledRecipients.Count == 0)
            return;

        var soundTask = recipients.Count > 0 ? GenerateSpeech(speaker, text) : System.Threading.Tasks.Task.FromResult<byte[]?>(null);
        var muffledTask = muffledRecipients.Count > 0 ? GenerateSpeech(speaker, args.ObfuscatedMessage!) : System.Threading.Tasks.Task.FromResult<byte[]?>(null);
        var soundData = await soundTask;
        var muffledData = await muffledTask;
        await delivery.Previous;

        if (!_isEnabled ||
            roundGeneration != _roundGeneration || _timing.RealTime - requestedAt >= MaxSpeechAge ||
            !Exists(uid) || TerminatingOrDeleted(uid))
            return;

        var currentRecipients = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, whisper, ignoreXenos: args.IgnoreXenos);
        foreach (var (session, listener) in recipients)
        {
            if (soundData is { Length: > 0 } && currentRecipients.TryGetValue(session, out var currentListener) && currentListener == listener)
                RaiseNetworkEvent(LocalPlayback(soundData, uid, listener, whisper), session);
        }
        if (muffledData is { Length: > 0 })
        {
            var currentMuffled = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, true, true, args.IgnoreXenos);
            foreach (var (session, listener) in muffledRecipients)
            {
                if (currentMuffled.TryGetValue(session, out var currentListener) && currentListener == listener)
                    RaiseNetworkEvent(LocalPlayback(muffledData, uid, listener, true), session);
            }
        }
    }

    private void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        _ = CMUTTSRequestRunner.Run(() => SendPreviewTTS(ev, args),
            e => Logger.Warning($"TTS preview failed: {e}"));
    }

    private async System.Threading.Tasks.Task SendPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled || string.IsNullOrWhiteSpace(ev.VoiceId) || ev.VoiceId.Length > 128)
            return;

        var generation = _roundGeneration;
        var requestedAt = _timing.RealTime;
        var limit = _previewLimits.GetValue(args.SenderSession, _ => new PreviewLimit());
        if (limit.Pending)
            return;
        if (_timing.CurTime >= limit.ResetAt)
        {
            limit.ResetAt = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(0.1f, _cfg.GetCVar(CCCVars.TTSRateLimitPeriod)));
            limit.Count = 0;
        }
        if (limit.Count >= _cfg.GetCVar(CCCVars.TTSRateLimitCount))
            return;

        limit.Count++;
        limit.Pending = true;
        try
        {
            if (CustomTTSVoice.TryGetSpeaker(ev.VoiceId, out _))
                await EnsureReferenceVoiceCatalogLoaded();
            else if (!_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var voice) ||
                     !HumanoidCharacterProfile.IsSelectableTTSVoice(voice))
                return;
            if (!_isEnabled || generation != _roundGeneration)
                return;
            if (!TryResolveSpeaker(ev.VoiceId, out var speaker))
                return;
            var data = await GenerateSpeech(speaker,
                Loc.GetString("tts-preview-text"));
            if (_isEnabled && generation == _roundGeneration && _timing.RealTime - requestedAt < MaxSpeechAge &&
                data is { Length: > 0 } &&
                args.SenderSession.Status == Robust.Shared.Enums.SessionStatus.InGame)
                RaiseNetworkEvent(new PlayTTSEvent(data), args.SenderSession);
        }
        finally
        {
            limit.Pending = false;
        }
    }

    private static string Sanitize(string text)
    {
        var clean = FormattedMessage.RemoveMarkupPermissive(text);

        // Символы, которые TTS не должен пытаться произносить.
        ReadOnlySpan<char> ignored = [
            '~',
        '`',
        '^',
        '|',
        '\\',
        '_',
        '*'
        ];

        var result = new System.Text.StringBuilder(clean.Length);
        var lastWasSpace = false;

        foreach (var c in clean)
        {
            if (char.IsControl(c) ||
                char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.PrivateUse ||
                c is '<' or '>' ||
                ignored.Contains(c))
            {
                // Не склеиваем слова: "привет~мир" -> "привет мир".
                if (!lastWasSpace && result.Length > 0)
                {
                    result.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && result.Length > 0)
                {
                    result.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            result.Append(c);
            lastWasSpace = false;
        }

        return result.ToString().Trim();
    }
}
