namespace Content.Server.Imperial.Medieval.Magic.BindStoreOnEquip;


/// <summary>
/// This component binds a store to an entity when it is picked up or initialized in it.
/// </summary>
[RegisterComponent, Access(typeof(BindStoreOnEquipSystem))]
public sealed partial class BindStoreOnEquipComponent : Component
{
    [DataField]
    public bool CanEquipWhenAnotherStoreBinded = false;

    /// <summary>
    /// The entity the store is linked to
    /// </summary>
    [ViewVariables]
    public EntityUid? BindedEntity;
}
