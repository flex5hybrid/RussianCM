using System.Numerics;

namespace Content.Shared.NodeContainer;

/// <summary>
/// Explicit volumetric connection points for node-container entries.
/// The dictionary key is the node name in <see cref="NodeContainerComponent.Nodes"/>.
/// </summary>
[RegisterComponent]
public sealed partial class NodePort3DComponent : Component
{
    [DataField]
    public Dictionary<string, NodePort3D> Ports = new();
}

[DataDefinition]
public sealed partial class NodePort3D
{
    /// <summary>
    /// Port centre in entity-local metres.
    /// </summary>
    [DataField]
    public Vector3 Offset;

    /// <summary>
    /// Outward local direction. Zero accepts a connection from any direction.
    /// </summary>
    [DataField]
    public Vector3 Direction;

    /// <summary>
    /// Maximum gap from this port to its peer.
    /// </summary>
    [DataField]
    public float Reach = 0.08f;
}
