namespace Content.Client.CMU14.Lighting.Configuration;

/// <summary>Independent, bounded budgets; a preset change recreates viewport caches once.</summary>
public readonly record struct CinematicLightingQuality(
    int Level,
    int Sources,
    int AtmosphericSources,
    int BloomLevels,
    int ResolutionDivisor,
    int CircleSegments,
    int LocalEdges)
{
    public bool SurfaceEffects => Level >= 2;

    public static CinematicLightingQuality For(int value) => Math.Clamp(value, 0, 3) switch
    {
        1 => new(1, 12, 0, 2, 4, 24, 96),
        2 => new(2, 40, 6, 4, 2, 40, 192),
        3 => new(3, 64, 12, 5, 2, 64, 320),
        _ => default,
    };
}
