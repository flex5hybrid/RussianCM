using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Automatically added to an entity that has used magic at least once.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedievalSpellCasterComponent : Component
{
    /// <summary>
    /// Speed ​​modifiers that spells add
    /// <para>
    /// The smallest one is selected
    /// </para>
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<float> SpeedModifiers = new();

    /// <summary>
    /// Stack of words for the caster to say. Not related to DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(TimeSpan, MedievalSpellSpeech)> SpellWordsStack = new();

    /// <summary>
    /// Stack of targets from <see href="TargetOverlay">.
    /// </summary>
    /// <param name="EntityUid">Spell action</param>
    /// <param name="List<(MapCoordinates CursorPosition, NetEntity? Target)>">Targets</param>
    [DataField]
    public Dictionary<EntityUid, List<(MapCoordinates CursorPosition, NetEntity? Target)>> TargetStack = new();

    /// <summary>
    /// Stack of spell data. Required to cast ecntity aiming spells
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, MedievalEntityAimingSpellData> SpellStack = new();
}
