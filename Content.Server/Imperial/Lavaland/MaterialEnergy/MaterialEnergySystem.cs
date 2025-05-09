// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 Koting <jekalavrovih@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// main from https://github.com/Goob-Station/Goob-Station/blob/15377c36e0231c21f34c0df57f1e92455894e696/Content.Goobstation.Server/MaterialEnergy/MaterialEnergySystem.cs
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.Lavaland.MaterialEnergy
{
    public sealed class MaterialEnergySystem : EntitySystem
    {
        [Dependency] private readonly BatterySystem _batterySystem = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MaterialEnergyComponent, InteractUsingEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, MaterialEnergyComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.MaterialWhiteList == null)
                return;

            if (!TryComp<PhysicalCompositionComponent>(args.Used, out var composition))
                return;

            // Get the amount of material (for stacks and single items)
            var count = 1;
            if (TryComp<StackComponent>(args.Used, out var stack))
                count = stack.Count;

            foreach (var fuelType in component.MaterialWhiteList)
            {
                if (composition.MaterialComposition.TryGetValue(fuelType, out var materialPerItem))
                {
                    if (TryAddBatteryCharge(uid, args.Used, materialPerItem, count))
                    {
                        _audio.PlayPvs(component.ReplenishmentOfFirearm, uid);
                        args.Handled = true;
                        break;
                    }
                }
            }
        }

        private bool TryAddBatteryCharge(EntityUid batteryUid, EntityUid materialUid, int materialPerItem, int itemCount)
        {
            if (!TryComp<BatteryComponent>(batteryUid, out var battery))
                return false;

            var chargeNeeded = battery.MaxCharge - battery.CurrentCharge;
            if (chargeNeeded <= 0)
                return false;

            var totalMaterial = materialPerItem * itemCount;
            var materialToConsume = (int)Math.Min(totalMaterial, chargeNeeded);

            if (materialToConsume <= 0)
                return false;

            // Adding energy
            _batterySystem.SetCharge(batteryUid, battery.CurrentCharge + materialToConsume, battery);

            // Remove the used material
            var itemsToConsume = (int)Math.Ceiling((float)materialToConsume / materialPerItem);
            if (itemsToConsume <= 0)
                return false;

            if (TryComp<StackComponent>(materialUid, out var stack))
            {
                var toDelete = _stack.Split(materialUid, itemsToConsume, Transform(materialUid).Coordinates);
                QueueDel(toDelete);
            }
            else
            {
                QueueDel(materialUid);
            }

            return true;
        }
    }
}
