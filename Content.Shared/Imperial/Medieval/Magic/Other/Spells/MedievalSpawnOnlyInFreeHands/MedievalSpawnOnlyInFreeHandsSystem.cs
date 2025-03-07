using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.Medieval.Magic;
using Content.Shared.Imperial.Medieval.Magic.Mana;
using Content.Shared.Interaction;
using Content.Shared.Magic.Events;

namespace Content.Shared.Imperial.Medieval.Magic.MedievalSpawnOnlyInFreeHands;


public sealed partial class MedievalSpawnOnlyInFreeHandsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalSpawnOnlyInFreeHandsComponent, MedievalBeforeCastSpellEvent>(OnBeforeSpellCast, before: new[] { typeof(ManaSystem) });
    }

    private void OnBeforeSpellCast(EntityUid uid, MedievalSpawnOnlyInFreeHandsComponent component, ref MedievalBeforeCastSpellEvent args)
    {
        if (_handsSystem.TryGetEmptyHand(args.Performer, out var _)) return;

        _actionsSystem.ClearCooldown(uid);
        args.Cancelled = true;
    }
}
