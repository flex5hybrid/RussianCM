using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RuCM.Balance;

/// <summary>Recent traumatic limb loss, retained when a different limb is injured or replaced.</summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RepeatSeveranceComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastSeverance;
}
