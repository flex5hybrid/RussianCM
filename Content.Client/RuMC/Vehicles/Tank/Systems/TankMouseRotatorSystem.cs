using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.RuMC.Vehicles.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.RuMC.Vehicles.Tank.Systems;

/// <summary>
/// <inheritdoc/>
/// </summary>
public sealed class TankMouseRotatorSystem : SharedTankMouseRotatorSystem
{
    [Dependency] private readonly IInputManager _input = null!;
    [Dependency] private readonly IPlayerManager _player = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly IEyeManager _eye = null!;
    [Dependency] private readonly SharedTransformSystem _transform = null!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var playerEnt = _player.LocalEntity;
        if (playerEnt == null)
            return;

        // 1) Находим мех, которым управляет игрок
        if (!TryComp<ActiveTankPilotComponent>(playerEnt.Value, out var pilotComp))
            return;

        var mech = pilotComp.Mech;
        if (mech == default)
            return;

        // 2) Получаем у него EntityMouseRotatorComponent
        if (!TryComp<TankMouseRotatorComponent>(mech, out var rotator))
            return;

        // 3) Конвертируем экран → мир
        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);
        if (mapPos.MapId == MapId.Nullspace)
            return;

        // 4) Берём Transform **танка**, а не игрока
        var mechXform = Transform(mech);
        var mechWorldPos = _transform.GetMapCoordinates(mech, xform: mechXform).Position;

        // 5) Считаем нужный угол
        var angle = (mapPos.Position - mechWorldPos).ToWorldAngle();

        // 6) Текущий поворот танка
        var curRot = _transform.GetWorldRotation(mechXform);

        if (rotator.Simple4DirMode)
        {
            var eyeRot = _eye.CurrentEye.Rotation; // camera rotation
            var angleDir =
                (angle + eyeRot).GetCardinalDir(); // apply GetCardinalDir in the camera frame, not in the world frame
            if (angleDir == (curRot + eyeRot).GetCardinalDir())
                return;

            var rotation = angleDir.ToAngle() - eyeRot; // convert back to world frame
            if (rotation >= Math.PI) // convert to [-PI, +PI)
                rotation -= 2 * Math.PI;
            else if (rotation < -Math.PI)
                rotation += 2 * Math.PI;
            RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
            {
                Rotation = rotation
            });

            return;
        }

        var diff = Angle.ShortestDistance(angle, curRot);
        if (Math.Abs(diff.Theta) < rotator.AngleTolerance.Theta)
            return;

        if (rotator.GoalRotation != null)
        {
            var goalDiff = Angle.ShortestDistance(angle, rotator.GoalRotation.Value);
            if (Math.Abs(goalDiff.Theta) < rotator.AngleTolerance.Theta)
                return;
        }

        // 7) Отправляем событие с поворотом танка
        RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
        {
            Rotation = angle,
        });
    }
}
