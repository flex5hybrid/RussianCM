using Robust.Shared.Maths;
using Content.Server.Disposal.Unit;

namespace Content.Server.Disposal.Tube;

/// <summary>
/// Volumetric disposal-tube ports expressed in entity-local cardinal XYZ directions.
/// </summary>
[RegisterComponent, Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
public sealed partial class DisposalPort3DComponent : Component
{
    [DataField]
    public List<Vector3i> Connections = new();

    [DataField]
    public Vector3i DefaultDirection;

    /// <summary>
    /// Optional tag to local output direction mapping for routers.
    /// </summary>
    [DataField]
    public Dictionary<string, Vector3i> TaggedRoutes = new();
}
