using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CMU14.Lighting;

/// <summary>
/// Content-authored presentation descriptors. Only configuration changes are replicated.
/// The client reads the existing point light and power receiver every rendered frame.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUCinematicEmitterComponent : Component
{
    [DataField, AutoNetworkedField] public ProtoId<CMUCinematicLightProfilePrototype> Profile = "CMUFixture";
    [DataField, AutoNetworkedField] public Vector2 EmissionOffset;
    [DataField, AutoNetworkedField] public float Intensity = 1f;
    [DataField, AutoNetworkedField] public bool Enabled = true;

    // Client-local start time for an existing transient effect. Never replicated.
    public TimeSpan? PulseStarted;
}

/// <summary>Optional surface override for a structure or a floor decal.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUCinematicMaterialComponent : Component
{
    [DataField(required: true), AutoNetworkedField] public ProtoId<CMUCinematicMaterialPrototype> Material;
    [DataField, AutoNetworkedField] public Vector2 Size = Vector2.One;
    [DataField, AutoNetworkedField] public Vector2 Offset;
}

/// <summary>Optional room/grid atmosphere override, used only by the cosmetic beam pass.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUCinematicAtmosphereComponent : Component
{
    [DataField, AutoNetworkedField] public float Density = 0.3f;
}
