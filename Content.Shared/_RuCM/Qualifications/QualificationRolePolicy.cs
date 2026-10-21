using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RuCM.Qualifications;

// CMU14: public role_timer_override extension; instructor admission remains mandatory in every mode.
public sealed partial class QualificationRolePolicy : EntitySystem
{
    public const string OverrideId = "RuCMInsurgencyQualifications";
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    public readonly Dictionary<string, bool> Eligibility = new();
    public bool ClientPolicy { get; private set; }
    private string _baseOverride = "";
    private bool _active;
    private bool _renamed;
    public Dictionary<string, HashSet<JobRequirement>> BaseRequirements => _baseRequirements;
    public override void Initialize()
    { base.Initialize(); ProtoMan.PrototypesReloaded += OnReload; }
    public int PrototypeRevision { get; private set; } // CMU14: server cache invalidation.
    // CMU14 method: replacing prototypes invalidates server policy caches.
    private void OnReload(PrototypesReloadedEventArgs _)
    {
        _renamed = false; _active = false; _signature = "";
        PrototypeRevision++;
    }
    public override void Shutdown()
    { ProtoMan.PrototypesReloaded -= OnReload; base.Shutdown(); }
    private string _signature = "";
    private readonly Dictionary<string, HashSet<JobRequirement>> _baseRequirements = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_renamed) return;
        _renamed = true;
        // Keep existing IDs, trackers, gear, ranks and job preferences compatible.
        foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>().Where(j => QualificationRules.IsDrillInstructor(j.ID)))
        {
            job.Name = "rucm-qualifications-drill-instructor-name";
            job.Description = "rucm-qualifications-drill-instructor-description";
            job.SpawnMenuRoleName = "rucm-qualifications-drill-instructor-spawn";
        }
    }

    public string Apply(bool active, IEnumerable<string> jobs, bool client = false, string? baseline = null, Dictionary<string, HashSet<JobRequirement>>? requirements = null)
    {
        ClientPolicy = client;
        // CMU14: disabling general qualification gates must not open instructor jobs without accreditation.
        var ids = jobs.Where(id => (active || QualificationRules.IsDrillInstructor(id)) &&
            ProtoMan.TryIndex<JobPrototype>(id, out var job) && !job.IsSynthetic).OrderBy(j => j).ToArray();
        var enabled = active || ids.Length > 0;
        var current = _cfg.GetCVar(CCVars.GameRoleTimerOverride);
        if (baseline != null) _baseOverride = baseline;
        else if (current != OverrideId) _baseOverride = current;
        var signature = active + ":" + _baseOverride + ":" + string.Join(",", ids);
        if (_signature == signature && (!enabled || client || current == OverrideId)) return _baseOverride;
        _signature = signature;
        if (enabled)
        {
            var owned = ProtoMan.Index<JobRequirementOverridePrototype>(OverrideId);
            JobRequirementOverridePrototype? original = null;
            if (_baseOverride.Length > 0) ProtoMan.TryIndex(_baseOverride, out original);
            owned.Jobs = original?.Jobs.ToDictionary(p => p.Key, p => new HashSet<JobRequirement>(p.Value)) ?? new();
            owned.Antags = original?.Antags.ToDictionary(p => p.Key, p => new HashSet<JobRequirement>(p.Value)) ?? new();
            if (requirements != null)
            {
                _baseRequirements.Clear();
                foreach (var (id, values) in requirements) _baseRequirements[id] = new(values);
            }
            else if (!_active || current != OverrideId)
            {
                _baseRequirements.Clear();
                foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>())
                    _baseRequirements[job.ID] = new(_roles.GetRoleRequirements(job) ?? new());
            }
            foreach (var id in ids)
            {
                if (!ProtoMan.TryIndex<JobPrototype>(id, out var job)) continue;
                // Read base requirements through the public API, never mutate job.Requirements.
                var baseRequirements = original?.Jobs.GetValueOrDefault(id) ?? _baseRequirements.GetValueOrDefault(id);
                var preserved = new HashSet<JobRequirement>((baseRequirements ?? new()).Where(r => r is not OverallPlaytimeRequirement and not RoleTimeRequirement and not DepartmentTimeRequirement and not QualificationAccessRequirement));
                preserved.Add(new QualificationAccessRequirement { Job = id });
                owned.Jobs[id] = preserved;
            }
            if (!client) _cfg.SetCVar(CCVars.GameRoleTimerOverride, OverrideId);
        }
        else if (!client && current == OverrideId) _cfg.SetCVar(CCVars.GameRoleTimerOverride, _baseOverride);
        _active = enabled;
        return _baseOverride;
    }
}

[Serializable, NetSerializable]
public sealed partial class QualificationAccessRequirement : JobRequirement
{
    [DataField] public string Job = "";
    public override bool Check(IEntityManager entManager, IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile, IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var policy = entManager.System<QualificationRolePolicy>();
        // Server identity checks remain in QualificationSystem's public job events.
        // CMU14: instructor buttons stay locked until the server has supplied positive eligibility.
        if (!policy.ClientPolicy || policy.Eligibility.GetValueOrDefault(Job, !QualificationRules.IsDrillInstructor(Job))) { reason = null; return true; }
        reason = new FormattedMessage();
        reason.AddText(Loc.GetString("rucm-qualifications-instructor-access-required"));
        return false;
    }
}

[Serializable, NetSerializable]
public sealed class QualificationEntryRequest : EntityEventArgs { }
[Serializable, NetSerializable]
public sealed class QualificationPolicyRequest : EntityEventArgs { }
[Serializable, NetSerializable]
public sealed class QualificationPolicyState : EntityEventArgs
{
    public bool Active;
    public bool Staff;
    public string Baseline = "";
    public Dictionary<string, HashSet<JobRequirement>> BaseRequirements = new();
    public HashSet<string> Jobs = new();
    public Dictionary<string, bool> Eligibility = new();
}
