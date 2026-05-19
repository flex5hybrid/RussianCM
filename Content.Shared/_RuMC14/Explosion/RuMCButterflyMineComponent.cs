using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RuMCButterflyMineComponent : Component
{
    /// <summary>Whether the mine is armed and ready to trigger.</summary>
    [DataField, AutoNetworkedField]
    public bool Armed;

    /// <summary>How long it takes to place the mine.</summary>
    [DataField, AutoNetworkedField]
    public float PlacementDelay = 2f;

    /// <summary>How long it takes to disarm the mine.</summary>
    [DataField, AutoNetworkedField]
    public float DisarmDelay = 3f;

    /// <summary>Tool quality required to disarm the mine.</summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> DisarmTool = "Pulsing";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeploySound;

    /// <summary>The entity that placed this mine. Gets a short immunity window.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Installer;

    /// <summary>Until this time the installer is immune to their own mine.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InstallerImmunityUntil = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan InstallerImmunityDuration = TimeSpan.FromSeconds(5);
}
