using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Storage.Components;

namespace Content.Server._RuMC14.Chemistry;

public sealed class RuMCCoolingChamberSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RuMCCoolingChamberComponent, StorageAfterCloseEvent>(OnStorageClosed);
    }

    // После закрытия двери PVS ещё не успел обработать переход сущностей в контейнер —
    // помечаем флаг, чтобы пропустить один тик и избежать PVS-ассерта на грязных сетевых компонентах.
    private void OnStorageClosed(Entity<RuMCCoolingChamberComponent> ent, ref StorageAfterCloseEvent args)
    {
        ent.Comp.SkipNextTick = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RuMCCoolingChamberComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var cooler, out var storage))
        {
            // Если он открыт, или внутри ничего нет, пропускаем
            if (storage.Open || storage.Contents.ContainedEntities.Count == 0)
                continue;

            // Если не запитан - пропускаем
            if (!_power.IsPowered(uid))
                continue;

            // Пропускаем один тик после закрытия — PVS должен завершить скрытие содержимого
            if (cooler.SkipNextTick)
            {
                cooler.SkipNextTick = false;
                continue;
            }

            foreach (var contained in storage.Contents.ContainedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(contained, out var manager))
                    continue;

                var energy = -cooler.CoolPerSecond * frameTime;
                foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((contained, manager)))
                {
                    if (solution.Comp.Solution.Temperature <= cooler.TargetTemperature)
                        continue;

                    _solutionContainer.AddThermalEnergy(solution, energy);
                }
            }
        }
    }
}
