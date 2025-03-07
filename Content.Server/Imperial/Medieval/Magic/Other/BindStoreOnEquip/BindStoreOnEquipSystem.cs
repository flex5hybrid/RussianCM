using System.Linq;
using Content.Server.Imperial.ImperialStore;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Robust.Server.Containers;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Medieval.Magic.BindStoreOnEquip;


/// <summary>
/// This system binds a store to an entity when it is picked up or when it is initialized in a container.
/// </summary>
public sealed partial class BindStoreOnEquipSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly ImperialStoreSystem _storeSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn); // We can't use the ComponentStart/ComponentInit/MapInit event, because the entity itself hasn't loaded yet.
        SubscribeLocalEvent<BindStoreOnEquipComponent, GotEquippedHandEvent>(OnGotEquipped);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        var enumerator = EntityQueryEnumerator<BindStoreOnEquipComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (component.BindedEntity != null) continue;

            TryBindMind(uid, component);
        }
    }

    private void OnGotEquipped(EntityUid uid, BindStoreOnEquipComponent component, GotEquippedHandEvent args)
    {
        if (component.BindedEntity != null) return;

        TryBindMind(uid, component);
    }

    #region Public API

    /// <summary>
    /// inds the store to the entity by updating the <see cref="BindStoreOnEquipComponent" />
    /// </summary>
    /// <param name="uid">Store uid</param>
    /// <param name="component"></param>
    public void TryBindMind(EntityUid uid, BindStoreOnEquipComponent? component = null)
    {
        if (!Resolve(uid, ref component)) return;

        var transformComponent = Transform(uid);

        if (!_containerSystem.TryGetOuterContainer(uid, transformComponent, out var container)) return;
        if (!HasComp<ActorComponent>(container.Owner)) return;

        var ownerHasStore = EntityQuery<BindStoreOnEquipComponent>().Aggregate(false, (acc, comp) => acc || comp.BindedEntity == container.Owner);

        if (ownerHasStore) return;

        component.BindedEntity = container.Owner;
        _storeSystem.BindMind(uid, container.Owner);
    }

    #endregion
}
