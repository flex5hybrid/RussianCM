// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using Content.Shared.CMU14.Logistics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Content.Shared.Physics;

namespace Content.Server.CMU14.Logistics;

// Opens deploy boxes: spawns their contents in front of the user and removes the box.
public sealed class AU14DeployBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AU14DeployBoxComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, AU14DeployBoxComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = FindSpawnPoint(args.User, comp.Distance);

        foreach (var entry in comp.Entries)
        {
            var coordinates = origin.Offset(entry.Offset);
            if (entry.Offset != default && IsBlocked(coordinates))
                coordinates = origin;

            var spawned = Spawn(entry.Prototype, coordinates);

            foreach (var (slotId, itemProto) in entry.Install)
            {
                var item = Spawn(itemProto, coordinates);
                if (!_itemSlots.TryInsert(spawned, slotId, item, null))
                {
                    Log.Warning($"{ToPrettyString(uid)} couldn't fit {itemProto} into slot {slotId} of {ToPrettyString(spawned)}");
                    QueueDel(item);
                }
            }

            if (comp.Anchor && Transform(spawned).GridUid != null)
                _transform.AnchorEntity(spawned);
        }

        if (comp.Sound != null)
            _audio.PlayPvs(comp.Sound, origin);

        QueueDel(uid);
    }

    // Picks a spot in front of the user, or the user's own tile if it is blocked.
    private EntityCoordinates FindSpawnPoint(EntityUid user, float distance)
    {
        var forward = _transform.GetWorldRotation(user).ToWorldVec() * distance;
        var target = _transform.ToCoordinates(_transform.GetMapCoordinates(user).Offset(forward));

        return IsBlocked(target) ? Transform(user).Coordinates : target;
    }

    private bool IsBlocked(EntityCoordinates coordinates)
    {
        return !_turf.TryGetTileRef(coordinates, out var tile) ||
               _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable);
    }
}
