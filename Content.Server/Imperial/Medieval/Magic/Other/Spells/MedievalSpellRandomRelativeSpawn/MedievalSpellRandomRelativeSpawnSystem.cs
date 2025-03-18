using Content.Shared.Imperial.Medieval.Magic;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Imperial.Medieval.Magic.MedievalSpellRandomRelativeSpawn;


public sealed partial class MedievalSpellRandomRelativeSpawnSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalSpellRandomRelativeSpawnComponent, MedievalBeforeSpawnEntityBySpellEvent>(OnBeforeSpawn);
    }

    private void OnBeforeSpawn(EntityUid uid, MedievalSpellRandomRelativeSpawnComponent component, ref MedievalBeforeSpawnEntityBySpellEvent args)
    {
        if (args.Cancelled) return;

        var randomRelativeCoords = _random.NextVector2(component.MinSpawnRadius, component.MaxSpawnRadius);

        args.Coordinates += new EntityCoordinates(args.Coordinates.EntityId, randomRelativeCoords);
    }
}
