using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads.Specialization;

public sealed partial class SharedSquadSpecializationSystem : EntitySystem
{
    [Dependency] private SquadSystem _squad = default!;

    public bool TryGetSquadSpecialization(EntityUid marine, out ProtoId<SquadSpecializationPrototype> specId)
    {
        specId = default;

        // 1. Direct check on the marine entity (replicated to client)
        if (TryComp<SquadSpecializationComponent>(marine, out var directComp) && directComp.Specialization != null)
        {
            specId = directComp.Specialization.Value;
            return true;
        }

        // 2. Check on squad entity (server)
        if (_squad.TryGetMemberSquad(marine, out var squad))
        {
            if (TryComp<SquadSpecializationComponent>(squad, out var specComp) && specComp.Specialization != null)
            {
                specId = specComp.Specialization.Value;
                return true;
            }
        }

        return false;
    }

    public ProtoId<SquadSpecializationPrototype>? GetSquadSpecialization(EntityUid marine)
    {
        if (TryGetSquadSpecialization(marine, out var specId))
            return specId;

        return null;
    }
}
