using System.Linq;
using Content.Server.AU14.Round;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Server.Station.Components;
using Content.Shared.AU14;
using Content.Shared.AU14.Threats;
using Content.Shared.AU14.util;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.AU14.Round;

[UsedImplicitly]
public sealed partial class AddJobsRuleSystem : GameRuleSystem<AddJobsRuleComponent>
{
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private AuRoundSystem _auRoundSystem = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private PlatoonSpawnRuleSystem _platoonSpawnRule = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AU14ShipsLoadedEvent>(OnShipsLoaded);
    }

    protected override void Started(EntityUid uid, AddJobsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        PlatoonPrototype? platoon = null;
        var planet = _auRoundSystem.GetSelectedPlanet();
        var protoMgr = IoCManager.Resolve<IPrototypeManager>();
        var platoonSpawnRule = _platoonSpawnRule;

        var presetId = _gameTicker.CurrentPreset?.ID ?? _gameTicker.Preset?.ID;
        var isDistressPreset = !string.IsNullOrEmpty(presetId) && (
            presetId.Equals("distresssignal", StringComparison.InvariantCultureIgnoreCase)
        );
        var isColonyFallPreset = !string.IsNullOrEmpty(presetId) && presetId.Equals("ColonyFall", StringComparison.InvariantCultureIgnoreCase);

        if (component.ShipFaction != null && component.ShipFaction.ToLower() == "opfor")
        {
            platoon = platoonSpawnRule.SelectedOpforPlatoon;
        }
        else
        {
            platoon = platoonSpawnRule.SelectedGovforPlatoon;
            if (platoon == null && planet != null && planet.PlatoonsGovfor.Count > 0)
            {
                if (protoMgr.TryIndex<PlatoonPrototype>(planet.PlatoonsGovfor[0], out var foundPlatoon))
                    platoon = foundPlatoon;
            }
        }

        // If the platoon has a jobSlotOverride, use ONLY those jobs and skip all other job logic
        if (platoon != null && platoon.JobSlotOverride.Count > 0)
        {
            var jobsToAdd = new Dictionary<ProtoId<JobPrototype>, int>();
            var team = (component.ShipFaction != null && component.ShipFaction.ToLower() == "opfor") ? "Opfor" : "GOVFOR";
            foreach (var (jobClass, slotCount) in platoon.JobSlotOverride)
            {
                var jobId = $"AU14Job{team}{jobClass}";
                if (protoMgr.TryIndex<JobPrototype>(jobId, out var proto))
                    jobsToAdd[proto.ID] = slotCount;
                else
                    Logger.GetSawmill("content").Warning($"[AddJobsRuleSystem] Could not find job prototype: {jobId}");
            }
            component.Jobs = jobsToAdd;
        }

        // --- Job Scaling Logic ---
        {
            var playerCount = _playerManager.PlayerCount;
            JobScalePrototype? scaleDef = null;

            var isInsurgency = !string.IsNullOrEmpty(presetId) &&
                               presetId.Equals("insurgency", StringComparison.InvariantCultureIgnoreCase);
            var isFof = !string.IsNullOrEmpty(presetId) &&
                        presetId.Equals("forceonforce", StringComparison.InvariantCultureIgnoreCase);

            if (isDistressPreset || isColonyFallPreset)
            {
                var threat = _auRoundSystem._selectedthreat;
                if (threat?.JobScaling != null)
                    protoMgr.TryIndex<JobScalePrototype>(threat.JobScaling.Value, out scaleDef);
            }
            else if (isFof)
            {
                if (planet?.JobScalingFof != null)
                    protoMgr.TryIndex<JobScalePrototype>(planet.JobScalingFof.Value, out scaleDef);
            }
            else if (isInsurgency)
            {
                if (planet?.JobScalingIns != null)
                    protoMgr.TryIndex<JobScalePrototype>(planet.JobScalingIns.Value, out scaleDef);
            }

            if (scaleDef != null)
            {
                component.Jobs ??= new Dictionary<ProtoId<JobPrototype>, int>();

                var stationOnlyScaling = new Dictionary<ProtoId<JobPrototype>, JobScaleEntry>();

                foreach (var (jobId, entry) in scaleDef.Jobs)
                {
                    var jobProtoId = new ProtoId<JobPrototype>(jobId);
                    var isComponentJob = component.Jobs.ContainsKey(jobProtoId);

                    if (isComponentJob)
                    {
                        component.Jobs.TryGetValue(jobProtoId, out var existingSlots);
                        var baseSlots = entry.Benchmark ?? existingSlots;
                        var extra = JobScaling.CalculateExtraSlots(playerCount, entry);
                        var scaledSlots = JobScaling.CalculateScaledSlots(playerCount, existingSlots, entry);

                        component.Jobs[jobProtoId] = scaledSlots;
                        Logger.GetSawmill("content").Info($"[AddJobsRuleSystem] Job scaling (component): {jobId} => {scaledSlots} slots " +
                                    $"(base={baseSlots}, extra={extra}, players={playerCount}, " +
                                    $"benchmark={entry.Benchmark?.ToString() ?? "null"}, " +
                                    $"maximum={entry.Maximum?.ToString() ?? "null"}, " +
                                    $"scale={entry.Scale}, threshold={entry.WhenToBeginScaling})");
                    }
                    else
                    {
                        var extra = JobScaling.CalculateExtraSlots(playerCount, entry);
                        stationOnlyScaling[jobProtoId] = entry;

                        Logger.GetSawmill("content").Info($"[AddJobsRuleSystem] Job scaling (station): {jobId} => " +
                                    $"(extra={extra}, players={playerCount}, benchmark={entry.Benchmark?.ToString() ?? "null"}, " +
                                    $"maximum={entry.Maximum?.ToString() ?? "null"}, " +
                                    $"scale={entry.Scale}, threshold={entry.WhenToBeginScaling})");
                    }
                }

                if (stationOnlyScaling.Count > 0)
                {
                    var mapId = _gameTicker.DefaultMap;
                    var stationUid = _stationSystem.GetStationInMap(mapId);
                    if (stationUid != null && Exists(stationUid.Value))
                    {
                        var stationJobs = EntityManager.GetComponentOrNull<StationJobsComponent>(stationUid.Value);
                        if (stationJobs != null)
                        {
                            foreach (var (jobProtoId, entry) in stationOnlyScaling)
                            {
                                _stationJobs.TryGetJobSlot(stationUid.Value, jobProtoId.ToString(), out var existingMaybe, stationJobs);
                                if (existingMaybe == null)
                                    continue;

                                var existingSlots = existingMaybe.Value;
                                var scaledSlots = JobScaling.CalculateScaledSlots(playerCount, existingSlots, entry);

                                if (entry.Benchmark != null)
                                {
                                    _stationJobs.TrySetJobSlot(stationUid.Value, jobProtoId.ToString(), scaledSlots, true, stationJobs);
                                }
                                else
                                {
                                    var delta = scaledSlots - existingSlots;
                                    if (delta != 0)
                                        _stationJobs.TryAdjustJobSlot(stationUid.Value, jobProtoId.ToString(), delta, false, false, stationJobs);
                                }
                            }
                        }
                    }
                }
            }
        }
        // --- END: Job Scaling Logic ---

        if (component.Jobs == null || component.Jobs.Count == 0)
            return;

        // If this is ColonyFall, don't add GOVFOR jobs
        if (isColonyFallPreset && !string.IsNullOrEmpty(component.ShipFaction) && component.ShipFaction.Equals("govfor", StringComparison.InvariantCultureIgnoreCase))
            return;

        if (planet != null && !string.IsNullOrEmpty(component.ShipFaction))
        {
            var faction = component.ShipFaction.ToLower();
            var addToShip = false;
            var addToPlanet = false;

            if (faction == "govfor")
            {
                addToShip = planet.GovforInShip;
                addToPlanet = !planet.GovforInShip;
            }
            else if (faction == "opfor")
            {
                addToShip = planet.OpforInShip;
                addToPlanet = !planet.OpforInShip;
            }

            // Ship-side jobs are added in OnShipsLoaded (after ships are actually loaded).

            if (addToPlanet)
            {
                var mapId = _gameTicker.DefaultMap;
                var stationUid = _stationSystem.GetStationInMap(mapId);
                if (stationUid != null && Exists(stationUid.Value))
                {
                    var stationJobs = EntityManager.GetComponentOrNull<StationJobsComponent>(stationUid.Value);
                    if (stationJobs != null)
                    {
                        if (isDistressPreset)
                        {
                            var existing = stationJobs.JobList.Keys.ToList();
                            foreach (var jobKey in existing)
                                _stationJobs.TrySetJobSlot(stationUid.Value, jobKey.ToString(), 0, false, stationJobs);
                        }

                        AddJobsToStation(stationUid.Value, stationJobs, component.Jobs!);
                    }
                }
            }
            return;
        }

        if (planet != null)
        {
            var addToPlanet = true;
            if (component.ShipFaction != null && component.ShipFaction.ToLower() == "opfor")
                addToPlanet = false;
            else if (component.ShipFaction != null && component.ShipFaction.ToLower() == "govfor")
                addToPlanet = !planet.GovforInShip;

            if (addToPlanet)
            {
                var mapId = _gameTicker.DefaultMap;
                var stationUid = _stationSystem.GetStationInMap(mapId);
                if (stationUid != null && Exists(stationUid.Value))
                {
                    var stationJobs = EntityManager.GetComponentOrNull<StationJobsComponent>(stationUid.Value);
                    if (stationJobs != null)
                    {
                        if (isDistressPreset)
                        {
                            var existing = stationJobs.JobList.Keys.ToList();
                            foreach (var jobKey in existing)
                                _stationJobs.TrySetJobSlot(stationUid.Value, jobKey.ToString(), 0, false, stationJobs);
                        }

                        AddJobsToStation(stationUid.Value, stationJobs, component.Jobs!);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds jobs to the given station, updating both the live slot count
    /// and the round-start setup slots.
    /// </summary>
    private void AddJobsToStation(
        EntityUid stationUid,
        StationJobsComponent stationJobs,
        Dictionary<ProtoId<JobPrototype>, int> jobs)
    {
        foreach (var (jobId, amount) in jobs)
        {
            _stationJobs.TryAdjustJobSlot(stationUid, jobId.ToString(), amount, true, false, stationJobs);
            try
            {
                if (stationJobs.SetupAvailableJobs.TryGetValue(jobId, out var arr) && arr.Length > 0)
                    _stationJobs.SetRoundStartJobSlot(stationUid, jobId, arr[0] + amount, stationJobs);
                else
                    _stationJobs.SetRoundStartJobSlot(stationUid, jobId, amount, stationJobs);
            }
            catch (Exception ex)
            {
                Logger.GetSawmill("content").Error(
                    $"[AddJobsRuleSystem] SetRoundStartJobSlot failed for job {jobId}: {ex}");
            }
        }
    }

    private void OnShipsLoaded(AU14ShipsLoadedEvent ev)
    {
        var planet = _auRoundSystem.GetSelectedPlanet();
        if (planet == null)
            return;

        var presetId = _gameTicker.CurrentPreset?.ID ?? _gameTicker.Preset?.ID;
        var isColonyFallPreset = !string.IsNullOrEmpty(presetId) &&
                                 presetId.Equals("ColonyFall", StringComparison.InvariantCultureIgnoreCase);

        var query = AllEntityQuery<AddJobsRuleComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.Jobs == null || component.Jobs.Count == 0)
                continue;
            if (string.IsNullOrEmpty(component.ShipFaction) || !component.AddToShip)
                continue;

            var faction = component.ShipFaction.ToLower();

            // Mirror the ColonyFall govfor guard from Started().
            if (isColonyFallPreset && faction == "govfor")
                continue;

            var addToShip = faction switch
            {
                "govfor" => planet.GovforInShip,
                "opfor"  => planet.OpforInShip,
                _        => false,
            };
            if (!addToShip)
                continue;

            // Find the ship station for this faction.
            var shipQuery = AllEntityQuery<ShipFactionComponent>();
            while (shipQuery.MoveNext(out var shipUid, out var shipFaction))
            {
                if (string.IsNullOrEmpty(shipFaction.Faction) ||
                    shipFaction.Faction.ToLower() != faction)
                    continue;

                var stationUid = _stationSystem.GetOwningStation(shipUid);
                if (stationUid == null || !Exists(stationUid.Value))
                    continue;

                var stationJobs = EntityManager.GetComponentOrNull<StationJobsComponent>(stationUid.Value);
                if (stationJobs == null)
                    continue;

                AddJobsToStation(stationUid.Value, stationJobs, component.Jobs);
                break; // Only the first matching ship's station.
            }
        }
    }
}


