using Content.Shared.CMU14.Marines.Orders;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.Marines.Orders;

public sealed partial class AU14SilenceOrderSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AU14SilenceOrderComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<AU14SilenceOrderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!IsPaused(uid) && comp.ExpiresAt <= time)
                RemCompDeferred<AU14SilenceOrderComponent>(uid);
        }
    }

    private void OnUnpaused(Entity<AU14SilenceOrderComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.ExpiresAt += args.PausedTime;
        Dirty(ent.Owner, ent.Comp);
    }

    public void ApplySilenceFor(EntityUid target, TimeSpan duration)
        => ApplySilenceUntil(target, _timing.CurTime + duration);

    public void ApplySilenceUntil(EntityUid target, TimeSpan expiresAt)
    {
        var silence = EnsureComp<AU14SilenceOrderComponent>(target);
        if (silence.ExpiresAt >= expiresAt)
            return;

        silence.ExpiresAt = expiresAt;
        Dirty(target, silence);
    }

}
