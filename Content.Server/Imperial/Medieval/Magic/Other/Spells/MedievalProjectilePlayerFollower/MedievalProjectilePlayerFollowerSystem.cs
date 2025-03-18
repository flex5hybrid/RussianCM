using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Shared.Imperial.Medieval.Magic;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Magic.MedievalProjectilePlayerFollower;


public sealed partial class MedievalProjectilePlayerFollowerSystem : EntitySystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<MedievalProjectilePlayerFollowerComponent, MedievalAfterSpawnEntityBySpellEvent>(OnStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<MedievalProjectilePlayerFollowerComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (component.Binded) continue;
            if (_timing.CurTime <= component.NextTargetSelect) continue;

            component.Binded = true;
            _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(component.Target, Vector2.Zero));
        }
    }

    public void OnStartup(EntityUid uid, MedievalProjectilePlayerFollowerComponent component, MedievalAfterSpawnEntityBySpellEvent args)
    {
        component.Target = args.Performer;
        component.NextTargetSelect = _timing.CurTime + component.ActivateTime;
    }
}
