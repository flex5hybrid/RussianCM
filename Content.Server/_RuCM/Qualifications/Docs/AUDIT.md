# GOVFOR qualifications: integration audit

Audited ColonialMarinesUniverse working checkout, 2026-10-03, HEAD
`9ef6bfdc2d08b11ec19a154b7097fb32a24fc949`. All findings below are from source inspection, not assumed APIs.

## Isolation and initial state

Initial status and hashes were saved outside the repository before edits. Existing changes are
`Content.CMU/Resources/Locale/ru-RU/CMU14/emotes/pushups.ftl`,
`Content.CMU/Resources/Locale/ru-RU/CMU14/language/language.ftl`,
`Content.CMU/Resources/Locale/ru-RU/CMU14/medical/treatment/surgery_ux.ftl`,
`Resources/Locale/ru-RU/_RMC14/Entities/Objects/Misc/pamphlets.ftl`,
`Resources/Locale/ru-RU/ghost/ghost-gui.ftl`, and the untracked
`Content.CMU/Resources/Locale/ru-RU/CMU14/upp-equipment.ftl`.
None is edited by this implementation. The requested RuCM-owned layout takes precedence over
the repository's default CMU layout. New files are confined to `_RuCM/Qualifications`,
`Resources/RuCM/Qualifications` and matching `_RuCM` locale/test directories. No upstream file,
project file, DbContext, JobPrototype class, admin flag or RobustToolbox file is modified.

## Existing extension points and coverage

| Area | Actual source/API | RuCM integration |
|---|---|---|
| Round-start candidates | `Content.Server/Station/Systems/StationJobsSystem.Roundstart.cs:GetJobCandidates`, `StationJobsGetCandidatesEvent` | Remove only disallowed candidates in Enforce; retain existing bans/playtime/whitelist filters. |
| Explicit/fallback spawn | `Content.Server/GameTicking/GameTicker.Spawning.cs:SpawnPlayer`, `Content.Shared/GameTicking/PlayerBeforeSpawnEvent.cs` | Clear a disallowed explicit JobId before the normal picker. This existing event's JobId already has a public setter. Covers ordinary round-start, forced/overflow assignments reaching SpawnPlayer and explicit latejoin. |
| Automatic job picker | `GetDisallowedJobsEvent` | Union additional exclusions; never replace existing restrictions. |
| Role admission requests | `Content.Server/GameTicking/Events/IsRoleAllowedEvent.cs` | Set Cancelled only in Enforce and only for missing RuCM requirements. |
| Completed spawn | `Content.Shared/GameTicking/PlayerSpawnCompleteEvent.cs` | Record job-specific GOVFOR participation with server/round/time. |
| Current officer identity | `SharedMindSystem.TryGetMind`, `SharedJobSystem.MindTryGetJobId`, `GameTicker.RunLevel`, session AttachedEntity and mind OwnedEntity | Resolve authority on the server when processing each queued request. Ghosts/lobby players do not count as current officers/instructors. Re-resolve the first officer when confirming. |
| Playtime | `IServerDbManager.GetPlayTimes(Guid)`, `PlayTimeTrackingManager` | Read persisted tracker totals for the explicitly selected migration roster; use independently configurable aliases from inspected legacy timer mappings. Do not alter upstream playtime. |
| Administration | `IAdminManager.HasAdminFlag`, existing `AdminFlags.Host` | Active Host access; separate persisted RuCM management ACL. Console-only `qualifications_acl` bootstraps that ACL. No new upstream admin flags. |
| Notes | Existing moderation notes infrastructure is independent | Separate training-note data, authorization and UI. Never request upstream moderation notes. |
| Lobby UI | `EuiManager.OpenEui`, matching client/server EUI classes | `qualifications` command opens own card even without a character. |
| Character BUI | `SharedUserInterfaceSystem.SetUi`, `OpenUi`, `ServerSendUiMessage`, message-attempt event | `qualifications bui` attaches a new RuCM marker and personal interface. Only the owning actor can open/send requests. Sensitive views use addressed BUI messages; never PVS-replicated UserInterfaceComponent.States. |
| Client sandbox | `Robust.Shared/ContentPack/Sandbox.yml`, `ModLoader.SetEnableSandboxing`, `ClientIntegrationInstance.CheckSandboxed` | Typed NetSerializable requests/views and explicit deep-copy; no System.Text.Json in Client/Shared. Real loader regression checks Client and Shared with the production whitelist and IL verification. Server-only JSON preserves storage format. |
| PostgreSQL | `CCVars.DatabaseEngine`, `DatabasePgHost/Port/Database/Username/Password`; `ServerDbManager.CreatePostgresOptions` | Read the same public game database configuration. Own `rucm_training` tables in the existing game PostgreSQL database; no separate database/connection CVar, no upstream EF changes. Advisory-locked schema migration. |
| SQLite | `CCVars.DatabaseSqliteDbPath`, `IResourceManager.UserData.RootDir`; `ServerDbManager.SetupSqlite`, `ServerDbSqlite.GetDbImpl`, `IServerDbManager.GetPlayTimes` | Read the same game file after the public read API waits for game migrations. Own `rucm_training_*` tables, immediate transactions, CAS and own migration ledger. Worker-thread I/O; no new file, ATTACH, game schema/history/journal changes or upstream modification. |
| Identity | Authenticated session NetUserId | Stable account GUID. No inferred Discord identity or linkage. Discord appeals remain manual. |

