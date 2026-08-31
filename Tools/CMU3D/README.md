# CMU3D engine work

This directory bootstraps the real 3D migration of CMU without pretending that Content-side components can replace engine changes.

## Current engine baseline

RussianCM pins the `RobustToolbox` submodule to:

`03e28a812104b70761244fca084245e0dab75d2a`

The first migration phase is deliberately additive: introduce engine-level 3D transform primitives while leaving the existing 2D transform authoritative. This keeps the normal CMU build usable while the 3D transform, replication, physics and renderer are brought up one subsystem at a time.

## Patch order

1. `0001-add-transform3d-component.patch`
   - adds an engine-owned `Transform3DComponent` to `Robust.Shared`;
   - stores local XYZ position and quaternion rotation;
   - does not replace `TransformComponent` yet;
   - does not alter current 2D gameplay.

Next patches should implement, in this order:

1. `SharedTransform3DSystem` and hierarchy/world transforms;
2. network component state and interpolation;
3. 3D spatial queries and broadphase abstraction;
4. 3D physics backend;
5. 3D PVS bounds;
6. client camera and mesh renderer;
7. map/tile extrusion bridge;
8. migration of aiming/projectiles/movement from 2D queries.

## Applying the patches locally

From the RussianCM repository root on Windows PowerShell:

```powershell
./Tools/CMU3D/apply-engine-patches.ps1
```

The script refuses to apply against an unexpected RobustToolbox commit. After a dedicated RobustToolbox fork exists, these patches should be committed there and RussianCM's submodule URL/pointer should be switched to that fork. Keeping engine source as patch files in RussianCM is only the bootstrap stage.
