using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Lighting;

/// <summary>
/// Explicit, networked opt-in for experimental normal-mapped sprite illumination.
/// Does not affect collision, light components, or gameplay visibility.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUCinematicNormalComponent : Component
{
    [DataField, AutoNetworkedField] public bool Enabled = true;
    [DataField, AutoNetworkedField] public float Strength = 0.75f;
}
