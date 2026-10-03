# RuCM GOVFOR qualifications

## Deployment

Add the independent PostgreSQL connection to server configuration before startup:

```toml
[rucm.qualifications]
enabled = false
enforce = false
fail_open = true
connection = "Host=...;Database=...;Username=...;Password=..."
server_id = "CMU-production"
```

The connection CVar is confidential. Never publish its value. Use a distinct server_id for each
server so round IDs from separate servers cannot match officer confirmations or participation records.
The DB login needs schema-migration privileges during deployment; application writes are confined to
`rucm_training`. A numbered schema-migration ledger and PostgreSQL advisory transaction lock protect
schema initialization. No gameplay grants/migration execute automatically at startup.

Modes: `enabled=false` is Disabled; `enabled=true,enforce=false` is Warn;
`enabled=true,enforce=true` is Enforce. Existing bans, whitelist, allegiance and playtime requirements
continue to apply. Roll out in Warn after bootstrapping management and previewing migration.
Unknown users fail open by default during storage failure with a Critical/Fatal log. Last-loaded
records remain effective. `fail_open=false` selects fail closed. Failed mutations do not publish cache
or lose prior data. A background 30-second refresh retries storage and observes other writers.
Configuration is immediately effective on the committing server; other servers refresh within 30 seconds.

## Access and UI

Active accounts with existing Host permission can open management. To bootstrap a non-admin manager,
run on the **local server console**, using the account's authenticated GUID and a quoted reason:

```text
qualifications_acl <account-guid> true "initial management appointment"
```

Players use `qualifications`; characters can use `qualifications bui`. Both lead to the same UI/service.
Ordinary players see only their own preparation and suspension reasons. Instructors must have a current
round character and explicit persisted accreditation, independent of Sergeant/Officer qualifications.
Instructors can select current participants, complete authorized checklists, certify them and add/read
training notes. They cannot self-certify, train Officer/CO, manage roles or view global audit.
Managers can select offline accounts by GUID, grant/restore/revoke, correct progress, edit definitions,
stable checklist items, requirements for any loaded job, instructors, ACL, command authorities and
migration groups/legacy tracker aliases. Save/correction actions require a reason and UI confirmation.
All rights are revalidated on the server; UI flags confer no authority.

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

## Migration

Management first edits the groups, then enters an explicit comma-separated GUID roster. No upstream
public API enumerates all historical accounts through the qualification repository; supply the approved
roster. Dry-run reads existing role-timer totals through the public DB API and writes nothing.
It displays per-qualification counts and records. Execute requires the server-issued, account-bound preview
token, an unchanged revision and a preview less than ten minutes old. The atomic transaction stores
`govfor-training-v1`; repeat execute does nothing. Existing suspended/revoked records are never restored.
Trackers shared by faction variants are deduplicated; persisted aliases normalize inspected legacy timers.
CO roles and independent CO admission are excluded from hours-based migration.

**Historical limitation:** only job-specific GOVFOR activity recorded by this addon can establish the
14-day Enlisted rule. Old account login dates or unrelated round participation are not substituted.
See AUDIT.md for the missing upstream history API. Do not mark that acceptance item complete without
a verified historical job-activity source.

## Persistence and audit

`IRuCMQualificationRepository` is the sole storage boundary. The PostgreSQL repository atomically writes
a versioned aggregate plus indexed `(kind,key,player,jsonb)` projections. The aggregate is authoritative;
projections support reporting. Stable keys enforce one player/qualification and one player/item entry.
Audit has its own append-only table and DB update/delete trigger. The application's DB role must not
have TRUNCATE/drop privileges during normal operation. Checklist corrections remove current progress
projections but retain the immutable audit. Schema migrations and qualification rollout migration are
separate mechanisms.

The service serializes mutations, uses copy-on-write cache publication after commit and PostgreSQL
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
and independent PostgreSQL roundtrip/concurrency tests in `Content.IntegrationTests/_RuCM/Qualifications`.
`QualificationSandboxTests` loads both Client and Shared through the real ModLoader with sandbox
type checks and IL verification enabled. Ordinary engine-pair tests alone do not prove sandbox compatibility.
The DB test requires `RUCM_QUALIFICATIONS_TEST_CONNECTION` targeting a disposable database whose name
starts with `rucm_qualifications_test`; its login needs permission to create/drop its randomly named fixture
database. Never point it at gameplay storage.

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
five native-renderer screenshots into local client userdata `QualificationPreview/` and exits
that preview client. `off` closes the local preview. Normal players use `qualifications`.
