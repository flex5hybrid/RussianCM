# RuCM GOVFOR qualifications

## Deployment

Qualifications use **the existing game database**, selected automatically by `database.engine`:

- `postgres`: exactly the same `database.pg_host`, `pg_port`, `pg_database`, `pg_username`,
  `pg_password` settings as `ServerDbManager`.
- `sqlite`: exactly the same `database.sqlite_dbpath` resolved against the server userdata directory
  as `ServerDbManager.SetupSqlite` (normally `preferences.db`). Custom absolute paths are respected.

Keep the existing game database configuration; do not create another database or supply a second
connection string. Add only the feature settings:

```toml
[rucm.qualifications]
enabled = true
enforce = true
fail_open = false
server_id = "CMU-production"
```

The game password remains in the existing confidential `database.pg_password` CVar and is never
sent to the client or logged by qualifications. `rucm.qualifications.connection` has been removed;
delete it from older configurations. There is no fallback to a separate database. Restart after
changing game DB settings.
Use a distinct server_id for each
server so round IDs from separate servers cannot match officer confirmations or participation records.
For PostgreSQL, the DB login needs schema-migration privileges during deployment; writes are confined to
`rucm_training` tables **inside that same game database**: `schema_migration`, `state`, `record`
and `audit`. The schema is a namespace, not a database. Existing upstream tables, DbContext and EF
migration history remain unchanged. A numbered schema-migration ledger and PostgreSQL advisory
transaction lock protect initialization. No gameplay grants/migration execute automatically at startup.

For SQLite, the four tables in the **same game file** are `rucm_training_schema_migration`,
`rucm_training_state`, `rucm_training_record`, `rucm_training_audit`. Game migrations are awaited
through the public playtime read API before opening the file in ReadWrite mode; only the game
creates the file. Qualification migrations use their own ledger and an immediate transaction,
without changing the game's EF history, PRAGMA user_version, journal mode or existing tables.
CAS, the aggregate, indexed projections and append-only audit follow the PostgreSQL contract.
Update, delete and implicit INSERT OR REPLACE of audit entries are rejected by triggers.
SQLite work runs on workers because its async APIs perform synchronous file I/O. Lock waits are
bounded by a five-second timeout and storage failure follows the configured cache/failure policy.

Anonymous in-memory SQLite used by engine test hosts is an integration limitation: upstream's
`ServerDbManager._sqliteInMemoryConnection` is private and each `:memory:` connection is a different DB.
No second in-memory database or disk fallback is created. Such hosts must explicitly inject the
existing test repository; normal servers with filesystem userdata use SQLite automatically.

If an earlier installation already wrote qualification records to a different database, this
change does not copy them or connect to that old database. Stop qualification writers, back up
both databases, and transfer only the `rucm_training` schema into the existing game database
using PostgreSQL backup/restore tools before restarting. If that schema already exists in the
destination, review the records and resolve conflicts before importing; never overwrite it blindly.

General qualification gates enforce role access for GOVFOR in **Insurgency**. Lobby mode votes use
`AuRoundSystem.SelectedPreset`; an active round uses `GameTicker.CurrentPreset` first. Other modes
restore the original requirements for ordinary roles. The three GOVFOR Drill Instructor variants
always require Sergeant plus active Instructor Accreditation, with no separate job whitelist.
Defaults are enabled/enforce/fail-closed. Existing explicit settings continue to take precedence:
`enabled=false` is Disabled, `enabled=true,enforce=false` is Warn. Disabled/Warn retain the original
timers. Enforce uses `JobRequirementOverridePrototype` to replace only Overall/Role/Department timer
requirements for enabled qualification roles. Age/traits, bans and allegiance remain intact. Whitelists remain intact for all other jobs. The three GOVFOR Drill Instructor roles have no
independent role whitelist: Sergeant plus active Instructor Accreditation is required server-side
for every mode, even when the general qualifications gate is disabled.
Storage failures deny admission when no player cache exists, with a Critical/Fatal log; `fail_open=true`
explicitly enables fail-open for ordinary qualification roles. Drill Instructor always requires positive
Sergeant/accreditation evidence, including with fail-open enabled. Last-loaded records remain effective. Failed mutations do not publish cache
or lose prior data. A background 30-second refresh retries storage and observes other writers.
Configuration is immediately effective on the committing server; other servers refresh within 30 seconds.

