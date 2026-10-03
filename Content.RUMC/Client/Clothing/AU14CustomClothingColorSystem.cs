// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.CMU14.Clothing;
using Content.Shared.Clothing;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.CMU14.Clothing;

// Applies the custom color to the item sprite and to the worn clothing layers.
public sealed class AU14CustomClothingColorSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private ItemSystem _item = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AU14CustomClothingColorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AU14CustomClothingColorComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<AU14CustomClothingColorComponent, GetEquipmentVisualsEvent>(OnGetVisuals,
            after: [typeof(ClientClothingSystem)]);
    }

    private void OnStartup(Entity<AU14CustomClothingColorComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void OnAfterState(Entity<AU14CustomClothingColorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Apply(ent);
        _item.VisualsChanged(ent);
    }

    private void Apply(Entity<AU14CustomClothingColorComponent> ent)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetColor((ent.Owner, sprite), ent.Comp.Color);
    }

    private void OnGetVisuals(Entity<AU14CustomClothingColorComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        for (var i = 0; i < args.Layers.Count; i++)
        {
            var (key, layer) = args.Layers[i];
            var tinted = _serialization.CreateCopy(layer, notNullableOverride: true);
            tinted.Color = (tinted.Color ?? Color.White) * ent.Comp.Color;
            args.Layers[i] = (key, tinted);
        }
    }
}
