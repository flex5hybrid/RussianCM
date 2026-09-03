using Robust.Shared.Maths;

namespace Content.Server.Fluids.Components;

/// <summary>
/// Marks a puddle as a volumetric liquid cell owned by a native 3D grid.
/// The existing solution component remains the chemical source of truth.
/// </summary>
[RegisterComponent]
public sealed partial class FluidCell3DComponent : Component
{
    [DataField(required: true)]
    public EntityUid Root;

    [DataField(required: true)]
    public Vector3i Cell;
}
