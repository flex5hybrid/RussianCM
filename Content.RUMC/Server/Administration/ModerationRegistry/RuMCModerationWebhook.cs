using Content.Server.Discord;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

internal static class RuMCModerationWebhook
{
    public static bool TryParse(string url, out WebhookIdentifier webhook)
    {
        webhook = default;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host is not ("discord.com" or "discordapp.com")
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
