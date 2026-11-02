using Content.Shared.Corvax.TTS;
using Content.Shared.CMU14.Yautja;
using Content.Shared.Humanoid;
using Robust.Shared.Random;

namespace Content.Server.Corvax.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private void InitializeVoices()
    {
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnVoiceMapInit);
        SubscribeLocalEvent<TTSComponent, CMUTTSVoiceAppliedEvent>(OnVoiceApplied);
    }

    private void OnVoiceMapInit(Entity<TTSComponent> ent, ref MapInitEvent args)
    {
        EnsureVoiceAssigned(ent);
    }

    private void OnVoiceApplied(Entity<TTSComponent> ent, ref CMUTTSVoiceAppliedEvent args)
    {
        EnsureVoiceAssigned(ent);
    }

    private bool IsYautjaSpeaker(EntityUid uid)
    {
        return HasComp<YautjaComponent>(uid)
            || (TryComp<HumanoidProfileComponent>(uid, out var profile)
                && profile.Species.Id == "Yautja");
    }

    private string EnforceYautjaVoice(EntityUid uid, string voiceId)
    {
        if (!IsYautjaSpeaker(uid))
            return voiceId;

        return _prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var voice)
            && CMUTTSVoiceSelection.IsYautjaSelectable(voice)
            ? voiceId
            : CMUTTSVoiceSelection.DefaultYautjaVoice;
    }

    /// <summary>
    /// Restores the selected hunter voice after the ordinary humanoid profile system has applied its sex-based TTS rules.
    /// No changes to shared humanoid profile validation are necessary.
    /// </summary>
    public void ApplyYautjaProfileVoice(EntityUid uid, string? voiceId)
    {
        if (!IsYautjaSpeaker(uid) || !TryComp<TTSComponent>(uid, out var tts))
            return;

        tts.VoicePrototypeId = EnforceYautjaVoice(uid, voiceId ?? string.Empty);
    }

    private void EnsureVoiceAssigned(Entity<TTSComponent> ent)
    {
        if (IsYautjaSpeaker(ent.Owner))
        {
            ent.Comp.VoicePrototypeId = EnforceYautjaVoice(
                ent.Owner,
                ent.Comp.VoicePrototypeId ?? string.Empty);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ent.Comp.VoicePrototypeId)
            && ent.Comp.VoicePrototypeId != CMUTTSVoiceSelection.RandomVoice)
            return;

        var sex = GetSpeakerSex(ent);
        var voices = CMUTTSVoiceSelection.GetRandomVoices(_prototypeManager.EnumeratePrototypes<TTSVoicePrototype>(), sex);
        if (voices.Length > 0)
            ent.Comp.VoicePrototypeId = _random.Pick(voices).ID;
    }

    private Sex GetSpeakerSex(EntityUid? uid)
    {
        return TryComp<HumanoidProfileComponent>(uid, out var profile) ? profile.Sex : Sex.Unsexed;
    }
}
