using Content.Shared.Database;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

internal enum RuMCModerationKind
{
    ServerBan,
    RoleBan,
    ChatBan,
    Warning,
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
