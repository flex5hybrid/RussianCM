# RussianCM authoritative 3D migration

## Objective

RussianCM must become a server-authoritative 3D game while preserving the behavior of the existing 2D game.
The current perspective renderer is a migration harness, not the final architecture.

## Non-negotiable contracts

1. The server owns position, rotation, physics, raycasts, reach, line of sight and hit results.
2. Migrated entities use `Transform3DComponent` as their spatial source of truth.
3. The legacy transform may receive a derived XY projection during migration, but it may not overwrite an
   authoritative 3D pose.
4. World actions carry a 3D origin/direction or 3D coordinates. A hidden 2D cursor is never a gameplay target.
5. Client prediction is corrected from server snapshots; cross-platform bit-identical physics is not assumed.
6. Map, replay and network formats version their 3D data explicitly.
7. Every subsystem is removed from the 2D runtime only after its parity scenarios pass in 3D.

## Runtime ownership

```text
Input command
    -> predicted CharacterController3D
    -> server Physics3D
    -> authoritative Transform3D
    -> 3D spatial queries and gameplay systems
    -> snapshot / correction
    -> client interpolation and renderer
```

`MapGrid3D` supplies collision geometry, navigation cells, atmosphere adjacency and render meshes. Gameplay
systems consume 3D spatial interfaces rather than a particular physics backend.

## Migration gates

### Gate 1: spatial core

- XYZ position, quaternion orientation and scale compose through entity parents.
- local, map and network coordinate types round-trip without dropping Z.
- component states, prototypes and map serialization preserve the full pose.
- migrated entities are replicated independently of legacy XY state.

### Gate 2: true 3D room

- fixed-step server physics;
- capsule character, static floor/walls and dynamic boxes;
- camera-relative movement, gravity and a physical jump;
- ray, shape and overlap queries;
- client prediction and server correction.

### Gate 3: 3D station core

- sparse volumetric grid and multi-deck map;
- doors, windows, anchoring, construction and item interaction;
- melee, hitscan, projectiles, throws and explosions;
- no world action reads a hidden 2D cursor.

### Gate 4: playable round

- atmosphere, fire, power, pipes and disposal;
- AI navigation and perception;
- roles, abilities, vehicles, shuttles and admin tools;
- multiplayer round completion.

### Gate 5: visual and mechanical parity

- production mesh/material/animation pipeline;
- all entries in `3D_MECHANICS_PARITY.md` pass;
- performance budgets pass on target server and client profiles;
- legacy world renderer, physics and coordinate adapters are deleted.

## Physics backend decision

BepuPhysics v2 is the initial backend because it is pure C#, uses `System.Numerics`, supports the required
convex/compound/mesh shapes, continuous collision detection and scene queries. It is always hidden behind
Robust-owned interfaces. Physics snapshots and correction are authoritative because floating-point simulation
is not assumed to be bit-identical across machines.

## Existing bridge disposition

- Keep the Content.Client render-target integration, mouse-look transport and sprite-atlas extraction as
  migration assets.
- Replace visual jump state with a physical body pose at Gate 2.
- Replace client sprite-extruded raycasts with server Physics3D queries at Gate 2.
- Replace map-per-floor composition with `MapGrid3D` at Gate 3.
- Keep billboard rendering only as an explicit missing-model fallback until Gate 5.

## Implemented vertical slice

The `3droom` host command creates an isolated map whose floor, ceiling, walls and obstacles are native
`Transform3D` + `PhysicsBody3D` + `Collider3D` + `Primitive3D` entities. It promotes the invoking player to
an upright dynamic capsule and moves them into that map. This slice deliberately contains no tile or 2D fixture
geometry.

Current controls are mouse look, WASD movement and Space jump. F8 releases or recaptures relative mouse mode.
The server runs 3D physics at a fixed 60 Hz and replicates authoritative XYZ transforms and velocity; the old
planar mover exits immediately for entities carrying `CharacterController3DComponent`.

This is a migration validation environment, not the final content path. The remaining physics work is compound
and mesh shapes, sweep/overlap queries, contact events, per-body gravity/damping/CCD, client prediction and
reconciliation.
