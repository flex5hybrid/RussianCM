using System.Text;
using Discord;
using Discord.WebSocket;

namespace Content.DiscordBot.Governance;

public sealed class CourtDiscordCoordinator(
    DiscordSocketClient client,
    CommunityCourtService court,
    CourtSourceMaterialService materials,
    CourtPunishmentService punishments,
    EventGovernanceService events,
    ModerationGovernanceService moderation,
    Config config,
    DiscordGuildMemberCache guildMembers)
{
    private bool _courtChannelValidated;

    public async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Clamp(config.CourtSchedulerSeconds, 10, 3600));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync();
            }
            catch (Exception exception)
            {
                await Logger.Error("Community Court scheduler iteration failed", exception);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task ProcessOnceAsync()
    {
        if (!config.CourtEnabled)
            return;
        if (client.ConnectionState != ConnectionState.Connected)
            return;
        _ = client.GetGuild(config.Guild)
            ?? throw new InvalidOperationException($"Discord bot cannot access configured guild {config.Guild}.");
        await ValidateCourtChannelAsync();
        var available = await GuildMembersAsync();
        await court.ProcessDeadlinesAsync(available);
        await events.ProcessDeadlinesAsync();
        await punishments.ExecutePendingAsync();
        foreach (var courtCase in await court.CasesWithoutThreadsAsync())
            await EnsureCaseThreadAsync(courtCase);
        foreach (var caseId in await materials.CasesNeedingMaterialsAsync())
            await PublishCaseMaterialsAsync(caseId);
        foreach (var ticket in await moderation.AHelpsWithoutThreadsAsync())
            await EnsureAHelpThreadAsync(ticket);
        await NotifyJurorsAsync();
        await NotifyEventReviewersAsync();
        await PublishVerdictsAsync();
    }

    private async Task ValidateCourtChannelAsync()
    {
        if (_courtChannelValidated)
            return;
        if (config.CourtChannel == 0)
            throw new InvalidOperationException("CourtChannel is not configured.");

        var channel = client.GetChannel(config.CourtChannel)
            ?? throw new InvalidOperationException($"Court channel {config.CourtChannel} is unavailable.");
        if (channel is not SocketForumChannel && channel is not SocketTextChannel)
            throw new InvalidOperationException($"Court channel {config.CourtChannel} is not a forum or text channel.");

        _courtChannelValidated = true;
        var name = channel is SocketGuildChannel guildChannel ? guildChannel.Name : channel.Id.ToString();
        await Logger.Info($"Community Court channel '{name}' ({channel.Id}) is available as {channel.GetType().Name}.");
    }

    private async Task<IReadOnlySet<ulong>> GuildMembersAsync()
    {
        return await guildMembers.ExistingMembersAsync(
            await court.LinkedDiscordIdsAsync(),
            forceRefresh: config.CourtTestMode);
    }

    public async Task<IThreadChannel> EnsureCaseThreadAsync(GovernanceCourtCase courtCase)
    {
        if (courtCase.DiscordThreadId is { } existing && client.GetChannel((ulong) existing) is SocketThreadChannel cached)
            return cached;
        if (config.CourtChannel == 0)
            throw new InvalidOperationException("CourtChannel is not configured.");
        var channel = client.GetChannel(config.CourtChannel)
            ?? throw new InvalidOperationException($"Court channel {config.CourtChannel} is unavailable.");
        var name = $"суд-{courtCase.Id:000000}";
        var embed = await BuildCaseEmbedAsync(courtCase);
        IThreadChannel thread;
        if (channel is SocketForumChannel forum)
        {
            thread = await forum.CreatePostAsync(
                name,
                ThreadArchiveDuration.OneWeek,
                null,
                string.Empty,
                embed);
        }
        else if (channel is SocketTextChannel text)
        {
            thread = await text.CreateThreadAsync(name, ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
            await thread.SendMessageAsync(embed: embed);
        }
        else
        {
            throw new InvalidOperationException($"Court channel {config.CourtChannel} is not a forum or text channel.");
        }
        await court.AttachThreadAsync(courtCase.Id, thread.Id);
        await thread.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("Панель Community Court")
                .WithDescription("Тред является журналом дела и доступен только для чтения. Все действия выполняются через интерактивную панель.")
                .WithColor(Color.DarkBlue)
                .Build(),
            components: GovernanceDiscordUi.CourtThreadLauncher(courtCase.Id));
        return thread;
    }

    private async Task PublishCaseMaterialsAsync(long caseId)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var thread = await EnsureCaseThreadAsync(courtCase);
        var source = await materials.GetAsync(caseId);
        if (source == null)
        {
            await materials.MarkMaterialsPublishedAsync(caseId);
            return;
        }

        var transcriptLines = source.Transcript.Select(line =>
        {
            var role = line.FromResponder ? "Дежурный" : "Игрок";
            return $"**{line.CreatedAt.ToLocalTime():HH:mm:ss} • {role} • {EscapeDiscord(line.SenderName)}**\n{EscapeDiscord(line.Body)}";
        });
        await PublishChunksAsync(
            thread,
            $"AHelp #{source.AHelpTicketId} • полный диалог",
            transcriptLines,
            Color.Blue,
            "В исходном AHelp нет сообщений.");

        var historyLines = source.PlayerHistory.Select(entry =>
            $"**{entry.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm} • {EscapeDiscord(entry.Kind)}**\n{EscapeDiscord(entry.Message)}");
        await PublishChunksAsync(
            thread,
            $"История ответчика • {EscapeDiscord(source.DefendantName)}",
            historyLines,
            Color.DarkGrey,
            "У ответчика нет сохранённых заметок, watchlist-записей или отображаемых банов.");

        await materials.MarkMaterialsPublishedAsync(caseId);
    }

    private static async Task PublishChunksAsync(
        IThreadChannel thread,
        string title,
        IEnumerable<string> lines,
        Color color,
        string emptyText)
    {
        var chunks = new List<string>();
        var buffer = new StringBuilder();
        foreach (var line in lines)
        {
            var safeLine = line.Length > 3400 ? line[..3400] + "…" : line;
            if (buffer.Length > 0 && buffer.Length + safeLine.Length + 2 > 3800)
            {
                chunks.Add(buffer.ToString());
                buffer.Clear();
            }
            if (buffer.Length > 0)
                buffer.Append("\n\n");
            buffer.Append(safeLine);
        }
        if (buffer.Length > 0)
            chunks.Add(buffer.ToString());
        if (chunks.Count == 0)
            chunks.Add(emptyText);

        for (var index = 0; index < chunks.Count; index++)
        {
            var partTitle = chunks.Count == 1 ? title : $"{title} • {index + 1}/{chunks.Count}";
            await thread.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle(partTitle)
                .WithDescription(chunks[index])
                .WithColor(color)
                .Build());
        }
    }

    public async Task PublishStatementAsync(long caseId, GovernanceCourtStatement statement)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var thread = await EnsureCaseThreadAsync(courtCase);
        var author = await court.GetAccountAsync(statement.AuthorUserId);
        var embed = new EmbedBuilder()
            .WithTitle(statement.Kind == "defense" ? $"Защита по делу №{caseId}" : $"Материал по делу №{caseId}")
            .WithDescription(statement.Body)
            .WithColor(statement.Kind == "defense" ? Color.Blue : Color.Orange)
            .WithFooter($"{author.Name} • SS14 {author.PlayerId}")
            .WithCurrentTimestamp();
        if (!string.IsNullOrWhiteSpace(statement.EvidenceReference))
            embed.AddField("Доказательство", statement.EvidenceReference);
        await thread.SendMessageAsync(embed: embed.Build());
    }

    public async Task<Embed> BuildStatusEmbedAsync(long caseId)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var embed = await BuildCaseEmbedAsync(courtCase);
        return embed.ToEmbedBuilder()
            .AddField("Вердикт", VerdictText(courtCase.Verdict), true)
            .AddField("Наказание", SanctionText(courtCase), true)
            .Build();
    }

    public async Task PublishLeadershipNoticeAsync(long caseId, string title, string description, Color color)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var thread = await EnsureCaseThreadAsync(courtCase);
        await thread.ModifyAsync(properties => properties.Archived = false);
        await thread.ModifyAsync(properties => properties.Locked = false);
        await thread.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle(title).WithDescription(description).WithColor(color).WithCurrentTimestamp().Build());
        await thread.ModifyAsync(properties =>
        {
            properties.Locked = true;
            properties.Archived = true;
        });
    }

    public async Task<IThreadChannel> EnsureEventThreadAsync(GovernanceEventProposal proposal)
    {
        if (proposal.DiscordThreadId is { } existing && client.GetChannel((ulong) existing) is SocketThreadChannel cached)
            return cached;
        var channelId = config.GovernanceChannel != 0 ? config.GovernanceChannel : config.CourtChannel;
        var channel = client.GetChannel(channelId)
            ?? throw new InvalidOperationException($"Governance channel {channelId} is unavailable.");
        var manifest = System.Text.Json.JsonSerializer.Deserialize<EventManifestRequest[]>(proposal.Manifest) ?? [];
        var manifestText = string.Join("\n", manifest.Select(value => $"• `{value.Capability}` / `{value.Resource}` × {value.MaxUses}"));
        var embed = new EmbedBuilder().WithTitle($"Событие №{proposal.Id} • {proposal.Title}")
            .WithDescription(proposal.Description).AddField("Продолжительность", $"{proposal.DurationMinutes} мин.", true)
            .AddField("Рецензирование до", $"<t:{new DateTimeOffset(proposal.ReviewDeadline).ToUnixTimeSeconds()}:F>", true)
            .AddField("Манифест", manifestText).WithColor(Color.Teal).WithCurrentTimestamp().Build();
        IThreadChannel thread;
        if (channel is SocketForumChannel forum)
            thread = await forum.CreatePostAsync($"событие-{proposal.Id:000000}", ThreadArchiveDuration.OneWeek, null, string.Empty, embed);
        else if (channel is SocketTextChannel text)
        {
            thread = await text.CreateThreadAsync($"событие-{proposal.Id:000000}", ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
            await thread.SendMessageAsync(embed: embed);
        }
        else
            throw new InvalidOperationException($"Governance channel {channelId} is unavailable as a forum or text channel.");
        await events.AttachThreadAsync(proposal.Id, thread.Id);
        return thread;
    }

    public async Task PublishEventStatusAsync(long proposalId, string message)
    {
        var proposal = await events.GetProposalAsync(proposalId);
        var thread = await EnsureEventThreadAsync(proposal);
        await thread.SendMessageAsync(embed: new EmbedBuilder().WithTitle($"Состояние события №{proposalId}")
            .WithDescription(message).AddField("Статус", proposal.Status).WithColor(Color.Teal).WithCurrentTimestamp().Build());
    }

    public async Task<IThreadChannel?> EnsureAHelpThreadAsync(GovernanceAHelpTicket ticket)
    {
        if (config.GovernanceChannel == 0)
            return null;
        if (ticket.DiscordThreadId is { } existing && client.GetChannel((ulong) existing) is SocketThreadChannel cached)
            return cached;
        var channel = client.GetChannel(config.GovernanceChannel)
            ?? throw new InvalidOperationException($"Governance channel {config.GovernanceChannel} is unavailable.");
        var reporter = await moderation.GetReporterAsync(ticket);
        var reporterText = reporter.DiscordId is { } discordId && discordId > 0
            ? $"<@{discordId}> ({reporter.Name})"
            : reporter.Name;
        var embed = new EmbedBuilder().WithTitle($"AHelp №{ticket.Id} • раунд {ticket.RoundId}")
            .WithDescription(ticket.Summary).AddField("Заявитель", reporterText, true)
            .AddField("Статус", ticket.Status, true).WithColor(Color.Gold).WithCurrentTimestamp().Build();
        IThreadChannel thread;
        if (channel is SocketForumChannel forum)
            thread = await forum.CreatePostAsync($"ahelp-{ticket.Id:000000}", ThreadArchiveDuration.OneWeek, null, string.Empty, embed);
        else if (channel is SocketTextChannel text)
        {
            thread = await text.CreateThreadAsync($"ahelp-{ticket.Id:000000}", ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
            await thread.SendMessageAsync(embed: embed);
        }
        else
            throw new InvalidOperationException($"Governance channel {config.GovernanceChannel} is not a forum or text channel.");
        await moderation.AttachThreadAsync(ticket.Id, thread.Id);
        return thread;
    }

    private async Task NotifyJurorsAsync()
    {
        foreach (var (invitation, user) in await court.PendingNotificationsAsync())
        {
            if (user.DiscordUserId is not > 0)
                continue;

            try
            {
                var discordId = checked((ulong) user.DiscordUserId.Value);
                IUser? discordUser = client.GetUser(discordId);
                discordUser ??= await client.Rest.GetUserAsync(discordId);
                if (discordUser == null)
                    continue;

                var components = new ComponentBuilder()
                    .WithButton("Принять", $"court-jury-accept:{invitation.EntityId}", ButtonStyle.Success, new Emoji("✅"))
                    .WithButton("Отказаться", $"court-jury-decline:{invitation.EntityId}", ButtonStyle.Danger, new Emoji("✖️"))
                    .WithButton("Самоотвод", $"court-jury-recuse:{invitation.EntityId}", ButtonStyle.Secondary, new Emoji("↩️"))
                    .Build();
                var dm = await discordUser.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"Приглашение в коллегию • дело №{invitation.EntityId}")
                        .WithDescription("Вас выбрали кандидатом в коллегию Community Court. Обсуждать дело с другими присяжными нельзя; голосование проводится тайно через панель.")
                        .AddField("Ответить до", $"<t:{new DateTimeOffset(invitation.ExpiresAt).ToUnixTimeSeconds()}:F>")
                        .AddField("Что дальше", "Принятие, отказ и самоотвод сами по себе не меняют репутацию. После принятия оценивается только выполнение взятой обязанности.")
                        .WithColor(Color.DarkOrange)
                        .WithCurrentTimestamp()
                        .Build(),
                    components: components);
                await court.MarkInvitationNotifiedAsync(invitation.Id);
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not notify juror {user.DiscordUserId} for invitation {invitation.Id}", exception);
            }
        }
    }

    private async Task NotifyEventReviewersAsync()
    {
        foreach (var (invitation, user, proposal) in await events.PendingReviewNotificationsAsync())
        {
            if (user.DiscordUserId is not > 0)
                continue;

            try
            {
                var discordId = checked((ulong) user.DiscordUserId.Value);
                IUser? discordUser = client.GetUser(discordId);
                discordUser ??= await client.Rest.GetUserAsync(discordId);
                if (discordUser == null)
                    continue;

                var dm = await discordUser.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"Рецензирование события №{proposal.Id}")
                        .WithDescription(proposal.Title)
                        .AddField("Ответить до", $"<t:{new DateTimeOffset(invitation.ExpiresAt).ToUnixTimeSeconds()}:F>", true)
                        .AddField("Репутация", "Принятие, отказ и самоотвод нейтральны. После принятия учитывается только завершение или срыв рецензии.", true)
                        .AddField("Порядок", "Сначала примите приглашение. После этого появятся кнопки «Одобрить» и «Отклонить» с модалкой для обоснования.")
                        .WithColor(Color.Teal)
                        .WithCurrentTimestamp()
                        .Build(),
                    components: GovernanceDiscordUi.EventReviewInvite(proposal.Id));
                await events.MarkInvitationNotifiedAsync(invitation.Id);
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not notify event reviewer candidate {user.DiscordUserId} for proposal {proposal.Id}", exception);
            }
        }
    }

    private async Task PublishVerdictsAsync()
    {
        foreach (var courtCase in await court.UnpublishedVerdictsAsync())
        {
            var thread = await EnsureCaseThreadAsync(courtCase);
            var message = await thread.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle($"Решение Community Court по делу №{courtCase.Id}")
                .WithDescription(VerdictText(courtCase.Verdict))
                .AddField("Назначенная мера", SanctionText(courtCase))
                .WithColor(courtCase.Verdict == CourtVerdicts.Guilty ? Color.Red : Color.Green)
                .WithCurrentTimestamp()
                .Build());
            await court.MarkPublishedAsync(courtCase.Id, message.Id);
            await thread.ModifyAsync(properties =>
            {
                properties.Locked = true;
                properties.Archived = true;
            });
        }
    }

    private async Task<Embed> BuildCaseEmbedAsync(GovernanceCourtCase courtCase)
    {
        var source = await materials.GetAsync(courtCase.Id);
        var claimantText = await CourtAccountTextAsync(courtCase.ClaimantUserId, source?.ClaimantName);
        var defendantText = await CourtAccountTextAsync(courtCase.DefendantUserId, source?.DefendantName);
        if (source != null)
        {
            claimantText += $"\nSS14 `{source.ClaimantSs14UserId}`";
            defendantText = $"{defendantText}\nПерсонаж: **{EscapeDiscord(source.DefendantCharacterName)}**\nSS14 `{source.DefendantSs14UserId}`";
        }

        var statements = await court.GetStatementsAsync(courtCase.Id);
        var complaint = statements.FirstOrDefault(value => value.Kind == "complaint");
        var embed = new EmbedBuilder()
            .WithTitle($"Community Court • дело №{courtCase.Id}")
            .WithDescription(courtCase.Summary)
            .WithColor(Color.DarkOrange)
            .AddField("Раунд", courtCase.RoundId, true)
            .AddField("Истец", claimantText, true)
            .AddField("Ответчик", defendantText, true)
            .AddField("Стадия", StatusText(courtCase.Status), true)
            .AddField("Срок защиты", $"<t:{new DateTimeOffset(courtCase.DefenseDeadline).ToUnixTimeSeconds()}:F>", true)
            .WithCurrentTimestamp();
        if (!string.IsNullOrWhiteSpace(complaint?.EvidenceReference))
            embed.AddField("Источник дела", complaint.EvidenceReference);
        return embed.Build();
    }

    private async Task<string> CourtAccountTextAsync(Guid governanceUserId, string? fallbackName = null)
    {
        try
        {
            var account = await court.GetAccountAsync(governanceUserId);
            return account.DiscordId is > 0
                ? $"<@{account.DiscordId}> ({EscapeDiscord(account.Name)})"
                : $"{EscapeDiscord(account.Name)} • Discord не привязан";
        }
        catch (CourtRuleException)
        {
            return string.IsNullOrWhiteSpace(fallbackName)
                ? $"Governance `{governanceUserId}` • Discord не привязан"
                : $"{EscapeDiscord(fallbackName)} • Discord не привязан";
        }
    }

    private static string EscapeDiscord(string value)
    {
        return value.Replace("@", "@\u200B", StringComparison.Ordinal)
            .Replace("`", "ˋ", StringComparison.Ordinal);
    }

    private static string StatusText(string status) => status switch
    {
        CourtStatuses.Defense => "Защита",
        CourtStatuses.AwaitingJury => "Формирование коллегии",
        CourtStatuses.Jury => "Голосование о виновности",
        CourtStatuses.Sentencing => "Голосование о наказании",
        CourtStatuses.Verdict => "Решение вынесено",
        CourtStatuses.Executed => "Решение исполнено",
        CourtStatuses.Overturned => "Решение отменено",
        _ => status,
    };

    private static string VerdictText(string? verdict) => verdict switch
    {
        CourtVerdicts.Guilty => "Виновен",
        CourtVerdicts.NotGuilty => "Не виновен",
        CourtVerdicts.InsufficientEvidence => "Недостаточно доказательств",
        _ => "Ещё не вынесен",
    };

    private static string SanctionText(GovernanceCourtCase courtCase) => courtCase.SanctionType switch
    {
        CourtSanctions.Warning => "Предупреждение",
        CourtSanctions.GameBan => $"Блокировка игры на {courtCase.SanctionDays} дн.",
        CourtSanctions.JobBan => $"Блокировка роли `{courtCase.SanctionRole}` на {courtCase.SanctionDays} дн.",
        _ => "Не назначено",
    };
}
