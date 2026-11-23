using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Discord;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.Administration.ModerationRegistry;

public sealed partial class CMUModerationRegistrySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IServerDbManager _database = default!;
    [Dependency] private DiscordWebhook _discord = default!;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan TransientRetry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RejectedRetry = TimeSpan.FromMinutes(5);

    private WebhookIdentifier? _webhook;
    private Task? _delivery;
    private TimeSpan _nextPoll;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, CMUModerationRegistryCVars.DiscordWebhook, url =>
        {
            _webhook = null;
            _nextPoll = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (CMUModerationRecord.TryGetWebhook(url, out var webhook))
                _webhook = webhook;
            else
                Log.Error("Invalid URL for discord.cmu_moderation_webhook.");
        }, true);
    }

    public override void Update(float frameTime)
    {
        if (_webhook is not { } webhook || _timing.RealTime < _nextPoll || _delivery is { IsCompleted: false })
            return;

        _nextPoll = _timing.RealTime + PollInterval;
        _delivery = DeliverNext(webhook);
    }

    private async Task DeliverNext(WebhookIdentifier webhook)
    {
        try
        {
            var entry = await _database.ClaimModerationRecord(webhook.Id);
            if (entry == null)
                return;

            TimeSpan? retry = TransientRetry;
            try
            {
                var record = JsonSerializer.Deserialize<CMUModerationRecord>(entry.Payload)!;
                var ids = record.Admin is { } admin ? record.Players.Append(admin).Distinct().ToArray() : record.Players;
                var names = await _database.GetModerationNames(ids);

                // A configuration change can invalidate the destination while the database work is pending.
                if (_webhook != webhook)
                {
                    await _database.CompleteModerationRecord(entry, TimeSpan.Zero);
                    return;
                }

                var payload = CMUModerationEmbed.Build(record, entry.Id, names, _configuration.GetCVar(CVars.GameHostName));
                using var response = await _discord.CreateMessage(webhook, payload);
                retry = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => RateLimitDelay(response),
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound => RejectedRetry,
                    _ when response.IsSuccessStatusCode => null,
                    _ when (int) response.StatusCode >= 500 => TransientRetry,
                    _ => RejectedRetry,
                };

                if (_webhook == webhook)
                {
                    if (retry == null)
                        _nextPoll = TimeSpan.Zero;
                    else if (response.StatusCode != HttpStatusCode.BadRequest)
                        _nextPoll = _timing.RealTime + retry.Value;
                }

                if (retry != null)
                    Log.Warning($"Discord rejected moderation record {entry.Id:N} (HTTP {(int) response.StatusCode}).");
            }
            catch (Exception error)
            {
                // An HTTP exception can contain the webhook URL, including its token.
                Log.Warning($"Discord delivery failed for moderation record {entry.Id:N} ({error.GetType().Name}).");
                if (_webhook == webhook)
                    _nextPoll = _timing.RealTime + TransientRetry;
            }

            await _database.CompleteModerationRecord(entry, retry);
        }
        catch (Exception error)
        {
            Log.Error($"Could not process the Discord moderation queue ({error.GetType().Name}).");
            if (_webhook == webhook)
                _nextPoll = _timing.RealTime + TransientRetry;
        }
    }

    private static TimeSpan RateLimitDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta > PollInterval ? delta : PollInterval;

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            return remaining > PollInterval ? remaining : PollInterval;
        }

        // Discord may send fractional seconds, which RetryAfter only parses as integers.
        if (response.Headers.TryGetValues("Retry-After", out var values)
            && double.TryParse(values.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds is >= 0 and <= 86_400)
        {
            return TimeSpan.FromSeconds(Math.Max(seconds, PollInterval.TotalSeconds));
        }

        return TransientRetry;
    }
}
