using System.Linq;
using System.Text;
using Content.Server.Discord;
using Content.Shared.Database;
using Robust.Shared.Localization;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

internal static class RuMCModerationEmbed
{
    public static WebhookPayload Build(
        RuMCModerationRecord record, Guid id, IReadOnlyDictionary<Guid, string> names, string server, ILocalizationManager loc)
    {
        string Text(string key) => loc.GetString($"rumc-moderation-registry-{key}");

        var (title, color) = record.Kind switch
        {
            RuMCModerationKind.ServerBan => (Text("server-ban"), 0xED4245),
            RuMCModerationKind.RoleBan => (Text("role-ban"), 0x9B59B6),
            RuMCModerationKind.ChatBan => (Text("chat-ban"), 0x3498DB),
            _ => (Text("warning"), 0xFEE75C),
        };

        string Account(Guid account) => names.TryGetValue(account, out var name)
            ? $"{PlainText(name, 160)} ({account})"
            : account.ToString();

        var players = record.Players.Length == 0
            ? Text("no-account")
            : string.Join("\n", record.Players.Select(Account));

        var reason = PlainText(record.Reason, 3500);
        var embed = new WebhookEmbed
        {
            Title = title,
            Color = color,
            Description = $"**{Text("reason")}**\n{(reason.Length > 0 ? reason : Text("unspecified"))}",
            Fields =
            [
                new() { Name = record.Players.Length == 1 ? Text("player") : Text("players"), Value = Truncate(players, 1024), Inline = false },
                new() { Name = Text("admin"), Value = record.Admin is { } admin ? Account(admin) : Text("system") },
                new() { Name = Text("issued"), Value = Timestamp(record.IssuedAt) },
                new()
                {
                    Name = Text("expires"),
                    Value = record.ExpiresAt is { } expiry ? Timestamp(expiry) : Text("permanent"),
                },
            ],
            Footer = new WebhookEmbedFooter { Text = $"{Text("name")} · {PlainText(server, 150)} · {id:N}" },
        };

        if (record.Kind == RuMCModerationKind.ChatBan)
        {
            var channels = new List<string>(3);
            if (record.ChatChannels.HasFlag(ChatType.Dead))
                channels.Add(loc.GetString("rmc-chat-bans-dead"));
            if (record.ChatChannels.HasFlag(ChatType.Looc))
                channels.Add(loc.GetString("rmc-chat-bans-looc"));
            if (record.ChatChannels.HasFlag(ChatType.Ooc))
                channels.Add(loc.GetString("rmc-chat-bans-ooc"));

            if (channels.Count > 0)
                embed.Fields.Add(new() { Name = Text("channels"), Value = string.Join(", ", channels) });
        }
        else if (record.Scope is { Length: > 0 } scope)
        {
            embed.Fields.Add(new() { Name = Text("roles"), Value = PlainText(scope, 512) });
        }

        if (record.Severity is { } severity)
        {
            var label = severity switch
            {
                NoteSeverity.Minor => Text("severity-minor"),
                NoteSeverity.Medium => Text("severity-medium"),
                NoteSeverity.High => Text("severity-high"),
                _ => Text("unspecified"),
            };
            embed.Fields.Add(new() { Name = Text("severity"), Value = label });
        }

        if (record.Rounds.Length > 0)
            embed.Fields.Add(new() { Name = Text("rounds"), Value = Truncate(string.Join(", ", record.Rounds), 200) });

        return new WebhookPayload
        {
            Username = Text("name"),
            Embeds = [embed],
            AllowedMentions = new WebhookMentions(),
        };
    }

    private static string Timestamp(DateTime time) =>
        $"<t:{new DateTimeOffset(time, TimeSpan.Zero).ToUnixTimeSeconds()}:f>";

    private static string Truncate(string text, int limit)
    {
        if (text.Length <= limit)
            return text;

        var length = limit - 1;
        if (char.IsHighSurrogate(text[length - 1]) || text[length - 1] == '\\')
            length--;
        return text[..length] + "…";
    }

    private static string PlainText(string text, int limit)
    {
        var escaped = new StringBuilder();
        foreach (var character in text.Trim())
        {
            if (char.IsControl(character) && character != '\n')
                continue;

            if (character is '\\' or '*' or '_' or '~' or '`' or '|' or '<' or '>' or '[' or ']' or '#' or '-')
                escaped.Append('\\');

            escaped.Append(character);
            if (escaped.Length > limit)
                break;
        }

        // A cut right before a low surrogate may make the builder end with an incomplete Unicode character.
        if (escaped.Length > 0 && char.IsHighSurrogate(escaped[^1]))
            escaped.Length--;

        return Truncate(escaped.ToString(), limit);
    }
}
