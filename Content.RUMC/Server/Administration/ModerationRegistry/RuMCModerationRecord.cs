using Content.Server.Discord;
using Content.Shared.Database;
using Robust.Shared.Configuration;

namespace Content.Server.CMU14.Administration.ModerationRegistry;

[CVarDefs]
public static class CMUModerationRegistryCVars
{
    public static readonly CVarDef<string> DiscordWebhook =
        CVarDef.Create("discord.cmu_moderation_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

internal enum CMUModerationKind
{
    ServerBan,
    RoleBan,
    ChatBan,
    Warning,
}

internal sealed record CMUModerationRecord(
    CMUModerationKind Kind,
    Guid[] Players,
    Guid? Admin,
    string Reason,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    int[] Rounds,
    NoteSeverity? Severity = null,
    string? Scope = null)
{
    public static bool TryGetWebhook(string url, out WebhookIdentifier webhook)
    {
        webhook = default;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host != "discord.com"
            || !uri.IsDefaultPort
            || uri.UserInfo.Length != 0
            || uri.Query.Length != 0
            || uri.Fragment.Length != 0)
        {
            return false;
        }

        var path = uri.AbsolutePath.Split('/');
        if (path is not ["", "api", "webhooks", _, _]
            && path is not ["", "api", "v10", "webhooks", _, _])
        {
            return false;
        }

        var id = path[^2];
        var token = path[^1];
        if (!ulong.TryParse(id, out _) || token.Length == 0)
            return false;

        foreach (var character in token)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-'))
                return false;
        }

        webhook = new WebhookIdentifier(id, token);
        return true;
    }
}
