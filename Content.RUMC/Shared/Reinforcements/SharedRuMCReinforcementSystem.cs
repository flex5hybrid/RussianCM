namespace Content.Shared._RuMC14.Reinforcements;

public abstract class SharedRuMCReinforcementSystem : EntitySystem
{
    public const int MessageLimit = 1000;

    // onSubmit must re-check the call: the console may change while the window is open
    public abstract void Request(EntityUid actor, string side, Action<RuMCReinforcementCall> onSubmit);
}
