using Content.Shared.Database;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

// These numeric values are persisted in the moderation outbox.
internal enum RuMCModerationKind
{
    ServerBan = 0,
    RoleBan = 1,
    ChatBan = 2,
    Warning = 3,
}

internal sealed record RuMCModerationRecord(
    RuMCModerationKind Kind,
    Guid[] Players,
    Guid? Admin,
    string Reason,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    int[] Rounds,
    NoteSeverity? Severity = null,
    string? Scope = null,
    ChatType ChatChannels = ChatType.None);
