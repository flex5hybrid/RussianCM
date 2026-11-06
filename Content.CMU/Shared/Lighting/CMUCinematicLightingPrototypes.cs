using System.Numerics;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Graphics.RSI;

namespace Content.Shared.CMU14.Lighting;

/// <summary>A reusable emissive signature, independent of the underlying gameplay light and its animation.</summary>
[Prototype("cmuCinematicLightProfile")]
public sealed partial class CMUCinematicLightProfilePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public Color Color = Color.White;
    /// <summary>Neutral fixture color balance; saturated signal colors remain authoritative.</summary>
    [DataField] public float Tint;
    [DataField] public float TextureColor = 0.35f;
    /// <summary>Exclude neutral metal/plastic pixels from screen emissive masks.</summary>
    [DataField] public float EmissionChroma;
    [DataField] public Vector2 EmissionSize = new(0.4f, 0.15f);
    [DataField] public float Bloom = 0.65f;
    [DataField] public float InnerGlow = 1f;
    [DataField] public float OuterGlow = 0.35f;
    [DataField] public CMUCinematicSourceShape Shape = CMUCinematicSourceShape.Point;
    [DataField] public bool WallMounted;
    /// <summary>Optional mount-relative signatures; native saturated signal colors still win.</summary>
    [DataField] public Dictionary<Direction, ProtoId<CMUCinematicLightProfilePrototype>> MountProfiles = new();
    [DataField] public float Falloff = 2.8f;
    [DataField] public float Indirect = 0.12f;
    [DataField] public ProtoId<CMUCinematicEmissionMaskPrototype>? EmissionMask;
    /// <summary>Linear HDR radiance of bright, opaque pixels in the actual sprite frame.</summary>
    [DataField] public float Core = 4f;
    /// <summary>Local sprite UV bounds, left/bottom/right/top; screens exclude their keyboard.</summary>
    [DataField] public Vector4 EmissionRegion = new(0f, 0f, 1f, 1f);
    [DataField] public float Threshold = 0.38f;
    [DataField] public float Spill = 0.18f;
    [DataField] public float Reflection = 1f;
    [DataField] public float Radius = 3f;
    [DataField] public float Atmosphere;
    [DataField] public float BeamWidth = 1f;
    [DataField] public float BeamFalloff = 1.6f;
    [DataField] public bool BeamCone;
    [DataField] public bool RequireLight = true;
    [DataField] public bool RequirePower = true;
    [DataField] public bool ProceduralEmission;
    [DataField] public bool FollowParentLight;
    [DataField] public float PulseDuration;
    [DataField] public int Priority;
}

/// <summary>
/// Stylized surface response. Tile IDs match the tile itself or its inherited parents;
/// entity overrides use CMUCinematicMaterial. Unlisted tiles have no additional response.
/// </summary>
[Prototype("cmuCinematicMaterial")]
public sealed partial class CMUCinematicMaterialPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public float Reflectivity;
    [DataField] public float SpecularStrength = 1f;
    [DataField] public float Roughness = 0.8f;
    [DataField] public float Stretch;
    [DataField] public int Priority;
    [DataField] public HashSet<ProtoId<ContentTileDefinition>> Tiles = new();
}

public enum CMUCinematicSourceShape : byte
{
    Point,
    Linear,
    Rectangular,
    Cone,
    EmissiveSurface,
}

/// <summary>Authored luminous regions in sprite-local bottom-left UVs, intersected with the live frame.</summary>
[Prototype("cmuCinematicEmissionMask")]
public sealed partial class CMUCinematicEmissionMaskPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public Vector2i FrameSize;
    [DataField] public Vector4 Region = new(0f, 0f, 1f, 1f);
    [DataField] public string Rsi = string.Empty;
    [DataField] public Dictionary<RsiDirection, Vector4> Directions = new();
}
