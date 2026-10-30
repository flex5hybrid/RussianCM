using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines.Squads.Specialization;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Marines.Squads.Specialization;

public sealed partial class SquadSpecializationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SquadSystem _squad = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SquadSpecializationTabletComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<SquadSpecializationTabletComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<SquadMemberAddedEvent>(OnSquadMemberAdded);
        SubscribeLocalEvent<SquadMemberRemovedEvent>(OnSquadMemberRemoved);

        Subs.BuiEvents<SquadSpecializationTabletComponent>(SquadSpecializationUI.Key, subs =>
        {
            subs.Event<SquadSpecializationSelectBuiMsg>(OnSpecializationSelected);
        });
    }

    private void OnUIOpenAttempt(Entity<SquadSpecializationTabletComponent> tablet, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;

        if (!TryComp<SquadMemberComponent>(user, out var member) || member.Squad is not { } squad)
        {
            _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-no-squad"), tablet, user, PopupType.SmallCaution);
            args.Cancel();
            return;
        }

        if (!HasComp<SquadLeaderComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-not-leader"), tablet, user, PopupType.SmallCaution);
            args.Cancel();
            return;
        }
    }

    private void OnUIOpened(Entity<SquadSpecializationTabletComponent> tablet, ref BoundUIOpenedEvent args)
    {
        UpdateUIState(tablet, args.Actor);
    }

    private void UpdateUIState(Entity<SquadSpecializationTabletComponent> tablet, EntityUid actor)
    {
        if (!TryComp<SquadMemberComponent>(actor, out var member) || member.Squad is not { } squad)
            return;

        var squadName = Name(squad);
        var squadColor = TryComp<SquadTeamComponent>(squad, out var team) ? team.Color : Color.White;

        var squadSpec = EnsureComp<SquadSpecializationComponent>(squad);
        var currentSpec = squadSpec.Specialization;
        var isLocked = squadSpec.Locked;

        var available = new List<ProtoId<SquadSpecializationPrototype>>();
        foreach (var proto in _prototypes.EnumeratePrototypes<SquadSpecializationPrototype>())
        {
            if (tablet.Comp.Faction != null)
            {
                if (proto.Faction != null && !string.Equals(proto.Faction, tablet.Comp.Faction, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            available.Add(proto.ID);
        }

        var taken = new Dictionary<ProtoId<SquadSpecializationPrototype>, string>();
        var squads = EntityQueryEnumerator<SquadSpecializationComponent, MetaDataComponent>();
        while (squads.MoveNext(out var sUid, out var sSpec, out var sMeta))
        {
            if (sUid != squad && sSpec.Locked && sSpec.Specialization != null)
            {
                taken[sSpec.Specialization.Value] = sMeta.EntityName;
            }
        }

        var state = new SquadSpecializationBuiState(squadName, squadColor, currentSpec, isLocked, available, taken);
        _ui.SetUiState(tablet.Owner, SquadSpecializationUI.Key, state);
    }

    private void OnSpecializationSelected(Entity<SquadSpecializationTabletComponent> tablet, ref SquadSpecializationSelectBuiMsg args)
    {
        var actor = args.Actor;

        if (!TryComp<SquadMemberComponent>(actor, out var member) || member.Squad is not { } squad)
            return;

        if (!HasComp<SquadLeaderComponent>(actor))
        {
            _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-not-leader"), tablet, actor, PopupType.SmallCaution);
            return;
        }

        var squadSpec = EnsureComp<SquadSpecializationComponent>(squad);
        if (squadSpec.Locked)
        {
            _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-already-locked"), tablet, actor, PopupType.SmallCaution);
            return;
        }

        if (!_prototypes.TryIndex(args.Specialization, out var proto))
            return;

        // Check if another squad has already claimed and locked this doctrine
        var allSquads = EntityQueryEnumerator<SquadSpecializationComponent, MetaDataComponent>();
        while (allSquads.MoveNext(out var otherSquadUid, out var otherSpec, out var otherMeta))
        {
            if (otherSquadUid != squad && otherSpec.Locked && otherSpec.Specialization == args.Specialization)
            {
                _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-already-taken", ("specialization", Loc.GetString(proto.Name)), ("squad", otherMeta.EntityName)), tablet, actor, PopupType.SmallCaution);
                UpdateUIState(tablet, actor);
                return;
            }
        }

        squadSpec.Specialization = args.Specialization;
        squadSpec.Locked = true;
        squadSpec.SelectedAt = _timing.CurTime;
        Dirty(squad, squadSpec);

        SyncSquadMembersSpecialization(squad, args.Specialization, squadSpec.SelectedAt);

        _audio.PlayPvs(ConfirmSound, tablet);

        var specName = Loc.GetString(proto.Name);
        _popup.PopupEntity(Loc.GetString("rmc-squad-specialization-confirmed", ("specialization", specName)), tablet, actor, PopupType.Medium);

        // Radio broadcast to squad channel
        if (TryComp<SquadTeamComponent>(squad, out var team) && team.Radio is { } radioChannel)
        {
            var msg = proto.Announcement != null
                ? Loc.GetString(proto.Announcement.Value)
                : Loc.GetString("rmc-squad-specialization-announced", ("squad", Name(squad)), ("specialization", specName));

            _radio.SendRadioMessage(actor, msg, radioChannel, actor);
        }

        UpdateUIState(tablet, actor);
    }

    public void SyncSquadMembersSpecialization(EntityUid squad, ProtoId<SquadSpecializationPrototype> specId, TimeSpan? selectedAt = null)
    {
        var members = EntityQueryEnumerator<SquadMemberComponent>();
        while (members.MoveNext(out var memberUid, out var member))
        {
            if (member.Squad == squad)
            {
                var memberSpec = EnsureComp<SquadSpecializationComponent>(memberUid);
                memberSpec.Specialization = specId;
                memberSpec.Locked = true;
                memberSpec.SelectedAt = selectedAt;
                Dirty(memberUid, memberSpec);
            }
        }
    }

    private void OnSquadMemberAdded(ref SquadMemberAddedEvent ev)
    {
        if (TryComp<SquadSpecializationComponent>(ev.Squad, out var specComp) && specComp.Specialization != null)
        {
            var memberSpec = EnsureComp<SquadSpecializationComponent>(ev.Member);
            memberSpec.Specialization = specComp.Specialization;
            memberSpec.Locked = true;
            memberSpec.SelectedAt = specComp.SelectedAt;
            Dirty(ev.Member, memberSpec);
        }
    }

    private void OnSquadMemberRemoved(ref SquadMemberRemovedEvent ev)
    {
        RemComp<SquadSpecializationComponent>(ev.Member);
    }
}