## Blocked upstream integration

These functions were deliberately not patched:

0. **Anonymous in-memory SQLite sharing in engine test hosts.**
   `ServerDbManager.SetupSqlite` retains its anonymous `:memory:` connection in the private
   `_sqliteInMemoryConnection` field when UserData.RootDir is null. The public repository API does
   not expose that connection or an SQL executor; a new connection would create another database.
   Minimal upstream hook: a public callback that executes custom SQL inside the existing DB guard,
   or a public factory for a connection to the same named shared in-memory database.
   No reflection/private-field access or separate DB fallback is used. Existing fixtures explicitly
   inject their repository. Normal filesystem SQLite is supported and tested with real game migrations.

1. **Native lobby/job requirements integrated through the public override mechanism.**
   `SharedRoleSystem.GetRoleRequirements` + `JobRequirementOverridePrototype` selected by
   `CCVars.GameRoleTimerOverride` provide the inspected public extension point. RuCM composes
   a dedicated override, retaining the prior override and all non-time restrictions, then restores
   it outside Insurgency/Enforce. A private per-session policy packet supplies admission booleans,
   no third-party training history. A RuCM JobRequirement supplies the instructor-access reason.
   Public HumanoidProfileEditor.RefreshJobs and public JobButton properties refresh existing controls.
   Public lobby ObserveButton.Parent and EscapeMenu.RulesButton.Parent accept owned buttons;
   no replacement upstream controller, reflection or private fields are used in production.
   Explicit ghost-role requirement overrides are retained; they are not GOVFOR ordinary job admission.
   Global role-timer exclusions remain a privileged upstream bypass of timer/requirement presentation;
   qualification server events still enforce normal role admission.

2. **Arbitrary direct role reassignment / direct DoSpawn callers.**
   `StationJobsSystem.TryAssignJob` changes its slot/player dictionary directly and has no cancellable
   per-player qualification event. `GameTicker.DoSpawn` is public and does not itself raise
   PlayerBeforeSpawnEvent. `SharedRoleSystem` job-role changes also lack a universal cancellable
   admission hook. These privileged/server paths cannot be universally gated from an isolated addon.
   Minimal upstream hook: a cancellable `(session/user, requestedJob, reason)` event immediately
   before every authoritative assignment/spawn mutation. Do not use post-spawn deletion or undo as a substitute.

3. **Historical participation dates are unavailable; migration uses cumulative hours.**
   Upstream role timers have no per-job activity dates. Initial migration therefore uses the
   approved human GOVFOR total **> 3 h**, Sergeant **≥ 5 h**, Officer **≥ 10 h**, and professional
   groups **≥ 5 h**. The read-only PostgreSQL/SQLite scanner covers all historical accounts,
   including accounts without timers and timer-only history. Dry Run never writes grants or
   markers; management must explicitly Execute the preview. Synthetic service and unknown
   trackers cannot prove human qualification. The existing one-time marker is preserved.

## Actual roles

`role-matrix.tsv` lists the 93 concrete seed job IDs and their exact source files, including
RMC/UPP/CMBCIU/WYPMC variants and `CMUJobGOVFORDroneOperator` (not an invented AU14 ID).
`AU14JobGOVFORAuxTech` has the actual Logistics Technician spawn-menu label.
`AU14JobGOVFOROfficerMedical` / `AU14JobGOVFORMilitaryDoctor` cover the existing medical officer/doctor
jobs; a distinct additional Chief Medical Officer JobPrototype was not invented from a legacy tracker name.
The five actual `AU14JobGOVFORPlatCo*` roles require independent `commanding_officer` admission.
Officer rank never grants that admission; migration never grants it.
This checkout also supplies `AU14JobGOVFORFighterPilot`, `AU14JobGOVFORFighterSystemsOfficer`
and `AU14JobGOVFORGunshipPilot`; their real aviation prototypes are included in the matrix.

At first initialization the loaded PrototypeManager enumerates every concrete job and creates a
separate disabled/no-requirements rule for all jobs outside the seed matrix. Seed requirements
are not embedded in JobPrototype or C# role-ID lists.

## Recruit semantics

No effective Enlisted level means Recruit in the domain and UI. Enforce denies the configured
combat roles. This addon does not silently replace station job slots, inventory/vending systems,
or upstream equipment rules. A base-access/unarmed Recruit role or map training area must be
configured by the server's map/role maintainers if recruits are to spawn on a GOVFOR base.
E4+ RP supervision remains manual. Recruit tasks and training are restricted to the GOVFOR base by server rules; emergency deployment is forbidden, including by CO order. A GOVFOR carrier counts as the base; a FOB does not. No scripted training game mode is added.
