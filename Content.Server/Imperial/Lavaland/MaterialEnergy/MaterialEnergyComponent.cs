// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 yglop <95057024+yglop@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 Koting <jekalavrovih@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// from https://github.com/Goob-Station/Goob-Station/blob/15377c36e0231c21f34c0df57f1e92455894e696/Content.Goobstation.Server/MaterialEnergy/MaterialEnergyComponent.cs

using Robust.Shared.Audio;

namespace Content.Server.Imperial.Lavaland.MaterialEnergy;

[RegisterComponent]
public sealed partial class MaterialEnergyComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string>? MaterialWhiteList;

    [DataField]
    public SoundSpecifier ReplenishmentOfFirearm { get; set; } = new SoundPathSpecifier("/Audio/Imperial/Lavaland/PlasmaCutter/replenishment-of-the-firearm.ogg");
}