## Access and UI

Active accounts with existing Host permission can open management. To bootstrap a non-admin manager,
run on the **local server console**, using the account's authenticated GUID and a quoted reason:

```text
qualifications_acl <account-guid> true "initial management appointment"
```

Players open **Personal record** in the lobby. Staff open **Records and training** from Esc.
Accredited instructors and on-duty GOVFOR Drill Instructors can right-click another online character
and choose **Open personal record**; the recruit is selected immediately. Actions recheck live session,
permissions and physical reach on the server. The same entry works for management/current officers.
`qualifications` and `qualifications bui` remain available as optional console shortcuts.
The Esc button is shown for active administrators, management, officers/CO and instructors;
existing Host/ACL rights still govern management actions.
Platoon Advisor is displayed as **Drill Instructor / Сержант-инструктор** on the three real GOVFOR
IDs (`AU14JobGOVFORadvisor`, `...RMC`, `...UPP`); IDs, preferences, gear and ranks stay
compatible. In Enforce, those roles require at least Sergeant and active Instructor Accreditation.
Revoking accreditation immediately denies further admission; holding the job never creates accreditation.
Their job whitelist is permanently removed; outside Insurgency the ordinary non-time role requirements remain in force, alongside the instructor accreditation gate. A one-time audited additive upgrade enables
their default role requirements in existing stores unless management has explicitly edited that role.
Holding the role allows browsing; certification still requires explicit instructor accreditation.
Every loaded job with `isSynthetic=true` is excluded from human qualification admission, participation,
historical-hours Enlisted migration and all hours-based migration groups. Prototype facts override
old stored classification and management edits. Synthetic tracker hours are excluded before alias folding;
ambiguous shared trackers are excluded conservatively. Historical synthetic participation is retained
as history but cannot grant Enlisted. This does not revoke previously migrated account grants.
The synthetic role whitelist remains unchanged. A currently synthetic character has a separate-admission
record heading instead of Recruit, and cannot receive human checklist completions, certifications or
Recruit resets. The server rechecks the current job/body after queueing an action. Accounts are not
permanently classified as synthetic: their genuine human-job evidence remains eligible.
Ordinary players see only their own preparation and suspension reasons. Accredited instructors can
browse online recruits even before spawning. Mutations require the instructor's current round character
and explicit persisted accreditation, independent of Sergeant/Officer qualifications.
Instructors can select connected accounts (including lobby/observer accounts), complete authorized checklists, certify them and add/read
training notes. They cannot self-certify, train Officer/CO, manage roles or view global audit.
Managers can select offline accounts by exact last account nickname from the existing game database
(GUID lookup remains an explicitly expanded advanced option), grant/restore/revoke, correct progress, edit definitions,
stable checklist items, requirements for any loaded job, instructors, ACL, command authorities and
migration groups/legacy tracker aliases. Save/correction actions require a reason and UI confirmation.
All rights are revalidated on the server; UI flags confer no authority.

The terminal starts with **Choose player** for staff. Search the online roster by account nickname and
press **Open record**, then use **Instructor assessment** for training or **Player clearances** for
management decisions. The selected nickname and online status remain visible in every section.
Enter a reason and confirm the selected name before crediting a skill or certifying a qualification.
Missing mandatory skills, self-assessment, off-duty instructors and disconnected recruits have visible
explanations. Accreditation applies to the selected account; it does not grant management authority.
Instructor Complete/Certify/Note requests recheck live Connected/InGame session status when dequeued,
including stale session entries after disconnection. Management operations can still target offline accounts.
EUI and BUI acknowledge request IDs so unsolicited roster/name notifications cannot release pending
actions or apply a target switch prematurely. Failed account searches keep the previous selection.
Nickname resolution uses only permission-filtered references and public game player-record APIs;
no private history or global account database is sent to ordinary players. Historical character names
remain alongside account nicknames in assessment notes. GUIDs are retained internally and in expanded
technical audit details, not as default player-facing labels. ACL and migration selection use named checkboxes.

