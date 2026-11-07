using System.Linq;
using Content.Server._RuCM.Kaban;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Maps;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RuCM.Kaban;
using Content.Shared.CCVar;
using Content.Shared.CMU14.util;
using Content.Shared.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Round;

// CMU14: the paid constraint belongs to this vote generation, never to persistent CVars/admin locks.
public sealed partial class AuRoundSystem
{
    private KabanForceChoice? _kabanChoice;
    private GamePresetPrototype? _kabanPreviousPreset;
    private CamouflageType? _kabanPreviousCamo;
    private KabanForceOption[] _kabanOptions = [];
    private KabanForceOption[] _kabanCatalogCache = [];
    private int _kabanCatalogPlayers = -1;
    private DateTime _kabanCatalogUntil;
    internal bool KabanSelectionOpen => _gameTicker.RunLevel == GameRunLevel.PreRoundLobby
        && !EntityManager.System<SharedMapSystem>().MapExists(_gameTicker.DefaultMap);

    public KabanForceOption[] KabanCatalog(bool fresh = false)
    {
        var count = _playerManager.PlayerCount;
        if (!fresh && count == _kabanCatalogPlayers && DateTime.UtcNow < _kabanCatalogUntil)
            return _kabanCatalogCache;
        _kabanCatalogCache = BuildKabanCatalog(count);
        _kabanCatalogPlayers = count;
        _kabanCatalogUntil = DateTime.UtcNow.AddSeconds(2);
        return _kabanCatalogCache;
    }

