namespace Content.Server.CMU14.Yautja;

[RegisterComponent, Access(typeof(YautjaYoungbloodSystem), typeof(YautjaHuntConsoleSystem))]
public sealed partial class YautjaYoungbloodGhostRoleComponent : Component
{
    [DataField]
    public bool BypassEligibility;

    public bool SetupComplete;
}
