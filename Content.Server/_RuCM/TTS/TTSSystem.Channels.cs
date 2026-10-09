using System.Linq;
using System.Threading.Tasks;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Corvax.TTS;
using Content.Shared.Preferences;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Corvax.TTS;

// CMU14 class: TTS voice selection, ordered delivery and playback.
public sealed partial class TTSSystem
{
    private PlayTTSEvent LocalPlayback(byte[] data, EntityUid source, EntityUid listener, bool whisper)
    {
        var origin = Transform(source);
        var target = Transform(listener);
        var range = whisper ? Content.Shared.Chat.SharedChatSystem.WhisperMuffledRange : Content.Shared.Chat.SharedChatSystem.VoiceRange;
        // Camera, vehicle, overwatch and other chat relays have already authorized this listener.
        var spatial = origin.MapID == target.MapID &&
                      origin.Coordinates.TryDistance(EntityManager, target.Coordinates, out var distance) && distance < range;
        return new PlayTTSEvent(data, spatial ? GetNetEntity(source) : null, isWhisper: whisper,
            speakerUid: GetNetEntity(source));
    }

    [Dependency] private readonly SharedLanguageSystem _language = default!;
    private CMUTTSSynthesisQueue _speechQueue = default!;
    private readonly CMUTTSDeliveryOrder<EntityUid> _deliveryOrder = new();
    private readonly CMUTTSDeliveryOrder<ICommonSession> _radioOrder = new();
    private readonly CMUTTSDeliveryOrder<bool> _announcementOrder = new();
    private readonly CMUTTSRadioDeliveryCache<(ICommonSession Session, ulong Transmission)> _radioDeliveries = new();

    private void InitializeChannels()
    {
        _speechQueue = new CMUTTSSynthesisQueue(_ttsManager.ConvertTextToSpeech, () => _timing.RealTime,
            onError: e => Logger.Warning($"TTS synthesis failed: {e.Message}"));
        SubscribeLocalEvent<ActorComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetTTS);
        // SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicTTS);
        SubscribeLocalEvent<RMCAnnouncementMadeEvent>(OnAnnouncementTTS);
    }

    private Task<byte[]?> GenerateSpeech(string speaker, string text)
    {
        text = Sanitize(text);
        if (!_isEnabled || string.IsNullOrWhiteSpace(text) || text.Length > 4000)
            return Task.FromResult<byte[]?>(null);
        return _speechQueue.Enqueue(speaker, text);
    }

    private void OnHeadsetTTS(EntityUid uid, ActorComponent actor, ref HeadsetRadioReceiveRelayEvent args)
    {
        _ = SendRadioTTS(uid, actor.PlayerSession, args.RelayedEvent);
    }

    // private void OnIntrinsicTTS(Entity<IntrinsicRadioReceiverComponent> ent, ref RadioReceiveEvent args)
    // {
    //     if (TryComp<ActorComponent>(ent, out var actor))
    //         _ = SendRadioTTS(ent.Owner, actor.PlayerSession, args);
    // }

    private async Task SendRadioTTS(EntityUid listener, ICommonSession session, RadioReceiveEvent args)
    {
        if (!_isEnabled || !_prototypeManager.TryIndex(args.Language, out LanguagePrototype? language) || !language.NeedsSpeech ||
            !_language.CanUnderstand(listener, args.Language) ||
            !TryComp<TTSComponent>(args.MessageSource, out var tts))
            return;

        EnsureVoiceAssigned((args.MessageSource, tts));
        if (tts.VoicePrototypeId == null)
            return;

        if (args.TransmissionId != 0 && !_radioDeliveries.TryAdd((session, args.TransmissionId), _timing.CurTime))
            return;

        var generation = _roundGeneration;
        var requestedAt = _timing.RealTime;
        var speakerUid = GetNetEntity(args.MessageSource);
        using var delivery = _radioOrder.Reserve(session);
        var voice = tts.VoicePrototypeId;
        var voiceEvent = new TransformSpeakerVoiceEvent(args.MessageSource, voice);
        RaiseLocalEvent(args.MessageSource, voiceEvent);
        voice = voiceEvent.VoiceId;
        if (CustomTTSVoice.TryGetSpeaker(voice, out _))
            await EnsureReferenceVoiceCatalogLoaded();
        if (!_isEnabled || generation != _roundGeneration)
            return;
        if (!TryResolveSpeaker(voice, out var speaker, GetSpeakerSex(args.MessageSource)))
            return;
        var data = await GenerateSpeech(speaker, args.Message);
        await delivery.Previous;
        if (data is not { Length: > 0 } || !_isEnabled || generation != _roundGeneration ||
            _timing.RealTime - requestedAt >= MaxSpeechAge ||
            session.Status != SessionStatus.InGame || session.AttachedEntity != listener || TerminatingOrDeleted(listener))
            return;

        // Radio audio has no spatial source: the remote speaker need not be in the client's PVS.
        RaiseNetworkEvent(new PlayTTSEvent(data, isRadio: true, speakerUid: speakerUid), session);
    }

    private async void OnAnnouncementTTS(RMCAnnouncementMadeEvent args)
    {
        if (!_isEnabled || args.Filter == null)
            return;
        var recipients = args.Filter.Recipients
            .Where(s => s.Status == SessionStatus.InGame)
            .Select(s => (Session: s, Entity: s.AttachedEntity)).ToArray();
        if (recipients.Length == 0)
            return;

        var generation = _roundGeneration;
        var requestedAt = _timing.RealTime;
        using var delivery = _announcementOrder.Reserve(true);
        var voice = TryComp<TTSComponent>(args.Source, out var tts) ? tts.VoicePrototypeId : "TURRET_FLOOR";
        if (tts != null)
        {
            EnsureVoiceAssigned((args.Source!.Value, tts));
            voice = tts.VoicePrototypeId;
        }
        voice ??= HumanoidCharacterProfile.DefaultTTSVoice;
        if (CustomTTSVoice.TryGetSpeaker(voice, out _))
            await EnsureReferenceVoiceCatalogLoaded();
        if (!_isEnabled || generation != _roundGeneration)
            return;
        if (!TryResolveSpeaker(voice, out var speaker, GetSpeakerSex(args.Source)))
            return;
        var data = await GenerateSpeech(speaker, args.RawMessage);
        await delivery.Previous;
        if (data is not { Length: > 0 } || !_isEnabled || generation != _roundGeneration ||
            _timing.RealTime - requestedAt >= MaxSpeechAge)
            return;
        foreach (var (session, entity) in recipients)
        {
            if (session.Status == SessionStatus.InGame && session.AttachedEntity == entity)
                RaiseNetworkEvent(new PlayTTSEvent(data, isAnnouncement: true), session);
        }
    }
}
