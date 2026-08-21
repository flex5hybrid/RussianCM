using System.Collections.Immutable;
using System.Reflection;
using Content.DiscordBot.Modules;
using Content.DiscordBot.Governance;
using Content.Server.Database;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot;

public sealed class CommandHandler(
    DiscordSocketClient client,
    CommandService commands,
    InteractionService interaction,
    Func<ServerDbContext> databaseFactory,
    GovernanceIdentityService identities,
    DiscordGuildMemberCache guildMembers,
    IServiceProvider services,
    ulong guild)
{
    private ImmutableDictionary<ulong, RMCPatronTier>? _patronTiers;
    private ImmutableArray<RMCPatronTier> _tierPriority;
    private Task? _refreshPatronsTask;

    public int Running = 1;

    public async Task InstallCommandsAsync()
    {
        await using var db = databaseFactory();
        var patronTiers = await db.RMCPatronTiers.ToListAsync();
        _tierPriority = [..patronTiers.OrderBy(t => t.Priority)];
        _patronTiers = patronTiers.ToImmutableDictionary(t => t.DiscordRole, t => t);

        client.MessageReceived += HandleCommandAsync;
        client.ButtonExecuted += HandleButtonAsync;
        client.ModalSubmitted += HandleModalAsync;
        client.InteractionCreated += HandleInteractionAsync;
        client.Ready += RegisterInteractionsAsync;
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        await interaction.AddModulesAsync(Assembly.GetEntryAssembly(), services);

        interaction.AddModalInfo<LinkAccountModal>();

        _refreshPatronsTask = Task.Run(async () => await RefreshPatrons());
    }

    private async Task RegisterInteractionsAsync()
    {
        await interaction.RegisterCommandsToGuildAsync(guild, true);
        await ConfigureGovernanceChannelPermissionsAsync();
        await Logger.Info($"Registered Discord interactions in guild {guild}.");
    }

    private async Task ConfigureGovernanceChannelPermissionsAsync()
    {
        if (services.GetService(typeof(Config)) is not Config config)
            return;

        var socketGuild = client.GetGuild(guild);
        if (socketGuild == null)
            return;

        var channelIds = new[] { config.CourtChannel, config.GovernanceChannel }
            .Where(value => value != 0)
            .Distinct()
            .ToArray();

        foreach (var channelId in channelIds)
        {
            if (client.GetChannel(channelId) is not SocketGuildChannel channel)
                continue;

            try
            {
                var everyone = socketGuild.EveryoneRole;
                var everyoneCurrent = channel.GetPermissionOverwrite(everyone) ?? OverwritePermissions.InheritAll;
                var everyoneReadOnly = everyoneCurrent.Modify(
                    sendMessages: PermValue.Deny,
                    createPublicThreads: PermValue.Deny,
                    createPrivateThreads: PermValue.Deny,
                    sendMessagesInThreads: PermValue.Deny);
                if (!everyoneCurrent.Equals(everyoneReadOnly))
                    await channel.AddPermissionOverwriteAsync(everyone, everyoneReadOnly);

                var bot = socketGuild.CurrentUser;
                var botCurrent = channel.GetPermissionOverwrite(bot) ?? OverwritePermissions.InheritAll;
                var botWritable = botCurrent.Modify(
                    sendMessages: PermValue.Allow,
                    embedLinks: PermValue.Allow,
                    attachFiles: PermValue.Allow,
                    manageThreads: PermValue.Allow,
                    createPublicThreads: PermValue.Allow,
                    createPrivateThreads: PermValue.Allow,
                    sendMessagesInThreads: PermValue.Allow);
                if (!botCurrent.Equals(botWritable))
                    await channel.AddPermissionOverwriteAsync(bot, botWritable);

                await Logger.Info($"Governance Discord channel '{channel.Name}' ({channel.Id}) configured read-only for regular members.");
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not configure read-only Governance permissions for channel {channelId}", exception);
            }
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction socketInteraction)
    {
        var context = new SocketInteractionContext(client, socketInteraction);
        var result = await interaction.ExecuteCommandAsync(context, services);
        if (!result.IsSuccess && result.Error != InteractionCommandError.UnknownCommand)
            await Logger.Info($"Interaction failed for {socketInteraction.User.Id}: {result.ErrorReason}");
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Governance channels are read-only at the Discord permission layer. Do not delete
        // messages after the fact: preventing them is deterministic and leaves no ACL race.
        var message = messageParam as SocketUserMessage;
        if (message == null || message.Author.IsBot)
            return;

        var argPos = 0;
        if (!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            return;

        var context = new SocketCommandContext(client, message);
        var result = await commands.ExecuteAsync(context, argPos, null);
        if (!result.IsSuccess)
        {
            var reason = result.ErrorReason ?? "неизвестная ошибка команды";
            await Logger.Info($"Command '{message.Content}' failed for {message.Author.Username}: {reason}");

            if (result.Error != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync($"Команда не выполнена: {reason}");
        }
    }

    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        switch (component.Data.CustomId)
        {
            case "link-ss14-account":
                await component.RespondWithModalAsync<LinkAccountModal>("link-ss14-account");
                break;
        }
    }

    private async Task HandleModalAsync(SocketModal modal)
    {
        await using var db = databaseFactory();
        switch (modal.Data.CustomId)
        {
            case "link-ss14-account":
                if (modal.GuildId is not { } guildId)
                    break;

                var codeStr = modal.Data.Components.First(c => c.CustomId == "account_code").Value.Trim();
                if (string.IsNullOrWhiteSpace(codeStr))
                    break;

                await modal.DeferAsync(true);
                if (!Guid.TryParse(codeStr, out var code))
                {
                    await modal.FollowupAsync(
                        $"`{codeStr}` — некорректный код привязки. Получите новый код в лобби игры и повторите попытку.",
                        ephemeral: true);
                    break;
                }

                var authorId = modal.User.Id;
                var discord = await db.RMCDiscordAccounts
                    .Include(d => d.LinkedAccount)
                    .ThenInclude(l => l.Player)
                    .ThenInclude(p => p.Patron)
                    .FirstOrDefaultAsync(a => a.Id == authorId);
                var codes = await db.RMCLinkingCodes
                    .Include(l => l.Player)
                    .ThenInclude(player => player.Patron)
                    .FirstOrDefaultAsync(p => p.Code == code);

                if (codes == null)
                {
                    await modal.FollowupAsync(
                        "Код привязки не найден. Зайдите на игровой сервер, получите новый код в лобби и повторите попытку.",
                        ephemeral: true);
                    break;
                }

                if (codes.CreationTime < DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
                {
                    await modal.FollowupAsync(
                        "Срок действия кода привязки истёк. Получите новый код в лобби игры.",
                        ephemeral: true);
                    break;
                }

                var targetPlayerId = codes.Player.UserId;
                if (discord?.LinkedAccount is { } currentDiscordLink && currentDiscordLink.PlayerId != targetPlayerId)
                {
                    await modal.FollowupAsync(
                        $"Ваш Discord уже связан с SS14-аккаунтом **{currentDiscordLink.Player.LastSeenUserName}**. Перепривязка запрещена.",
                        ephemeral: true);
                    break;
                }

                var currentPlayerLink = await db.RMCLinkedAccounts.AsNoTracking()
                    .SingleOrDefaultAsync(value => value.PlayerId == targetPlayerId);
                if (currentPlayerLink != null && currentPlayerLink.DiscordId != authorId)
                {
                    await modal.FollowupAsync(
                        "Этот SS14-аккаунт уже связан с другим Discord. Перепривязка запрещена.",
                        ephemeral: true);
                    break;
                }

                try
                {
                    // Permanent Governance identity is checked before any game-database mutation.
                    await identities.ValidatePermanentLinkAsync(targetPlayerId, authorId);
                }
                catch (CourtRuleException exception)
                {
                    await modal.FollowupAsync(exception.Message, ephemeral: true);
                    break;
                }

                var createdLink = false;
                if (discord?.LinkedAccount == null)
                {
                    discord ??= db.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = authorId }).Entity;
                    discord.LinkedAccount = db.RMCLinkedAccounts.Add(new RMCLinkedAccount { Discord = discord }).Entity;
                    discord.LinkedAccount.Player = codes.Player;
                    createdLink = true;
                }

                var memberLookup = await guildMembers.LookupAsync(authorId);
                var roles = memberLookup.User?.RoleIds.ToArray() ?? [];
                var tiers = await db.RMCPatronTiers
                    .Where(t => roles.Contains(t.DiscordRole))
                    .ToListAsync();
                if (tiers.Count == 0)
                {
                    discord!.LinkedAccount.Player.Patron = null;
                }
                else
                {
                    tiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    var tier = tiers[0];
                    discord!.LinkedAccount.Player.Patron ??= db.RMCPatrons.Add(new RMCPatron
                    {
                        PlayerId = discord.LinkedAccount.PlayerId,
                    }).Entity;
                    discord.LinkedAccount.Player.Patron.TierId = tier.Id;
                }

                if (createdLink)
                {
                    db.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
                    {
                        Discord = discord!,
                        Player = discord!.LinkedAccount.Player,
                    });
                }

                db.ChangeTracker.DetectChanges();
                await db.SaveChangesAsync();
                try
                {
                    await identities.SyncLinkedAccountAsync(targetPlayerId, authorId);
                }
                catch (CourtRuleException exception)
                {
                    await Logger.Error(
                        $"Governance identity synchronization rejected game link Discord {authorId} -> SS14 {targetPlayerId}",
                        exception);
                    await modal.FollowupAsync(
                        "Игровая связь сохранена, но проверка Governance обнаружила конфликт постоянной идентичности. Обратитесь к руководству; автоматическая перепривязка не выполнялась.",
                        ephemeral: true);
                    break;
                }

                var msg = $"SS14-аккаунт **{codes.Player.LastSeenUserName}** успешно связан с вашим Discord. Эта связь постоянная и не может быть перепривязана к другому аккаунту.";
                if (codes.Player.Patron != null)
                    msg += $" Уровень поддержки: **{codes.Player.Patron.Tier.Name}**.";

                await modal.FollowupAsync(msg, ephemeral: true);
                break;
        }
    }

    private async Task RefreshPatrons()
    {
        while (Interlocked.CompareExchange(ref Running, 1, 1) == 1)
        {
            try
            {
                await using var db = databaseFactory();
                var patrons = await db.RMCLinkedAccounts
                    .Include(l => l.Player)
                    .ThenInclude(p => p.Patron)
                    .ThenInclude(p => p!.Tier)
                    .ToListAsync();

                foreach (var linked in patrons)
                {
                    try
                    {
                        var lookup = await guildMembers.LookupAsync(linked.DiscordId);
                        if (!lookup.IsDefinitive)
                            continue;

                        var user = lookup.User;
                        if (user == null)
                        {
                            if (linked.Player.Patron != null)
                            {
                                linked.Player.Patron = null;
                                await Logger.Info($"Removed patron {linked.DiscordId}:{linked.Player.LastSeenUserName}");
                            }

                            continue;
                        }

                        var isPatron = false;
                        foreach (var tier in _tierPriority)
                        {
                            if (user.RoleIds.Contains(tier.DiscordRole))
                            {
                                isPatron = true;
                                if (linked.Player.Patron?.Tier.DiscordRole == tier.DiscordRole)
                                    break;

                                linked.Player.Patron ??= db.RMCPatrons.Add(new RMCPatron { PlayerId = linked.PlayerId })
                                    .Entity;
                                linked.Player.Patron.TierId = tier.Id;
                                await Logger.Info($"Updated patron {user.Username}:{linked.DiscordId}:{linked.Player.LastSeenUserName} with tier {tier.Name}");
                                break;
                            }
                        }

                        if (!isPatron && linked.Player.Patron != null)
                        {
                            linked.Player.Patron = null;
                            await Logger.Info($"Removed patron {user.Username}:{linked.DiscordId}:{linked.Player.LastSeenUserName}");
                        }
                    }
                    catch (Exception e)
                    {
                        await Logger.Error($"Error updating patron with discord id {linked.DiscordId} and player id {linked.PlayerId}", e);
                    }
                }

                await db.SaveChangesAsync();
                await Task.Delay(60000);
            }
            catch (Exception e)
            {
                await Logger.Error("Error refreshing patrons", e);
            }
        }
    }
}
