
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// The sprite that <see cref="ShowSpawnedEntity"/> should draw
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PrespawnedEntitySprite
{
    /// <summary>
    /// Rendered RSI
    /// </summary>
    [DataField]
    public SpriteSpecifier.Rsi DrawedSprite = default!;

    /// <summary>
    /// The rotation angle at which the sprite will be drawn is required.
    /// </summary>
    [DataField]
    public Angle RequiredAngle = Angle.FromDegrees(0);

    /// <summary>
    /// The distance at which <see cref="InRangeColor"> will be applied to the sprite, and when <see cref="OutOfRangeColor"> will be applied to the sprite.
    /// </summary>
    [DataField]
    public float Range = float.PositiveInfinity;

    /// <summary>
    /// Number affecting the overlap of sprites with color from the <see cref="InRangeColor"/> or <see cref="OutOfRangeColor"/>
    /// </summary>
    [DataField]
    public float MixColor = 0.7f;

    /// <summary>
    /// Sprite transparency
    /// </summary>
    [DataField]
    public float Opacity = 0.7f;

    /// <summary>
    /// If true, <see cref="RequiredAngle"> is ignored and the sprite is drawn at any rotation angle
    /// </summary>
    [DataField]
    public bool AlwaysRender = false;

    /// <summary>
    /// Color when in <see cref="Range">
    /// </summary>
    [DataField]
    public Color? InRangeColor;

    /// <summary>
    /// Color when out of <see cref="Range">
    /// </summary>
    [DataField]
    public Color? OutOfRangeColor;

    /// <summary>
    /// Sprite color, same as color in sprite component. Helps to recolor same type sprites
    /// </summary>
    [DataField]
    public Color? SpriteColor = default!;
}
