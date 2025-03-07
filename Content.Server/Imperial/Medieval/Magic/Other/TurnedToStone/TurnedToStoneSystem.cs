using Content.Shared.Imperial.Medieval.Magic.TurnedToStone;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Magic.TurnedToStone;


public sealed partial class TurnedToStoneSystem : SharedTurnedToStoneSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TurnedToStoneComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime <= component.DisposeTime) continue;

            RemComp<TurnedToStoneComponent>(uid);
        }
    }
}
