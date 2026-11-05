using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Reinforcements;

[RegisterComponent]
public sealed partial class RuMCReinforcementSideComponent : Component
{
    [DataField(required: true)]
    public LocId Side;
}
