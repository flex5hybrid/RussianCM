using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.PhaseSpace;


/// <summary>
/// Creates shadow copies behind the parent
/// </summary>
/// <remarks>
/// Shadow Cloning Technique
/// </remarks>
[RegisterComponent, NetworkedComponent, Serializable, AutoGenerateComponentState]
public sealed partial class PhaseSpaceShadowComponent : Component
{
    /// <summary>
    /// Reducing this setting may result in performance loss and "jerky" shadows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PositionUpdateRate = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// How often do we need to spawn shadows
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ShadowUpdateRate = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Lifetime of a shadow
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ShadowLifeTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Start time for the disappearance to begin
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartDisappearanceTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// A min opacity of shadow
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinOpacity = 0f;

    /// <summary>
    /// Animates the removal of the component. If false, the shadows will be removed immediately along with the component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AnimateComponentShutdown = true;

    /// <summary>
    /// If true dosent create a shadow copy
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedSpriteVisible = true;

    /// <summary>
    /// If true, the shadow will rotate based on the current rotation of the main sprite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckCurrentSpriteRotation = false;

    /// <summary>
    /// Current spawned shadows
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<PhaseSpaceShadow> Shadows = new();

    /// <summary>
    /// Try to guess
    /// </summary>
    [ViewVariables]
    public EntityCoordinates CachedPosition = new();

    /// <summary>
    /// Next position cache update
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPositionUpdate = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Next shadow cache update
    /// </summary>
    [ViewVariables]
    public TimeSpan NextShadowUpdate = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Try to guess
    /// </summary>
    [ViewVariables]
    public bool IsMove = false;
}