    internal KabanForceOption[] BuildKabanCatalog(int count)
    {
        var resources = IoCManager.Resolve<IResourceManager>();
        // Ships are auxiliary maps loaded directly by GameTicker; their bounds do not describe the whole server population.
        bool MapExists(string id, bool population) => _prototypeManager.TryIndex<GameMapPrototype>(id, out var map)
            && (!population || map.MinPlayers <= count && map.MaxPlayers >= count)
            && resources.ContentFileExists(map.MapPath)
            && map.MapsAbove.Concat(map.MapsBelow).All(path => resources.ContentFileExists(path));
        bool PlatoonReady(PlatoonPrototype platoon, bool ship) => (!ship || platoon.PossibleShips.Count > 0 && platoon.PossibleShips.All(id => MapExists(id, false)))
            && (platoon.TechTree == "" || _prototypeManager.HasIndex<EntityPrototype>(platoon.TechTree));
        Dictionary<string, string> Platoons(IEnumerable<ProtoId<PlatoonPrototype>> ids, bool ship, GamePresetPrototype preset)
            => ids.Distinct().Where(id => _prototypeManager.TryIndex(id, out PlatoonPrototype? platoon)
                && !preset.ExcludedPlatoons.Contains(platoon.ID) && PlatoonReady(platoon, ship))
                .Select(id => _prototypeManager.Index(id)).ToDictionary(p => p.ID, p => Loc.GetString(p.Name));
        var result = new List<KabanForceOption>();
        foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>().Where(p => p.ShowInVote))
        {
            if (preset.MinPlayers is { } min && count < min || preset.MaxPlayers is { } max && count > max)
                continue;
            foreach (var id in GamePlanetPoolPrototype.ExpandPlanetIds(_prototypeManager, preset.PlanetPool, preset.SupportedPlanets).Distinct())
            {
                if (!_prototypeManager.TryIndex<EntityPrototype>(id, out var entity)
                    || !entity.TryComp(out RMCPlanetMapPrototypeComponent? planet, IoCManager.Resolve<IComponentFactory>())
                    || !planet.InRotation || planet.MinPlayers > count || planet.MaxPlayers > 0 && count > planet.MaxPlayers
                    || !MapExists(planet.MapId, true))
                    continue;
                if (preset.MapPool is { } poolId && (!_prototypeManager.TryIndex<GameMapPoolPrototype>(poolId, out var pool)
                    || !pool.Maps.Contains(planet.MapId)))
                    continue;
                var govfor = Platoons(planet.PlatoonsGovfor, planet.GovforInShip, preset);
                var opfor = Platoons(planet.PlatoonsOpfor, planet.OpforInShip, preset);
                if (preset.RequiresGovforVote && govfor.Count == 0 || preset.RequiresOpforVote && opfor.Count == 0)
                    continue;
                // Defaults are consumed by PlatoonSpawnRuleSystem even if the preset doesn't run that side's vote.
                if (planet.DefaultGovforPlatoon is { Length: > 0 } gd
                        && (!_prototypeManager.TryIndex<PlatoonPrototype>(gd, out var gp) || !PlatoonReady(gp, planet.GovforInShip))
                    || planet.DefaultOpforPlatoon is { Length: > 0 } od
                        && (!_prototypeManager.TryIndex<PlatoonPrototype>(od, out var op) || !PlatoonReady(op, planet.OpforInShip)))
                    continue;
                result.Add(new(preset.ID, Loc.GetString(preset.ModeTitle), id,
                    string.IsNullOrWhiteSpace(planet.VoteName) ? planet.MapId : planet.VoteName,
                    preset.RequiresGovforVote ? govfor : new(), preset.RequiresOpforVote ? opfor : new()));
            }
        }
        return result.ToArray();
    }

    private void StartKabanVoteSequence(Action? onFinished)
    {
        if (!KabanSelectionOpen)
            return;
        StopVoteSequence();
        _state.Reset();
        SelectedPlanetMap = null;
        var generation = _voteSequenceId;
        void Start(KabanForceChoice? choice)
        {
            if (!IsCurrentVoteSequence(generation))
                return;
            if (!KabanSelectionOpen)
            {
                _voteSequenceRunning = false;
                EntityManager.System<KabanSystem>().CancelForcing("round-already-loading");
                return;
            }
            _voteSequenceRunning = false;
            _kabanChoice = choice;
            _kabanPreviousPreset = choice == null ? null : _gameTicker.Preset;
            _kabanPreviousCamo = choice == null ? null : _camo.CurrentMapCamouflage;
            _kabanOptions = choice == null ? [] : KabanCatalog(fresh: true).Where(choice.Matches).ToArray();
            if (choice != null && _kabanOptions.Length == 0)
            {
                EntityManager.System<KabanSystem>().CancelForcing("incompatible-before-vote");
                _kabanChoice = null;
            }
            StartFullVoteSequence();
            onFinished?.Invoke();
        }
        if (EntityManager.System<KabanSystem>().TryBeginForcing(Start))
            _voteSequenceRunning = true;
        else
            Start(null);
    }

    public void FilterKabanPresets(Dictionary<string, string> presets)
    {
        if (_kabanChoice == null)
            return;
        foreach (var id in presets.Keys.ToArray())
            if (!_kabanOptions.Any(o => o.Mode == id))
                presets.Remove(id);
    }

    private void FilterKabanPlanets(string preset, List<(string Id, RMCPlanetMapPrototypeComponent Planet)> planets)
    {
        if (_kabanChoice != null)
            planets.RemoveAll(p => !_kabanOptions.Any(o => o.Mode == preset && o.Planet == p.Id));
    }

    private bool KabanPlatoonAllowed(string id, bool govfor)
    {
        if (_kabanChoice == null)
            return true;
        var forced = govfor ? _kabanChoice.Govfor : _kabanChoice.Opfor;
        var opposing = govfor ? _kabanChoice.Opfor : _kabanChoice.Govfor;
        return (forced == "" || forced == id) && opposing != id
            && _kabanOptions.Any(o => o.Mode == _selectedPreset?.ID && o.Planet == _selectedPlanetId
                && (govfor ? o.Govfor : o.Opfor).ContainsKey(id));
    }

    private bool ApplyKabanPlatoon(bool govfor, Action<List<string>, string, string, Action<string>> startShipVote)
    {
        var id = govfor ? _kabanChoice?.Govfor : _kabanChoice?.Opfor;
        if (string.IsNullOrEmpty(id))
            return false;
        var platoon = _prototypeManager.Index<PlatoonPrototype>(id);
        var platoons = EntityManager.System<PlatoonSpawnRuleSystem>();
        if (govfor) platoons.SelectedGovforPlatoon = platoon;
        else platoons.SelectedOpforPlatoon = platoon;
        ApplyKabanTechTrees();
        _gameTicker.UpdateInfoText();
        if (govfor ? _selectedPlanet!.GovforInShip : _selectedPlanet!.OpforInShip)
            startShipVote(platoon.PossibleShips, Loc.GetString(govfor ? "au14-vote-title-govfor-ship" : "au14-vote-title-opfor-ship"),
                Loc.GetString(govfor ? "au14-vote-name-govfor-ship" : "au14-vote-name-opfor-ship"), govfor ? SetGovforShip : SetOpforShip);
        return true;
    }

    private bool TryCommitKabanFull()
    {
        if (_kabanChoice is not { Mode.Length: > 0, Planet.Length: > 0, Govfor.Length: > 0, Opfor.Length: > 0 } choice)
            return false;
        // Resolve every dependency before touching any selection. No old callback survives StartVoteSequence's generation change.
        if (!KabanCatalog(fresh: true).Any(choice.Matches))
            return false;
        var preset = _prototypeManager.Index<GamePresetPrototype>(choice.Mode);
        _prototypeManager.Index<EntityPrototype>(choice.Planet).TryComp(out RMCPlanetMapPrototypeComponent? planet, IoCManager.Resolve<IComponentFactory>());
        var govfor = _prototypeManager.Index<PlatoonPrototype>(choice.Govfor);
        var opfor = _prototypeManager.Index<PlatoonPrototype>(choice.Opfor);
        var platoons = EntityManager.System<PlatoonSpawnRuleSystem>();
        _gameTicker.SetGamePreset(preset);
        _selectedPreset = preset;
        _state.SetPlanet(choice.Planet, planet!);
        platoons.SelectedGovforPlatoon = govfor;
        platoons.SelectedOpforPlatoon = opfor;
        _selectedGovforShip = planet!.GovforInShip ? govfor.PossibleShips[0] : null;
        _selectedOpforShip = planet.OpforInShip ? opfor.PossibleShips[0] : null;
        ApplyKabanTechTrees();
        SetCamoType();
        ChooseThreat(planet);
        PreselectThirdParties();
        _gameTicker.UpdateInfoText();
        return true;
    }

    private bool CommitKabanFullOrFallback()
    {
        if (_kabanChoice is not { Mode.Length: > 0, Planet.Length: > 0, Govfor.Length: > 0, Opfor.Length: > 0 })
            return false;
        try
        {
            if (TryCommitKabanFull())
                return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Paid round selection failed; restoring ordinary voting: {e}");
        }
        EntityManager.System<KabanSystem>().CancelForcing("full-plan-rejected");
        _kabanChoice = null;
        _kabanOptions = [];
        ResetKabanSelection(true);
        return false;
    }

    private void ApplyKabanTechTrees()
    {
        var platoons = EntityManager.System<PlatoonSpawnRuleSystem>();
        var intel = EntityManager.System<IntelSystem>();
        intel.SetTeamTechTreeOverride(Team.GovFor, platoons.SelectedGovforPlatoon?.TechTree);
        intel.SetTeamTechTreeOverride(Team.OpFor, platoons.SelectedOpforPlatoon?.TechTree);
    }

    private void ResetKabanSelection(bool resetPreset)
    {
        var previous = _kabanPreviousPreset;
        _kabanPreviousPreset = null;
        if (_kabanPreviousCamo is { } camouflage)
            _camo.CurrentMapCamouflage = camouflage;
        _kabanPreviousCamo = null;
        _state.Reset();
        SelectedPlanetMap = null;
        var platoons = EntityManager.System<PlatoonSpawnRuleSystem>();
        platoons.SelectedGovforPlatoon = null;
        platoons.SelectedOpforPlatoon = null;
        ApplyKabanTechTrees();
        if (resetPreset)
            _gameTicker.SetGamePreset(previous ?? _gameTicker.FindGamePreset(_cfg.GetCVar(CCVars.GameLobbyDefaultPreset)));
    }

    public bool KabanSelectionMatches(KabanForceChoice choice)
    {
        var platoons = EntityManager.System<PlatoonSpawnRuleSystem>();
        return (choice.Mode == "" || choice.Mode == _selectedPreset?.ID)
            && (choice.Planet == "" || choice.Planet == _selectedPlanetId)
            && (choice.Govfor == "" || choice.Govfor == platoons.SelectedGovforPlatoon?.ID)
            && (choice.Opfor == "" || choice.Opfor == platoons.SelectedOpforPlatoon?.ID);
    }

    internal void CompleteKabanSelection()
    {
        _kabanChoice = null;
        _kabanOptions = [];
        _kabanPreviousPreset = null;
        _kabanPreviousCamo = null;
    }

    public void ValidateKabanBeforeLoad()
    {
        if (_kabanChoice == null)
            return;
        if (KabanCatalog(fresh: true).Any(o => o.Mode == _selectedPreset?.ID && o.Planet == _selectedPlanetId && _kabanChoice.Matches(o))
            && KabanSelectionMatches(_kabanChoice))
        {
            _voteSequence.Restart(); // Freeze the complete selection; no delayed vote may change a loaded round.
            return;
        }
        // Return to ordinary preset/map selection before any map/rule is loaded.
        StopVoteSequence();
    }

    public void KabanAdminMapOverride()
    {
        if (_kabanChoice == null && !EntityManager.System<KabanSystem>().HasPendingForcing)
            return;
        StopVoteSequence();
        _selectedPlanet = null;
        _selectedPlanetId = null;
        SelectedPlanetMap = null;
    }
}
