using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Player;
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
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(
            OnPlayerJobsAssigned,
            after: [typeof(AntagSelectionSystem)]);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // Round-start capes are handed out in OnPlayerJobsAssigned: antagonists keep getting assigned
        // until then, and some gamemodes (Distress Signal) move the player into another body afterwards.
        if (!ev.LateJoin)
            return;

        TryGiveCape(ev.Player, ev.Mob, ev.Profile);
    }

    private void OnPlayerJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        foreach (var player in ev.Players)
        {
            if (player.AttachedEntity is not { } mob ||
                !ev.Profiles.TryGetValue(player.UserId, out var profile))
            {
                continue;
            }

            TryGiveCape(player, mob, profile);
        }
    }

    private void TryGiveCape(ICommonSession player, EntityUid mob, HumanoidCharacterProfile profile)
    {
        // Never hand the cape to a body the player does not control (e.g. the corpse a larva bursts from).
        if (player.AttachedEntity != mob || HasComp<XenoComponent>(mob))
            return;

        if (_mind.TryGetMind(mob, out var mindId, out _) && _roles.MindIsAntagonist(mindId))
            return;

        if (profile.SelectedDonorCape is not { } selectedCape ||
            !_prototypes.TryIndex(selectedCape, out RMCDonorCapePrototype? cape) ||
            !DonorCapeAccess.HasAccess(_linkAccount.GetConnectedPatron(player)?.Tier, cape.RequiredPriority))
        {
            return;
        }

        var item = Spawn(cape.Item, Transform(mob).Coordinates);
        if (_hands.TryPickupAnyHand(mob, item, checkActionBlocker: false))
            return;

        if (TryInsertIntoStorage(mob, item))
            return;

        if (_inventory.TryEquip(mob, item, "neck", silent: true, force: true))
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