EUI/BUI requests and views use typed `[NetSerializable]` contracts. The client and shared assembly
never use System.Text.Json: it is forbidden by the production sandbox. JSON remains inside the
server persistence/API adapter, preserving the existing database format. Update client and server
assemblies together when changing these network contracts.

Definitions and checklist items are soft-disabled. Removed item IDs are retained disabled automatically.
Definition versions increase on edits; existing grants keep their original version and requirement snapshot.
An Officer implies the lower military levels, but an explicitly suspended/revoked prerequisite interrupts
inheritance. Professional suspensions are independent. CO admission is an independent management-only
grant and never follows from Officer or migration.

## Suspensions

A current officer creates a reasoned pending request. A different current officer confirms it in the
same server/round, after the first officer's authority is rechecked. Pending requests from a different
round expire when replaced. CO and active Host administrators can activate one-person suspensions.
Management ACL alone is not a one-person suspension authority. Restoration/revocation keeps the
original request and immutable transition audit. The UI tells players to appeal manually in Discord.

## Return to Recruit

Select the player, open the dedicated **Return to Recruit** page, enter a reason, confirm the selected
nickname and press **Return to Recruit**. Player clearances and Suspensions also link to this page. Management/Host/CO reset
immediately; a current officer creates a pending request for a different current officer in the same
round/server. Confirmation rechecks the initiating officer's current authority.
The atomic reset revokes **all** military/professional/CO grants, clears current checklists, disables
instructor accreditation and resolves pending/active suspensions. Notes and immutable audit retain
the history and previous checklists. Revoked grants can be earned again through fresh training.
The reset changes admission rights, not the currently spawned character/job. It does not kick or
delete a player. Training requires the target online for instructors; management can handle offline cases.

## Migration

Management opens **Clearance import** and selects **Scan all historical players (Dry Run)**.
A read-only scanner uses the configured game PostgreSQL database or SQLite file, reads all
`player.user_id` records plus timer-only historical accounts, and reads their `play_time` in
one consistent SQL statement. Accounts without timers are counted too. No manual roster or
10,000-account limit applies to the full scan. SQLite work and plan calculation run off the
simulation thread. No upstream schema, EF model or database interface is changed.

Dry Run reports scanned accounts, players receiving anything, totals for each qualification
and total grants. It changes neither game data nor qualification data. Advanced selected-account
preview remains available for deliberate partial imports; Execute consumes the same global
one-time marker, so use the full scan for the initial server migration.

- Enlisted: strictly **> 3 h** summed across all real human GOVFOR role trackers, including
  roles without an enabled qualification gate. Exactly 3:00:00 does not qualify.
- Sergeant: **≥ 5 h** in the Sergeant group; includes Enlisted.
- Officer: **≥ 10 h** in the Officer group; includes Sergeant and Enlisted.
- Professional: **≥ 5 h** in the corresponding role group; no military rank is awarded by
  professional certification itself (the independent total-human-hours rule still applies).
- Commanding Officer qualification is never migrated. CO service counts toward human
  Enlisted hours, but CO jobs do not contribute to rank/professional migration groups.
- Synthetic jobs, their trackers, aliases and ambiguous shared trackers are excluded.
  Non-GOVFOR, unknown, negative and non-finite timers cannot prove service.

Aliases fold legacy timers to verified canonical trackers; each canonical tracker is counted
once per group. Participation dates and account last-seen are not migration evidence.
Existing active, suspended and revoked grants are retained; migration never restores them.

Execute requires management authority, the actor's preview token, unchanged qualification
revision, and a preview younger than ten minutes (measured after the scan completes). It writes
only qualification tables and audit, atomically records `govfor-training-v1`, and publishes cache
changes after commit. Repeated execution and restart cannot issue grants again. A previous
completed migration remains completed; this update never resets its marker or runs at startup.

## Persistence and audit

`IRuCMQualificationRepository` is the sole storage boundary. PostgreSQL and SQLite atomically write
a versioned aggregate plus indexed `(kind,key,player,jsonb)` projections. The aggregate is authoritative;
projections support reporting. Stable keys enforce one player/qualification and one player/item entry.
Audit has its own append-only table and DB mutation triggers. PostgreSQL application roles must not
have TRUNCATE/drop privileges during normal operation. SQLite file owners can alter/drop their own
schema, so these triggers protect ordinary database writes rather than an adversary controlling the file.
Checklist corrections remove current progress
projections but retain the immutable audit. Schema migrations and qualification rollout migration are
separate mechanisms.

