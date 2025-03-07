using Content.Shared.Interaction;
using Robust.Server.Audio;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Imperial.Medieval.Magic.MedievalAddComponentOnInteract;


public sealed partial class MedievalAddComponentOnInteractSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalAddComponentOnInteractComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, MedievalAddComponentOnInteractComponent component, AfterInteractEvent args)
    {
        if (args.Target == null) return;
        if (!CheckBlackList(args.Target.Value, component.BlackListComponents)) return;
        if (!CheckWhiteList(args.Target.Value, component.WhiteListComponents)) return;

        component.UseCount += 1;

        var components = args.User == args.Target
            ? component.SelfUseComponents == null
                ? component.Components
                : component.SelfUseComponents
            : component.Components;

        foreach (var componentRegistry in components)
        {
            var addedComponent = componentRegistry.Value.Component;
            var copy = _serializationManager.CreateCopy(addedComponent, notNullableOverride: true);

            AddComp(args.Target.Value, copy, component.Hard);
        }

        _audioSystem.PlayPvs(component.SoundOnInteract, args.User);

        if (component.UseCount >= component.RemainingUses)
            QueueDel(uid);
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
