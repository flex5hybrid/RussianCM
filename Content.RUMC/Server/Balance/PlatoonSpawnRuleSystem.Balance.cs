using Content.Server.GameTicking.Presets;
using Content.Shared.CMU14.util;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Round;

public sealed partial class PlatoonSpawnRuleSystem
{
    /// <summary>Keep authored defaults in other modes while respecting this mode's automatic-selection exclusions.</summary>
    internal PlatoonPrototype? ResolveDefaultPlatoon(string defaultId,
        IReadOnlyList<ProtoId<PlatoonPrototype>> candidates,
        GamePresetPrototype? preset)
    {
        if (!_prototypeManager.TryIndex<PlatoonPrototype>(defaultId, out var authored))
            return null;

        if (preset == null || !preset.ExcludedPlatoons.Contains(defaultId))
            return authored;

        foreach (var id in candidates)
        {
            if (!preset.ExcludedPlatoons.Contains(id.Id)
                && _prototypeManager.TryIndex(id, out var platoon)
                && platoon.ShowInVote)
                return platoon;
        }

        return null;
    }
}
