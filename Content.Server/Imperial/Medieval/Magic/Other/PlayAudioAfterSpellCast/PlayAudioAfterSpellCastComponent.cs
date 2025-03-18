using Robust.Shared.Audio;

namespace Content.Server.Imperial.Medieval.Magic.PlayAudioAfterSpellCast;


[RegisterComponent]
public sealed partial class PlayAudioAfterSpellCastComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound;
}
