using System.Linq;
using Content.Server.Pinpointer;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared._RuMC14.CrewTracker;
using Content.Shared.CrewManifest;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Humanoid;
using Content.Shared.Examine;

namespace Content.Server._RuMC14.CrewTracker;

public sealed class RMCCrewPinpointerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCCrewPinpointerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCCrewPinpointerComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<RMCCrewPinpointerComponent, ExaminedEvent>(OnExamined);

        Subs.BuiEvents<RMCCrewPinpointerComponent>(RMCCrewPinpointerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<RMCCrewPinpointerRefreshMsg>(OnRefresh);
            subs.Event<RMCCrewPinpointerClearMsg>(OnClear);
            subs.Event<RMCCrewPinpointerToggleMsg>(OnToggle);
            subs.Event<RMCCrewPinpointerSelectMsg>(OnSelect);
            subs.Event<RMCCrewPinpointerFavoriteMsg>(OnFavorite);
        });
    }

    private void OnUseInHand(Entity<RMCCrewPinpointerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        OpenUi(ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnActivateInWorld(Entity<RMCCrewPinpointerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        OpenUi(ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnExamined(Entity<RMCCrewPinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp(ent, out PinpointerComponent? component))
            return;

        if (component.Target is not { } target || !Exists(target))
            return;

        var targetXform = Transform(target);
        if (targetXform.GridUid is { } gridUid)
            args.PushMarkup(Loc.GetString("examine-rmc-crew-pinpointer-target-grid", ("grid", MetaData(gridUid).EntityName)));
        else
            args.PushMarkup(Loc.GetString("examine-rmc-crew-pinpointer-target-no-grid"));

        var sameMap = targetXform.MapID == Transform(args.Examiner).MapID;
        args.PushMarkup(Loc.GetString(sameMap
            ? "examine-rmc-crew-pinpointer-target-same-map"
            : "examine-rmc-crew-pinpointer-target-different-map"));
    }

    private void OnUiOpened(Entity<RMCCrewPinpointerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnRefresh(Entity<RMCCrewPinpointerComponent> ent, ref RMCCrewPinpointerRefreshMsg args)
    {
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnClear(Entity<RMCCrewPinpointerComponent> ent, ref RMCCrewPinpointerClearMsg args)
    {
        if (!TryComp(ent, out PinpointerComponent? pinpointer))
            return;

        _pinpointer.SetTarget(ent.Owner, null, pinpointer);
        if (pinpointer.IsActive)
             _pinpointer.TogglePinpointer(ent.Owner, pinpointer);
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnToggle(Entity<RMCCrewPinpointerComponent> ent, ref RMCCrewPinpointerToggleMsg args)
    {
        if (!TryComp(ent, out PinpointerComponent? pinpointer))
            return;

        if (pinpointer.Target == null && !pinpointer.IsActive)
        {
            UpdateUi(ent.Owner, args.Actor);
            return;
        }

        _pinpointer.TogglePinpointer(ent.Owner, pinpointer);
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnSelect(Entity<RMCCrewPinpointerComponent> ent, ref RMCCrewPinpointerSelectMsg args)
    {
        if (!TryGetEntity(args.Target, out var target) ||
            !TryComp(ent, out PinpointerComponent? pinpointer))
        {
            UpdateUi(ent.Owner, args.Actor);
            return;
        }

        _pinpointer.SetTarget(ent.Owner, target.Value, pinpointer);
        if (!pinpointer.IsActive)
            _pinpointer.TogglePinpointer(ent.Owner, pinpointer);
        UpdateUi(ent.Owner, args.Actor);
    }

    private void OnFavorite(Entity<RMCCrewPinpointerComponent> ent, ref RMCCrewPinpointerFavoriteMsg args)
    {
        var name = Normalize(args.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateUi(ent.Owner, args.Actor);
            return;
        }

        if (!ent.Comp.FavoriteNames.Add(name))
            ent.Comp.FavoriteNames.Remove(name);

        UpdateUi(ent.Owner, args.Actor);
    }

    private void OpenUi(EntityUid uid, EntityUid user)
    {
        UpdateUi(uid, user);
        _ui.TryOpenUi(uid, RMCCrewPinpointerUiKey.Key, user);
    }

    private void UpdateUi(EntityUid uid, EntityUid user)
    {
        _ui.SetUiState(uid, RMCCrewPinpointerUiKey.Key, BuildState(uid, user));
    }

    private RMCCrewPinpointerBuiState BuildState(EntityUid uid, EntityUid user)
    {
        var sections = new List<RMCCrewPinpointerSection>();

        if (!TryComp(uid, out PinpointerComponent? pinpointer) || !TryComp(uid, out RMCCrewPinpointerComponent? crewPinpointer))
            return new(sections, null, false);

        var station = _station.GetOwningStation(user) ?? _station.GetOwningStation(uid);
        var userGrid = Transform(user).GridUid;
        if (station == null || userGrid == null)
            return new(sections, pinpointer.TargetName, pinpointer.IsActive);

        var presentCrew = GetPresentCrew(station.Value, userGrid.Value);
        if (presentCrew.Count == 0)
            return new(sections, pinpointer.TargetName, pinpointer.IsActive);

        var entries = new List<(JobPrototype? Job, RMCCrewPinpointerEntry Entry)>();
        foreach (var (_, record) in _records.GetRecordsOfType<GeneralStationRecord>(station.Value))
        {
            if (!presentCrew.TryGetValue(record.Name, out var target))
                continue;

            _prototype.TryIndex(record.JobPrototype, out JobPrototype? job);
            var manifestEntry = new CrewManifestEntry(
                record.Name,
                record.JobTitle,
                record.JobIcon,
                record.JobPrototype,
                record.Squad,
                record.SquadColor);

            entries.Add((job, new RMCCrewPinpointerEntry(
                GetNetEntity(target),
                manifestEntry,
                crewPinpointer.FavoriteNames.Contains(Normalize(record.Name)))));
        }

        entries.Sort((a, b) =>
        {
            var cmp = JobUIComparer.Instance.Compare(a.Job, b.Job);
            if (cmp != 0)
                return cmp;

            return string.Compare(a.Entry.Entry.Name, b.Entry.Entry.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        var jobDepartments = new Dictionary<string, List<DepartmentPrototype>>();
        foreach (var department in _prototype.EnumeratePrototypes<DepartmentPrototype>())
        {
            foreach (var roleId in department.Roles)
            {
                if (!_prototype.HasIndex<JobPrototype>(roleId))
                    continue;

                jobDepartments.GetOrNew(roleId).Add(department);
            }
        }

        var departmentEntries = new Dictionary<DepartmentPrototype, List<RMCCrewPinpointerEntry>>();
        var squadEntries = new Dictionary<string, List<RMCCrewPinpointerEntry>>();
        foreach (var (_, entry) in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Entry.Squad))
            {
                squadEntries.GetOrNew(entry.Entry.Squad!).Add(entry);
                continue;
            }

            if (!jobDepartments.TryGetValue(entry.Entry.JobPrototype, out var departments))
                continue;

            foreach (var department in departments)
            {
                departmentEntries.GetOrNew(department).Add(entry);
            }
        }

        foreach (var (department, departmentSectionEntries) in departmentEntries.OrderBy(kvp => kvp.Key, DepartmentUIComparer.Instance))
        {
            sections.Add(new RMCCrewPinpointerSection(department.Name, departmentSectionEntries));
        }

        foreach (var (squad, squadSectionEntries) in squadEntries.OrderBy(kvp => kvp.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            sections.Add(new RMCCrewPinpointerSection(squad, squadSectionEntries));
        }

        return new(sections, pinpointer.TargetName, pinpointer.IsActive);
    }

    private Dictionary<string, EntityUid> GetPresentCrew(EntityUid station, EntityUid userGrid)
    {
        var present = new Dictionary<string, EntityUid>(StringComparer.Ordinal);

        var humanoidQuery = EntityQueryEnumerator<HumanoidAppearanceComponent>();
        while (humanoidQuery.MoveNext(out var humanoid, out _))
        {
            var xform = Transform(humanoid);

            if (_station.GetOwningStation(humanoid) != station || xform.GridUid != userGrid)
                continue;

            var name = CompOrNull<MindComponent>(humanoid)?.CharacterName ?? MetaData(humanoid).EntityName;
            if (string.IsNullOrWhiteSpace(name) || present.ContainsKey(name))
                continue;

            present.Add(name, humanoid);
        }

        return present;
    }

    private static string Normalize(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
