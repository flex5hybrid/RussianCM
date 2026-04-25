using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipHijackerComponent : Component
{
    /// <summary>
    ///     If true, this is a human-faction hijacker (CLF/OPFOR/GOVFOR) that must spend intel points.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsHumanHijacker;

    /// <summary>
    ///     The intel point cost required for human hijackers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double IntelCost = 40;

    /// <summary>
    ///     The time in seconds for the hijack do-after.
    ///     Xeno queens use 3s (handled by existing lockout), human hijackers use 60s.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HijackDoAfterSeconds = 60f;
}
