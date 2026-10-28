using System.Numerics;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

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
    [DataField] public float Roughness = 0.8f;
    [DataField] public float Stretch;
    [DataField] public int Priority;
    [DataField] public HashSet<ProtoId<ContentTileDefinition>> Tiles = new();
}
