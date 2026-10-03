// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
// Tints an item (and how it looks when worn) with a custom color.
public sealed partial class AU14CustomClothingColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
