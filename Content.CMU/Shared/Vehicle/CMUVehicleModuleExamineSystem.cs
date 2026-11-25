using Content.Shared._RMC14.Vehicle;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;

namespace Content.Shared.CMU14.Vehicle;

// CMU14 class
public sealed class CMUVehicleModuleExamineSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<HardpointSlotsComponent> ent, ref ExaminedEvent args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (_itemSlots.GetItemOrNull(ent.Owner, slot.Id) is not { } item ||
                !TryComp(item, out CMUVehicleModuleExamineComponent? module))
            {
                continue;
            }

            args.PushMarkup(Loc.GetString(module.Examine));
        }
    }
}
