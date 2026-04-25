using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Rules;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Server.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules
{
    /// <summary>
    /// Handles remapping of colony job overrides declared by planet/rule prototypes.
    /// When a rule declares a mapping of override -> overridden job, any ready players
    /// who selected the override job will have their profile updated to select the
    /// overridden job instead. This ensures assignment consumes the overridden job's slots.
    /// </summary>
    public sealed class ColonyJobOverrideSystem : EntitySystem
    {
        [Dependency] private readonly RMCPlanetSystem _planetSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning);
        }

        private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
        {
            // Profiles is exposed as an IReadOnlyDictionary on the event, but the GameTicker
            // passes its concrete Dictionary instance. Try to cast so we can mutate profiles
            // before job assignment runs.
            if (ev.Profiles is not Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
                return;

            // Read the planet prototype data directly. Prefer any planet in rotation.
            var all = _planetSystem.GetAllPlanetsInRotation();
            if (all.Count == 0)
                return;

            var planetComp = all[0].Comp;
            if (planetComp.ColonyJobOverrides == null)
                return;

            // The mapping is override -> overriden (key -> value).
            foreach (var (overrideJob, overridenJob) in planetComp.ColonyJobOverrides)
            {
                // Iterate a stable list of users to avoid modifying the collection while iterating.
                var users = profiles.Keys.ToList();
                foreach (var user in users)
                {
                    var profile = profiles[user];

                    // Make a mutable copy of job priorities and apply the remap.
                    var current = new Dictionary<ProtoId<JobPrototype>, JobPriority>(profile.JobPriorities);
                    if (!current.TryGetValue(overrideJob, out var priority))
                        continue;

                    if (priority <= JobPriority.Never)
                        continue;

                    if (current.TryGetValue(overridenJob, out var existing))
                    {
                        current[overridenJob] = (JobPriority)Math.Max((int)existing, (int)priority);
                    }
                    else
                    {
                        current[overridenJob] = priority;
                    }

                    current.Remove(overrideJob);

                    // Replace the profile with a copy that has updated priorities.
                    profiles[user] = profile.WithJobPriorities(current);
                }
            }
        }
    }
}






