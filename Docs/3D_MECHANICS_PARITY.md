# 3D mechanics parity ledger

Status values: `reference`, `foundation`, `partial`, `parity`, `blocked`.

| Domain | Required scenarios | Status |
|---|---|---|
| Spatial hierarchy | parent movement/rotation/scale, grid transfer, anchoring, map serialization | foundation |
| Networking | snapshots, interpolation, prediction, correction, PVS, reconnect, replay | foundation |
| Character movement | walk, run, diagonal speed, crouch, crawl, jump, fall, slopes, stairs | foundation: native mouse yaw/pitch, dynamic capsule, grounding, jump, WASD |
| Collision | static/dynamic/kinematic, sensors, layers, CCD, joints, contacts | foundation: rigid bodies, primitive colliders, filtering, sensors, raycast, fixed server step |
| Gravity and zero-G | gravity zones, drifting, push-off, magboots, knockback | reference |
| Interaction | use, alternate use, examine, context menu, reach, LOS, pull | partial |
| Inventory and hands | pickup, drop, wield, storage, equipping, dragging | reference |
| Combat | melee, blocking, hitscan, projectiles, recoil, lag compensation | reference |
| Throwing and explosions | arcs, impacts, blast occlusion, structural damage | reference |
| Construction | placement preview, rotation, anchoring, dismantling, multi-deck placement | reference |
| Atmosphere | six-direction adjacency, pressure, vacuum, vents, breaches, fire, smoke | reference |
| Power | cables, machines, generators, APCs, grid movement | reference |
| Pipes and disposal | networks, vertical connections, packets, machinery | reference |
| Lighting and vision | lights, shadows, occlusion, darkness, blindness, scopes | reference |
| Audio | 3D attenuation, occlusion, ambience, UI audio | reference |
| AI | navigation, multi-floor routes, perception, combat behavior | reference |
| Station grids | sparse chunks, decks, shuttles, docking, rotation, splitting | reference |
| Roles and round flow | lobby, spawn, jobs, objectives, respawn, round end | reference |
| Human abilities | actions, medicine, surgery, status effects, emotes | reference |
| Xeno and special mobs | movement modes, abilities, structures, targeting, large bodies | reference |
| Vehicles and mechs | entry, seats, movement, collision, turrets, destruction | reference |
| UI and accessibility | cursor capture, HUD, chat, windows, key rebinding, spectator camera | partial |
| Admin and tools | ghosts, admin eye, entity inspection, mapping, debug overlays | reference |
| Rendering | meshes, PBR/stylized materials, lights, shadows, animation, particles, decals | partial |
| Content pipeline | glTF import, prototypes, generated fallbacks, validation, hot reload | reference |
| Performance | server tick, bandwidth, memory, frame time, culling, load tests | reference |

An entry reaches `parity` only when its reference scenarios pass in single-player simulation, multiplayer,
prediction/correction and replay where applicable.

The native validation slice is entered with the server command `3droom`. Compilation and runtime validation are
intentionally still unrecorded because the current task explicitly forbids compilation attempts; these rows only
claim source implementation.
