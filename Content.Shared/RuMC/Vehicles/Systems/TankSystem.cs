using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.RuMC.Vehicles.Systems;

/// <summary>
/// Система танка
/// </summary>
public abstract class SharedTankSystem : EntitySystem
{
    [Dependency] private readonly SharedMoverController _mover = null!;
    [Dependency] private readonly SharedInteractionSystem _interaction = null!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = null!;
    [Dependency] private readonly SharedContainerSystem _container = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = null!;

    /// <summary>
    /// Инициализация, подписка на ивенты
    /// </summary>
    public override void Initialize()
    {
        SubscribeLocalEvent<TankComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<TankComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<TankComponent, ComponentStartup>(OnStartup);
    }

    /// <summary>
    /// Запуск танка, когда туда входит пилот
    /// </summary>
    /// <param name="uid">ID игрока</param>
    /// <param name="component">Компонент танка</param>
    /// <param name="args">Аргументы запуска</param>
    private void OnStartup(EntityUid uid, TankComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
    }

    private void OnMechEntry(EntityUid uid, TankComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryInsert(uid, args.Args.User, component);
        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;
    }

    /// <summary>
    /// Попытка войти в танк
    /// </summary>
    /// <param name="uid">ID игрока</param>
    /// <param name="toInsert">ID танка</param>
    /// <param name="component">Компонент танка</param>
    private void TryInsert(EntityUid uid, EntityUid? toInsert, TankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (toInsert == null || component.PilotSlot.ContainedEntity == toInsert)
            return;

        SetupUser(uid, toInsert.Value);
        _container.Insert(toInsert.Value, component.PilotSlot);
    }

    /// <summary>
    /// Вход игрока в танк
    /// </summary>
    /// <param name="mech">ID танка</param>
    /// <param name="pilot">ID игрока</param>
    /// <param name="component">Компонент танка</param>
    private void SetupUser(EntityUid mech, EntityUid pilot, TankComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<ActiveTankPilotComponent>(pilot);

        var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        _mover.SetRelay(pilot, mech);
        _interaction.SetRelay(pilot, mech, irelay);
        rider.Mech = mech;
        Dirty(pilot, rider);

        // Отключаем ротацию у танка при WASD движении
        EnsureComp<NoRotateOnMoveComponent>(mech);
    }

    /// <summary>
    /// Возможные действия из контесткного меню танка
    /// </summary>
    /// <param name="uid">ID танка</param>
    /// <param name="component">Компонент танка</param>
    /// <param name="args">Аргументы взаимодействия</param>
    private void OnAlternativeVerb(EntityUid uid, TankComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!CanInsert(uid, args.User, component))
            return;
        var enterVerb = new AlternativeVerb
        {
            Text = Loc.GetString("mech-verb-enter"),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    component.EntryDelay,
                    new MechEntryEvent(),
                    uid,
                    target: uid)
                {
                    BreakOnMove = true,
                };

                _doAfter.TryStartDoAfter(doAfterEventArgs);
            },
        };
        args.Verbs.Add(enterVerb);
    }

    /// <summary>
    /// Проверка: может ли игрок войти в танк
    /// </summary>
    /// <param name="uid">ID игрока</param>
    /// <param name="toInsert">ID танка</param>
    /// <param name="component">Компонент танка</param>
    /// <returns></returns>
    private bool CanInsert(EntityUid uid, EntityUid toInsert, TankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert);
    }

    /// <summary>
    /// Проверка: слот пилота в танке пуст
    /// </summary>
    /// <param name="component">Компонент танка</param>
    /// <returns></returns>
    private static bool IsEmpty(TankComponent component)
    {
        return component.PilotSlot.ContainedEntity == null;
    }
}
