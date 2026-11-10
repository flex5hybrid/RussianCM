using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Reinforcements;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RuMCReinforcementHolopadComponent : Component
{
    [DataField(required: true)]
    public EntProtoId HologramProto;

    [DataField]
    public EntProtoId SparksProto = "EffectSparks";

    [DataField]
    public SoundSpecifier VanishSound = new SoundCollectionSpecifier("sparks");

    [DataField]
    public TimeSpan StepDelay = TimeSpan.FromSeconds(2);

    public RuMCReinforcementStep Step;

    // Null until unfolded
    [AutoPausedField]
    public TimeSpan? NextStepAt;

    public EntityUid? Hologram;
}

public enum RuMCReinforcementStep : byte
{
    Greeting,
    Message,
    Vanish,
}
