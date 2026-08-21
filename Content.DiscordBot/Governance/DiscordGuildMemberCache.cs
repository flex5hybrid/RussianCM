using System.Collections.Concurrent;
using System.Net;
using Discord;
using Discord.WebSocket;

namespace Content.DiscordBot.Governance;

public readonly record struct DiscordGuildMemberLookup(IGuildUser? User, bool IsDefinitive);

/// <summary>
/// Shared cache for guild-member lookups used by Governance and patron synchronization.
/// Discord's per-member REST route is rate limited; without a shared cache, Court,
/// Moderation Trust and the patron refresher independently request the same users.
/// </summary>
public sealed class DiscordGuildMemberCache(DiscordSocketClient client, ulong guildId)
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StaleFor = TimeSpan.FromHours(1);
    private static readonly TimeSpan TransientWarningCooldown = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<ulong, CacheEntry> _entries = new();
    private readonly SemaphoreSlim _restGate = new(1, 1);
    private DateTime _lastTransientWarningAt;

    public async Task<DiscordGuildMemberLookup> LookupAsync(
        ulong discordId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (discordId == 0)
            return new DiscordGuildMemberLookup(null, true);

        if (!forceRefresh && client.GetGuild(guildId)?.GetUser(discordId) is { } socketUser)
        {
            _entries[discordId] = new CacheEntry(socketUser, DateTime.UtcNow, true);
            return new DiscordGuildMemberLookup(socketUser, true);
        }

        var now = DateTime.UtcNow;
        if (!forceRefresh && _entries.TryGetValue(discordId, out var cached) && now - cached.RefreshedAt < FreshFor)
            return new DiscordGuildMemberLookup(cached.User, cached.IsDefinitive);

        await _restGate.WaitAsync(cancellationToken);
        try
        {
            now = DateTime.UtcNow;
            if (!forceRefresh && _entries.TryGetValue(discordId, out cached) && now - cached.RefreshedAt < FreshFor)
                return new DiscordGuildMemberLookup(cached.User, cached.IsDefinitive);

            try
            {
                var user = await client.Rest.GetGuildUserAsync(guildId, discordId);
                var entry = new CacheEntry(user, now, true);
                _entries[discordId] = entry;
                return new DiscordGuildMemberLookup(user, true);
            }
            catch (Discord.Net.HttpException exception) when (exception.HttpCode == HttpStatusCode.NotFound)
            {
                _entries[discordId] = new CacheEntry(null, now, true);
                return new DiscordGuildMemberLookup(null, true);
            }
            catch (Discord.Net.HttpException exception) when (IsTransient(exception.HttpCode))
            {
                await LogTransientAsync(exception.HttpCode);

                // During a short Discord outage, stale positive membership is safer than pretending
                // that the user left the guild. The caller can inspect IsDefinitive before mutating
                // persistent state such as patron tiers.
                if (_entries.TryGetValue(discordId, out cached) && now - cached.RefreshedAt < StaleFor)
                    return new DiscordGuildMemberLookup(cached.User, false);

                return new DiscordGuildMemberLookup(null, false);
            }
        }
        finally
        {
            _restGate.Release();
        }
    }

    public async Task<IReadOnlySet<ulong>> ExistingMembersAsync(
        IEnumerable<ulong> discordIds,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var members = new HashSet<ulong>();
        foreach (var discordId in discordIds.Distinct())
        {
            if (discordId == 0 || discordId > long.MaxValue)
                continue;

            var lookup = await LookupAsync(discordId, forceRefresh, cancellationToken);
            if (lookup.User != null)
                members.Add(discordId);
        }

        return members;
    }

    private async Task LogTransientAsync(HttpStatusCode statusCode)
    {
        var now = DateTime.UtcNow;
        if (now - _lastTransientWarningAt < TransientWarningCooldown)
            return;

        _lastTransientWarningAt = now;
        await Logger.Info(
            $"[WARNING] Discord guild-member REST lookup temporarily unavailable ({(int) statusCode} {statusCode}); using cached membership where possible.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests || (int) statusCode >= 500;
    }

    private sealed record CacheEntry(IGuildUser? User, DateTime RefreshedAt, bool IsDefinitive);
}
