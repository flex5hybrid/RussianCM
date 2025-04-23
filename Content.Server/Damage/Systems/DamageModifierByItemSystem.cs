using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Linq;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageModifierByItemSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageModifierByItemComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, DamageModifierByItemComponent component, MeleeHitEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out EntityStorageComponent? storage))
                return;

            var hasItem = storage.Contents.ContainedEntities.Any(e =>
            {
                var meta = _entityManager.GetComponentOrNull<MetaDataComponent>(e);
                var protoId = meta?.EntityPrototype?.ID;
                return protoId != null && component.ItemIds.Contains(protoId);
            });

            if (!hasItem)
                return;

            _damageableSystem.TryChangeDamage(uid, component.Damage, ignoreResistances: false);
        }
    }
}
