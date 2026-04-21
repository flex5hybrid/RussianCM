using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
/// Реализует механику SSCM13: если в контейнере с раствором накапливается слишком много взрывчатых
/// веществ (суммарный powerMod×кол-во ≥ порога), контейнер взрывается.
/// Подписывается на все контейнеры с <see cref="SolutionContainerManagerComponent"/>.
/// </summary>
public sealed class RMCChemicalExplosionSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCReagentSystem _reagents = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    // Суммарная взрывная мощность (powerMod × units) при которой контейнер взрывается.
    private const float PowerThreshold = 25f;

    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<SolutionContainerManagerComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (_net.IsClient)
            return;

        // Только стандартные бикеры — не корпуса бомб
        if (args.SolutionId != "beaker")
            return;

        if (HasComp<RMCChembombCasingComponent>(ent))
            return;

        float totalPower = 0f;

        foreach (var reagent in args.Solution)
        {
            if (!_reagents.TryIndex(reagent.Reagent, out var proto))
                continue;

            if (proto.PowerMod <= FixedPoint2.Zero)
                continue;

            totalPower += (float) reagent.Quantity * (float) proto.PowerMod;
        }

        if (totalPower < PowerThreshold)
            return;

        // Взрыв масштабируется с мощностью накопленных реагентов
        var power = totalPower * 8f;
        var slope = MathF.Max(1.5f, 60f / 14f);
        var maxIntensity = MathF.Max(5f, power / 15f);

        _explosion.QueueExplosion(
            _transform.GetMapCoordinates(ent.Owner),
            "RMC",
            power,
            slope,
            maxIntensity,
            ent.Owner);

        QueueDel(ent.Owner);
    }
}
