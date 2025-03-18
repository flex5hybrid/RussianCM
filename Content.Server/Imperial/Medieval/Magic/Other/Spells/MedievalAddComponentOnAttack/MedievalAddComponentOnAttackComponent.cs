using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.Magic.MedievalAddComponentOnInteract;


/// <summary>
/// Add a component on interact with this item
/// </summary>
[RegisterComponent]
public sealed partial class MedievalAddComponentOnAttackComponent : Component
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
}
