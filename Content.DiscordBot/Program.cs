using System.Text.Json;
using Content.DiscordBot;
using Content.DiscordBot.Governance;
using Content.Server.Database;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents =
        GatewayIntents.Guilds |
        GatewayIntents.GuildMessages,
});
client.Log += Logger.Log;
var seedBoostyTiers = args.Contains("--seed-boosty-tiers");
var listBoostyTiers = args.Contains("--list-boosty-tiers");
var listTestPatrons = args.Contains("--list-test-patrons");
var grantTestTierIndex = Array.IndexOf(args, "--grant-test-tier");
var migrateOnly = args.Contains("--migrate-only");
var governanceDoctor = args.Contains("--governance-doctor");
var environmentFileIndex = Array.IndexOf(args, "--env-file");
if (environmentFileIndex >= 0)
{
    if (args.Length <= environmentFileIndex + 1)
        throw new ArgumentException("Usage: --env-file <path>");
    ConfigurationLoader.LoadEnvironmentFile(args[environmentFileIndex + 1]);
}

string? token = null;
string? connectionString = null;
var guild = 0UL;
var config = new Config();
if (File.Exists("config.json"))
{
    config = await JsonSerializer.DeserializeAsync<Config>(File.OpenRead("config.json")) ?? new Config();
    token = config.Token;
    connectionString = config.DatabaseString;
    guild = config.Guild;
}

ConfigurationLoader.ApplyEnvironment(config, ref token, ref connectionString, ref guild);

if (string.IsNullOrWhiteSpace(connectionString))
    throw new ArgumentException("No database connection string found.");

ServerDbContext CreateConfiguredDatabase()
{
    var postgresBuilder = new DbContextOptionsBuilder<PostgresServerDbContext>();
    postgresBuilder.UseNpgsql(connectionString);
    return new PostgresServerDbContext(postgresBuilder.Options);
}

GovernanceDbContext CreateGovernanceDatabase()
{
    var builder = new DbContextOptionsBuilder<GovernanceDbContext>();
    builder.UseNpgsql(connectionString);
    return new GovernanceDbContext(builder.Options);
}

async Task WithConfiguredDatabase(Func<ServerDbContext, Task> action)
{
    await using var db = CreateConfiguredDatabase();
    await action(db);
}

if (seedBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.Seed);
    Console.WriteLine("Boosty sponsor tiers seeded.");
    return;
}

if (listBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintTiers);
    return;
}

if (listTestPatrons)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintPatrons);
    return;
}

if (grantTestTierIndex >= 0)
{
    if (args.Length <= grantTestTierIndex + 2)
        throw new ArgumentException("Usage: --grant-test-tier <player-name-or-user-id> <tier-name>");

    var playerNameOrId = args[grantTestTierIndex + 1];
    var tierName = args[grantTestTierIndex + 2];
    await WithConfiguredDatabase(db => BoostyTierSeeder.GrantTestTier(db, playerNameOrId, tierName));
    Console.WriteLine($"Granted '{tierName}' to '{playerNameOrId}'.");
    return;
}

await using (var governance = CreateGovernanceDatabase())
    await governance.Database.MigrateAsync();

if (migrateOnly)
{
    Console.WriteLine("Governance migrations applied successfully.");
    return;
}

