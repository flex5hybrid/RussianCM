using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Robust.Shared.Timing;

namespace Content.Shared.CMU14.Marines.Orders;

public sealed class SharedAU14StandAtAttentionSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AU14StandAtAttentionComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<AU14StandAtAttentionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnUpdateCanMove(Entity<AU14StandAtAttentionComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.ExpiresAt > _gameTiming.CurTime)
            args.Cancel();
    }

    private void OnShutdown(Entity<AU14StandAtAttentionComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            _actionBlocker.UpdateCanMove(ent.Owner);
    }
}
