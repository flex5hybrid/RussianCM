using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed partial class QualificationService
{
    private async Task<QualificationStore> UpgradeTrainingProgrammes(QualificationStore current)
    {
        const string migration = "govfor-enlisted-and-squad-leader-v1";
        if (current.Migrations.Contains(migration) || !_seed.Definitions.TryGetValue("enlisted", out var enlisted) ||
            !_seed.Definitions.TryGetValue("squad_leader", out var squadLeader))
            return current;

        var next = CloneForMutation(current);
        var required = enlisted.Items.Where(item => item.Enabled && item.Required).Select(item => item.Id).ToHashSet();
        var disabled = enlisted.Items.Where(item => !item.Enabled).Select(item => item.Id).ToHashSet();
        if (next.Definitions.TryGetValue(enlisted.Id, out var existing) &&
            (!required.SetEquals(existing.Items.Where(item => item.Enabled && item.Required).Select(item => item.Id)) ||
             existing.Items.Any(item => item.Enabled && disabled.Contains(item.Id))))
        {
            foreach (var item in existing.Items)
            {
                item.Required = required.Contains(item.Id);
                item.Enabled = item.Required || item.Enabled && !disabled.Contains(item.Id);
            }
            existing.Items.AddRange(enlisted.Items.Where(item => required.Contains(item.Id) && existing.Items.All(old => old.Id != item.Id)));
            existing.Version++;
        }
        next.Definitions.TryAdd(enlisted.Id, enlisted);

        next.Definitions.TryAdd(squadLeader.Id, squadLeader);
        foreach (var seedRole in _seed.Roles.Values.Where(role => role.Professional.Contains(squadLeader.Id)))
        {
            if (next.Roles.TryGetValue(seedRole.JobId, out var role) && role.MinimumLevel == MilitaryLevel.Sergeant)
            {
                role.MinimumLevel = seedRole.MinimumLevel;
                role.Professional.Add(squadLeader.Id);
            }
        }

        next.Migrations.Add(migration);
        next.Audit.Add(new(Guid.NewGuid(), "TrainingProgrammeDefaults", Guid.Empty, null, DateTimeOffset.UtcNow, 0,
            "schema-upgrade", JsonSerializer.Serialize(current.Definitions.GetValueOrDefault(enlisted.Id)),
            JsonSerializer.Serialize(next.Definitions[enlisted.Id]), "GOVFOR enlisted and squad leader programmes", migration));
        next.Revision++;
        await PersistChanges(next, current);
        return next;
    }
}