The service serializes mutations, uses copy-on-write cache publication after commit and database
revision compare-and-swap. Conflicting writers cannot overwrite one another silently. Full aggregate
serialization favors isolation/correctness for the initial system; it is not a claim of high-volume
multi-server capacity. Large deployments should profile and split aggregate storage before expanding it.

Own events: RuCMQualificationGrantedEvent, RuCMQualificationSuspendedEvent,
RuCMQualificationRestoredEvent and RuCMChecklistItemCompletedEvent. They are server-local;
the network request contains no trusted actor, admin flag, management right or current job.

Metrics count known training records, not every registered account or a player ranking. Recruit→Enlisted
time is elapsed wall time from the first recorded training participation/progress; round count is the
number of distinct training rounds with completions. Imported grants are not RP training performance.

## Verification

Domain/security tests live in `Content.Tests/_RuCM/Qualifications`; real engine EUI/BUI/job-event tests
and SQLite/PostgreSQL coexistence/roundtrip/concurrency tests in `Content.IntegrationTests/_RuCM/Qualifications`.
`QualificationSandboxTests` loads both Client and Shared through the real ModLoader with sandbox
type checks and IL verification enabled. Ordinary engine-pair tests alone do not prove sandbox compatibility.
The DB test requires `RUCM_QUALIFICATIONS_TEST_CONNECTION` targeting a disposable database whose name
starts with `rucm_qualifications_test`. It creates the real upstream EF game model if needed, then
checks that qualification tables coexist in that same database without changing game preferences,
playtime or public table definitions. It refuses an existing `rucm_training` schema and cleans up
only its own schema and inserted game rows. It does not create/drop databases; the test login needs
table/schema DDL privileges, not CREATEDB. Never point the test at gameplay storage.

SQLite tests always create their own temporary game file, apply the real upstream SQLite migrations,
and use the same file for qualifications. They cover restart, every persisted field, role clearance,
progress removal, accreditation, notes, suspension/restoration, migration dry-run/idempotency,
concurrent CAS, transaction rollback/cache recovery, audit update/delete/REPLACE protection, missing-file
refusal and worker-thread lock waiting. They require no PostgreSQL or operator database configuration.

Build the affected Server and Client projects and run the qualification filters on Content.Tests and
Content.IntegrationTests in the target ColonialMarinesUniverse checkout. Verification must use that
checkout's real sources; results from a different fork are not a substitute. See the delivered
verification report for exact build/test results, any pre-existing resource failures and name-status.
## CRT terminal interface

The private EUI/BUI uses the game's CRT palette, window chrome, cards, corner brackets,
buttons, scrollbars and static screen shader. Body copy uses the readable game font.
Roll, moving grain, curvature and artifacts are disabled for this reading surface;
the game's CRT toggle and effect intensity still apply.

The sidebar separates service record, role clearance, instructor assessment, suspension,
and management sections. Long captions wrap; only the selected program's checklist is shown.
Program progress precedes its explanation. Role search includes a missing-clearance filter.
Every section and restricted workflow has localized ru-RU/en-US explanations.

Unsaved fields survive refresh and section navigation; switching accounts clears them.
A successful write reloads authoritative values. Confirmation resets after a server response
or a change of section. Requests disable repeat actions and input until the server replies.
Readonly players only see the service record and role-clearance pages. Server authorization
continues to decide every mutation.

`qualificationpreview` opens a clearly labeled local visual preview using synthetic records
and real job prototypes. It does not send network requests or persist data. `capture` exports
eight native-renderer screenshots into local client userdata `QualificationPreview/` and exits
that preview client. `off` closes the local preview. Normal players use the lobby/Esc/context buttons.
`qualificationentrypreview` is disconnected visual QA: it renders the actual lobby action column and
Escape window, exports two native screenshots and exits. It cannot run while connected. The complete
lobby's live lineup/character panels need initialized entity systems; the integration test loads the
real connected LobbyGui and verifies the button's network path. The disconnected screenshot is
explicitly a column preview, not a claim of testing every live lobby element.
