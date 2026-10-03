using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._RuCM.Qualifications;

[Serializable, NetSerializable]
public enum MilitaryLevel { None, Enlisted, Sergeant, Officer }
[Serializable, NetSerializable]
public enum QualificationStatus { Active, Suspended, Revoked }
public enum QualificationMode { Disabled, Warn, Enforce }

[Serializable, NetSerializable]
public sealed class ChecklistItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }
}

[Serializable, NetSerializable]
public sealed class QualificationDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int Version { get; set; } = 1;
    public List<ChecklistItem> Items { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed class RoleRequirement
{
    public string JobId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public MilitaryLevel MinimumLevel { get; set; }
    public HashSet<string> Professional { get; set; } = new();
    public string Tracker { get; set; } = "";
    public bool Govfor { get; set; }
}

[Serializable, NetSerializable]
public readonly record struct QualificationTimestamp(long Ticks, long OffsetTicks)
{
    // NetSerializer does not support DateTimeOffset's ISerializable implementation.
    // Public domain properties still expose DateTimeOffset, preserving server JSON storage.
    public DateTimeOffset Value => new(Ticks, new TimeSpan(OffsetTicks));
    public static QualificationTimestamp From(DateTimeOffset value) => new(value.Ticks, value.Offset.Ticks);
}

[Serializable, NetSerializable]
public sealed record TrainingContext(Guid Actor, string Character, string Job, int Round, string Server, DateTimeOffset At)
{
    private QualificationTimestamp _at = QualificationTimestamp.From(At);
    public DateTimeOffset At { get => _at.Value; init => _at = QualificationTimestamp.From(value); }
}
[Serializable, NetSerializable]
public sealed record QualificationGrant(string Id, QualificationStatus Status, int Version, string[] RequiredItems,
    TrainingContext Issuer, string Reason);
[Serializable, NetSerializable]
public sealed record ChecklistCompletion(string Qualification, string Item, TrainingContext By, string Note);
[Serializable, NetSerializable]
public sealed record TrainingNote(Guid Id, Guid Target, TrainingContext Author, string Text);
[Serializable, NetSerializable]
public sealed record AuditEntry(Guid Id, string Action, Guid Actor, Guid? Target, DateTimeOffset At, int Round,
    string Server, string OldState, string NewState, string Reason, string Metadata)
{
    private QualificationTimestamp _at = QualificationTimestamp.From(At);
    public DateTimeOffset At { get => _at.Value; init => _at = QualificationTimestamp.From(value); }
}
[Serializable, NetSerializable]
public sealed record InstructorAccreditation(bool Active, bool Enlisted, bool Sergeant, HashSet<string> Professional,
    Guid GrantedBy, DateTimeOffset GrantedAt, Guid ModifiedBy, DateTimeOffset ModifiedAt)
{
    private QualificationTimestamp _grantedAt = QualificationTimestamp.From(GrantedAt);
    private QualificationTimestamp _modifiedAt = QualificationTimestamp.From(ModifiedAt);
    public DateTimeOffset GrantedAt { get => _grantedAt.Value; init => _grantedAt = QualificationTimestamp.From(value); }
    public DateTimeOffset ModifiedAt { get => _modifiedAt.Value; init => _modifiedAt = QualificationTimestamp.From(value); }
}

[Serializable, NetSerializable]
public sealed class Suspension
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Target { get; set; }
    public string Qualification { get; set; } = "";
    public TrainingContext Initiator { get; set; } = default!;
    public TrainingContext? Second { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "pending";
    private QualificationTimestamp? _resolvedAt;
    public DateTimeOffset? ResolvedAt
    {
        get => _resolvedAt?.Value;
        set => _resolvedAt = value is { } at ? QualificationTimestamp.From(at) : null;
    }
    public Guid? ResolvedBy { get; set; }
    public string ResolutionReason { get; set; } = "";
}

[Serializable, NetSerializable]
public sealed class PlayerTrainingState
{
    public Guid Player { get; set; }
    private QualificationTimestamp _firstTrainingAt;
    public DateTimeOffset FirstTrainingAt
    {
        get => _firstTrainingAt.Value;
        set => _firstTrainingAt = QualificationTimestamp.From(value);
    }
    public Dictionary<string, QualificationGrant> Grants { get; set; } = new();
    // Nested keys avoid delimiter collisions and enforce one completion per stable item ID.
    public Dictionary<string, Dictionary<string, ChecklistCompletion>> Progress { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed class QualificationStore
{
    public long Revision { get; set; }
    public Dictionary<string, QualificationDefinition> Definitions { get; set; } = new();
    public Dictionary<string, RoleRequirement> Roles { get; set; } = new();
    public Dictionary<Guid, PlayerTrainingState> Players { get; set; } = new();
    public Dictionary<Guid, InstructorAccreditation> Instructors { get; set; } = new();
    public HashSet<Guid> Management { get; set; } = new();
    public HashSet<string> OfficerJobs { get; set; } = new();
    public HashSet<string> CommandingOfficerJobs { get; set; } = new();
    public List<TrainingNote> Notes { get; set; } = new();
    public List<Suspension> Suspensions { get; set; } = new();
    public List<AuditEntry> Audit { get; set; } = new();
    public HashSet<string> Migrations { get; set; } = new();
    public List<GovforParticipation> Participation { get; set; } = new();
    public Dictionary<string, HashSet<string>> MigrationGroups { get; set; } = new();
    public Dictionary<string, string> TrackerAliases { get; set; } = new();
    /// <summary>Copies every mutable collection and value without reflection or client-forbidden JSON APIs.</summary>
    public QualificationStore Clone() => new()
    {
        Revision = Revision,
        Definitions = Definitions.ToDictionary(p => p.Key, p => new QualificationDefinition
        {
            Id = p.Value.Id, Name = p.Value.Name, Description = p.Value.Description,
            Enabled = p.Value.Enabled, Version = p.Value.Version,
            Items = p.Value.Items.Select(i => new ChecklistItem
            {
                Id = i.Id, Name = i.Name, Description = i.Description,
                Required = i.Required, Enabled = i.Enabled, SortOrder = i.SortOrder
            }).ToList()
        }),
        Roles = Roles.ToDictionary(p => p.Key, p => new RoleRequirement
        {
            JobId = p.Value.JobId, Enabled = p.Value.Enabled, MinimumLevel = p.Value.MinimumLevel,
            Professional = new(p.Value.Professional), Tracker = p.Value.Tracker, Govfor = p.Value.Govfor
        }),
        Players = Players.ToDictionary(p => p.Key, p => new PlayerTrainingState
        {
            Player = p.Value.Player, FirstTrainingAt = p.Value.FirstTrainingAt,
            Grants = p.Value.Grants.ToDictionary(g => g.Key, g => g.Value with { RequiredItems = g.Value.RequiredItems.ToArray() }),
            Progress = p.Value.Progress.ToDictionary(g => g.Key, g => new Dictionary<string, ChecklistCompletion>(g.Value))
        }),
        Instructors = Instructors.ToDictionary(p => p.Key, p => p.Value with { Professional = new(p.Value.Professional) }),
        Management = new(Management), OfficerJobs = new(OfficerJobs), CommandingOfficerJobs = new(CommandingOfficerJobs),
        Notes = new(Notes), Audit = new(Audit), Migrations = new(Migrations), Participation = new(Participation),
        MigrationGroups = MigrationGroups.ToDictionary(p => p.Key, p => new HashSet<string>(p.Value)),
        TrackerAliases = new(TrackerAliases),
        Suspensions = Suspensions.Select(s => new Suspension
        {
            Id = s.Id, Target = s.Target, Qualification = s.Qualification, Initiator = s.Initiator,
            Second = s.Second, Reason = s.Reason, Status = s.Status, ResolvedAt = s.ResolvedAt,
            ResolvedBy = s.ResolvedBy, ResolutionReason = s.ResolutionReason
        }).ToList()
    };
}

[Serializable, NetSerializable]
public sealed record GovforParticipation(Guid Player, string Job, DateTimeOffset At, int Round, string Server)
{
    private QualificationTimestamp _at = QualificationTimestamp.From(At);
    public DateTimeOffset At { get => _at.Value; init => _at = QualificationTimestamp.From(value); }
}

public sealed record JobEligibility(bool Allowed, string[] Missing);

/// <summary>Pure domain rules. The caller supplies server-resolved identity and authority.</summary>
public static class QualificationRules
{
    public static readonly string[] Levels = { "enlisted", "sergeant", "officer" };

    public static MilitaryLevel EffectiveLevel(PlayerTrainingState? player)
    {
        if (player == null) return MilitaryLevel.None;
        var highest = MilitaryLevel.None;
        for (var i = 0; i < Levels.Length; i++)
        {
            if (player.Grants.TryGetValue(Levels[i], out var grant) && grant.Status == QualificationStatus.Active)
                highest = (MilitaryLevel) (i + 1);
        }
        // Higher grants imply lower levels; explicit suspension/revocation interrupts that chain.
        for (var i = 0; i < (int) highest; i++)
        {
            if (player.Grants.TryGetValue(Levels[i], out var grant) && grant.Status != QualificationStatus.Active)
                return (MilitaryLevel) i;
        }
        return highest;
    }

    public static JobEligibility CanTakeJob(PlayerTrainingState? player, RoleRequirement? role)
    {
        if (role == null || !role.Enabled) return new(true, Array.Empty<string>());
        var missing = new List<string>();
        if (EffectiveLevel(player) < role.MinimumLevel) missing.Add(role.MinimumLevel.ToString().ToLowerInvariant());
        foreach (var id in role.Professional.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (player == null || !player.Grants.TryGetValue(id, out var grant) || grant.Status != QualificationStatus.Active)
                missing.Add(id);
        }
        return new(missing.Count == 0, missing.ToArray());
    }

    public static bool CanTrain(InstructorAccreditation? instructor, string id)
    {
        if (instructor == null || !instructor.Active) return false;
        return id switch
        {
            "enlisted" => instructor.Enlisted,
            "sergeant" => instructor.Sergeant,
            "officer" or "commanding_officer" => false,
            _ => instructor.Professional.Contains(id)
        };
    }

    public static bool ChecklistComplete(PlayerTrainingState player, QualificationDefinition definition) =>
        definition.Enabled && definition.Items.Any(i => i.Enabled) && definition.Items.Where(i => i.Enabled && i.Required)
            .All(i => player.Progress.TryGetValue(definition.Id, out var progress) && progress.ContainsKey(i.Id));
}
