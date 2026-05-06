namespace Content.Shared._RuMC14.CrewTracker;

[RegisterComponent]
public sealed partial class RMCCrewPinpointerComponent : Component
{
	public HashSet<string> FavoriteNames = new();
}