if (governanceDoctor)
{
    await using var governance = CreateGovernanceDatabase();
    var requiredTables = new HashSet<string>(StringComparer.Ordinal)
    {
        "users", "identity_links", "identity_bindings", "service_paths", "rating_entries", "reputation_observations",
        "reputation_snapshots", "game_activity_snapshots", "contribution_events", "qualifications", "conflicts",
        "invitations", "court_cases", "court_participants", "court_statements", "jurors", "guilt_votes",
        "sentencing_votes", "friendships", "service_assignments", "punishment_executions", "duty_sessions",
        "capability_grants", "ahelp_tickets", "ahelp_messages", "live_incidents", "moderation_actions",
        "moderation_approvals", "moderation_reviews", "event_proposals", "event_reviews", "event_sessions",
        "event_manifest_items", "event_actions", "leadership_overrides", "audit_events",
    };
    var existingTables = (await governance.Database.SqlQueryRaw<string>(
        "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'governance'").ToListAsync()).ToHashSet();
    var missing = requiredTables.Except(existingTables).OrderBy(value => value).ToArray();
    if (missing.Length > 0)
        throw new InvalidOperationException($"Governance schema is incomplete: {string.Join(", ", missing)}");

    var applied = await governance.Database.GetAppliedMigrationsAsync();
    if (!applied.Contains("20260821034000_ImmutableIdentityBinding"))
        throw new InvalidOperationException("The immutable Governance identity binding migration is not recorded as applied.");

    var identityColumns = (await governance.Database.SqlQueryRaw<string>("""
        SELECT column_name || ':' || is_nullable AS "Value"
        FROM information_schema.columns
        WHERE table_schema = 'governance' AND table_name = 'users'
          AND column_name IN ('ss14_user_id', 'discord_user_id', 'civic_rating_cache')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    if (!identityColumns.SetEquals(["ss14_user_id:NO", "discord_user_id:YES", "civic_rating_cache:NO"]))
        throw new InvalidOperationException("Governance Identity v2 requires mandatory SS14 and optional Discord identity.");

    var discordIndex = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_indexes
        WHERE schemaname = 'governance'
          AND indexname = 'users_discord_user_id_unique_idx'
          AND indexdef ILIKE '%discord_user_id IS NOT NULL%'
        """).SingleAsync();
    if (discordIndex != 1)
        throw new InvalidOperationException("The optional Discord identity partial unique index is unavailable.");

    var identityBindingConstraints = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_constraint
        WHERE conrelid = 'governance.identity_bindings'::regclass
          AND contype IN ('p', 'u')
        """).SingleAsync();
    if (identityBindingConstraints < 3)
        throw new InvalidOperationException("Permanent Governance identity binding uniqueness is unavailable.");

    var identityTriggers = (await governance.Database.SqlQueryRaw<string>("""
        SELECT tgname AS "Value"
        FROM pg_trigger
        WHERE tgenabled <> 'D' AND tgname IN (
            'governance_users_identity_immutable_insert',
            'governance_users_identity_immutable_update',
            'governance_users_identity_remember_insert',
            'governance_users_identity_remember_update',
            'governance_identity_bindings_immutable')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    var requiredIdentityTriggers = new HashSet<string>(StringComparer.Ordinal)
    {
        "governance_users_identity_immutable_insert",
        "governance_users_identity_immutable_update",
        "governance_users_identity_remember_insert",
        "governance_users_identity_remember_update",
        "governance_identity_bindings_immutable",
    };
    if (!requiredIdentityTriggers.SetEquals(identityTriggers))
        throw new InvalidOperationException("Immutable Governance identity triggers are unavailable.");

    var ahelpColumns = (await governance.Database.SqlQueryRaw<string>("""
        SELECT column_name || ':' || is_nullable AS "Value"
        FROM information_schema.columns
        WHERE table_schema = 'governance' AND table_name = 'ahelp_tickets'
          AND column_name IN ('reporter_user_id', 'reporter_ss14_user_id')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    if (!ahelpColumns.SetEquals(["reporter_user_id:YES", "reporter_ss14_user_id:NO"]))
        throw new InvalidOperationException("The in-game AHelp ticket identity contract is invalid.");

    var eventActionColumns = (await governance.Database.SqlQueryRaw<string>("""
        SELECT column_name || ':' || is_nullable AS "Value"
        FROM information_schema.columns
        WHERE table_schema = 'governance' AND table_name = 'event_actions'
          AND column_name IN ('server_status', 'server_executed_at', 'server_execution_error')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    if (!eventActionColumns.SetEquals([
            "server_status:NO",
            "server_executed_at:YES",
            "server_execution_error:YES",
        ]))
        throw new InvalidOperationException("The event server execution contract is invalid.");

    var activeAHelpIndex = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_indexes
        WHERE schemaname = 'governance'
          AND indexname = 'ahelp_one_active_reporter_idx'
          AND indexdef LIKE '%escalated_to_court%'
        """).SingleAsync();
    if (activeAHelpIndex != 1)
        throw new InvalidOperationException("The active AHelp uniqueness index does not include court-escalated tickets.");

    var immutableTriggers = (await governance.Database.SqlQueryRaw<string>("""
        SELECT tgname AS "Value"
        FROM pg_trigger
        WHERE tgenabled <> 'D' AND tgname IN (
            'ahelp_messages_immutable',
            'moderation_reviews_immutable',
            'reputation_observations_immutable',
            'contribution_events_immutable')
        """).ToListAsync();
    var requiredTriggers = new HashSet<string>(StringComparer.Ordinal)
    {
        "ahelp_messages_immutable", "moderation_reviews_immutable",
        "reputation_observations_immutable", "contribution_events_immutable",
    };
    if (!requiredTriggers.SetEquals(immutableTriggers))
        throw new InvalidOperationException("One or more immutable Governance evidence triggers are unavailable.");

    var sentencingChecks = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_constraint
        WHERE conrelid = 'governance.sentencing_votes'::regclass
          AND conname IN ('sentencing_votes_sanction_type_valid','sentencing_votes_sanction_days_valid','sentencing_votes_shape_valid')
        """).SingleAsync();
    if (sentencingChecks != 3)
        throw new InvalidOperationException("Community Court sentencing constraints are not current.");

    await using var game = CreateConfiguredDatabase();
    _ = await game.Player.AsNoTracking().CountAsync();
    _ = await game.RMCLinkedAccounts.AsNoTracking().CountAsync();
    _ = await game.PlayTime.AsNoTracking().CountAsync();
    _ = await game.ConnectionLog.AsNoTracking().CountAsync();

    var doctorIdentities = new GovernanceIdentityService(CreateGovernanceDatabase, CreateConfiguredDatabase);
    await doctorIdentities.EnsureAllSs14UsersAsync();
    var doctorReputation = new ReputationService(CreateGovernanceDatabase, CreateConfiguredDatabase);
    var doctorSelection = new CandidateSelectionService(CreateGovernanceDatabase, CreateConfiguredDatabase, doctorReputation, config);
    _ = await doctorSelection.SelectAsync("jury", 1, "doctor", "read-only", 1, [], null, TimeSpan.Zero);
    Console.WriteLine($"Governance doctor OK: {requiredTables.Count} workflow/reputation tables, immutable Identity v2, Bayesian evidence, AHelp, Court, event execution and game activity contracts.");
    return;
}

if (string.IsNullOrWhiteSpace(token))
    throw new ArgumentException("No token found.");

if (guild == 0)
    throw new ArgumentException("No Discord guild found.");

config.Guild = guild;
if (config.CourtEnabled && config.CourtChannel == 0)
    throw new ArgumentException("Community Court is enabled but CourtChannel is not configured.");

await using CourtInstanceLock? courtInstanceLock = config.CourtEnabled
    ? await CourtInstanceLock.AcquireAsync(connectionString)
    : null;

var identities = new GovernanceIdentityService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var reputation = new ReputationService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var reputationHistory = new ReputationHistoryService(CreateGovernanceDatabase);
var selection = new CandidateSelectionService(CreateGovernanceDatabase, CreateConfiguredDatabase, reputation, config);
var courtPolicy = CourtPolicy.FromConfig(config);
var court = new CommunityCourtService(
    CreateGovernanceDatabase,
    CreateConfiguredDatabase,
    courtPolicy,
    selection);
var courtFiling = new CourtFilingService(identities, CreateGovernanceDatabase, CreateConfiguredDatabase, courtPolicy);
var courtMaterials = new CourtSourceMaterialService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var community = new GovernanceCommunityService(identities, CreateGovernanceDatabase, CreateConfiguredDatabase);
var courtTestLinks = new CourtTestAccountLinkingService(CreateConfiguredDatabase, CreateGovernanceDatabase, community, config);
var punishments = new CourtPunishmentService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var moderation = new ModerationGovernanceService(CreateGovernanceDatabase, CreateConfiguredDatabase, community);
var moderationTrust = new ModerationTrustService(CreateGovernanceDatabase, community, selection, config);
var events = new EventGovernanceService(CreateGovernanceDatabase, community, selection, config);
var eventStatus = new EventGovernanceStatusService(CreateGovernanceDatabase);
var guildMembers = new DiscordGuildMemberCache(client, config.Guild);
var coordinator = new CourtDiscordCoordinator(client, court, courtMaterials, punishments, events, moderation, config, guildMembers);
var moderationTrustCoordinator = new ModerationTrustCoordinator(client, moderationTrust, court, config, guildMembers);
var reputationCoordinator = new ReputationCoordinator(identities, reputation, config);
var services = new ServiceCollection()
    .AddSingleton(client)
    .AddSingleton(config)
    .AddSingleton(guildMembers)
    .AddSingleton(identities)
    .AddSingleton(reputation)
    .AddSingleton(reputationHistory)
    .AddSingleton(selection)
    .AddSingleton(court)
    .AddSingleton(courtFiling)
    .AddSingleton(courtMaterials)
    .AddSingleton(community)
    .AddSingleton(courtTestLinks)
    .AddSingleton(punishments)
    .AddSingleton(moderation)
    .AddSingleton(moderationTrust)
    .AddSingleton(events)
    .AddSingleton(eventStatus)
    .AddSingleton(coordinator)
    .AddSingleton(moderationTrustCoordinator)
    .AddSingleton(reputationCoordinator)
    .BuildServiceProvider();

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

var interaction = new InteractionService(client);
var handler = new CommandHandler(
    client,
    new CommandService(),
    interaction,
    CreateConfiguredDatabase,
    identities,
    guildMembers,
    services,
    guild);

using var shutdown = new CancellationTokenSource();

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Interlocked.Decrement(ref handler.Running);
    shutdown.Cancel();
};

await handler.InstallCommandsAsync();
var scheduler = Task.Run(() => coordinator.RunSchedulerAsync(shutdown.Token));
var moderationTrustScheduler = Task.Run(() => moderationTrustCoordinator.RunSchedulerAsync(shutdown.Token));
var reputationScheduler = Task.Run(() => reputationCoordinator.RunSchedulerAsync(shutdown.Token));

try
{
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException)
{
    // Normal process shutdown.
}

await client.StopAsync();
await services.DisposeAsync();
try
{
    await Task.WhenAll(scheduler, moderationTrustScheduler, reputationScheduler);
}
catch (OperationCanceledException)
{
    // Normal scheduler shutdown.
}
