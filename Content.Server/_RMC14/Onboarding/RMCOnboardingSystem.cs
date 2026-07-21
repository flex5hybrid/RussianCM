using System.Linq;
using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Chat.V2;
using Content.Server.Corvax.TTS;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Server._CMU14.Medical.Anatomy.BodyParts;
using Content.Server._CMU14.Medical.Anatomy.Bones;
using Content.Shared.Access.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared._CMU14.Medical.Anatomy.Bones;
using Content.Shared._CMU14.Medical.Injuries.Trauma;
using Content.Shared._CMU14.Medical.Injuries.Wounds.Events;
using Content.Shared._RMC14.Armor.Magnetic;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.EntityPreset;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.CPR;
using Content.Shared._RMC14.Onboarding;
using Content.Shared.CombatMode;
using Content.Shared.Corvax.TTS;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Roles;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Onboarding;

public sealed partial class RMCOnboardingSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private BodyPartHealthSystem _bodyPartHealth = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityPresetSystem _entityPreset = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private FractureSystem _fracture = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SkillsSystem _skills = default!;
    [Dependency] private ITileDefinitionManager _tile = default!;
    [Dependency] private TransformSystem _transform = default!;

    private readonly RMCOnboardingSessionStore<RMCOnboardingSession> _activeSessions = new();
    private readonly Dictionary<NetUserId, HashSet<RMCOnboardingTrack>> _completed = new();

    private enum RMCOnboardingEndReason : byte
    {
        Completed,
        ManualExit,
        Disconnected,
        Died,
        CharacterDeleted,
        MapDeleted,
        RoundRestart,
        LoadFailed,
    }

    private sealed record RMCOnboardingStep(
        RMCOnboardingStepKind Kind,
        string Title,
        string LinePrefix,
        int LineCount,
        string Completed,
        float LineDelay = 6f);

    private sealed class RMCOnboardingSession(
        NetUserId userId,
        RMCOnboardingTrack track,
        EntityUid mapEntity,
        MapId map,
        EntityUid grid,
        EntityUid ares,
        EntityUid foodVendor,
        EntityUid mob,
        EntityUid mind,
        TimeSpan startedAt)
    {
        public NetUserId UserId = userId;
        public RMCOnboardingTrack Track = track;
        public EntityUid MapEntity = mapEntity;
        public MapId Map = map;
        public EntityUid Grid = grid;
        public EntityUid Ares = ares;
        public EntityUid FoodVendor = foodVendor;
        public EntityUid Mob = mob;
        public EntityUid Mind = mind;
        public EntityUid? ObjectiveEntity;
        public TimeSpan StartedAt = startedAt;
        public List<EntityUid> StepEntities = new();
        public int Step;
        public int Line;
        public bool Ready;
        public TimeSpan NextSpeech;
        public bool StepStartPending;
        public TimeSpan StepStartAt;
        public EntityUid? FoodEntity;
        public int HandSwitches;
        public bool FoodFinished;
        public bool CompletionPending;
        public TimeSpan CompleteAt;
        public bool WaitingForInstructorTts;
        public bool InstructorTtsUnavailable;
        public bool ReadyAfterSpeech;
        public uint InstructorPlaybackId;
        public TimeSpan InstructorFallbackAt;
        public EntityUid? Drone;
        public bool DroneKilled;
        public EntityUid? IssuedGrenade;
        public List<EntityUid> EquipmentVendors = new();
        public List<EntityUid> GrenadeTargets = new();
        public bool GrenadeRespawnPending;
        public bool GrenadeMissingCheckPending;
        public bool GrenadeTargetsKilled;
        public bool GrenadePickedUp;
        public bool GrenadeAttemptActive;
        public bool GrenadeTargetDamaged;
        public FixedPoint2 GrenadePlayerDamageAtActivation;
        public Dictionary<EntityUid, FixedPoint2> GrenadeTargetDamageAtActivation = new();
        public TimeSpan GrenadeResolutionAt;
        public EntityUid? MedicalPatient;
        public EntityUid? CprBody;
        public Vector2 MedicalSafePosition = new(0.5f, 0.5f);
        public bool MedicalPatientExamined;
        public int MedicalWoundsTreated;
        public bool TramadolGiven;
        public bool TricordrazineGiven;
        public int CprSuccesses;
    }

    private static readonly RMCOnboardingStep[] FullNewbieSteps =
    [
        Step(RMCOnboardingStepKind.ApproachFoodVendor, "approach-food", 3),
        Step(RMCOnboardingStepKind.TakeFood, "take-food", 2),
        Step(RMCOnboardingStepKind.SwitchHandsTwice, "switch-hands", 2),
        Step(RMCOnboardingStepKind.TakeFirstBite, "first-bite", 2),
        Step(RMCOnboardingStepKind.FinishFood, "finish-food", 1),
        Step(RMCOnboardingStepKind.SayNearby, "say-nearby", 3),
        Step(RMCOnboardingStepKind.SayLooc, "say-looc", 2),
    ];

    private static readonly RMCOnboardingStep[] MilitaryEquipmentSteps =
    [
        Step(RMCOnboardingStepKind.SayNearby, "military-uniform", 2),
        Step(RMCOnboardingStepKind.SayNearby, "military-vendors", 6),
        Step(RMCOnboardingStepKind.InsertMagazine, "military-magazine", 5),
        Step(RMCOnboardingStepKind.KillDrone, "military-drone", 1),
        Step(RMCOnboardingStepKind.AttachSling, "military-sling", 3),
        Step(RMCOnboardingStepKind.DropSlungWeapon, "military-drop-sling", 1),
        Step(RMCOnboardingStepKind.PickUpGrenade, "military-grenade-pickup", 2),
        Step(RMCOnboardingStepKind.KillGrenadeTargets, "military-grenade-throw", 2),
        Step(RMCOnboardingStepKind.Automatic, "military-finish", 1),
    ];

    private static readonly RMCOnboardingStep[] MedicalSteps =
    [
        Step(RMCOnboardingStepKind.MoveMedicalPatient, "medical-move-patient", 4),
        Step(RMCOnboardingStepKind.ExamineMedicalPatient, "medical-examine", 2),
        Step(RMCOnboardingStepKind.TreatFirstBleed, "medical-first-bleed", 3),
        Step(RMCOnboardingStepKind.TreatSecondBleed, "medical-second-bleed", 1),
        Step(RMCOnboardingStepKind.GiveTramadol, "medical-tramadol", 1),
        Step(RMCOnboardingStepKind.GiveTricordrazine, "medical-tricordrazine", 1),
        Step(RMCOnboardingStepKind.MedicalRecovery, "medical-recovery", 3),
        Step(RMCOnboardingStepKind.MedicalAftercare, "medical-aftercare", 3),
        Step(RMCOnboardingStepKind.PerformCpr, "medical-cpr", 6),
        Step(RMCOnboardingStepKind.Automatic, "medical-first-aid-finish", 1),
    ];

    private static readonly ResPath FirstLevelMap = new("/Maps/_RUCM/Guides/FirstLevel.yml");
    private static readonly ResPath MedicalLevelMap = new("/Maps/_RUCM/Guides/MedicalLevel.yml");
    private static readonly EntProtoId<SkillPresetComponent> RiflemanSkillPreset = "AU14SkillPresetPlatoonRifleman";

    private const int ArenaHalfWidth = 10;
    private const int ArenaHalfHeight = 7;
    private const string TrainingFloor = "FloorSteel";
    private const string BriefingFloor = "CMFloorWood";
    private const string CleanFloor = "CMFloorSteelWhiteFull";
    private const string MedicalFloor = "RMCFloorVehicleInteriorMedType";
    private const string EngineeringFloor = "RMCFloorVehicleInteriorFloor0";
    private const string TrainingWall = "CMWallMetal";
    private const string TrainingAres = "RMCAICore";
    private const string TrainingAresVoice = "GLADOS";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<InputMoverComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<ItemComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<ItemComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<ItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<InputMoverComponent, ActiveHandChangedEvent>(OnActiveHandChanged);
        SubscribeLocalEvent<FoodComponent, AfterFoodEatenEvent>(OnFoodEaten);
        SubscribeLocalEvent<FoodComponent, AfterFullyEatenEvent>(OnFoodFullyEaten);
        SubscribeLocalEvent<HeadsetComponent, MapInitEvent>(OnTrainingHeadsetMapInit);
        SubscribeLocalEvent<MobStateComponent, ExaminedEvent>(OnMedicalPatientExamined);
        SubscribeLocalEvent<WoundTreatedEvent>(OnMedicalWoundTreated);
        SubscribeLocalEvent<CPRAttemptFinishedEvent>(OnMedicalCprFinished);
        SubscribeLocalEvent<GunComponent, EntInsertedIntoContainerMessage>(OnMagazineInserted);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<ItemComponent, DroppedEvent>(OnMagneticWeaponDropped);
        SubscribeLocalEvent<InputMoverComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<InputMoverComponent, LoocCreatedEvent>(OnLoocSpoke);
        SubscribeLocalEvent<TTSComponent, TTSUtteranceDispatchedEvent>(OnTtsDispatched);
        SubscribeLocalEvent<TTSComponent, TTSUtteranceUnavailableEvent>(OnTtsUnavailable);
        SubscribeLocalEvent<ActiveTimerTriggerEvent>(OnActiveTimerTrigger);
        SubscribeLocalEvent<TriggerEvent>(OnGrenadeTriggered);
        SubscribeLocalEvent<InputMoverComponent, ToggleCombatActionEvent>(OnToggleCombat,
            after: new[] { typeof(SharedCombatModeSystem) });
        SubscribeLocalEvent<RMCOnboardingTrainingObjectComponent, InteractHandEvent>(OnTrainingObjectInteract);
        SubscribeNetworkEvent<RMCOnboardingRequestMenuEvent>(OnRequestMenu);
        SubscribeNetworkEvent<RMCOnboardingSelectTrackEvent>(OnSelectTrack);
        SubscribeNetworkEvent<RMCOnboardingExitEvent>(OnExit);
        SubscribeNetworkEvent<TTSPlaybackFinishedEvent>(OnTtsPlaybackFinished);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeSessions.Count == 0)
            return;

        foreach (var (userId, active) in _activeSessions.Snapshot())
        {
            if (active.GrenadeRespawnPending && _timing.CurTime >= active.GrenadeResolutionAt)
            {
                if (_players.TryGetSessionById(userId, out var retrySession))
                    RespawnGrenadeAttempt(active, retrySession);
                else
                    EndOnboarding(userId, RMCOnboardingEndReason.Disconnected, returnToLobby: false);

                continue;
            }

            if (active.GrenadeMissingCheckPending && _timing.CurTime >= active.GrenadeResolutionAt)
            {
                active.GrenadeMissingCheckPending = false;
                active.GrenadeAttemptActive = false;
                if (!HasGrenadeTargetTakenDamage(active) || HasGrenadePlayerTakenDamage(active))
                {
                    active.GrenadeRespawnPending = true;
                    active.GrenadeResolutionAt = _timing.CurTime + TimeSpan.FromSeconds(0.1);
                    continue;
                }

                CleanupGrenadeTargets(active);
                active.GrenadeTargetsKilled = true;
                if (_players.TryGetSessionById(userId, out var successSession))
                    TryCompleteStep(successSession, RMCOnboardingStepKind.KillGrenadeTargets);
            }

            if (active.WaitingForInstructorTts &&
                active.InstructorTtsUnavailable &&
                _timing.CurTime >= active.InstructorFallbackAt)
            {
                FinishInstructorSpeech(active);
            }

            if (active.CompletionPending)
            {
                if (!active.WaitingForInstructorTts && _timing.CurTime >= active.CompleteAt)
                    EndOnboarding(userId, RMCOnboardingEndReason.Completed);

                continue;
            }

            if (active.StepStartPending)
            {
                if (_timing.CurTime >= active.StepStartAt)
                {
                    active.StepStartPending = false;
                    StartCurrentStep(active, 1f);
                }

                continue;
            }

            ProcessSpeechQueue(userId, active);

            if (!active.Ready || !_players.TryGetSessionById(userId, out var session))
                continue;

            var steps = GetSteps(active.Track);
            if (active.Step < 0 || active.Step >= steps.Count)
                continue;

            var kind = steps[active.Step].Kind;
            if (kind == RMCOnboardingStepKind.ApproachFoodVendor &&
                !Deleted(active.FoodVendor) &&
                _transform.GetMapCoordinates(active.Mob).InRange(_transform.GetMapCoordinates(active.FoodVendor), 2.25f))
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.FinishFood && active.FoodFinished)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.KillDrone && active.Drone is null && !active.DroneKilled)
            {
                active.Drone = SpawnAtPosition(
                    "CMXenoDrone",
                    new EntityCoordinates(active.Grid, new Vector2(0.5f, 3.25f)));
            }
            else if (kind == RMCOnboardingStepKind.KillDrone && active.DroneKilled)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.KillGrenadeTargets && active.GrenadeTargetsKilled)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.PickUpGrenade && active.GrenadePickedUp)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.MoveMedicalPatient &&
                     active.MedicalPatient is { } patient &&
                     !Deleted(patient) &&
                     Vector2.Distance(Transform(patient).Coordinates.Position, active.MedicalSafePosition) <= 1.25f)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.ExamineMedicalPatient && active.MedicalPatientExamined)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.TreatFirstBleed && active.MedicalWoundsTreated >= 1)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.TreatSecondBleed && active.MedicalWoundsTreated >= 2)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.GiveTramadol && active.TramadolGiven)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.GiveTricordrazine && active.TricordrazineGiven)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.MedicalRecovery)
            {
                RecoverMedicalPatient(active);
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.MedicalAftercare)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.PerformCpr && active.CprSuccesses >= 3)
            {
                TryCompleteStep(session, kind);
            }
            else if (kind == RMCOnboardingStepKind.Automatic)
            {
                TryCompleteStep(session, kind);
            }
        }
    }

    private void OnRequestMenu(RMCOnboardingRequestMenuEvent ev, EntitySessionEventArgs args)
    {
        if (!_config.GetCVar(RMCCVars.RMCOnboardingEnabled))
            return;

        if (!IsInLobby(args.SenderSession) || _activeSessions.Contains(args.SenderSession.UserId))
            return;

        var tracks = new RMCOnboardingTrackStatus[RMCOnboardingTracks.Default.Length];
        for (var i = 0; i < RMCOnboardingTracks.Default.Length; i++)
        {
            var track = RMCOnboardingTracks.Default[i];
            tracks[i] = new RMCOnboardingTrackStatus(track, track.IsAvailable(), IsCompleted(args.SenderSession.UserId, track));
        }

        RaiseNetworkEvent(new RMCOnboardingOfferEvent(tracks), args.SenderSession.Channel);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        foreach (var userId in _activeSessions.UserSnapshot())
            EndOnboarding(userId, RMCOnboardingEndReason.RoundRestart, returnToLobby: false);
    }

    private void OnSelectTrack(RMCOnboardingSelectTrackEvent ev, EntitySessionEventArgs args)
    {
        if (!_config.GetCVar(RMCCVars.RMCOnboardingEnabled))
            return;

        if (!ev.Accepted)
            return;

        if (!Enum.IsDefined(ev.Track) || !ev.Track.IsAvailable())
            return;

        if (_activeSessions.Contains(args.SenderSession.UserId))
        {
            _chat.DispatchServerMessage(args.SenderSession, Loc.GetString("rmc-onboarding-already-active"));
            return;
        }

        if (!IsInLobby(args.SenderSession))
        {
            _chat.DispatchServerMessage(args.SenderSession, Loc.GetString("rmc-onboarding-lobby-only"));
            return;
        }

        if (_activeSessions.Count >= _config.GetCVar(RMCCVars.RMCOnboardingMaxSessions))
        {
            _chat.DispatchServerMessage(args.SenderSession, Loc.GetString("rmc-onboarding-capacity"));
            return;
        }

        _gameTicker.ToggleReady(args.SenderSession, false);

        if (!TrySpawnTrainingMap(args.SenderSession,
                ev.Track,
                out var mapEntity,
                out var mapId,
                out var grid,
                out var ares,
                out var foodVendor,
                out var mob,
                out var mind,
                out var medicalPatient))
        {
            _chat.DispatchServerMessage(args.SenderSession, Loc.GetString("rmc-onboarding-spawn-failed"));
            LogSession(args.SenderSession.UserId, ev.Track, RMCOnboardingEndReason.LoadFailed, TimeSpan.Zero, 0);
            return;
        }

        var active = new RMCOnboardingSession(
            args.SenderSession.UserId,
            ev.Track,
            mapEntity,
            mapId,
            grid,
            ares,
            foodVendor,
            mob,
            mind,
            _timing.CurTime);
        active.MedicalPatient = medicalPatient.Valid ? medicalPatient : null;
        if (ev.Track == RMCOnboardingTrack.Medical)
            active.MedicalSafePosition = Transform(mob).Coordinates.Position;
        var addResult = _activeSessions.TryAdd(
            args.SenderSession.UserId,
            active,
            _config.GetCVar(RMCCVars.RMCOnboardingMaxSessions));
        if (addResult != RMCOnboardingStartResult.Added)
        {
            _mind.WipeMind(active.Mind);
            QueueDel(active.Mind);
            if (_map.MapExists(active.Map))
                _map.QueueDeleteMap(active.Map);
            _chat.DispatchServerMessage(args.SenderSession, Loc.GetString(addResult == RMCOnboardingStartResult.AlreadyActive
                ? "rmc-onboarding-already-active"
                : "rmc-onboarding-capacity"));
            return;
        }

        RaiseNetworkEvent(new TickerJoinGameEvent(), args.SenderSession.Channel);
        StartCurrentStep(active, 1f);
        Log.Info($"Onboarding started user={args.SenderSession.UserId} track={ev.Track} activeSessions={_activeSessions.Count}");
    }

    private void OnExit(RMCOnboardingExitEvent ev, EntitySessionEventArgs args)
    {
        EndOnboarding(args.SenderSession.UserId, RMCOnboardingEndReason.ManualExit);
    }

    private bool IsInLobby(ICommonSession session)
    {
        return _gameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) &&
               status is PlayerGameStatus.NotReadyToPlay or PlayerGameStatus.ReadyToPlay &&
               session.AttachedEntity == null;
    }

    private void OnMoveInput(Entity<InputMoverComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement || !_players.TryGetSessionByEntity(ent, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.Move);
    }

    private void OnGotEquippedHand(Entity<ItemComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.User, out var session))
            return;

        if (!_activeSessions.TryGetValue(session.UserId, out var active))
            return;

        if (HasComp<FoodComponent>(ent))
        {
            active.FoodEntity = ent.Owner;
            TryCompleteStep(session, RMCOnboardingStepKind.TakeFood);
        }

        if (active.IssuedGrenade == ent.Owner)
        {
            active.GrenadePickedUp = true;
            TryCompleteStep(session, RMCOnboardingStepKind.PickUpGrenade);
        }
    }

    private void OnActiveHandChanged(Entity<InputMoverComponent> ent, ref ActiveHandChangedEvent args)
    {
        if (!_players.TryGetSessionByEntity(ent, out var session) ||
            !_activeSessions.TryGetValue(session.UserId, out var active) ||
            !active.Ready ||
            GetSteps(active.Track)[active.Step].Kind != RMCOnboardingStepKind.SwitchHandsTwice)
        {
            return;
        }

        active.HandSwitches++;
        if (active.HandSwitches >= 2)
            TryCompleteStep(session, RMCOnboardingStepKind.SwitchHandsTwice);
    }

    private void OnFoodEaten(Entity<FoodComponent> ent, ref AfterFoodEatenEvent args)
    {
        foreach (var (_, medicalActive) in _activeSessions.Snapshot())
        {
            if (medicalActive.Track != RMCOnboardingTrack.Medical ||
                medicalActive.MedicalPatient != args.User)
            {
                continue;
            }

            var prototype = MetaData(ent).EntityPrototype?.ID;
            if (prototype == "AU14PillCMUTramadol")
                medicalActive.TramadolGiven = true;
            else if (prototype == "CMPillTricordrazine")
                medicalActive.TricordrazineGiven = true;

            if (!medicalActive.Ready || medicalActive.WaitingForInstructorTts)
                return;

            if (IsCurrentStep(medicalActive, RMCOnboardingStepKind.GiveTramadol))
            {
                if (medicalActive.TramadolGiven)
                    CompleteMedicalStep(medicalActive, RMCOnboardingStepKind.GiveTramadol);
                else
                    MedicalRetry(medicalActive, "rmc-onboarding-step-medical-wrong-pill");

                return;
            }

            if (IsCurrentStep(medicalActive, RMCOnboardingStepKind.GiveTricordrazine))
            {
                if (medicalActive.TricordrazineGiven)
                    CompleteMedicalStep(medicalActive, RMCOnboardingStepKind.GiveTricordrazine);
                else
                    MedicalRetry(medicalActive, "rmc-onboarding-step-medical-wrong-pill");

                return;
            }
        }

        if (!_players.TryGetSessionByEntity(args.User, out var session) ||
            !_activeSessions.TryGetValue(session.UserId, out var active))
        {
            return;
        }

        active.FoodEntity = ent.Owner;
        TryCompleteStep(session, RMCOnboardingStepKind.TakeFirstBite);
    }

    private void OnMedicalPatientExamined(Entity<MobStateComponent> ent, ref ExaminedEvent args)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.MedicalPatient == ent.Owner &&
                active.Mob == args.Examiner)
            {
                active.MedicalPatientExamined = true;
                if (IsCurrentStep(active, RMCOnboardingStepKind.ExamineMedicalPatient))
                    CompleteMedicalStep(active, RMCOnboardingStepKind.ExamineMedicalPatient);
                return;
            }
        }
    }

    private void OnMedicalWoundTreated(ref WoundTreatedEvent args)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.MedicalPatient != args.Body)
                continue;

            active.MedicalWoundsTreated++;
            if (IsCurrentStep(active, RMCOnboardingStepKind.TreatFirstBleed) && active.MedicalWoundsTreated >= 1)
                CompleteMedicalStep(active, RMCOnboardingStepKind.TreatFirstBleed);
            else if (IsCurrentStep(active, RMCOnboardingStepKind.TreatSecondBleed) && active.MedicalWoundsTreated >= 2)
                CompleteMedicalStep(active, RMCOnboardingStepKind.TreatSecondBleed);

            return;
        }
    }

    private void OnMedicalCprFinished(ref CPRAttemptFinishedEvent args)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.Mob != args.Performer ||
                active.CprBody != args.Target ||
                !IsMedicalCprStep(active))
            {
                continue;
            }

            if (!args.Success)
            {
                active.CprSuccesses = 0;
                if (active.Ready && !active.WaitingForInstructorTts)
                    MedicalRetry(active, "rmc-onboarding-step-medical-cpr-retry");
                return;
            }

            active.CprSuccesses++;
            if (active.CprSuccesses >= 3 && active.Ready)
                CompleteMedicalStep(active, RMCOnboardingStepKind.PerformCpr);

            return;
        }
    }

    private static bool IsMedicalCprStep(RMCOnboardingSession active)
    {
        var steps = GetSteps(active.Track);
        return active.Step >= 0 &&
               active.Step < steps.Count &&
               steps[active.Step].Kind == RMCOnboardingStepKind.PerformCpr;
    }

    private void CompleteMedicalStep(RMCOnboardingSession active, RMCOnboardingStepKind kind)
    {
        if (_players.TryGetSessionById(active.UserId, out var session))
            TryCompleteStep(session, kind);
    }

    private void MedicalRetry(RMCOnboardingSession active, string loc)
    {
        active.Ready = false;
        active.ReadyAfterSpeech = true;
        AresSay(active, Loc.GetString(loc));
    }

    private void OnFoodFullyEaten(Entity<FoodComponent> ent, ref AfterFullyEatenEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.User, out var session) ||
            !_activeSessions.TryGetValue(session.UserId, out var active))
        {
            return;
        }

        active.FoodFinished = true;
        RaiseLocalEvent(active.Mob, new RejuvenateEvent());
        TryCompleteStep(session, RMCOnboardingStepKind.FinishFood);
    }

    private void OnEntitySpoke(EntityUid uid, InputMoverComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null || string.IsNullOrWhiteSpace(args.Message) ||
            !_players.TryGetSessionByEntity(uid, out var session))
        {
            return;
        }

        TryCompleteStep(session, RMCOnboardingStepKind.SayNearby);
    }

    private void OnLoocSpoke(EntityUid uid, InputMoverComponent component, LoocCreatedEvent args)
    {
        if (string.IsNullOrWhiteSpace(args.Message) || !_players.TryGetSessionByEntity(uid, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.SayLooc);
    }

    private void OnMagazineInserted(Entity<GunComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != SharedGunSystem.MagazineSlot)
            return;

        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.Track != RMCOnboardingTrack.MilitaryEquipment ||
                !IsCurrentStep(active, RMCOnboardingStepKind.InsertMagazine) ||
                Transform(ent).MapID != active.Map ||
                !_players.TryGetSessionById(active.UserId, out var session))
            {
                continue;
            }

            TryCompleteStep(session, RMCOnboardingStepKind.InsertMagazine);
            return;
        }
    }

    private void OnAttachableAltered(Entity<AttachableHolderComponent> ent, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (args.Alteration != AttachableAlteredType.Attached || !HasComp<AttachableMagneticComponent>(args.Attachable))
            return;

        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (!IsCurrentStep(active, RMCOnboardingStepKind.AttachSling) ||
                Transform(ent).MapID != active.Map ||
                !_players.TryGetSessionById(active.UserId, out var session))
            {
                continue;
            }

            TryCompleteStep(session, RMCOnboardingStepKind.AttachSling);
            return;
        }
    }

    private void OnMagneticWeaponDropped(Entity<ItemComponent> ent, ref DroppedEvent args)
    {
        if (!HasComp<RMCMagneticItemComponent>(ent) ||
            !_players.TryGetSessionByEntity(args.User, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.DropSlungWeapon);
    }

    private void OnTtsDispatched(Entity<TTSComponent> ent, ref TTSUtteranceDispatchedEvent args)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.Ares != ent.Owner || !active.WaitingForInstructorTts)
                continue;

            active.InstructorPlaybackId = args.PlaybackId;
            if (!args.Recipients.Contains(active.UserId))
                active.InstructorTtsUnavailable = true;
        }
    }

    private void OnTtsUnavailable(Entity<TTSComponent> ent, ref TTSUtteranceUnavailableEvent args)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.Ares == ent.Owner && active.WaitingForInstructorTts)
                active.InstructorTtsUnavailable = true;
        }
    }

    private void OnTtsPlaybackFinished(TTSPlaybackFinishedEvent ev, EntitySessionEventArgs args)
    {
        if (!_activeSessions.TryGetValue(args.SenderSession.UserId, out var active) ||
            !active.WaitingForInstructorTts ||
            active.InstructorPlaybackId == 0 ||
            ev.PlaybackId != active.InstructorPlaybackId ||
            ev.SourceUid is not { } source ||
            GetEntity(source) != active.Ares)
        {
            return;
        }

        if (ev.Played)
            FinishInstructorSpeech(active);
        else
            active.InstructorTtsUnavailable = true;
    }

    private static void FinishInstructorSpeech(RMCOnboardingSession active)
    {
        active.WaitingForInstructorTts = false;
        active.InstructorTtsUnavailable = false;
        active.InstructorPlaybackId = 0;

        if (!active.ReadyAfterSpeech)
            return;

        active.ReadyAfterSpeech = false;
        active.Ready = true;
    }

    private void OnGotUnequippedHand(Entity<ItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.User, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.Drop);
    }

    private void OnUseInHand(Entity<ItemComponent> ent, ref UseInHandEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.User, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.UseInHand);
    }

    private void OnTrainingObjectInteract(Entity<RMCOnboardingTrainingObjectComponent> ent, ref InteractHandEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.User, out var session))
            return;

        if (TryCompleteStep(session, ent.Comp.Step, deferNextStep: true))
            args.Handled = true;
    }

    private void OnToggleCombat(Entity<InputMoverComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(ent, out var combatMode) ||
            !combatMode.IsInCombatMode ||
            !_players.TryGetSessionByEntity(ent, out var session))
            return;

        TryCompleteStep(session, RMCOnboardingStepKind.ToggleCombat);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        foreach (var (userId, active) in _activeSessions.Snapshot())
        {
            if (active.Drone == ev.Target)
            {
                active.Drone = null;
                active.DroneKilled = true;
                CleanupCombatMess(active, ev.Target);
                if (_players.TryGetSessionById(userId, out var session))
                    TryCompleteStep(session, RMCOnboardingStepKind.KillDrone);
                return;
            }

            if (active.GrenadeTargets.Contains(ev.Target))
                return;

            if (active.Mob != ev.Target)
                continue;

            if (IsGrenadeStage(active))
            {
                active.GrenadeAttemptActive = false;
                active.GrenadeRespawnPending = true;
                active.Ready = false;
                active.GrenadeResolutionAt = _timing.CurTime + TimeSpan.FromSeconds(0.75);
                return;
            }

            EndOnboarding(userId, RMCOnboardingEndReason.Died);
            return;
        }
    }

    private void OnActiveTimerTrigger(ref ActiveTimerTriggerEvent ev)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.IssuedGrenade != ev.Triggered ||
                !IsGrenadeStage(active))
            {
                continue;
            }

            active.GrenadeAttemptActive = true;
            active.GrenadeTargetDamaged = false;
            active.GrenadePlayerDamageAtActivation = GetTotalDamage(active.Mob);
            active.GrenadeTargetDamageAtActivation.Clear();
            foreach (var target in active.GrenadeTargets)
            {
                if (!Deleted(target))
                    active.GrenadeTargetDamageAtActivation[target] = GetTotalDamage(target);
            }
            return;
        }
    }

    private void OnGrenadeTriggered(TriggerEvent ev)
    {
        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.IssuedGrenade != ev.Triggered ||
                !active.GrenadeAttemptActive ||
                !IsGrenadeStage(active))
            {
                continue;
            }

            // Explosion damage is applied by other TriggerEvent handlers. Evaluate shortly
            // afterwards instead of relying on the grenade entity being deleted.
            active.GrenadeMissingCheckPending = true;
            active.GrenadeResolutionAt = _timing.CurTime + TimeSpan.FromSeconds(0.5);
            return;
        }
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent ev)
    {
        foreach (var (userId, active) in _activeSessions.Snapshot())
        {
            if (active.GrenadeAttemptActive && active.GrenadeTargets.Contains(ev.Entity.Owner))
                active.GrenadeTargetDamaged = true;

            if (active.Mob == ev.Entity.Owner)
            {
                EndOnboarding(userId, RMCOnboardingEndReason.CharacterDeleted);
                return;
            }

            if (active.IssuedGrenade == ev.Entity.Owner &&
                IsGrenadeStage(active))
            {
                active.IssuedGrenade = null;
                if (!active.GrenadeMissingCheckPending)
                {
                    active.GrenadeMissingCheckPending = true;
                    active.GrenadeResolutionAt = _timing.CurTime + TimeSpan.FromSeconds(1);
                }
            }

            if (active.MapEntity == ev.Entity.Owner || active.Grid == ev.Entity.Owner)
            {
                EndOnboarding(userId, RMCOnboardingEndReason.MapDeleted);
                return;
            }

            if (active.ObjectiveEntity == ev.Entity.Owner && !active.StepStartPending)
            {
                active.ObjectiveEntity = null;
                active.Ready = false;
                active.StepStartPending = true;
                active.StepStartAt = _timing.CurTime + TimeSpan.FromSeconds(0.1);
                AresSay(active, Loc.GetString("rmc-onboarding-objective-replaced"));
                return;
            }
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus is not (SessionStatus.Disconnected or SessionStatus.Zombie))
            return;

        if (_activeSessions.Contains(args.Session.UserId))
            EndOnboarding(args.Session.UserId, RMCOnboardingEndReason.Disconnected, returnToLobby: false);

        _completed.Remove(args.Session.UserId);
    }

    private void ProcessSpeechQueue(NetUserId userId, RMCOnboardingSession active)
    {
        if (active.Ready || active.WaitingForInstructorTts || _timing.CurTime < active.NextSpeech)
            return;

        var steps = GetSteps(active.Track);
        if (active.Step < 0 || active.Step >= steps.Count)
            return;

        var step = steps[active.Step];
        if (active.Line >= step.LineCount)
        {
            active.Ready = true;
            return;
        }

        if (active.Track == RMCOnboardingTrack.MilitaryEquipment)
            PrepareMilitaryLine(active);
        else if (active.Track == RMCOnboardingTrack.Medical)
            PrepareMedicalLine(active);

        AresSay(active, Loc.GetString($"{step.LinePrefix}-{active.Line + 1}"));
        active.Line++;

        if (active.Line >= step.LineCount)
        {
            active.ReadyAfterSpeech = true;
            return;
        }
    }

    private bool TryCompleteStep(ICommonSession session, RMCOnboardingStepKind kind, bool deferNextStep = false)
    {
        if (!_activeSessions.TryGetValue(session.UserId, out var active))
            return false;

        var steps = GetSteps(active.Track);
        if (active.Step < 0 || active.Step >= steps.Count)
            return false;

        var current = steps[active.Step];
        if (!active.Ready || current.Kind != kind)
            return false;

        if (kind == RMCOnboardingStepKind.KillGrenadeTargets)
        {
            active.GrenadeRespawnPending = false;
            active.GrenadeMissingCheckPending = false;
            active.GrenadeTargetsKilled = false;
            active.GrenadeAttemptActive = false;
        }

        AresSay(active, Loc.GetString(current.Completed));

        var nextStep = active.Step + 1;
        if (nextStep >= steps.Count)
        {
            MarkCompleted(session.UserId, active.Track);
            active.Ready = false;
            active.CompletionPending = true;
            active.CompleteAt = _timing.CurTime + TimeSpan.FromSeconds(8);
            return true;
        }

        active.Step = nextStep;
        if (deferNextStep)
        {
            active.Ready = false;
            active.StepStartPending = true;
            active.StepStartAt = _timing.CurTime + TimeSpan.FromSeconds(0.1);
            return true;
        }

        StartCurrentStep(active, 1f);
        return true;
    }

    private void StartCurrentStep(RMCOnboardingSession active, float delay)
    {
        active.Line = 0;
        active.Ready = false;
        active.NextSpeech = _timing.CurTime + TimeSpan.FromSeconds(delay);

        var steps = GetSteps(active.Track);
        if (active.Step < 0 || active.Step >= steps.Count || !_players.TryGetSessionById(active.UserId, out var session))
            return;

        var step = steps[active.Step];
        RaiseNetworkEvent(new RMCOnboardingTaskEvent(
                true,
                step.Title,
                $"{step.LinePrefix}-{step.LineCount}",
                step.Kind.LocHint(),
                active.Step + 1,
                steps.Count),
            session.Channel);
    }

    private bool TrySpawnTrainingMap(
        ICommonSession session,
        RMCOnboardingTrack track,
        out EntityUid mapEntity,
        out MapId mapId,
        out EntityUid gridUid,
        out EntityUid ares,
        out EntityUid foodVendor,
        out EntityUid mob,
        out EntityUid mind,
        out EntityUid medicalPatient)
    {
        mapEntity = default;
        mapId = MapId.Nullspace;
        gridUid = default;
        ares = default;
        foodVendor = default;
        mob = default;
        mind = default;
        medicalPatient = default;

        try
        {
            var mapPath = track == RMCOnboardingTrack.Medical ? MedicalLevelMap : FirstLevelMap;
            var options = DeserializationOptions.Default with { InitializeMaps = true };
            if (!_mapLoader.TryLoadMap(mapPath, out var loadedMap, out var grids, options) || grids.Count != 1)
                throw new InvalidOperationException($"Could not load onboarding map {mapPath} with exactly one grid.");

            mapEntity = loadedMap.Value.Owner;
            mapId = loadedMap.Value.Comp.MapId;
            _meta.SetEntityName(mapEntity, Loc.GetString("rmc-onboarding-map-name",
                ("track", Loc.GetString(track.LocName()))));

            var grid = grids.Single();
            gridUid = grid.Owner;
            _meta.SetEntityName(grid, Loc.GetString("rmc-onboarding-grid-name",
                ("track", Loc.GetString(track.LocName()))));

            EntityUid playerMarker = default;
            EntityUid patientMarker = default;
            var query = EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var metadata, out var xform))
            {
                if (xform.MapID != mapId)
                    continue;

                switch (metadata.EntityPrototype?.ID)
                {
                    case "PlayerStationAiEmpty":
                        ares = uid;
                        break;
                    case "AU14VAISOMercenary" when track == RMCOnboardingTrack.Medical:
                        playerMarker = uid;
                        break;
                    case "AU14VAISOMachinegunner" when track == RMCOnboardingTrack.Medical:
                        patientMarker = uid;
                        break;
                }
            }

            if (!ares.Valid)
                throw new InvalidOperationException($"{mapPath} does not contain a PlayerStationAiEmpty instructor.");

            var instructorTts = EnsureComp<TTSComponent>(ares);
            instructorTts.VoicePrototypeId = TrainingAresVoice;
            Dirty(ares, instructorTts);

            var spawnCoordinates = new EntityCoordinates(gridUid, new Vector2(0.5f, 0.5f));
            var profile = _gameTicker.GetPlayerProfile(session);
            if (track == RMCOnboardingTrack.Medical)
            {
                spawnCoordinates = playerMarker.Valid
                    ? Transform(playerMarker).Coordinates
                    : new EntityCoordinates(gridUid, new Vector2(0.5f, 3.5f));
                if (playerMarker.Valid)
                    Del(playerMarker);

                var medicalRole = SpawnMedicalRole("AU14VAISOMercenary", spawnCoordinates);
                mob = _stationSpawning.SpawnPlayerMob(spawnCoordinates, null, profile, null, medicalRole);
                _entityPreset.ApplyPreset(mob, "AU14VAISOPreset");
                EquipMedicalIfak(mob);
                ApplyMedicalMercenarySkills(mob);
                var patientCoordinates = patientMarker.Valid
                    ? Transform(patientMarker).Coordinates
                    : new EntityCoordinates(gridUid, new Vector2(0.5f, 0.5f));
                if (patientMarker.Valid)
                    Del(patientMarker);

                medicalPatient = PrepareMedicalPatient(
                    patientCoordinates);
            }
            else
            {
                mob = _stationSpawning.SpawnPlayerMob(spawnCoordinates, null, profile, null);
            }

            if (track == RMCOnboardingTrack.MilitaryEquipment)
                ApplyRiflemanSkills(mob);

            if (track == RMCOnboardingTrack.FullNewbie)
                foodVendor = SpawnAtPosition("ColMarTechFood", new EntityCoordinates(gridUid, new Vector2(0.5f, -2.5f)));

            mind = _mind.CreateMind(session.UserId, profile.Name);
            _mind.TransferTo(mind, mob);
            RemoveTrainingHeadsets(mapId);

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to spawn onboarding map for {session.Name}: {e}");

            if (_map.MapExists(mapId))
                _map.QueueDeleteMap(mapId);

            if (mind.Valid)
            {
                _mind.WipeMind(mind);
                QueueDel(mind);
            }

            return false;
        }
    }

    private void ApplyRiflemanSkills(EntityUid mob)
    {
        if (!RiflemanSkillPreset.TryGet(out var preset, _prototypes, _componentFactory))
            throw new InvalidOperationException($"Could not resolve rifleman skill preset {RiflemanSkillPreset}.");

        _skills.SetSkills(mob, new Dictionary<EntProtoId<SkillDefinitionComponent>, int>(preset.Skills));
    }

    private void OnTrainingHeadsetMapInit(Entity<HeadsetComponent> ent, ref MapInitEvent args)
    {
        var mapId = Transform(ent).MapID;
        if (mapId == MapId.Nullspace)
            return;

        foreach (var (_, active) in _activeSessions.Snapshot())
        {
            if (active.Map != mapId)
                continue;

            QueueDel(ent);
            return;
        }
    }

    private void RemoveTrainingHeadsets(MapId mapId)
    {
        var query = EntityQueryEnumerator<HeadsetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == mapId)
                QueueDel(uid);
        }
    }

    private void ApplyMedicalMercenarySkills(EntityUid mob)
    {
        _skills.SetSkills(mob, new Dictionary<EntProtoId<SkillDefinitionComponent>, int>
        {
            ["RMCSkillCqc"] = 1,
            ["RMCSkillEngineer"] = 1,
            ["RMCSkillConstruction"] = 1,
            ["RMCSkillFirearms"] = 2,
            ["RMCSkillMedical"] = 1,
            ["RMCSkillMeleeWeapons"] = 1,
            ["RMCSkillPolice"] = 2,
            ["RMCSkillFireman"] = 3,
            ["RMCSkillVehicles"] = 2,
            ["RMCSkillJtac"] = 1,
            ["RMCSkillEndurance"] = 2,
        });
    }

    private EntityUid SpawnMedicalRole(string prototype, EntityCoordinates coordinates)
    {
        var role = EntityManager.CreateEntityUninitialized(prototype, coordinates);
        _entityPreset.SuppressPreset(role);
        EntityManager.InitializeAndStartEntity(role);
        return role;
    }

    private EntityUid PrepareMedicalPatient(EntityCoordinates coordinates)
    {
        var patient = SpawnMedicalRole("AU14VAISOMachinegunner", coordinates);
        _entityPreset.ApplyPreset(patient, "AU14VAISOMGPreset");

        var parts = _body.GetBodyChildren(patient).ToArray();
        var arm = parts.FirstOrDefault(part => part.Component.PartType == BodyPartType.Arm);
        var leg = parts.FirstOrDefault(part => part.Component.PartType == BodyPartType.Leg);
        if (!arm.Id.Valid || !leg.Id.Valid)
            throw new InvalidOperationException("VAISO machinegunner is missing an arm or leg body part.");

        var bulletDamage = new DamageSpecifier();
        bulletDamage.DamageDict["Piercing"] = FixedPoint2.New(12);
        var bulletImpact = new DamageImpact(
            DamageImpactDelivery.Projectile,
            DamageImpactContact.Stab,
            DamageImpactPenetration.Low,
            DamageImpactEnergy.Low);
        if (!_bodyPartHealth.TryApplyPartDamage(
                patient,
                arm.Id,
                bulletDamage,
                mechanism: CMUTraumaMechanism.Generic,
                impact: bulletImpact) ||
            !_bodyPartHealth.TryApplyPartDamage(
                patient,
                leg.Id,
                bulletDamage,
                mechanism: CMUTraumaMechanism.Generic,
                impact: bulletImpact))
        {
            throw new InvalidOperationException("Could not apply the two onboarding bullet wounds.");
        }

        var fracture = EnsureComp<FractureComponent>(leg.Id);
        _fracture.SetSeverity((leg.Id, fracture), FractureSeverity.Simple);

        var criticalDamage = new DamageSpecifier();
        criticalDamage.DamageDict["Asphyxiation"] = FixedPoint2.New(100);
        _damageable.TryChangeDamage(patient, criticalDamage, ignoreResistances: true);
        _mobState.ChangeMobState(patient, MobState.Critical);
        _standing.Down(patient, playSound: false, dropHeldItems: false, force: true);
        return patient;
    }

    private void EquipMedicalIfak(EntityUid mob)
    {
        if (_inventory.TryGetSlotEntity(mob, "pocket1", out var occupied))
            Del(occupied.Value);

        var ifak = SpawnAtPosition("AU14PouchIFAKFill", Transform(mob).Coordinates);
        if (_inventory.TryEquip(mob, ifak, "pocket1", silent: true, force: true))
            return;

        QueueDel(ifak);
        throw new InvalidOperationException("Could not equip the onboarding medical IFAK.");
    }

    private void RecoverMedicalPatient(RMCOnboardingSession active)
    {
        if (active.MedicalPatient is not { } patient || Deleted(patient))
            return;

        var healing = new DamageSpecifier();
        healing.DamageDict["Asphyxiation"] = FixedPoint2.New(-100);
        _damageable.TryChangeDamage(patient, healing, ignoreResistances: true);
        _mobState.ChangeMobState(patient, MobState.Alive);
        _standing.Stand(patient, force: true);

        _chatSystem.TrySendInGameICMessage(
            patient,
            Loc.GetString("rmc-onboarding-step-medical-patient-fracture"),
            InGameICChatType.Speak,
            ChatTransmitRange.GhostRangeLimit,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    private void PrepareMilitaryLine(RMCOnboardingSession active)
    {
        // Scene changes are tied to the exact line that describes them. This method is only
        // called after the previous line's TTS has finished.
        if (active.Step == 0 && active.Line == 1 && Deleted(active.FoodVendor))
        {
            active.FoodVendor = SpawnTrainingVendor(
                active,
                "AU14USCMShipsideUniformVendor",
                new Vector2(0.5f, -2.5f));
        }

        if (active.Step == 1 && active.Line == 1 && active.EquipmentVendors.Count == 0)
        {
            if (!Deleted(active.FoodVendor))
                QueueDel(active.FoodVendor);

            active.EquipmentVendors.Add(SpawnTrainingVendor(
                active,
                "AU14USCMequipmentvendor",
                new Vector2(-0.5f, -2.5f)));
            active.EquipmentVendors.Add(SpawnTrainingVendor(
                active,
                "AU14USCMclothingequipmentvendor",
                new Vector2(0.5f, -2.5f)));
            active.EquipmentVendors.Add(SpawnTrainingVendor(
                active,
                "AU14USCMWeaponsVendor",
                new Vector2(1.5f, -2.5f)));
        }

        if (active.Step == 6 && active.Line == 1 && active.GrenadeTargets.Count == 0)
            SpawnGrenadeTrial(active);
    }

    private void PrepareMedicalLine(RMCOnboardingSession active)
    {
        var steps = GetSteps(active.Track);
        if (active.Step < 0 ||
            active.Step >= steps.Count ||
            steps[active.Step].Kind != RMCOnboardingStepKind.PerformCpr ||
            active.Line != 5 ||
            active.CprBody is { } body && !Deleted(body))
        {
            return;
        }

        var corpse = SpawnAtPosition(
            "AU14VAISOMercenary",
            new EntityCoordinates(active.Grid, new Vector2(2.5f, 0.5f)));
        var lethalDamage = new DamageSpecifier();
        lethalDamage.DamageDict["Asphyxiation"] = FixedPoint2.New(300);
        _damageable.TryChangeDamage(corpse, lethalDamage, ignoreResistances: true);
        _mobState.ChangeMobState(corpse, MobState.Dead);
        _standing.Down(corpse, playSound: false, dropHeldItems: false, force: true);
        active.CprBody = corpse;
    }

    private EntityUid SpawnTrainingVendor(RMCOnboardingSession active, string prototype, Vector2 position)
    {
        var vendor = SpawnAtPosition(prototype, new EntityCoordinates(active.Grid, position));
        RemComp<AccessReaderComponent>(vendor);
        return vendor;
    }

    private void SpawnGrenadeTrial(RMCOnboardingSession active)
    {
        CleanupGrenadeTargets(active);
        active.GrenadeTargetsKilled = false;
        active.GrenadePickedUp = false;
        active.GrenadeAttemptActive = false;
        active.GrenadeTargetDamaged = false;
        active.GrenadePlayerDamageAtActivation = FixedPoint2.Zero;
        active.GrenadeTargetDamageAtActivation.Clear();

        for (var i = 0; i < 4; i++)
        {
            active.GrenadeTargets.Add(SpawnAtPosition(
                "AU14CLFSurvFighter",
                new EntityCoordinates(active.Grid, new Vector2(-1f + i, 3.25f))));
        }

        active.IssuedGrenade = SpawnAtPosition(
            "AU14GrenadeHighExplosiveRMC",
            Transform(active.Mob).Coordinates);
    }

    private void RespawnGrenadeAttempt(RMCOnboardingSession active, ICommonSession session)
    {
        var earlyAttempt = IsCurrentStep(active, RMCOnboardingStepKind.PickUpGrenade);
        active.GrenadeRespawnPending = false;
        active.GrenadeMissingCheckPending = false;

        if (TryComp<MobStateComponent>(active.Mob, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            var oldMob = active.Mob;
            var profile = _gameTicker.GetPlayerProfile(session);
            active.Mob = _stationSpawning.SpawnPlayerMob(
                new EntityCoordinates(active.Grid, new Vector2(0.5f, 0.5f)),
                null,
                profile,
                null);
            ApplyRiflemanSkills(active.Mob);
            _mind.TransferTo(active.Mind, active.Mob);
            QueueDel(oldMob);
        }
        else
        {
            RaiseLocalEvent(active.Mob, new RejuvenateEvent());
        }

        SpawnGrenadeTrial(active);
        if (earlyAttempt)
            return;

        active.Ready = false;
        active.ReadyAfterSpeech = true;
        AresSay(active, Loc.GetString("rmc-onboarding-step-military-grenade-retry"));
    }

    private bool IsGrenadeStage(RMCOnboardingSession active)
    {
        return IsCurrentStep(active, RMCOnboardingStepKind.PickUpGrenade) ||
               IsCurrentStep(active, RMCOnboardingStepKind.KillGrenadeTargets);
    }

    private bool HasGrenadeTargetTakenDamage(RMCOnboardingSession active)
    {
        if (active.GrenadeTargetDamaged)
            return true;

        foreach (var (target, initialDamage) in active.GrenadeTargetDamageAtActivation)
        {
            if (Deleted(target) || GetTotalDamage(target) > initialDamage)
                return true;
        }

        return false;
    }

    private bool HasGrenadePlayerTakenDamage(RMCOnboardingSession active)
    {
        return GetTotalDamage(active.Mob) > active.GrenadePlayerDamageAtActivation;
    }

    private FixedPoint2 GetTotalDamage(EntityUid entity)
    {
        return CompOrNull<DamageableComponent>(entity)?.TotalDamage ?? FixedPoint2.Zero;
    }

    private void CleanupGrenadeTargets(RMCOnboardingSession active)
    {
        foreach (var target in active.GrenadeTargets)
        {
            if (!Deleted(target))
                QueueDel(target);
        }

        active.GrenadeTargets.Clear();
        if (active.IssuedGrenade is { } grenade && !Deleted(grenade))
            QueueDel(grenade);
        active.IssuedGrenade = null;
        CleanupCombatMess(active);
    }

    private void CleanupCombatMess(RMCOnboardingSession active, EntityUid? corpse = null)
    {
        if (corpse is { } target && !Deleted(target))
            QueueDel(target);

        var puddles = EntityQueryEnumerator<PuddleComponent, TransformComponent>();
        while (puddles.MoveNext(out var puddle, out _, out var xform))
        {
            if (xform.MapID == active.Map)
                QueueDel(puddle);
        }
    }

    private static bool IsCurrentStep(RMCOnboardingSession active, RMCOnboardingStepKind kind)
    {
        var steps = GetSteps(active.Track);
        return active.Ready &&
               active.Step >= 0 &&
               active.Step < steps.Count &&
               steps[active.Step].Kind == kind;
    }

    private void BuildBaseTrainingDeck(Entity<MapGridComponent> grid, out EntityUid ares)
    {
        FillTiles(grid, -ArenaHalfWidth, -ArenaHalfHeight, ArenaHalfWidth, ArenaHalfHeight, TrainingFloor);
        FillTiles(grid, -2, -ArenaHalfHeight + 1, 2, ArenaHalfHeight - 1, CleanFloor);

        for (var x = -ArenaHalfWidth; x <= ArenaHalfWidth; x++)
        {
            SpawnTrainingWall(grid, x, -ArenaHalfHeight);
            SpawnTrainingWall(grid, x, ArenaHalfHeight);
        }

        for (var y = -ArenaHalfHeight + 1; y < ArenaHalfHeight; y++)
        {
            SpawnTrainingWall(grid, -ArenaHalfWidth, y);
            SpawnTrainingWall(grid, ArenaHalfWidth, y);
        }

        ares = SpawnNamed(grid, TrainingAres, new Vector2i(0, 5), "rmc-onboarding-object-ares");
        var tts = EnsureComp<TTSComponent>(ares);
        tts.VoicePrototypeId = TrainingAresVoice;
        Dirty(ares, tts);

        SpawnArenaLights(grid);
    }

    private void PrepareStepScene(RMCOnboardingSession active)
    {
        var steps = GetSteps(active.Track);
        if (active.Step < 0 || active.Step >= steps.Count)
            return;

        var step = steps[active.Step];
        if (step.Kind is not (RMCOnboardingStepKind.UseInHand or RMCOnboardingStepKind.Drop))
            ClearStepScene(active);

        if (!TryComp<MapGridComponent>(active.Grid, out var gridComp))
            return;

        var grid = new Entity<MapGridComponent>(active.Grid, gridComp);

        switch (step.Kind)
        {
            case RMCOnboardingStepKind.Move:
                break;
            case RMCOnboardingStepKind.AresConsole:
                SpawnAresConsoleScene(active, grid);
                break;
            case RMCOnboardingStepKind.PickUp:
                SpawnPickUpScene(active, grid);
                break;
            case RMCOnboardingStepKind.UseInHand:
                SpawnUseInHandScene(active, grid);
                break;
            case RMCOnboardingStepKind.Drop:
                SpawnDropScene(active, grid);
                break;
            case RMCOnboardingStepKind.Supply:
                SpawnSupplyScene(active, grid);
                break;
            case RMCOnboardingStepKind.ToggleCombat:
                SpawnCombatScene(active, grid);
                break;
            case RMCOnboardingStepKind.Medical:
                SpawnMedicalScene(active, grid);
                break;
            case RMCOnboardingStepKind.Engineering:
                SpawnEngineeringScene(active, grid);
                break;
            case RMCOnboardingStepKind.Command:
                SpawnCommandScene(active, grid);
                break;
        }
    }

    private void ClearStepScene(RMCOnboardingSession active)
    {
        active.ObjectiveEntity = null;
        foreach (var entity in active.StepEntities)
        {
            if (!Deleted(entity))
                QueueDel(entity);
        }

        active.StepEntities.Clear();

        if (TryComp<MapGridComponent>(active.Grid, out var gridComp))
            ResetArenaTiles(new Entity<MapGridComponent>(active.Grid, gridComp));
    }

    private void ResetArenaTiles(Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -ArenaHalfWidth + 1, -ArenaHalfHeight + 1, ArenaHalfWidth - 1, ArenaHalfHeight - 1, TrainingFloor);
        FillTiles(grid, -2, -ArenaHalfHeight + 1, 2, ArenaHalfHeight - 1, CleanFloor);
    }

    private void Track(RMCOnboardingSession active, EntityUid entity)
    {
        active.StepEntities.Add(entity);
    }

    private void TrackObjective(RMCOnboardingSession active, EntityUid entity)
    {
        active.ObjectiveEntity = entity;
        Track(active, entity);
    }

    private void SpawnAresConsoleScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -5, 3, 5, 6, BriefingFloor);
        TrackObjective(active, SpawnTrainingObject(grid, "RMCAIConsoleInterface", new Vector2i(0, 4), RMCOnboardingStepKind.AresConsole, "rmc-onboarding-object-ares-interface"));
        Track(active, SpawnAtTile(grid, "CMTableAlmayer", new Vector2i(-2, 5)));
        Track(active, SpawnAtTile(grid, "CMTableAlmayer", new Vector2i(2, 5)));
        Track(active, SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(-2, 4)));
        Track(active, SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(2, 4)));
        Track(active, SpawnAtTile(grid, "RMCPaperworkLocker", new Vector2i(-5, 5)));
        Track(active, SpawnAtTile(grid, "CMFilingCabinet", new Vector2i(5, 5)));
    }

    private void SpawnPickUpScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -6, -2, -2, 2, EngineeringFloor);
        Track(active, SpawnAtTile(grid, "CMRack", new Vector2i(-6, 1)));
        Track(active, SpawnAtTile(grid, "CMTableReinforced", new Vector2i(-5, -1)));
        var crowbar = SpawnNamed(grid, "CMCrowbar", new Vector2i(-4, 0), "rmc-onboarding-object-crowbar");
        Highlight(crowbar);
        TrackObjective(active, crowbar);
        Track(active, SpawnAtTile(grid, "CMWrench", new Vector2i(-3, 0)));
        Track(active, SpawnAtTile(grid, "CMScrewdriver", new Vector2i(-3, -1)));
    }

    private void SpawnUseInHandScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        Track(active, SpawnAtTile(grid, "CMTableReinforced", new Vector2i(-1, 0)));
        Track(active, SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(0, 0)));
    }

    private void SpawnDropScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        Track(active, SpawnAtTile(grid, "CMRack", new Vector2i(2, 0)));
        Track(active, SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(3, 0)));
    }

    private void SpawnSupplyScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, 5, 2, 9, 6, BriefingFloor);
        TrackObjective(active, SpawnTrainingObject(grid, "CratePlastic", new Vector2i(6, 4), RMCOnboardingStepKind.Supply, "rmc-onboarding-object-supply-crate"));
        Track(active, SpawnAtTile(grid, "RMCBoxMRE", new Vector2i(7, 4)));
        Track(active, SpawnAtTile(grid, "RMCBoxPackFlare", new Vector2i(8, 4)));
        Track(active, SpawnAtTile(grid, "CMRack", new Vector2i(9, 5)));
        Track(active, SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(8, 5)));
    }

    private void SpawnCombatScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -4, -2, 4, 2, CleanFloor);
        Track(active, SpawnAtTile(grid, "RMCTargetHuman", new Vector2i(3, 0)));
        Track(active, SpawnAtTile(grid, "RMCTargetXeno", new Vector2i(4, 1)));
        Track(active, SpawnAtTile(grid, "CMTableReinforced", new Vector2i(0, -2)));
        Track(active, SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(-1, -2)));
    }

    private void SpawnMedicalScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -9, -6, -3, -3, MedicalFloor);
        TrackObjective(active, SpawnTrainingObject(grid, "CMBed", new Vector2i(-6, -4), RMCOnboardingStepKind.Medical, "rmc-onboarding-object-medical-bed"));
        Track(active, SpawnAtTile(grid, "CMLockerMedical", new Vector2i(-9, -4)));
        Track(active, SpawnAtTile(grid, "CMLockerMedicalWhite", new Vector2i(-8, -4)));
        Track(active, SpawnAtTile(grid, "CMTable", new Vector2i(-6, -5)));
        Track(active, SpawnAtTile(grid, "RMCBoxBodyBag", new Vector2i(-5, -5)));
        Track(active, SpawnAtTile(grid, "RMCCPRDummy", new Vector2i(-4, -4)));
    }

    private void SpawnEngineeringScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, 3, -6, 9, -3, EngineeringFloor);
        TrackObjective(active, SpawnTrainingObject(grid, "CMRack", new Vector2i(6, -4), RMCOnboardingStepKind.Engineering, "rmc-onboarding-object-engineering-rack"));
        Track(active, SpawnAtTile(grid, "CMLockerEngineer", new Vector2i(9, -4)));
        Track(active, SpawnAtTile(grid, "CMLockerEngineerElectrical", new Vector2i(8, -4)));
        Track(active, SpawnAtTile(grid, "CMTableReinforced", new Vector2i(6, -5)));
        Track(active, SpawnAtTile(grid, "RMCBoxLightsMixed", new Vector2i(5, -5)));
        Track(active, SpawnAtTile(grid, "CMFireExtinguisher", new Vector2i(4, -4)));
    }

    private void SpawnCommandScene(RMCOnboardingSession active, Entity<MapGridComponent> grid)
    {
        FillTiles(grid, -6, 3, 6, 6, BriefingFloor);
        TrackObjective(active, SpawnTrainingObject(grid, "RMCPropRemoteFlightComputer0", new Vector2i(0, 4), RMCOnboardingStepKind.Command, "rmc-onboarding-object-command-console"));
        Track(active, SpawnAtTile(grid, "CMTableAlmayer", new Vector2i(-3, 5)));
        Track(active, SpawnAtTile(grid, "CMTableAlmayer", new Vector2i(3, 5)));
        Track(active, SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(-3, 4)));
        Track(active, SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(3, 4)));
        Track(active, SpawnAtTile(grid, "CMFilingCabinetTall", new Vector2i(-6, 5)));
        Track(active, SpawnAtTile(grid, "RMCPaperworkLocker", new Vector2i(6, 5)));
    }

    private void FillTiles(Entity<MapGridComponent> grid, int x1, int y1, int x2, int y2, string tileId)
    {
        var tile = new Tile(_tile[tileId].TileId);
        for (var x = x1; x <= x2; x++)
        {
            for (var y = y1; y <= y2; y++)
            {
                _map.SetTile(grid, new Vector2i(x, y), tile);
            }
        }
    }

    private void SpawnBriefingArea(Entity<MapGridComponent> grid)
    {
        for (var x = -6; x <= 6; x += 3)
            SpawnAtTile(grid, "CMTableAlmayer", new Vector2i(x, 5));

        for (var x = -6; x <= 6; x += 3)
            SpawnAtTile(grid, "CMChairOfficeDark", new Vector2i(x, 4));

        SpawnAtTile(grid, "RMCPaperworkLocker", new Vector2i(-9, 6));
        SpawnAtTile(grid, "CMFilingCabinet", new Vector2i(-8, 6));
        SpawnAtTile(grid, "RMCCabinet", new Vector2i(8, 6));
        SpawnAtTile(grid, "RMCExtinguisherCabinet", new Vector2i(9, 6));
    }

    private void SpawnMedicalArea(Entity<MapGridComponent> grid)
    {
        SpawnAtTile(grid, "CMLockerMedical", new Vector2i(-9, -4));
        SpawnAtTile(grid, "CMLockerMedicalWhite", new Vector2i(-8, -4));
        SpawnAtTile(grid, "CMTable", new Vector2i(-6, -5));
        SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(-5, -5));
        SpawnAtTile(grid, "RMCStool", new Vector2i(-4, -4));
    }

    private void SpawnEngineeringArea(Entity<MapGridComponent> grid)
    {
        SpawnAtTile(grid, "CMLockerEngineer", new Vector2i(9, -4));
        SpawnAtTile(grid, "CMLockerEngineerElectrical", new Vector2i(8, -4));
        SpawnAtTile(grid, "CMTableReinforced", new Vector2i(6, -5));
        SpawnAtTile(grid, "RMCBoxLightsMixed", new Vector2i(5, -5));
        SpawnAtTile(grid, "CMFireExtinguisher", new Vector2i(4, -4));
    }

    private void SpawnSupplyArea(Entity<MapGridComponent> grid)
    {
        SpawnAtTile(grid, "CratePlastic", new Vector2i(7, 4));
        SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(8, 4));
        SpawnAtTile(grid, "CMRack", new Vector2i(9, 5));
        SpawnAtTile(grid, "RMCBoxCardboard", new Vector2i(8, 5));
        SpawnAtTile(grid, "RMCBoxMRE", new Vector2i(6, 4));
    }

    private void SpawnLooseTrainingTools(Entity<MapGridComponent> grid)
    {
        SpawnAtTile(grid, "CMCrowbar", new Vector2i(-4, 0));
        SpawnAtTile(grid, "CMWrench", new Vector2i(5, -1));
        SpawnAtTile(grid, "CMScrewdriver", new Vector2i(6, -1));
        SpawnAtTile(grid, "RMCMotionDetector", new Vector2i(-2, 5));
        SpawnAtTile(grid, "RMCBinoculars", new Vector2i(2, 5));
    }

    private void SpawnArenaLights(Entity<MapGridComponent> grid)
    {
        SpawnArenaLight(grid, new Vector2i(-7, 4), 7.5f, 1.5f);
        SpawnArenaLight(grid, new Vector2i(0, 4), 8f, 1.8f);
        SpawnArenaLight(grid, new Vector2i(7, 4), 7.5f, 1.5f);
        SpawnArenaLight(grid, new Vector2i(-7, -4), 7f, 1.6f);
        SpawnArenaLight(grid, new Vector2i(0, -4), 8f, 1.7f);
        SpawnArenaLight(grid, new Vector2i(7, -4), 7f, 1.6f);
    }

    private EntityUid SpawnTrainingObject(
        Entity<MapGridComponent> grid,
        string prototype,
        Vector2i tile,
        RMCOnboardingStepKind step,
        string nameLoc)
    {
        var uid = SpawnNamed(grid, prototype, tile, nameLoc);
        var comp = EnsureComp<RMCOnboardingTrainingObjectComponent>(uid);
        comp.Step = step;
        Dirty(uid, comp);
        Highlight(uid);
        return uid;
    }

    private void Highlight(EntityUid uid)
    {
        var light = EnsureComp<PointLightComponent>(uid);
        _pointLight.SetEnabled(uid, true, light);
        _pointLight.SetRadius(uid, 2.5f, light);
        _pointLight.SetEnergy(uid, 1.8f, light);
        _pointLight.SetColor(uid, Color.FromHex("#ffe26a"), light);
        _pointLight.SetCastShadows(uid, false, light);
    }

    private EntityUid SpawnNamed(Entity<MapGridComponent> grid, string prototype, Vector2i tile, string nameLoc)
    {
        var uid = SpawnAtTile(grid, prototype, tile);
        _meta.SetEntityName(uid, Loc.GetString(nameLoc));
        return uid;
    }

    private EntityUid SpawnAtTile(Entity<MapGridComponent> grid, string prototype, Vector2i tile)
    {
        return SpawnAtPosition(prototype, _map.GridTileToLocal(grid.Owner, grid.Comp, tile));
    }

    private void SpawnTrainingWall(Entity<MapGridComponent> grid, int x, int y)
    {
        SpawnAtTile(grid, TrainingWall, new Vector2i(x, y));
    }

    private void SpawnArenaLight(Entity<MapGridComponent> grid, Vector2i tile, float radius, float energy)
    {
        var lightEntity = SpawnAtPosition(null, _map.GridTileToLocal(grid.Owner, grid.Comp, tile));
        var light = EnsureComp<PointLightComponent>(lightEntity);
        _pointLight.SetEnabled(lightEntity, true, light);
        _pointLight.SetRadius(lightEntity, radius, light);
        _pointLight.SetEnergy(lightEntity, energy, light);
        _pointLight.SetColor(lightEntity, Color.FromHex("#e8f4ff"), light);
        _pointLight.SetCastShadows(lightEntity, false, light);
    }

    private void AresSay(RMCOnboardingSession active, string message)
    {
        active.WaitingForInstructorTts = true;
        active.InstructorTtsUnavailable = false;
        active.InstructorPlaybackId = 0;
        active.InstructorFallbackAt = _timing.CurTime + TimeSpan.FromSeconds(5);

        if (Deleted(active.Ares))
        {
            active.InstructorTtsUnavailable = true;
            return;
        }

        _chatSystem.TrySendInGameICMessage(
            active.Ares,
            NormalizeAresSpeech(message),
            InGameICChatType.Speak,
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: "Синтетический Инструктор",
            checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    private static string NormalizeAresSpeech(string message)
    {
        const string aresSpeechPrefix = "A.R.E.S.: ";
        const string aresNarrationPrefix = "A.R.E.S. ";

        if (message.StartsWith(aresSpeechPrefix, StringComparison.Ordinal))
            return message[aresSpeechPrefix.Length..];

        if (message.StartsWith(aresNarrationPrefix, StringComparison.Ordinal))
            return message[aresNarrationPrefix.Length..];

        return message;
    }

    private void EndOnboarding(
        NetUserId userId,
        RMCOnboardingEndReason reason,
        bool returnToLobby = true)
    {
        if (!_activeSessions.TryRemove(userId, out var active))
            return;

        if (_players.TryGetSessionById(userId, out var session))
            RaiseNetworkEvent(new RMCOnboardingTaskEvent(false, string.Empty, string.Empty, string.Empty, 0, 0), session.Channel);

        if (active.Mind.Valid && !Deleted(active.Mind))
        {
            _mind.WipeMind(active.Mind);
            QueueDel(active.Mind);
        }

        if (_map.MapExists(active.Map))
            _map.QueueDeleteMap(active.Map);

        if (returnToLobby && session is { Status: SessionStatus.InGame })
            _gameTicker.ReturnPlayerToLobby(session);

        var duration = _timing.CurTime - active.StartedAt;
        LogSession(userId, active.Track, reason, duration, active.Step + 1);
    }

    private void LogSession(
        NetUserId userId,
        RMCOnboardingTrack track,
        RMCOnboardingEndReason reason,
        TimeSpan duration,
        int lastStep)
    {
        Log.Info($"Onboarding ended user={userId} track={track} reason={reason} durationSeconds={duration.TotalSeconds:F1} lastStep={lastStep} activeSessions={_activeSessions.Count}");
    }

    private bool IsCompleted(NetUserId userId, RMCOnboardingTrack track)
    {
        return _completed.TryGetValue(userId, out var tracks) && tracks.Contains(track);
    }

    private void MarkCompleted(NetUserId userId, RMCOnboardingTrack track)
    {
        // TODO RMC14: persist onboarding completion in the server player database.
        if (!_completed.TryGetValue(userId, out var tracks))
        {
            tracks = new HashSet<RMCOnboardingTrack>();
            _completed[userId] = tracks;
        }

        tracks.Add(track);
    }

    private static IReadOnlyList<RMCOnboardingStep> GetSteps(RMCOnboardingTrack track)
    {
        return track switch
        {
            RMCOnboardingTrack.MilitaryEquipment => MilitaryEquipmentSteps,
            RMCOnboardingTrack.Medical => MedicalSteps,
            _ => FullNewbieSteps,
        };
    }

    private static RMCOnboardingStep Step(RMCOnboardingStepKind kind, string id, int lines, float delay = 8f)
    {
        return new RMCOnboardingStep(
            kind,
            $"rmc-onboarding-step-{id}-title",
            $"rmc-onboarding-step-{id}",
            lines,
            $"rmc-onboarding-step-{id}-complete",
            delay);
    }
}
