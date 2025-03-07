using Robust.Shared.Map;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// A specified event rised from spells with target overlay.
/// </summary>
[ByRefEvent]
public sealed class MedievalBeforeAimingSpawnBySpellEvent : MedievalBeforeSpawnEntityBySpellEvent
{
    public (MapCoordinates CursorPosition, EntityUid? TargetEntity) Target;
}
