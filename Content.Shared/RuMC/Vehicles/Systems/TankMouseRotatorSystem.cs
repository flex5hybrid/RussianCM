using Content.Shared.Interaction;
using Content.Shared.RuMC.Vehicles.Components;

namespace Content.Shared.RuMC.Vehicles.Systems;

/// <summary>
/// Обработчик перемещения мыши для танка
/// <see cref="TankMouseRotatorComponent"/>
/// </summary>
public abstract class SharedTankMouseRotatorSystem : EntitySystem
{
    [Dependency] private readonly RotateToFaceSystem _rotate = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestMouseRotatorRotationEvent>(OnRequestTankRotation);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TankMouseRotatorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rotator, out var xform))
        {
            if (rotator.GoalRotation == null)
                continue;

            if (!_rotate.TryRotateTo(
                    uid,
                    rotator.GoalRotation.Value,
                    frameTime,
                    rotator.AngleTolerance,
                    MathHelper.DegreesToRadians(rotator.RotationSpeed),
                    xform))
                continue;
            // Stop rotating if we finished
            rotator.GoalRotation = null;
            Dirty(uid, rotator);
        }
    }

    private void OnRequestTankRotation(RequestMouseRotatorRotationEvent msg, EntitySessionEventArgs args)
    {
        // Получаем сущность игрока
        if (args.SenderSession.AttachedEntity is not { } pilot)
            return;

        // Проверяем, что игрок — активный пилот танка
        if (!TryComp<ActiveTankPilotComponent>(pilot, out var pilotComp))
            return;

        // Извлекаем UID танка
        var mech = pilotComp.Mech;
        // Проверяем, что UID валидный (не default/Invalid)
        if (mech == default)
            return;

        // На танке должен быть MouseRotatorComponent
        if (!TryComp<TankMouseRotatorComponent>(mech, out var rotator))
            return;

        // Устанавливаем цель поворота на танке
        rotator.GoalRotation = msg.Rotation;
        Dirty(mech, rotator);
    }
}
