using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Maps.Components;

/// <summary>
/// Marks a legacy map grid whose tile payload has been imported into the incompatible native 3D world format.
/// </summary>
[RegisterComponent]
public sealed partial class Native3DMigratedGridComponent : Component
{
    public const int CurrentVersion = 1;

    [DataField]
    public int Version = CurrentVersion;
}
