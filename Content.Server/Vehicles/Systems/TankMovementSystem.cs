using System.Linq;
using System.Numerics;
using Content.Server.Vehicles.Components;
using Content.Shared.Vehicles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Vehicles.Systems;

/// <summary>
/// Система перемещения танка
/// </summary>
public sealed class TankMovementSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly TransformSystem _transform = null!; // Изменено на TransformSystem

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

    /// <summary>
    /// Обработчик перемещения
    /// </summary>
    /// <param name="direction">направление</param>
    private sealed class MovementInputCmdHandler(Direction direction) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entityManager,
            ICommonSession? session,
            IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } entity)
                return false;

            // Сохраняем состояние движения в компоненте
            if (!entityManager.TryGetComponent<TankMovementComponent>(entity, out var movement))
                return false;

            // Двигаемся либо вперед, либо назад
            switch (message.State)
            {
                case BoundKeyState.Down:
                    movement.ActiveDirections.Add(direction);
                    break;
                case BoundKeyState.Up:
                    movement.ActiveDirections.Remove(direction);
                    break;
            }

            return true;
        }
    }

    /// <summary>
    /// Обновление позиции танка
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TankMovementComponent>();
        while (query.MoveNext(out var uid, out var movement))
        {
            if (!movement.CanMove)
                continue;

            // Получаем водителя через TankDriverComponent
            EntityUid? driver = null;
            var driverQuery = EntityQueryEnumerator<TankDriverComponent>();
            while (driverQuery.MoveNext(out var user, out var driverComp))
            {
                if (driverComp.Tank == uid)
                {
                    driver = user;
                    break;
                }
            }

            if (driver == null || !TryComp<ActorComponent>(driver.Value, out _))
                continue;

            // Рассчитываем вектор движения на основе активных направлений
            var moveDir =
                movement.ActiveDirections.Aggregate(Vector2.Zero, (current, direction) => current + direction.ToVec());

            // Нормализуем вектор для диагонального движения
            if (moveDir.LengthSquared() > 1.0f)
            {
                moveDir = Vector2.Normalize(moveDir);
            }

            // Обрабатываем движение
            ProcessMovement(uid, movement, moveDir, frameTime);
        }
    }

    /// <summary>
    /// Процесс перемещения
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="movement"></param>
    /// <param name="moveDir"></param>
    /// <param name="frameTime"></param>
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
