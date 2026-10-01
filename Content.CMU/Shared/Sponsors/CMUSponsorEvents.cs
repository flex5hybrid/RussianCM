using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Sponsors;

[Serializable, NetSerializable]
public sealed class CMUSponsorSaveSettingsEvent(CMUSponsorSettings settings) : EntityEventArgs
{
    public readonly CMUSponsorSettings Settings = settings;
}

[Serializable, NetSerializable]
public sealed class CMUSponsorSaveResultEvent(bool success) : EntityEventArgs
{
    public readonly bool Success = success;
}
