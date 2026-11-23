using System.Linq;
using System.Text;
using Content.Server.Discord;
using Content.Shared.Database;

namespace Content.Server.CMU14.Administration.ModerationRegistry;

internal static class CMUModerationEmbed
{
    public static WebhookPayload Build(
        CMUModerationRecord record, Guid id, IReadOnlyDictionary<Guid, string> names, string server)
    {
        var (title, color) = record.Kind switch
        {
            CMUModerationKind.ServerBan => ("Серверная блокировка", 0xED4245),
            CMUModerationKind.RoleBan => ("Ролевая блокировка", 0x9B59B6),
            CMUModerationKind.ChatBan => ("Блокировка чата", 0x3498DB),
            _ => ("Предупреждение", 0xFEE75C),
        };

        string Account(Guid account) => names.TryGetValue(account, out var name)
            ? $"{PlainText(name, 160)} ({account})"
            : account.ToString();

        var players = record.Players.Length == 0
            ? "Без привязки к аккаунту"
            : string.Join("\n", record.Players.Select(Account));

        var reason = PlainText(record.Reason, 3500);
        var embed = new WebhookEmbed
        {
            Title = title,
            Color = color,
            Description = reason.Length > 0 ? $"**Причина**\n{reason}" : "**Причина**\nНе указана",
            Fields =
            [
                new() { Name = record.Players.Length == 1 ? "Игрок" : "Игроки", Value = Truncate(players, 1024), Inline = false },
                new() { Name = "Выдал", Value = record.Admin is { } admin ? Account(admin) : "Система" },
                new() { Name = "Выдано", Value = Timestamp(record.IssuedAt) },
                new()
                {
                    Name = "Действует до",
                    Value = record.ExpiresAt is { } expiry ? Timestamp(expiry) : "Бессрочно",
                },
            ],
            Footer = new WebhookEmbedFooter { Text = $"Реестр модерации · {PlainText(server, 150)} · {id:N}" },
        };

        if (record.Scope is { Length: > 0 } scope)
            embed.Fields.Add(new() { Name = record.Kind == CMUModerationKind.ChatBan ? "Каналы" : "Роли", Value = PlainText(scope, 512) });

        if (record.Severity is { } severity)
        {
            var label = severity switch
            {
                NoteSeverity.Minor => "Низкая",
                NoteSeverity.Medium => "Средняя",
                NoteSeverity.High => "Высокая",
                _ => "Не указана",
            };
            embed.Fields.Add(new() { Name = "Серьёзность", Value = label });
        }

        if (record.Rounds.Length > 0)
            embed.Fields.Add(new() { Name = "Раунды", Value = Truncate(string.Join(", ", record.Rounds), 200) });

        return new WebhookPayload
        {
            Username = "Реестр модерации",
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
