using Content.Server.Vehicles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;
using Robust.Shared.Physics;

namespace Content.Server.Vehicles.Systems;

public sealed class TankMovementSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TransformSystem _transform = default!; // Изменено на TransformSystem

    public override void Initialize()
    {
        base.Initialize();

        // Регистрируем обработчики команд движения
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp,
                new MovementInputCmdHandler(Direction.North))
            .Bind(EngineKeyFunctions.MoveDown,
                new MovementInputCmdHandler(Direction.South))
            .Bind(EngineKeyFunctions.MoveLeft,
                new MovementInputCmdHandler(Direction.West))
            .Bind(EngineKeyFunctions.MoveRight,
                new MovementInputCmdHandler(Direction.East))
            .Register<TankMovementSystem>();
    }

    private sealed class MovementInputCmdHandler : InputCmdHandler
    {
        private readonly Direction _direction;

        public MovementInputCmdHandler(Direction direction)
        {
            _direction = direction;
        }

        public override bool HandleCmdMessage(IEntityManager entityManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } entity)
                return false;

            // Сохраняем состояние движения в компоненте
            if (entityManager.TryGetComponent<TankMovementComponent>(entity, out var movement))
            {
                if (message.State == BoundKeyState.Down)
                {
                    movement.ActiveDirections.Add(_direction);
                }
                else if (message.State == BoundKeyState.Up)
                {
                    movement.ActiveDirections.Remove(_direction);
                }
                return true;
            }
            return false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TankMovementComponent>();
        while (query.MoveNext(out var uid, out var movement))
        {
            if (!movement.CanMove)
                continue;

            // Рассчитываем вектор движения на основе активных направлений
            var moveDir = Vector2.Zero;
            foreach (var direction in movement.ActiveDirections)
            {
                moveDir += direction.ToVec();
            }

            // Нормализуем вектор для диагонального движения
            if (moveDir.LengthSquared() > 1.0f)
            {
                moveDir = Vector2.Normalize(moveDir);
            }

            // Обрабатываем движение
            ProcessMovement(uid, movement, moveDir, frameTime);
        }
    }

    private void ProcessMovement(EntityUid uid, TankMovementComponent movement, Vector2 moveDir, float frameTime)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        // Получаем компонент Transform
        var transform = Transform(uid);

        // Поворот
        if (moveDir.X != 0)
        {
            var sign = Math.Sign(moveDir.X);
            var rotationDelta = movement.RotationSpeed * sign * frameTime;

            // Используем прямой доступ к вращению
            var newRotation = transform.LocalRotation + rotationDelta;
            _transform.SetLocalRotation(uid, newRotation);
        }

        // Движение вперед/назад
        if (moveDir.Y != 0)
        {
            movement.MovingForward = moveDir.Y > 0;
            movement.CurrentSpeed = Math.Clamp(
                movement.CurrentSpeed + movement.Acceleration * frameTime * Math.Sign(moveDir.Y),
                -movement.MaxSpeed,
                movement.MaxSpeed
            );
        }
        else
        {
            // Торможение
            movement.CurrentSpeed = MathHelper.Lerp(movement.CurrentSpeed, 0f, 0.2f * frameTime);

            // Останавливаем танк, если скорость очень мала
            if (Math.Abs(movement.CurrentSpeed) < 0.01f)
            {
                movement.CurrentSpeed = 0f;
            }
        }

        // Применение скорости
        var worldRotation = transform.WorldRotation;
        var direction = movement.MovingForward
            ? worldRotation.ToWorldVec()
            : -worldRotation.ToWorldVec();

        _physics.SetLinearVelocity(uid, direction * movement.CurrentSpeed, body: physics);
    }
}
