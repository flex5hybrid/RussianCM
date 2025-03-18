using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.Medieval.Magic.Overlays;


/// <summary>
/// Draws sprites when switching actions with the ability to rotate them with the middle mouse button
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowSpawnedEntityComponent : Component
{
    /// <summary>
    /// Sprites that we need to draw
    /// </summary>
    [DataField]
    public List<PrespawnedEntitySprite> Sprites = default!;

    /// <summary>
    /// The current rotation angle of the sprite
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Rotation = Angle.FromDegrees(0);

    /// <summary>
    /// How much do we need to rotate the sprite when clicked
    /// </summary>
    [DataField]
    public Angle RotationPerClick = Angle.FromDegrees(90);


    [ViewVariables]
    public bool IsActive = false;
}
