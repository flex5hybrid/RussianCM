using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Imperial.Medieval.Magic.MedievalAddComponentOnInteract;


public sealed partial class MedievalAddComponentOnAttackSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalAddComponentOnInteractComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<MedievalAddComponentOnInteractComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnProjectileHit(EntityUid uid, MedievalAddComponentOnInteractComponent component, ProjectileHitEvent args)
    {
        if (!CheckBlackList(args.Target, component.BlackListComponents)) return;
        if (!CheckWhiteList(args.Target, component.WhiteListComponents)) return;

        var components = args.Shooter == args.Target
            ? component.SelfUseComponents == null
                ? component.Components
                : component.SelfUseComponents
            : component.Components;

        foreach (var componentRegistry in components)
        {
            var addedComponent = componentRegistry.Value.Component;
            var copy = _serializationManager.CreateCopy(addedComponent, notNullableOverride: true);

            AddComp(args.Target, copy, component.Hard);
        }
    }

    private void OnMeleeHit(EntityUid uid, MedievalAddComponentOnInteractComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit) return;

        foreach (var target in args.HitEntities)
        {
            if (!CheckBlackList(target, component.BlackListComponents)) continue;
            if (!CheckWhiteList(target, component.WhiteListComponents)) continue;

            var components = args.User == target
                ? component.SelfUseComponents == null
                    ? component.Components
                    : component.SelfUseComponents
                : component.Components;

            foreach (var componentRegistry in components)
            {
                var addedComponent = componentRegistry.Value.Component;
                var copy = _serializationManager.CreateCopy(addedComponent, notNullableOverride: true);

                AddComp(target, copy, component.Hard);
            }
        }
    }

    #region Helpers

    private bool CheckBlackList(EntityUid uid, HashSet<string> blackListComponents)
    {
        foreach (var componentRegistry in blackListComponents)
        {
            var checkedComponent = _componentFactory.GetRegistration(componentRegistry).Type;

            if (!HasComp(uid, checkedComponent)) continue;

            return false;
        }

        return true;
    }

    private bool CheckWhiteList(EntityUid uid, HashSet<string>? whiteListComponents)
    {
        if (whiteListComponents == null) return true;

        foreach (var componentRegistry in whiteListComponents)
        {
            var checkedComponent = _componentFactory.GetRegistration(componentRegistry).Type;

            if (HasComp(uid, checkedComponent)) continue;

            return false;
        }

        return true;
    }

    #endregion
}
