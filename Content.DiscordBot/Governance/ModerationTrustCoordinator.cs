using Discord;
using Discord.WebSocket;

namespace Content.DiscordBot.Governance;

public sealed class ModerationTrustCoordinator(
    DiscordSocketClient client,
    ModerationTrustService trust,
    CommunityCourtService court,
    Config config,
    DiscordGuildMemberCache guildMembers)
{
    public async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Clamp(config.ModerationReviewSchedulerSeconds, 10, 3600));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (client.ConnectionState == ConnectionState.Connected)
                    await ProcessOnceAsync();
            }
            catch (Exception exception)
            {
                await Logger.Error("Moderation Trust scheduler iteration failed", exception);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task ProcessOnceAsync()
    {
        await trust.ProcessDeadlinesAsync();
        var available = await GuildMembersAsync();
        await trust.EnsureAutomaticReviewsAsync(available);
        // Qualification promotion/demotion is authoritative in ReputationCoordinator. The legacy
        // TrustScore remains useful as an audit diagnostic but must not race the Bayesian policy.
        await NotifyReviewersAsync();
    }

    private async Task<IReadOnlySet<ulong>> GuildMembersAsync()
    {
        return await guildMembers.ExistingMembersAsync(await court.LinkedDiscordIdsAsync());
    }

    private async Task NotifyReviewersAsync()
    {
        foreach (var (invitation, user) in await trust.PendingReviewNotificationsAsync())
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
                    $"RUCM выбрал вас для независимого аудита действия дежурного №{invitation.EntityId}. " +
                    $"До <t:{new DateTimeOffset(invitation.ExpiresAt).ToUnixTimeSeconds()}:F> ответьте через " +
                    "`/дежурство аудит-ответ`. После согласия используйте `/дежурство аудит-материалы`, " +
                    "а затем `/дежурство аудит`. Ответ на приглашение сам по себе не меняет репутацию: " +
                    "статистически учитывается выполнение или срыв уже принятой обязанности.");
                await trust.MarkInvitationNotifiedAsync(invitation.Id);
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not notify moderation reviewer {user.DiscordUserId} for invitation {invitation.Id}", exception);
            }
        }
    }
}
