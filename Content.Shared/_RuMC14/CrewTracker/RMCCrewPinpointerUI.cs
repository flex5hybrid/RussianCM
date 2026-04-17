using Content.Shared.CrewManifest;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.CrewTracker;

[Serializable, NetSerializable]
public enum RMCCrewPinpointerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerEntry(NetEntity target, CrewManifestEntry entry, bool favorite)
{
    public readonly NetEntity Target = target;
    public readonly CrewManifestEntry Entry = entry;
    public readonly bool Favorite = favorite;
}

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerSection(string name, List<RMCCrewPinpointerEntry> entries)
{
    public readonly string Name = name;
    public readonly List<RMCCrewPinpointerEntry> Entries = entries;
}

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerBuiState(
    List<RMCCrewPinpointerSection> sections,
    string? targetName,
    bool isActive) : BoundUserInterfaceState
{
    public readonly List<RMCCrewPinpointerSection> Sections = sections;
    public readonly string? TargetName = targetName;
    public readonly bool IsActive = isActive;
}

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerSelectMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerRefreshMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerClearMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerToggleMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCrewPinpointerFavoriteMsg(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}
