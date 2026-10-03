// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.CMU14.Logistics;

[RegisterComponent]
// A box that unpacks machines or vehicles when used in hand.
public sealed partial class AU14DeployBoxComponent : Component
{
    [DataField(required: true)]
    public List<DeployBoxEntry> Entries = new();

    [DataField]
    public float Distance = 2f;

    [DataField]
    public bool Anchor;

    [DataField]
    public SoundSpecifier? Sound;
}

[DataDefinition]
// One thing to spawn when the box is opened.
public sealed partial class DeployBoxEntry
{
    [DataField(required: true)]
    public EntProtoId Prototype = default!;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public Dictionary<string, EntProtoId> Install = new();
}
