using Content.Shared.Imperial.Medieval.Magic;
using Robust.Server.Audio;


namespace Content.Server.Imperial.Medieval.Magic.PlayAudioAfterSpellCast;


public sealed partial class PlayAudioAfterSpellCastSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayAudioAfterSpellCastComponent, MedievalAfterCastSpellEvent>(OnCast);
    }

    private void OnCast(EntityUid uid, PlayAudioAfterSpellCastComponent component, MedievalAfterCastSpellEvent args)
    {
        _audioSystem.PlayPvs(component.Sound, args.Performer);
    }
}
