using Robust.Shared.Serialization;

using Content.Shared.CMU14.Sponsors; // CMU14

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCPatronFull(
    SharedRMCPatronTier? Tier,
    bool Linked,
    Color? GhostColor,
    SharedRMCLobbyMessage? LobbyMessage,
    SharedRMCRoundEndShoutouts? RoundEndShoutout,
    CMUSponsorSettings? SponsorSettings = null, // CMU14
    string ApprovedFigurineDescription = "", // CMU14
    string CustomItem = "", // CMU14
    string? FigurinePrototype = null // CMU14
);
