using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._RMC14.LinkAccount;
using Content.Server._RuCM.Discord;
using Content.Server.Database;
using Content.Shared._RMC14.CCVar;
using Discord;
using Discord.WebSocket;
using Robust.Shared.Asynchronous;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Server._RMC14.Discord;

// CMU14: account linking shares the game bot, database worker pool and lifetime.
public sealed partial class RMCDiscordManager
{
    [Dependency] private IServerDbManager _linkDb = default!;
    [Dependency] private ITaskManager _linkTasks = default!;
    [Dependency] private ILocalizationManager _linkLoc = default!;
    [Dependency] private LinkAccountManager _linkAccounts = default!;

    private const string LinkComponent = DiscordLinkPanel.Component;
    private readonly SemaphoreSlim _linkPublish = new(1, 1);
    private readonly SemaphoreSlim _linkRequests = new(4, 4);
    private readonly ConcurrentDictionary<ulong, DateTime> _linkAttempts = new();
    private ImmutableDictionary<string, string> _linkText = ImmutableDictionary<string, string>.Empty;
    private CancellationTokenSource _linkLifetime = new();
    private int _linkGeneration;
    private ulong _linkChannel;
    private ulong _linkGuild;
    private bool _linkPanelReady;
    private DateTime _linkRetry;

    public static GatewayIntents GameBotIntents(bool relayChat)
        => GatewayIntents.Guilds | (relayChat ? GatewayIntents.GuildMessages | GatewayIntents.MessageContent : 0);

    private void InitializeAccountLinking()
    {
        _linkLifetime.Dispose();
        _linkLifetime = new CancellationTokenSource();
        _linkGeneration++;
        _linkGuild = 0;
        _linkPanelReady = false;
        _linkRetry = default;
        var configured = _config.GetCVar(DiscordLinkingCVars.Channel);
        if ((!ulong.TryParse(configured, out _linkChannel) || _linkChannel == 0)
            && !string.IsNullOrWhiteSpace(configured))
            _sawmill.Warning("Account-link channel must contain a Discord channel ID.");
        // Capture localized text on the game thread; Discord callbacks do not have a game IoC context.
        _linkText = new[] { "message", "button", "title", "code", "wrong-channel", "invalid-code",
                "expired", "conflict", "success", "already-linked", "busy", "unavailable" }
            .ToImmutableDictionary(key => key, key => _linkLoc.GetString("rucm-discord-link-" + key));
    }

    private void StopAccountLinking()
    {
        _linkGuild = 0;
        _linkLifetime.Cancel();
    }

    private void RetryAccountLinkPanel()
    {
        if (_linkChannel == 0 || _linkPanelReady || !_running || DateTime.UtcNow < _linkRetry)
            return;
        _linkRetry = DateTime.UtcNow.AddMinutes(1);
        _ = PublishAccountLinkMessage();
    }

    private void AccountLinkBotReady()
    {
        _linkPanelReady = false;
        _linkRetry = default;
    }

    private async Task PublishAccountLinkMessage()
    {
        if (_linkChannel == 0 || !_running || !await _linkPublish.WaitAsync(0))
            return;
        try
        {
            var generation = _linkGeneration;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_linkLifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var options = new RequestOptions { CancelToken = timeout.Token, Timeout = 10000 };
            if (await _client.Rest.GetChannelAsync(_linkChannel, options) is not ITextChannel channel)
                throw new InvalidOperationException("Account-link channel must be a guild text channel.");
            _linkGuild = channel.GuildId;
            var panel = await DiscordLinkPanel.Publish(channel, _client.CurrentUser.Id,
                _linkText["message"], _linkText["button"], options);
            var url = $"https://discord.com/channels/{channel.GuildId}/{channel.Id}/{panel.Id}";
            _linkPanelReady = true;
            _linkTasks.RunOnMainThread(() =>
            {
                if (_running && generation == _linkGeneration)
                    _config.SetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink, url);
            });
            _sawmill.Info($"Account-link button ready: {url}");
        }
        catch (OperationCanceledException) when (!_running) { }
        catch (Exception e)
        {
            _sawmill.Error($"Could not prepare account-link button; check channel access and Read Message History: {e.Message}");
        }
        finally { _linkPublish.Release(); }
    }

    private bool AccountLinkChannel(ulong? guild, ulong channel)
        => _running && _linkGuild != 0 && guild == _linkGuild && channel == _linkChannel;

    private async Task OnAccountLinkButton(SocketMessageComponent component)
    {
        if (component.Data.CustomId != LinkComponent)
            return;
        try
        {
            if (!AccountLinkChannel(component.GuildId, component.Channel.Id))
            {
                await component.RespondAsync(_linkText["wrong-channel"], ephemeral: true);
                return;
            }
            var modal = new ModalBuilder().WithTitle(_linkText["title"]).WithCustomId(LinkComponent)
                .AddTextInput(_linkText["code"], "account_code", minLength: 36, maxLength: 36).Build();
            await component.RespondWithModalAsync(modal);
        }
        catch (Exception e) { _sawmill.Warning($"Could not open account-link form: {e.Message}"); }
    }

    private async Task OnAccountLinkModal(SocketModal modal)
    {
        if (modal.Data.CustomId != LinkComponent)
            return;
        var acquired = false;
        try
        {
            await modal.DeferAsync(ephemeral: true);
            if (!AccountLinkChannel(modal.GuildId, modal.Channel.Id))
            {
                await modal.FollowupAsync(_linkText["wrong-channel"], ephemeral: true);
                return;
            }
            var text = modal.Data.Components.FirstOrDefault(c => c.CustomId == "account_code")?.Value;
            if (!Guid.TryParse(text?.Trim(), out var code) || code == Guid.Empty)
            {
                await modal.FollowupAsync(_linkText["invalid-code"], ephemeral: true);
                return;
            }
            var now = DateTime.UtcNow;
            if (_linkAttempts.TryGetValue(modal.User.Id, out var last) && now - last < TimeSpan.FromSeconds(5)
                || !(acquired = await _linkRequests.WaitAsync(0)))
            {
                await modal.FollowupAsync(_linkText["busy"], ephemeral: true);
                return;
            }
            _linkAttempts[modal.User.Id] = now;
            if (_linkAttempts.Count > 4096)
                foreach (var attempt in _linkAttempts.Where(a => now - a.Value > TimeSpan.FromMinutes(1)))
                    _linkAttempts.TryRemove(attempt.Key, out _);
            var result = await _linkDb.CompleteDiscordLink(code, modal.User.Id, now);
            var key = result.Status switch
            {
                RMCDiscordLinkStatus.Success => "success",
                RMCDiscordLinkStatus.AlreadyLinked => "already-linked",
                RMCDiscordLinkStatus.Expired => "expired",
                RMCDiscordLinkStatus.Conflict => "conflict",
                _ => "invalid-code",
            };
            if (result.Player != Guid.Empty)
                _linkTasks.RunOnMainThread(async () =>
                {
                    try { await _linkAccounts.RefreshPatron(new NetUserId(result.Player)); }
                    catch (Exception e) { _sawmill.Warning($"Could not refresh linked player status: {e.Message}"); }
                });
            await modal.FollowupAsync(_linkText[key], ephemeral: true, allowedMentions: AllowedMentions.None);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Account linking could not complete: {e.GetType().Name}");
            try { await modal.FollowupAsync(_linkText["unavailable"], ephemeral: true); }
            catch (Exception) { /* Interaction may already have expired. */ }
        }
        finally { if (acquired) _linkRequests.Release(); }
    }
}
