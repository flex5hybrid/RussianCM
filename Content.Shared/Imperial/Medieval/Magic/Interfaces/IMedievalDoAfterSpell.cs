namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Interface for do after spell events
/// </summary>
public interface IMedievalDoAfterSpell
{
    /// <summary>
    /// Do After args for spells
    /// </summary>
    [DataField]
    public MedievalSpellDoAfterArgs? SpellCastDoAfter { get; }
}
