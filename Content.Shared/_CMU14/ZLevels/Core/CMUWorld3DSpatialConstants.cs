namespace Content.Shared._CMU14.ZLevels.Core;

/// <summary>
/// Physical dimensions used when projecting the legacy CMU z-level network into the new 3D spatial layer.
/// </summary>
public static class CMUWorld3DSpatialConstants
{
    /// <summary>
    /// Vertical distance between two adjacent CMU map depths in world-space metres/tiles.
    /// The current 3D presentation uses 2.6-high walls, leaving a small structural gap between decks.
    /// </summary>
    public const float ZLevelSpacing = 3.2f;
}
