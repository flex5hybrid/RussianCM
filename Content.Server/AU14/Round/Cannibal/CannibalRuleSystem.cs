using Content.Server.GameTicking.Rules;

namespace Content.Server.AU14.Round.Cannibal;

public sealed class CannibalRuleSystem : GameRuleSystem<CannibalRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CannibalComponent, ComponentStartup>(OnCannibalSpawned);
    }

    private void OnCannibalSpawned(EntityUid uid, CannibalComponent component, ComponentStartup args)
    {
    }
}
