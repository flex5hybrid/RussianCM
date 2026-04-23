using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Marines;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Radio.EntitySystems;
using Content.Server.Radio.Components;
using Content.Shared.Radio.Components;
using Content.Server.Radio;
using Content.Shared._RMC14.Radio;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;
    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke,
            before: new[] { typeof(RadioSystem), typeof(HeadsetSystem) });

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<ActorComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetRadioReceive);
        SubscribeLocalEvent<TTSComponent, RadioReceiveEvent>(OnIntrinsicRadioReceive);
        SubscribeLocalEvent<RMCAnnouncementMadeEvent>(OnAnnouncementMade);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;
        Logger.Debug("Вот что у нас вышло: " + ev.VoiceId);
        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;

        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null ||
            args.Channel != null)
            return;
        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;
        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        // Обработка шепота
        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker, component.Faction);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, HearingFaction faction)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null) return;
        var recipients = Filter.Pvs(uid).Recipients;

        foreach (var session in recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var listener = session.AttachedEntity.Value;

            if (!TryComp<TTSComponent>(listener, out var listenerTts))
                continue;

            if (listenerTts.Faction != faction)
                continue;

            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), session);
        }
    }
    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker)
    {
        var fullSoundData = await GenerateTTS(message, speaker, true);
        if (fullSoundData is null) return;

        var obfSoundData = await GenerateTTS(obfMessage, speaker, true);
        if (obfSoundData is null) return;

        var fullTtsEvent = new PlayTTSEvent(fullSoundData, GetNetEntity(uid), true);
        var obfTtsEvent = new PlayTTSEvent(obfSoundData, GetNetEntity(uid), true);

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue) continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
                continue;

            RaiseNetworkEvent(distance > ChatSystem.WhisperClearRange ? obfTtsEvent : fullTtsEvent, session);
        }
    }
    private void OnHeadsetRadioReceive(EntityUid uid, ActorComponent actor, ref HeadsetRadioReceiveRelayEvent args)
    {
        _ = HandleRadioTTS(uid, actor, args.RelayedEvent);
    }

    private void OnIntrinsicRadioReceive(EntityUid uid, TTSComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp<ActorComponent>(uid, out var actor))
            _ = HandleRadioTTS(uid, actor, args);
    }

    private async void OnAnnouncementMade(RMCAnnouncementMadeEvent args)
    {
        Logger.Debug("OnAnnouncementMade");
        var voiceId = "TURRET_FLOOR";
        if (TryComp<TTSComponent>(args.Source, out var component))
            voiceId = component.VoicePrototypeId;
        if (voiceId is null)
            voiceId = "TURRET_FLOOR";

        Logger.Debug(voiceId);
        if (!_isEnabled)
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;
        Logger.Debug("get him");
        var soundData = await GenerateTTS(args.RawMessage, protoVoice.Speaker);
        if (soundData is null)
            return;
        Logger.Debug("get his ass");

        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e => HasComp<MarineComponent>(e) || HasComp<GhostComponent>(e));

        foreach (var session in filter.Recipients)
        {
            RaiseNetworkEvent(new PlayTTSEvent(soundData, isRadio: true), session);
        }
    }

    public async Task HandleRadioTTS(
    EntityUid receiver,
    ActorComponent actor,
    RadioReceiveEvent ev)
    {
        if (!_isEnabled)
            return;

        var speaker = ev.MessageSource;

        if (!speaker.IsValid() || !TryComp<TTSComponent>(speaker, out var tts) || tts.VoicePrototypeId == null)
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(tts.VoicePrototypeId, out var protoVoice))
            return;

        var sound = await GenerateTTS(ev.Message, protoVoice.Speaker);

        if (sound == null)
            return;

        RaiseNetworkEvent(
            new PlayTTSEvent(sound, GetNetEntity(speaker), isRadio: true),
            Filter.SinglePlayer(actor.PlayerSession));
    }
    // ReSharper disable once InconsistentNaming
    public async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast;
        if (isWhisper)
            ssmlTraits = SoundTraits.PitchVerylow;
        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }
}
