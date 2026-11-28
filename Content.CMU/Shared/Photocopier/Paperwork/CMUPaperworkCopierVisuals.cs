using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Photocopier.Paperwork;

[Serializable, NetSerializable]
public enum CMUPaperworkCopierVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum CMUPaperworkCopierVisualState : byte
{
    Normal,
    Scanning,
    Printing
}
