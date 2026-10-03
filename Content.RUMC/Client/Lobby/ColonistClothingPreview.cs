using Content.Shared.CMU14.Clothing;
using Content.Shared.CMU14.Roles;
using Content.Shared.Inventory;
using Content.Shared.Lobby;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

// Puts the chosen custom clothing on the lobby preview character.
public static class ColonistClothingPreview
{
    public static void Apply(
        IEntityManager entMan,
        IPrototypeManager protoMan,
        EntityUid dummy,
        HumanoidCharacterProfile profile,
        string roleLoadoutKey)
    {
        if (!JobSpecialLoadouts.ByRoleLoadout.TryGetValue(roleLoadoutKey, out var specialId) ||
            !profile.Loadouts.TryGetValue(specialId, out var special))
        {
            return;
        }

        var inventory = entMan.System<InventorySystem>();

        foreach (var group in special.SelectedLoadouts.Values)
        {
            foreach (var selected in group)
            {
                if (!protoMan.TryIndex(selected.Prototype, out var loadoutProto) ||
                    !CustomClothingRules.TryGetEffect(loadoutProto, out var clothing) ||
                    selected.CustomEntity == null ||
                    !protoMan.HasIndex<EntityPrototype>(selected.CustomEntity))
                {
                    continue;
                }

                var item = entMan.SpawnEntity(selected.CustomEntity, MapCoordinates.Nullspace);
                entMan.EnsureComponent<LobbyPreviewEntityComponent>(item);

                if (selected.CustomColor is { } color)
                    entMan.EnsureComponent<AU14CustomClothingColorComponent>(item).Color = color;

                if (inventory.TryUnequip(dummy, clothing.Slot, out var replaced, silent: true, force: true, reparent: false))
                    entMan.DeleteEntity(replaced.Value);

                if (!inventory.TryEquip(dummy, item, clothing.Slot, silent: true, force: true))
                    entMan.DeleteEntity(item);
            }
        }
    }
}
