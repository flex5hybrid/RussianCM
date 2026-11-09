using Content.Shared.CMU14.Hearing;
using Content.Shared.CMU14.HumanAnimation;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.CMU14.HumanAnimation;

public sealed partial class CMUHumanActionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidProfileComponent, UserInteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<HumanoidProfileComponent, UserInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HumanoidProfileComponent, MeleeAttackEvent>(OnMelee);
        SubscribeLocalEvent<CMUGunFiredEvent>(OnShoot);
    }

    private void OnInteractHand(Entity<HumanoidProfileComponent> ent, ref UserInteractHandEvent args)
        => Send(ent, CMUHumanAction.Reach);

    private void OnInteractUsing(Entity<HumanoidProfileComponent> ent, ref UserInteractUsingEvent args)
        => Send(ent, CMUHumanAction.Reach);

    private void OnMelee(Entity<HumanoidProfileComponent> ent, ref MeleeAttackEvent args)
        => Send(ent, CMUHumanAction.Melee);

    private void OnShoot(ref CMUGunFiredEvent args)
    {
        if (TryComp<HumanoidProfileComponent>(args.User, out var profile))
            Send((args.User, profile), CMUHumanAction.Shoot);
    }

    private void Send(Entity<HumanoidProfileComponent> ent, CMUHumanAction action)
    {
        if (ent.Comp.Species.Id == "Human")
            RaiseNetworkEvent(new CMUHumanActionEvent(GetNetEntity(ent), action),
                Filter.Pvs(ent.Owner, entityManager: EntityManager));
    }
}
