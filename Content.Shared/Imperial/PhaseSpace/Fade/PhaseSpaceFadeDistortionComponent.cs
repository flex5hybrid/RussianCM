using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Imperial.PhaseSpace;


/// <summary>
/// Implementation of <see cref="PhaseSpaceDistortionComponent"/>.
/// Needed to beautifully animate the fading of shadows.
/// Because when deleting the component <see cref="PhaseSpaceDistortionComponent"/>, the shadows are also immediately deleted
/// </summary>
[RegisterComponent, NetworkedComponent, Serializable, AutoGenerateComponentState]
public sealed partial class PhaseSpaceFadeDistortionComponent : Component
{
    /// <summary>
    /// The number of shadows that will spawn
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<PhaseSpaceDistortion> Distortions = new();

    /// <summary>
    /// Reducing this setting may result in performance loss and "jerky" shadows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PositionUpdateRate = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// Start time for the disappearance to begin
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartDisappearanceTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// If true dosent create a shadow copy
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedSpriteVisible = true;

    /// <summary>
    /// Cached position
    /// </summary>
    [ViewVariables]
    public EntityCoordinates CachedPosition = new();

    /// <summary>
    /// Next position cache update
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPositionUpdate = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Try to guess
    /// </summary>
    [DataField]
    public bool IsMove = true;
}
