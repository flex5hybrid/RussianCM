using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Audio;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedGridAmbienceSystem))]
public sealed partial class GridAmbienceComponent : Component
{
    [DataField("enabled", readOnly: true)]
    public bool Enabled { get; set; } = true;

    [DataField("sound", required: true)]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RuMC14/Ambience/Almayer/almayerambience.ogg");

    [DataField("volume")]
    public float Volume = 0f;
}

[Serializable, NetSerializable]
public sealed class GridAmbienceComponentState : ComponentState
{
    public bool Enabled { get; init; }
    public SoundSpecifier Sound { get; init; } = default!;
    public float Volume { get; init; }
}
