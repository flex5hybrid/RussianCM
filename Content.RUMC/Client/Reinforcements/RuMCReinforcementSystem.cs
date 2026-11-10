using Content.Shared._RuMC14.Reinforcements;

namespace Content.Client._RuMC14.Reinforcements;

public sealed class RuMCReinforcementSystem : SharedRuMCReinforcementSystem
{
    // Stub for the shared dependency; server only
    public override void Request(EntityUid actor, string side, Action<RuMCReinforcementCall> onSubmit)
    {
    }
}
