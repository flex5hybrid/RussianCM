
using Robust.Shared.Map;

namespace Content.Shared.Imperial.CoordsHelper;

public sealed class CoordsHelper
{
    public static EntityCoordinates GetCoords(MapCoordinates coords, EntityUid? target, EntityManager entityManager)
    {
        var transformSystem = entityManager.System<SharedTransformSystem>();

        return transformSystem.ToCoordinates(
            target.HasValue
                ? transformSystem.GetMapCoordinates(target.Value)
                : coords
        );
    }

    public static EntityCoordinates GetCoords((MapCoordinates Coords, EntityUid? TargetEnt) target, EntityManager entityManager)
    {
        var transformSystem = entityManager.System<SharedTransformSystem>();

        return transformSystem.ToCoordinates(
            target.TargetEnt.HasValue
                ? transformSystem.GetMapCoordinates(target.TargetEnt.Value)
                : target.Coords
        );
    }
}



