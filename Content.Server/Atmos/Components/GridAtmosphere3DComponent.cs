using Content.Shared.Atmos;
using Robust.Shared.Maths;

using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Server-authoritative atmosphere stored in volumetric map cells.
/// A cell is present only when it belongs to a simulated volume; missing cells resolve to the map atmosphere.
/// </summary>
[RegisterComponent, Access(typeof(AtmosphereSystem))]
public sealed partial class GridAtmosphere3DComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Simulated = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UpdateInterval = 0.25f;

    /// <summary>
    /// Maximum fraction of the higher-pressure cell transferred across one open face per update.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Conductance = 0.18f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CellVolume = Atmospherics.CellVolume;

    [DataField]
    public List<AtmosphereRegion3D> Regions = new();

    [DataField, ViewVariables]
    public Dictionary<Vector3i, GasMixture> Cells = new();

    [ViewVariables]
    public float Accumulator;
}

/// <summary>
/// Inclusive rectangular seed volume for a native 3D atmosphere grid.
/// </summary>
[DataDefinition]
public sealed partial class AtmosphereRegion3D
{
    [DataField(required: true)]
    public Vector3i Min;

    [DataField(required: true)]
    public Vector3i Max;

    /// <summary>
    /// Optional exact mixture. When omitted, the region is initialized with standard breathable air.
    /// </summary>
    [DataField]
    public GasMixture? Mixture;

    /// <summary>
    /// Treat faces at the region bounds as hull faces. Used by procedural rooms whose walls are collider entities
    /// instead of voxels; authored station volumes should normally use airtight structural voxels instead.
    /// </summary>
    [DataField]
    public bool SealedBoundary;
}
