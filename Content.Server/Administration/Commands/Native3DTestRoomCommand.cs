using System.Numerics;
using Content.Shared.Administration;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics3D;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Creates the first completely native 3D validation room and moves the invoking player into it.
/// No tile, 2D fixture or planar collision is used for the room geometry.
/// </summary>
[AdminCommand(AdminFlags.Host)]
public sealed class Native3DTestRoomCommand : IConsoleCommand
{
    public string Command => "3droom";
    public string Description => "Creates a native 3D physics room and moves your character into it.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } player)
        {
            shell.WriteError("This command requires an attached player entity.");
            return;
        }

        var entities = IoCManager.Resolve<IEntityManager>();
        var systems = IoCManager.Resolve<IEntitySystemManager>();
        var maps = systems.GetEntitySystem<SharedMapSystem>();
        var transforms = systems.GetEntitySystem<SharedTransformSystem>();
        var transforms3D = systems.GetEntitySystem<SharedTransform3DSystem>();
        var physics3D = systems.GetEntitySystem<SharedPhysics3DSystem>();
        var atmosphere3D = systems.GetEntitySystem<AtmosphereSystem>();
        var lights = systems.GetEntitySystem<SharedPointLightSystem>();

        var mapUid = maps.CreateMap(out var mapId);
        entities.EnsureComponent<MapGrid3DComponent>(mapUid);
        atmosphere3D.AddAtmosphereRegion3D(
            mapUid,
            new Vector3i(-7, -7, 0),
            new Vector3i(6, 6, 2),
            sealedBoundary: true);
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(0f, 0f, -0.1f), new Vector3(14f, 14f, 0.2f), new Color(0.18f, 0.24f, 0.31f));
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(0f, 0f, 3.1f), new Vector3(14f, 14f, 0.2f), new Color(0.10f, 0.14f, 0.19f));
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(-7.1f, 0f, 1.5f), new Vector3(0.2f, 14f, 3f), new Color(0.24f, 0.32f, 0.42f));
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(7.1f, 0f, 1.5f), new Vector3(0.2f, 14f, 3f), new Color(0.24f, 0.32f, 0.42f));
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(0f, -7.1f, 1.5f), new Vector3(14f, 0.2f, 3f), new Color(0.21f, 0.29f, 0.39f));
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(0f, 7.1f, 1.5f), new Vector3(14f, 0.2f, 3f), new Color(0.21f, 0.29f, 0.39f));
        SpawnBox(
            entities,
            transforms3D,
            physics3D,
            mapId,
            new Vector3(2.2f, 1.8f, 0.6f),
            new Vector3(1.2f, 1.2f, 1.2f),
            new Color(0.76f, 0.34f, 0.16f),
            "/Models/Native3D/validation_barrel.obj");
        SpawnBox(entities, transforms3D, physics3D, mapId, new Vector3(-2.4f, 2.3f, 0.35f), new Vector3(2.4f, 0.8f, 0.7f), new Color(0.24f, 0.62f, 0.56f));
        SpawnLight(entities, transforms3D, lights, mapId, new Vector3(-3.6f, -1.5f, 2.75f), new Color(0.72f, 0.86f, 1f));
        SpawnLight(entities, transforms3D, lights, mapId, new Vector3(3.4f, 2.1f, 2.75f), new Color(1f, 0.72f, 0.42f));

        transforms.SetCoordinates(player, new EntityCoordinates(mapUid, Vector2.Zero));
        ConfigureCharacter(entities, transforms3D, physics3D, player, new Vector3(0f, -3f, 0.03f));
        shell.WriteLine($"Native 3D room created on map {mapId}. WASD moves, mouse looks, Space jumps, F8 releases/captures the mouse.");
    }

    private static void SpawnLight(
        IEntityManager entities,
        SharedTransform3DSystem transforms3D,
        SharedPointLightSystem lights,
        MapId mapId,
        Vector3 position,
        Color color)
    {
        var uid = entities.SpawnEntity(null, new MapCoordinates(new Vector2(position.X, position.Y), mapId));
        transforms3D.SetAuthoritative(uid, true);
        transforms3D.SetWorldPosition3D(uid, position);

        var light = lights.EnsureLight(uid);
        lights.SetEnabled(uid, true, light);
        lights.SetColor(uid, color, light);
        lights.SetRadius(uid, 8f, light);
        lights.SetEnergy(uid, 1.35f, light);
        lights.SetCastShadows(uid, true, light);
        entities.EnsureComponent<PointLight3DComponent>(uid);

        var primitive = entities.EnsureComponent<Primitive3DComponent>(uid);
        primitive.Size = new Vector3(0.28f, 0.28f, 0.12f);
        primitive.Color = color;
        primitive.Dirty(entities);
    }

    private static void SpawnBox(
        IEntityManager entities,
        SharedTransform3DSystem transforms3D,
        SharedPhysics3DSystem physics3D,
        MapId mapId,
        Vector3 position,
        Vector3 size,
        Color color,
        string? meshPath = null)
    {
        var uid = entities.SpawnEntity(null, new MapCoordinates(new Vector2(position.X, position.Y), mapId));
        transforms3D.SetAuthoritative(uid, true);
        transforms3D.SetWorldPosition3D(uid, position);

        var body = entities.EnsureComponent<PhysicsBody3DComponent>(uid);
        body.BodyType = PhysicsBodyType3D.Static;
        body.CanCollide = true;

        var collider = entities.EnsureComponent<Collider3DComponent>(uid);
        collider.Shapes.Clear();
        collider.Shapes.Add(new BoxShape3D
        {
            Size = size,
            CollisionLayer = 1,
            CollisionMask = int.MaxValue,
            Friction = 0.8f,
        });

        var primitive = entities.EnsureComponent<Primitive3DComponent>(uid);
        primitive.Size = size;
        primitive.Color = color;
        if (meshPath is not null)
        {
            var mesh = entities.EnsureComponent<Mesh3DComponent>(uid);
            mesh.Mesh = meshPath;
            mesh.Tint = color;
            mesh.Roughness = 0.62f;
            mesh.Metallic = 0.18f;
            mesh.Dirty(entities);
        }
        body.Dirty(entities);
        collider.Dirty(entities);
        primitive.Dirty(entities);
        physics3D.RefreshBody(uid);
    }

    private static void ConfigureCharacter(
        IEntityManager entities,
        SharedTransform3DSystem transforms3D,
        SharedPhysics3DSystem physics3D,
        EntityUid uid,
        Vector3 position)
    {
        transforms3D.SetAuthoritative(uid, true);
        transforms3D.SetWorldPosition3D(uid, position);
        transforms3D.SetWorldRotation3D(uid, Quaternion.Identity);

        var body = entities.EnsureComponent<PhysicsBody3DComponent>(uid);
        body.BodyType = PhysicsBodyType3D.Character;
        body.Mass = 80f;
        body.CanCollide = true;
        body.SleepingAllowed = false;

        var collider = entities.EnsureComponent<Collider3DComponent>(uid);
        collider.Shapes.Clear();
        collider.Shapes.Add(new CapsuleShape3D
        {
            Radius = 0.35f,
            Length = 1f,
            Offset = new Vector3(0f, 0f, 0.85f),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f),
            CollisionLayer = 1,
            CollisionMask = int.MaxValue,
            Friction = 0f,
        });

        entities.EnsureComponent<CharacterController3DComponent>(uid);
        body.Dirty(entities);
        collider.Dirty(entities);
        physics3D.RefreshBody(uid);
    }
}
