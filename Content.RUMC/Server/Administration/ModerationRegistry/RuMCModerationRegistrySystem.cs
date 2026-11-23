using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Discord;
using Content.Server.Database;
using Content.Server.Discord;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

public sealed partial class RuMCModerationRegistrySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRuMCModerationOutbox _database = default!;
    [Dependency] private DiscordWebhook _discord = default!;

    [Dependency] private ILocalizationManager _localization = default!;

    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinimumRateLimitDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan TransientRetry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RejectedRetry = TimeSpan.FromMinutes(5);

    private WebhookIdentifier? _webhook;
    private Task? _delivery;
    private TimeSpan _idleUntil;
    private TimeSpan _retryUntil;
    private long _observedQueueVersion;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, RuMCModerationRegistryCVars.Webhook, url =>
        {
            _webhook = null;
            _idleUntil = _retryUntil = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (RuMCModerationWebhook.TryParse(url, out var webhook))
                _webhook = webhook;
            else
                Log.Error("Invalid URL for discord.rumc_moderation_webhook.");
        }, true);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.RealTime;
        if (_webhook is not { } webhook || now < _retryUntil || _delivery is { IsCompleted: false })
            return;

        var version = _database.QueueVersion;
        if (now < _idleUntil && _observedQueueVersion == version)
            return;

        _observedQueueVersion = version;
        _delivery = DeliverNext(webhook);
    }

    private async Task DeliverNext(WebhookIdentifier webhook)
    {
        try
        {
            var entry = await _database.ClaimModerationRecord(webhook.Id);
            if (entry == null)
            {
                if (_webhook == webhook)
                    _idleUntil = _timing.RealTime + IdlePollInterval;
                return;
            }

            _idleUntil = TimeSpan.Zero;
            RuMCModerationRecord record;
            try
            {
                record = JsonSerializer.Deserialize<RuMCModerationRecord>(entry.Payload)
                    ?? throw new JsonException("Missing moderation record.");
                if (record.Players == null || record.Rounds == null || record.Reason == null || !Enum.IsDefined(record.Kind))
                    throw new JsonException("Incomplete moderation record.");
            }
            catch (JsonException)
            {
                await _database.RejectModerationRecord(entry);
                Log.Error($"Archived unreadable Discord moderation record {entry.Id:N}.");
                return;
            }

            var ids = record.Admin is { } admin ? record.Players.Append(admin).Distinct().ToArray() : record.Players;
            var accounts = await _database.GetModerationAccounts(ids);

            // A configuration change can invalidate the destination while the database work is pending.
            if (_webhook != webhook)
            {
                await _database.CompleteModerationRecord(entry, TimeSpan.Zero);
                return;
            }

            var payload = RuMCModerationEmbed.Build(record, entry.Id, accounts, _configuration.GetCVar(CVars.GameHostName), _localization);
            HttpResponseMessage response;
            try
            {
                response = await _discord.CreateMessage(webhook, payload);
            }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
            {
                // HTTP exception text can include the webhook token.
                Log.Warning($"Could not reach Discord for moderation record {entry.Id:N} ({error.GetType().Name}).");
                await Retry(webhook, entry, TransientRetry);
                return;
            }

            using (response)
                await HandleResponse(webhook, entry, response);
        }
        catch (Exception error)
        {
            Log.Error($"Could not process the Discord moderation queue ({error.GetType().Name}).");
            if (_webhook == webhook)
                _retryUntil = _timing.RealTime + TransientRetry;
        }
    }

    private async Task HandleResponse(WebhookIdentifier webhook, RuMCModerationOutbox entry, HttpResponseMessage response)
    {
        var status = (int) response.StatusCode;
        switch (response.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                var delay = RateLimitDelay(response);
                Log.Warning($"Discord rate limited the moderation webhook. Retrying after {delay.TotalSeconds:F1} seconds.");
                await Retry(webhook, entry, delay);
                break;
            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound:
                Log.Error($"Discord refused the moderation webhook (HTTP {status}). Check {RuMCModerationRegistryCVars.Webhook.Name}.");
                await Retry(webhook, entry, RejectedRetry);
                break;
            case HttpStatusCode.RequestTimeout or >= HttpStatusCode.InternalServerError:
                Log.Warning($"Discord service error for moderation record {entry.Id:N} (HTTP {status}).");
                await Retry(webhook, entry, TransientRetry);
                break;
            default:
                if (response.IsSuccessStatusCode)
                {
                    await _database.CompleteModerationRecord(entry, null);
                    break;
                }

                await _database.RejectModerationRecord(entry);
                Log.Error($"Archived Discord moderation record {entry.Id:N} after Discord rejected the message (HTTP {status}).");
                break;
        }
    }

    private async Task Retry(WebhookIdentifier webhook, RuMCModerationOutbox entry, TimeSpan delay)
    {
        await _database.CompleteModerationRecord(entry, delay);
        if (_webhook == webhook)
            _retryUntil = _timing.RealTime + delay;
    }

    private static TimeSpan RateLimitDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta > MinimumRateLimitDelay ? delta : MinimumRateLimitDelay;

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            return remaining > MinimumRateLimitDelay ? remaining : MinimumRateLimitDelay;
        }

        // Discord may send fractional seconds, which RetryAfter only parses as integers.
        if (response.Headers.TryGetValues("Retry-After", out var values)
            && double.TryParse(values.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds is >= 0 and <= 86_400)
        {
            return TimeSpan.FromSeconds(Math.Max(seconds, MinimumRateLimitDelay.TotalSeconds));
        }

        return TransientRetry;
    }
}
