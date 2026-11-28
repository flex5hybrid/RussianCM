using System;
using System.Linq;
using System.Threading;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed partial class QualificationService
{
    public QualificationAuthority GetAuthority(TrainingContext context, bool administrator, bool currentParticipant,
        Guid? verifiedInitiator = null)
    {
        // The published store is copy-on-write. Read one version without copying unrelated player history.
        var cache = Volatile.Read(ref _cache);
        return new(context, administrator, cache.Management.Contains(context.Actor),
            currentParticipant && cache.OfficerJobs.Contains(context.Job),
            currentParticipant && cache.CommandingOfficerJobs.Contains(context.Job),
            currentParticipant, verifiedInitiator);
    }

    public bool IsActiveInstructor(Guid player) =>
        QualificationRules.GetInstructorRank(false, Volatile.Read(ref _cache).Instructors.GetValueOrDefault(player)) != null;

    public bool IsJobAllowed(Guid player, string job)
    {
        var cache = Volatile.Read(ref _cache);
        cache.Players.TryGetValue(player, out var state);
        cache.Instructors.TryGetValue(player, out var instructor);
        cache.Roles.TryGetValue(job, out var requirement);
        // Old stores may omit instructor roles. The mandatory rank/accreditation gate still applies.
        if (requirement == null && QualificationRules.IsDrillInstructor(job))
            return QualificationRules.EffectiveLevel(state) >= MilitaryLevel.Sergeant && QualificationRules.GetInstructorRank(false, instructor) != null;
        return QualificationRules.IsJobAllowed(state, requirement, instructor);
    }

    public string? GetDefinitionName(string id) =>
        Volatile.Read(ref _cache).Definitions.TryGetValue(id, out var definition) ? definition.Name : null;

    public Guid? GetSuspensionInitiator(Guid id) =>
        Volatile.Read(ref _cache).Suspensions.SingleOrDefault(s => s.Id == id)?.Initiator.Actor;

    public PlayerTrainingState? PlayerSnapshot(Guid id)
    {
        var cache = Volatile.Read(ref _cache);
        if (!cache.Players.TryGetValue(id, out var player))
            return null;
        return ClonePlayer(player);
    }
}
