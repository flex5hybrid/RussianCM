using Content.Server.Antag;
using Content.Server.Hands.Systems;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.DonorCapes;

public sealed class DonorCapeSystem : EntitySystem
{
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LinkAccountManager _linkAccount = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private StorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(
            OnPlayerSpawnComplete,
            after: [typeof(AntagSelectionSystem)]);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (HasComp<XenoComponent>(ev.Mob) ||
            _mind.TryGetMind(ev.Mob, out var mindId, out _) && _roles.MindIsAntagonist(mindId))
        {
            return;
        }

        if (ev.Profile.SelectedDonorCape is not { } selectedCape ||
            !_prototypes.TryIndex(selectedCape, out RMCDonorCapePrototype? cape) ||
            !DonorCapeAccess.HasAccess(_linkAccount.GetConnectedPatron(ev.Player)?.Tier, cape.RequiredPriority))
        {
            return;
        }

        var item = Spawn(cape.Item, Transform(ev.Mob).Coordinates);
        if (_hands.TryPickupAnyHand(ev.Mob, item, checkActionBlocker: false))
            return;

        if (TryInsertIntoStorage(ev.Mob, item))
            return;

        if (_inventory.TryEquip(ev.Mob, item, "neck", silent: true, force: true))
            return;

        QueueDel(item);
    }

    private bool TryInsertIntoStorage(EntityUid mob, EntityUid item)
    {
        var slots = _inventory.GetSlotEnumerator(
            mob,
            SlotFlags.BACK | SlotFlags.BELT | SlotFlags.POCKET | SlotFlags.SUITSTORAGE);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } storageOwner ||
                !TryComp(storageOwner, out StorageComponent? storage))
            {
                continue;
            }

            if (_storage.Insert(storageOwner, item, out _, storageComp: storage))
                return true;
        }

        return false;
    }
}
