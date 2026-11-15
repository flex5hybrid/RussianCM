using System;
using System.Linq;
using Content.Server._RuCM.Qualifications;
using Content.Server.Chat.Managers;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Administration.Logs;
using Content.Shared.CMU14.Qualifications.Training;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Prototypes;
using Content.Shared.CMU14.Round.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Qualifications.Training;

[CVarDefs]
public sealed class CMUTrainingCVars
{
    public static readonly CVarDef<int> MaxRecruits = CVarDef.Create(
        "rucm.qualifications.max_recruits_per_instructor", 4, CVar.SERVERONLY);
}

/// <summary>Round-only assignments. Every mutation names exactly one live recruit.</summary>
public sealed class CMUGovforTrainingSystem : EntitySystem
{
    [Dependency] private QualificationSystem _qualifications = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SkillsSystem _skills = default!;
    [Dependency] private SquadLeaderTrackerSystem _tracker = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private sealed class Assignment(Guid recruitId, EntityUid recruit, Guid instructorId, EntityUid instructor)
    {
        public readonly Guid RecruitId = recruitId;
        public readonly EntityUid Recruit = recruit;
        public readonly Guid InstructorId = instructorId;
        public readonly EntityUid Instructor = instructor;
        public string? Topic;
    }
    private readonly Dictionary<Guid, Assignment> _assignments = new();
    private readonly Dictionary<Guid, Guid> _tracking = new();
    private readonly HashSet<EntityUid> _ownedTrackers = new();
    private float _refresh;
    private bool _viewsDirty;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CMUTrainingParticipantComponent, PlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<CMUTrainingParticipantComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<CMUTrainingParticipantComponent, ComponentShutdown>(OnParticipantShutdown);
        SubscribeLocalEvent<CMUTrainingSkillsComponent, CMUTrainingSkillsQueryEvent>(OnSkillQuery);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ResetRound());
        SubscribeLocalEvent<GameRunLevelChangedEvent>(ev => { if (ev.New != GameRunLevel.InRound) ResetRound(); });
        _players.PlayerStatusChanged += OnStatusChanged;
    }
    public override void Shutdown()
    {
        ResetRound();
        _players.PlayerStatusChanged -= OnStatusChanged;
        base.Shutdown();
    }

    private bool Live(EntityUid body, out ICommonSession session, out QualificationAuthority authority)
    {
        session = default!; authority = default!;
        if (TerminatingOrDeleted(body) || !TryComp<ActorComponent>(body, out var actor))
            return false;
        session = actor.PlayerSession;
        if (session.Status is not (SessionStatus.Connected or SessionStatus.InGame) || session.AttachedEntity != body || _qualifications.IsSynthetic(session.UserId))
            return false;
        authority = _qualifications.Authority(session);
        return authority.CurrentParticipant;
    }
    public bool IsRecruit(EntityUid body) =>
        Live(body, out _, out var authority) && authority.Context.Job == GOVFORRecruitJob.Id;

    private bool IsInstructor(EntityUid body) => IsInstructor(body, out _);

    private bool IsInstructor(EntityUid body, out QualificationAuthority authority) =>
        Live(body, out var player, out authority) && _qualifications.Service.Available
        && _qualifications.IsInsurgency && _cfg.GetCVar(QualificationCVars.Enabled)
        && authority.Context.Job != GOVFORRecruitJob.Id
        && ProtoMan.TryIndex<JobPrototype>(authority.Context.Job, out var job) && job.RoundSide == RoundJobSide.Govfor
        && (_qualifications.Service.IsManagement(authority) || _qualifications.Service.IsActiveInstructor(player.UserId))
        && (!QualificationRules.IsDrillInstructor(authority.Context.Job)
            || _qualifications.Service.IsJobAllowed(player.UserId, authority.Context.Job));

    private bool Resolve(Guid account, out EntityUid body)
    {
        body = default;
        if (!_players.TryGetSessionById(new NetUserId(account), out var session) || session.AttachedEntity is not { } entity)
            return false;
        body = entity;
        return true;
    }
    public Guid? AssignedInstructor(Guid recruit) => _assignments.TryGetValue(recruit, out var value) ? value.InstructorId : null;
    public string? ActiveTopic(Guid recruit) => _assignments.TryGetValue(recruit, out var value) ? value.Topic : null;

    public bool Owns(EntityUid instructor, EntityUid recruit) =>
        Owns(instructor, recruit, out _);

    private bool Owns(EntityUid instructor, EntityUid recruit, out QualificationAuthority authority) =>
        IsInstructor(instructor, out authority) &&
        Live(recruit, out var player, out _) && _assignments.TryGetValue(player.UserId, out var assignment)
        && assignment.Recruit == recruit && assignment.Instructor == instructor
        && IsRecruit(recruit);

    /// <summary>Called only through the existing size/rate-limited qualification request queue.</summary>
    public string Apply(ICommonSession player, QualificationAction action, QualificationRequest request)
    {
        if (!_qualifications.Service.Available || !_qualifications.IsInsurgency)
            return "permission";
        var authority = _qualifications.Authority(player);
        var manager = _qualifications.Service.IsManagement(authority) && !_qualifications.IsSynthetic(player.UserId);
        if (!Resolve(request.Target, out var recruit) || !IsRecruit(recruit))
            return "cmu-training-recruit";
        if (action == QualificationAction.TrainingAssign)
        {
            var id = manager && request.Instructor != Guid.Empty ? request.Instructor : (Guid) player.UserId;
            if (!Resolve(id, out var instructor) || !IsInstructor(instructor))
                return "cmu-training-instructor";
            if (!manager && (id != player.UserId || player.AttachedEntity != instructor))
                return "permission";
            return Assign(instructor, recruit, manager);
        }
        if (action == QualificationAction.TrainingRelease)
        {
            if (!manager && (player.AttachedEntity is not { } owner || !Owns(owner, recruit)))
                return "permission";
            Remove(request.Target, "released");
            return "";
        }
        if (player.AttachedEntity is not { } actor || !Owns(actor, recruit))
            return "permission";
        return action switch
        {
            QualificationAction.TrainingStart => Start(actor, recruit, request.Qualification),
            QualificationAction.TrainingFinish => Finish(request.Target, "instructor finished"),
            QualificationAction.TrainingTrack => Track(player.UserId, request.Target),
            _ => "request",
        };
    }

    public string Assign(EntityUid instructor, EntityUid recruit, bool reassign = false)
    {
        if (!IsRecruit(recruit) || !IsInstructor(instructor) || instructor == recruit ||
            !Live(recruit, out var recruitPlayer, out _) || !Live(instructor, out var teacher, out _))
            return "cmu-training-recruit";
        var recruitId = (Guid) recruitPlayer.UserId;
        var instructorId = (Guid) teacher.UserId;
        if (_assignments.TryGetValue(recruitId, out var old))
        {
            if (old.Instructor == instructor) return "";
            if (!reassign) return "cmu-training-assigned";
        }
        if (_assignments.Values.Count(a => a.InstructorId == instructorId) >= Math.Clamp(_cfg.GetCVar(CMUTrainingCVars.MaxRecruits), 0, 128))
            return "cmu-training-limit";
        if (old != null)
        {
            _logs.Add(LogType.Action, LogImpact.Medium,
                $"Recruit {ToPrettyString(recruit)} reassigned from {ToPrettyString(old.Instructor)} to {ToPrettyString(instructor)}");
            Remove(recruitId, "reassigned");
        }
        _assignments[recruitId] = new(recruitId, recruit, instructorId, instructor);
        EnsureComp<CMUTrainingParticipantComponent>(recruit);
        EnsureComp<CMUTrainingParticipantComponent>(instructor);
        SetNavigation(recruit, instructor, true);
        _logs.Add(LogType.Action, LogImpact.Medium, $"Instructor {ToPrettyString(instructor)} assigned recruit {ToPrettyString(recruit)}");
        _viewsDirty = true;
        return "";
    }

    public string Start(EntityUid instructor, EntityUid recruit, string topicId)
    {
        if (!Owns(instructor, recruit, out var teacher) || !Live(recruit, out var recruitPlayer, out _) ||
            !ProtoMan.TryIndex<CMUTrainingTopicPrototype>(topicId, out var topic) ||
            !_qualifications.Service.CMUCanTeach(teacher, topic.Qualification, topic.Item) ||
            topic.TemporarySkills.Any(s => s.Value is < 1 or > 5 || !ProtoMan.TryIndex(s.Key, out var skill) || !skill.HasComponent<SkillDefinitionComponent>()))
            return "cmu-training-topic";
        var assignment = _assignments[recruitPlayer.UserId];
        Finish(recruitPlayer.UserId, "topic changed");
        assignment.Topic = topic.ID;
        var overlay = EnsureComp<CMUTrainingSkillsComponent>(recruit);
        overlay.Skills = new(topic.TemporarySkills);
        Dirty(recruit, overlay);
        foreach (var (skill, _) in overlay.Skills)
        {
            var changed = new SkillChangedEvent(recruit, skill, _skills.GetSkill(recruit, skill));
            RaiseLocalEvent(recruit, ref changed);
        }
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"Instructor {ToPrettyString(instructor)} started {topic.ID} for recruit {ToPrettyString(recruit)}. Temporary skills: {string.Join(", ", topic.TemporarySkills.Select(s => s.Key + "=" + s.Value))}");
        _viewsDirty = true;
        return "";
    }

    public string Finish(Guid recruit, string reason)
    {
        if (!_assignments.TryGetValue(recruit, out var assignment) || assignment.Topic == null)
            return "";
        var topic = assignment.Topic;
        assignment.Topic = null; // Invalidate before any skill-change handler can query the overlay.
        if (!TerminatingOrDeleted(assignment.Recruit) && TryComp<CMUTrainingSkillsComponent>(assignment.Recruit, out var skills))
        {
            var changedSkills = skills.Skills.Keys.ToArray();
            RemComp<CMUTrainingSkillsComponent>(assignment.Recruit);
            foreach (var skill in changedSkills)
            {
                var changed = new SkillChangedEvent(assignment.Recruit, skill, _skills.GetSkill(assignment.Recruit, skill));
                RaiseLocalEvent(assignment.Recruit, ref changed);
            }
        }
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"Training {topic} for recruit {recruit} with instructor {assignment.InstructorId} ended: {reason}");
        _viewsDirty = true;
        return "";
    }
    private string Track(Guid instructor, Guid recruit)
    {
        _tracking[instructor] = recruit;
        var assignment = _assignments[recruit];
        SetNavigation(assignment.Instructor, assignment.Recruit, false);
        return "";
    }

    private void Remove(Guid recruit, string reason)
    {
        if (!_assignments.TryGetValue(recruit, out var assignment)) return;
        Finish(recruit, reason);
        _assignments.Remove(recruit);
        ClearNavigation(assignment.Recruit);
        if (_tracking.GetValueOrDefault(assignment.InstructorId) == recruit)
        {
            _tracking.Remove(assignment.InstructorId);
            ClearNavigation(assignment.Instructor);
        }
        if (reason == "instructor unavailable" && Live(assignment.Recruit, out var player, out _))
            _chat.DispatchServerMessage(player, Loc.GetString("cmu-training-instructor-lost"));
        _logs.Add(LogType.Action, LogImpact.Medium, $"Recruit {recruit} assignment removed: {reason}");
        _viewsDirty = true;
    }
    private void SetNavigation(EntityUid owner, EntityUid target, bool instructor)
    {
        if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(target)) return;
        if (!HasComp<SquadLeaderTrackerComponent>(owner))
        {
            EnsureComp<SquadLeaderTrackerComponent>(owner);
            _ownedTrackers.Add(owner);
        }
        var navigation = EnsureComp<CMUTrainingNavigationComponent>(owner);
        var coordinates = _transform.GetMapCoordinates(target);
        var name = Name(target);
        if (navigation.Instructor == instructor && navigation.TargetName == name && navigation.Coordinates == coordinates)
            return;
        navigation.Instructor = instructor;
        navigation.TargetName = name;
        navigation.Coordinates = coordinates;
        Dirty(owner, navigation);
        Comp<SquadLeaderTrackerComponent>(owner).UpdateAt = default;
    }
    private void ClearNavigation(EntityUid owner)
    {
        if (TerminatingOrDeleted(owner)) { _ownedTrackers.Remove(owner); return; }
        RemComp<CMUTrainingNavigationComponent>(owner);
        _tracker.ClearCMUTrainingNavigation(owner);
        if (_ownedTrackers.Remove(owner))
            RemComp<SquadLeaderTrackerComponent>(owner);
    }
    private void OnSkillQuery(Entity<CMUTrainingSkillsComponent> ent, ref CMUTrainingSkillsQueryEvent args)
    {
        args.Cancelled = !Live(ent.Owner, out var player, out _) ||
            !_assignments.TryGetValue(player.UserId, out var assignment) || assignment.Recruit != ent.Owner ||
            assignment.Topic == null || !Owns(assignment.Instructor, assignment.Recruit, out var instructor) ||
            !ProtoMan.TryIndex<CMUTrainingTopicPrototype>(assignment.Topic, out var topic) ||
            !_qualifications.Service.CMUCanTeach(instructor, topic.Qualification, topic.Item);
    }
    private void RemoveEntity(EntityUid entity)
    {
        foreach (var assignment in _assignments.Values.Where(a => a.Recruit == entity || a.Instructor == entity).ToArray())
            Remove(assignment.RecruitId, assignment.Instructor == entity ? "instructor unavailable" : "recruit unavailable");
    }
    private void OnDetached(Entity<CMUTrainingParticipantComponent> ent, ref PlayerDetachedEvent args) => RemoveEntity(ent.Owner);
    private void OnTerminating(Entity<CMUTrainingParticipantComponent> ent, ref EntityTerminatingEvent args) => RemoveEntity(ent.Owner);
    private void OnParticipantShutdown(Entity<CMUTrainingParticipantComponent> ent, ref ComponentShutdown args) => RemoveEntity(ent.Owner);
    private void OnStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus is SessionStatus.Connected or SessionStatus.InGame) return;
        foreach (var assignment in _assignments.Values.Where(a => a.RecruitId == args.Session.UserId || a.InstructorId == args.Session.UserId).ToArray())
            Remove(assignment.RecruitId, assignment.InstructorId == args.Session.UserId ? "instructor unavailable" : "recruit unavailable");
    }
    private void ResetRound()
    {
        foreach (var recruit in _assignments.Keys.ToArray()) Remove(recruit, "round ended");
        _tracking.Clear();
    }
    public override void Update(float frameTime)
    {
        // No session outlives a disabled training system, including in a still-running round.
        _refresh -= frameTime;
        if (_refresh > 0) return;
        _refresh = 0.5f;
        foreach (var assignment in _assignments.Values.ToArray())
        {
            if (!IsInstructor(assignment.Instructor, out var instructor)) { Remove(assignment.RecruitId, "instructor unavailable"); continue; }
            if (!IsRecruit(assignment.Recruit)) { Remove(assignment.RecruitId, "recruit unavailable"); continue; }
            if (assignment.Topic is { } topicId &&
                (!ProtoMan.TryIndex<CMUTrainingTopicPrototype>(topicId, out var topic) ||
                 !_qualifications.Service.CMUCanTeach(instructor, topic.Qualification, topic.Item)))
                Finish(assignment.RecruitId, "teaching authority or topic removed");
            if (assignment.Topic != null && !HasComp<CMUTrainingSkillsComponent>(assignment.Recruit))
                Finish(assignment.RecruitId, "temporary skill component removed");
            SetNavigation(assignment.Recruit, assignment.Instructor, true);
        }
        foreach (var (instructor, recruit) in _tracking)
            if (_assignments.TryGetValue(recruit, out var assignment))
                SetNavigation(assignment.Instructor, assignment.Recruit, false);
        if (_viewsDirty) { _viewsDirty = false; _qualifications.RefreshTrainingViews(); }
    }

    public void Decorate(QualificationView view, ICommonSession viewer)
    {
        var body = viewer.AttachedEntity;
        QualificationAuthority instructor = default!;
        var canTeach = body is { } teacher && IsInstructor(teacher, out instructor);
        view.TrainingInstructor = canTeach;
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } entity) continue;
            if (view.Management && IsInstructor(entity)) view.TrainingInstructors[session.UserId] = Name(entity);
            if (!IsRecruit(entity)) continue;
            _assignments.TryGetValue(session.UserId, out var assignment);
            if (!view.Management && (!canTeach || assignment != null && assignment.InstructorId != viewer.UserId)
                && session.UserId != viewer.UserId) continue;
            if (assignment != null && !TerminatingOrDeleted(assignment.Instructor))
                view.TrainingInstructors[assignment.InstructorId] = Name(assignment.Instructor);
            var (completed, required) = _qualifications.Service.CMUChecklistSummary(session.UserId);
            var entry = new CMUTrainingRosterEntry { Recruit = session.UserId, Name = Name(entity),
                Instructor = assignment?.InstructorId, Topic = assignment?.Topic ?? "", Completed = completed,
                Required = required, Available = Live(entity, out _, out _) };
            if (assignment?.Topic is { } current && ProtoMan.TryIndex<CMUTrainingTopicPrototype>(current, out var currentTopic))
                entry.TopicName = currentTopic.Name;
            if (body is { } owner)
            {
                var from = _transform.GetMapCoordinates(owner);
                var to = _transform.GetMapCoordinates(entity);
                if (from.MapId == to.MapId) entry.Distance = (from.Position - to.Position).Length();
            }
            view.TrainingRecruits.Add(entry);
        }
        if (canTeach)
            foreach (var topic in ProtoMan.EnumeratePrototypes<CMUTrainingTopicPrototype>())
                if (_qualifications.Service.CMUCanTeach(instructor, topic.Qualification, topic.Item))
                    view.TrainingTopics.Add(new CMUTrainingTopicView { Id = topic.ID, Name = topic.Name,
                        Qualification = topic.Qualification, Item = topic.Item,
                        Skills = topic.TemporarySkills.ToDictionary(s => ProtoMan.Index(s.Key).Name, s => s.Value) });
    }
}
