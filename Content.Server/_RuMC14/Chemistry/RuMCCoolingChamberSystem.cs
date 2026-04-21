using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Server._RuMC14.Chemistry;

public sealed class RuMCCoolingChamberSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RuMCCoolingChamberComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<RuMCCoolingChamberComponent, StorageAfterCloseEvent>(OnStorageAfterClose);
        SubscribeLocalEvent<RuMCCoolingChamberComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<RuMCCoolingChamberComponent, EntInsertedIntoContainerMessage>(OnContentsChanged);
        SubscribeLocalEvent<RuMCCoolingChamberComponent, EntRemovedFromContainerMessage>(OnContentsChanged);
    }

    private void OnPowerChanged(Entity<RuMCCoolingChamberComponent> ent, ref PowerChangedEvent args)
    {
        UpdateActive(ent);
    }

    private void OnStorageAfterClose(Entity<RuMCCoolingChamberComponent> ent, ref StorageAfterCloseEvent args)
    {
        UpdateActive(ent);
    }

    private void OnStorageAfterOpen(Entity<RuMCCoolingChamberComponent> ent, ref StorageAfterOpenEvent args)
    {
        UpdateActive(ent);
    }

    private void OnContentsChanged(Entity<RuMCCoolingChamberComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateActive(ent);
    }

    private void OnContentsChanged(Entity<RuMCCoolingChamberComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateActive(ent);
    }

    private void UpdateActive(Entity<RuMCCoolingChamberComponent> ent)
    {
        if (!TryComp<EntityStorageComponent>(ent, out var storage))
            return;

        if (_power.IsPowered(ent) && !storage.Open && storage.Contents.ContainedEntities.Count > 0)
            EnsureComp<ActiveRuMCCoolingChamberComponent>(ent);
        else
            RemCompDeferred<ActiveRuMCCoolingChamberComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveRuMCCoolingChamberComponent, RuMCCoolingChamberComponent, EntityStorageComponent>();
        while (query.MoveNext(out _, out _, out var cooler, out var storage))
        {
            foreach (var contained in new List<EntityUid>(storage.Contents.ContainedEntities))
            {
                if (!TryComp<SolutionContainerManagerComponent>(contained, out var manager))
                    continue;

                Entity<SolutionContainerManagerComponent?> entityManager = new(contained, manager);
                foreach (var (_, solution) in _solutionContainer.EnumerateSolutions(entityManager))
                {
                    if (solution.Comp.Solution.Temperature <= cooler.TargetTemperature)
                        continue;

                    _solutionContainer.AddThermalEnergy(solution, -cooler.CoolPerSecond * frameTime);

                    if (solution.Comp.Solution.Temperature < cooler.TargetTemperature)
                        _solutionContainer.SetTemperature(solution, cooler.TargetTemperature);
                }
            }
        }
    }
}

