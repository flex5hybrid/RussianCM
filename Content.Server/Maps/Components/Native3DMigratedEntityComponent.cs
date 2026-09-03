using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Maps.Components;

/// <summary>
/// Records that a legacy physics entity has crossed the one-way native 3D migration boundary.
/// </summary>
[RegisterComponent]
public sealed partial class Native3DMigratedEntityComponent : Component
{
    [DataField]
    public float Height;
}
