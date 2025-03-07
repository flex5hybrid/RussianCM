using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.Magic.MedievalAddComponentOnInteract;


/// <summary>
/// Add a component on interact with this item
/// </summary>
[RegisterComponent]
public sealed partial class MedievalAddComponentOnInteractComponent : Component
{
    /// <summary>
    /// Added Components after interact
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components add to self-use
    /// </summary>
    [DataField]
    public ComponentRegistry? SelfUseComponents;

    /// <summary>
    /// If an entity has any of the following components, it will not be able to turn into stone.
    /// </summary>
    [DataField]
    public HashSet<string> BlackListComponents = new();

    /// <summary>
    /// An entity must have all of the following components to turn into stone.
    /// If null then the entity always turns into stone.
    /// </summary>
    [DataField]
    public HashSet<string>? WhiteListComponents;

    /// <summary>
    /// If true remove component on entity and replace it from <see cref="Components"/>
    /// </summary>
    [DataField]
    public bool Hard = false;

    /// <summary>
    /// Sound on interact
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnInteract;

    /// <summary>
    /// A max use count
    /// </summary>
    [DataField]
    public int RemainingUses = 1;


    [ViewVariables]
    public int UseCount = 0;
}
