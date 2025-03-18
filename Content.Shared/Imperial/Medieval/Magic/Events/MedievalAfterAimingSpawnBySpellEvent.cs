using Robust.Shared.Map;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Raised on action and spawned entity after last a initialize
/// </summary>
public sealed class MedievalAfterAimingSpawnBySpellEvent : MedievalAfterSpawnEntityBySpellEvent
{
    public (MapCoordinates CursorPosition, EntityUid? TargetEntity) Target;
}
