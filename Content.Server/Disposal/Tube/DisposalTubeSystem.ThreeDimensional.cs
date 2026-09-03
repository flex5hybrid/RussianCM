using System;
using System.Linq;
using System.Numerics;
using Content.Server.Disposal.Unit;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server.Disposal.Tube;

public sealed partial class DisposalTubeSystem
{
    [Dependency] private SharedTransform3DSystem _transform3D = default!;
    [Dependency] private SharedMapGrid3DSystem _mapGrid3D = default!;

    public bool TryGetNextDirection3D(EntityUid tubeUid, DisposalHolderComponent holder, out Vector3i direction)
    {
        direction = Vector3i.Zero;
        if (!_transform3D.IsAuthoritative(tubeUid) ||
            !TryComp(tubeUid, out DisposalPort3DComponent? ports))
            return false;

        var incoming = -holder.PreviousDirection3D;
        foreach (var tag in holder.Tags.OrderBy(tag => tag, StringComparer.Ordinal))
        {
            if (ports.TaggedRoutes.TryGetValue(tag, out var tagged) &&
                ToWorldCardinal3D(tubeUid, tagged) != incoming &&
                ports.Connections.Contains(tagged))
            {
                direction = ToWorldCardinal3D(tubeUid, tagged);
                return true;
            }
        }

        if (ports.DefaultDirection != Vector3i.Zero &&
            ToWorldCardinal3D(tubeUid, ports.DefaultDirection) != incoming &&
            ports.Connections.Contains(ports.DefaultDirection))
        {
            direction = ToWorldCardinal3D(tubeUid, ports.DefaultDirection);
            return true;
        }

        foreach (var connection in ports.Connections)
        {
            var worldConnection = ToWorldCardinal3D(tubeUid, connection);
            if (worldConnection == -holder.PreviousDirection3D)
                continue;

            direction = worldConnection;
            return true;
        }

        return true;
    }

    public EntityUid? NextTubeFor3D(EntityUid target, Vector3i direction)
    {
        if (direction == Vector3i.Zero ||
            !_transform3D.IsAuthoritative(target) ||
            !TryComp(target, out DisposalTubeComponent? targetTube) ||
            !TryComp(target, out DisposalPort3DComponent? targetPorts))
            return null;

        var targetTransform = Transform(target);
        var root = targetTransform.GridUid ?? targetTransform.MapUid;
        if (root is not { } rootUid || !TryComp(rootUid, out MapGrid3DComponent? grid))
            return null;

        var targetCell = _mapGrid3D.WorldToCell((rootUid, grid), _transform3D.GetWorldPosition3D(target, targetTransform));
        var nextCell = targetCell + direction;
        var query = EntityQueryEnumerator<DisposalTubeComponent, DisposalPort3DComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tube, out var ports, out var transform))
        {
            if (uid == target ||
                !tube.Connected ||
                !_transform3D.IsAuthoritative(uid) ||
                (transform.GridUid ?? transform.MapUid) != rootUid)
                continue;

            var cell = _mapGrid3D.WorldToCell((rootUid, grid), _transform3D.GetWorldPosition3D(uid, transform));
            if (cell != nextCell || !CanConnect3D(uid, ports, -direction))
                continue;

            if (!targetTube.Connected || !CanConnect3D(target, targetPorts, direction))
                return null;

            return uid;
        }

        return null;
    }

    public Vector3 GetDisposalDirectionWorld3D(EntityUid tubeUid, Vector3i direction)
    {
        if (direction == Vector3i.Zero)
            return Vector3.Zero;

        var transform = Transform(tubeUid);
        var root = transform.GridUid ?? transform.MapUid;
        if (root is not { } rootUid || !TryComp(rootUid, out MapGrid3DComponent? grid))
            return Vector3.Normalize((Vector3) direction);

        var cell = _mapGrid3D.WorldToCell((rootUid, grid), _transform3D.GetWorldPosition3D(tubeUid, transform));
        var current = _mapGrid3D.CellToWorld((rootUid, grid), cell);
        var next = _mapGrid3D.CellToWorld((rootUid, grid), cell + direction);
        var transformed = next - current;
        return transformed.LengthSquared() > 1e-6f ? Vector3.Normalize(transformed) : Vector3.Zero;
    }

    private bool CanConnect3D(EntityUid tubeUid, DisposalPort3DComponent ports, Vector3i worldDirection)
    {
        foreach (var local in ports.Connections)
        {
            if (ToWorldCardinal3D(tubeUid, local) == worldDirection)
                return true;
        }

        return false;
    }

    private Vector3i ToWorldCardinal3D(EntityUid uid, Vector3i localDirection)
    {
        var direction = Vector3.Transform((Vector3) localDirection, _transform3D.GetWorldRotation3D(uid));
        var absolute = Vector3.Abs(direction);
        if (absolute.X >= absolute.Y && absolute.X >= absolute.Z)
            return direction.X >= 0f ? Vector3i.East : Vector3i.West;
        if (absolute.Y >= absolute.Z)
            return direction.Y >= 0f ? Vector3i.North : Vector3i.South;
        return direction.Z >= 0f ? Vector3i.Up : Vector3i.Down;
    }
}
