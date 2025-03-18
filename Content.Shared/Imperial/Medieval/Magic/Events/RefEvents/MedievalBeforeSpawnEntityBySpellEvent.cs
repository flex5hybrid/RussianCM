using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


[ByRefEvent, Virtual]
public class MedievalBeforeSpawnEntityBySpellEvent
{
    public EntityUid Action;

    public EntityUid Performer;

    public EntProtoId SpawnedEntityPrototype;

    public Angle Direction = Angle.Zero;

    public EntityCoordinates Coordinates;

    public Angle Rotation = Angle.FromDegrees(0);

    public bool Cancelled;
}
