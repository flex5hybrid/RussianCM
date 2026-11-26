namespace Content.Server.CMU14.Light;

/// <summary>Marks isolated maps owned by the lighting review command; never added to a game round.</summary>
[RegisterComponent]
public sealed partial class CMUCinematicReviewSceneComponent : Component
{
    public EntityUid? SelectedLamp;
    public readonly List<EntityUid> Sources = new();
    public readonly Dictionary<EntityUid, Angle> Rotations = new();
    public string State = "all";
}
