using System.Numerics;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.CMU14.Lighting;
using Content.Shared.FixedPoint;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.CMU14.Light;

public sealed partial class CMUCinematicDemoCommand
{
    private void IndexReviewSources(EntityUid map, MapId mapId, Vector2 camera)
    {
        var review = EntityManager.GetComponent<CMUCinematicReviewSceneComponent>(map);
        review.Sources.Clear();
        review.Rotations.Clear();
        review.SelectedLamp = null;
        var closest = float.MaxValue;
        var query = EntityManager.EntityQueryEnumerator<CMUCinematicEmitterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var emitter, out var xform))
        {
            if (xform.MapID != mapId)
                continue;
            review.Sources.Add(uid);
            review.Rotations[uid] = xform.LocalRotation;
            var profile = _prototypes.Index(emitter.Profile);
            var distance = Vector2.DistanceSquared(_transform.GetWorldPosition(xform), camera);
            if (profile.RequireLight && profile.Shape == CMUCinematicSourceShape.Linear
                && !profile.ProceduralEmission && distance < closest)
            {
                closest = distance;
                review.SelectedLamp = uid;
            }
        }
    }

    private void SetReviewState(IConsoleShell shell, EntityUid actor, string state)
    {
        var map = EntityManager.GetComponent<TransformComponent>(actor).MapUid;
        if (map == null || !EntityManager.TryGetComponent<CMUCinematicReviewSceneComponent>(map, out var review)
            || state is not ("all" or "lamp-off" or "screens-off" or "alarm" or "spotlight" or "alarm-off" or "spotlight-off"))
        {
            shell.WriteError(Loc.GetString("cmu-cinematic-review-state-error"));
            return;
        }
        var power = EntityManager.System<PowerReceiverSystem>();
        var powered = EntityManager.System<SharedPoweredLightSystem>();
        var appearance = EntityManager.System<SharedAppearanceSystem>();
        foreach (var uid in review.Sources)
        {
            if (!EntityManager.TryGetComponent<CMUCinematicEmitterComponent>(uid, out var emitter))
                continue;
            var profile = _prototypes.Index(emitter.Profile);
            var spotlight = emitter.Profile.Id is "CMUFloodlight" or "CMUSmallFloodlight";
            if (review.Rotations.TryGetValue(uid, out var initialRotation))
                _transform.SetLocalRotation(uid, initialRotation);
            var enabled = !(state == "lamp-off" && uid == review.SelectedLamp
                || state == "screens-off" && !profile.RequireLight
                || state == "alarm-off" && emitter.Profile.Id == "CMUBeacon"
                || state == "spotlight-off" && spotlight);
            emitter.Enabled = enabled;
            EntityManager.Dirty(uid, emitter);
            if (EntityManager.HasComponent<ApcPowerReceiverComponent>(uid))
                power.SetPowerDisabled(uid, !enabled);
            if (EntityManager.HasComponent<PoweredLightComponent>(uid))
                powered.SetState(uid, enabled);
            else if (profile.RequireLight)
            {
                _lights.SetEnabled(uid, enabled);
                if (EntityManager.HasComponent<AppearanceComponent>(uid) && !profile.ProceduralEmission)
                    appearance.SetData(uid, PoweredLightVisuals.BulbState,
                        enabled ? PoweredLightState.On : PoweredLightState.Off);
            }
            if (state == "alarm" && emitter.Profile.Id == "CMUBeacon")
                _transform.SetLocalRotation(uid, Angle.FromDegrees(65));
            if (state == "spotlight" && spotlight)
                _transform.SetLocalRotation(uid, Angle.FromDegrees(35));
        }
        review.State = state;
        shell.WriteLine(Loc.GetString("cmu-cinematic-review-state", ("state", state),
            ("lamp", review.SelectedLamp is { } lamp ? EntityManager.GetNetEntity(lamp).ToString() : "-")));
    }

    private void CreateMaterialReview(IConsoleShell shell, ICommonSession? session)
    {
        var map = _map.CreateMap(out var mapId, runMapInit: true);
        EntityManager.AddComponent<CMUCinematicReviewSceneComponent>(map);
        _map.SetAmbientLight(mapId, Color.FromHex("#6B6B6B"));
        AddAir(map);
        var grid = _map.CreateGridEntity(mapId);
        // Generic grids have a black implicit indoor roof, which otherwise overrides
        // map ambient. Keep the four control swatches readable in every preset.
        var roof = EntityManager.EnsureComponent<Content.Shared.Light.Components.ImplicitRoofComponent>(grid);
        roof.Color = Color.FromHex("#6B6B6B");
        EntityManager.Dirty(grid, roof);
        EntityManager.AddComponent<CMUCinematicAtmosphereComponent>(grid).Density = 0.5f;
        string[] materials = ["CMFloorSteel", "CMFloorPlate", "CMFloorWhiteSterile", "CMFloorPlate"];
        for (var x = -9; x <= 8; x++)
        for (var y = -4; y <= 5; y++)
        {
            var column = Math.Clamp((x + 8) / 4, 0, 3);
            _map.SetTile(grid.Owner, grid.Comp, new Vector2i(x, y), new Tile(_tiles[materials[column]].TileId));
            if (x is -9 or 8 || y is -4 or 5)
                EntityManager.SpawnEntity("CMWallMetalAlmayer",
                    new EntityCoordinates(grid.Owner, new Vector2(x + 0.5f, y + 0.5f)));
            if (column == 3 && x < 8 && y is >= 0 and <= 4)
            {
                var water = new Solution();
                water.AddReagent(Water.Id, FixedPoint2.New(60));
                _puddles.TrySpillAt(new EntityCoordinates(grid.Owner, new Vector2(x + 0.5f, y + 0.5f)),
                    water, out _, sound: false);
            }
        }
        for (var column = 0; column < 4; column++)
        {
            var lamp = EntityManager.SpawnEntity("RMCLightFixtureAlwaysPowered",
                new EntityCoordinates(grid.Owner, new Vector2(-6.5f + column * 4f, 4.5f)));
            var emitter = EntityManager.GetComponent<CMUCinematicEmitterComponent>(lamp);
            emitter.Profile = "CMUFixture";
            EntityManager.Dirty(lamp, emitter);
            _lights.SetColor(lamp, Color.FromHex("#F0E9DD"));
            _lights.SetRadius(lamp, 3.6f);
            _lights.SetEnergy(lamp, 1f);
            EntityManager.SpawnEntity("CMOverheadPipe",
                new EntityCoordinates(grid.Owner, new Vector2(-6.5f + column * 4f, 5.5f)));
        }
        PowerForReview(mapId);
        PlacePlayer(session, new EntityCoordinates(grid.Owner, new Vector2(-0.5f, -1.5f)));
        IndexReviewSources(map, mapId, new Vector2(-0.5f, -1.5f));
        shell.WriteLine(Loc.GetString("cmu-cinematic-demo-created", ("map", mapId),
            ("grid", EntityManager.GetNetEntity(grid.Owner))));
    }
}
