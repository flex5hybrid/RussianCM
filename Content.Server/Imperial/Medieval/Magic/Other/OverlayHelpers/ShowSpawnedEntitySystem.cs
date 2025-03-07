using Content.Shared.Imperial.Medieval.Magic.Overlays;

namespace Content.Server.Imperial.Medieval.Magic.Overlays;


public sealed partial class ShowSpawnedEntitySystem : SharedShowSpawnedEntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SpawnedEntitySpriteRotateEvent>(OnRotate);
    }

    private void OnRotate(SpawnedEntitySpriteRotateEvent args)
    {
        var action = GetEntity(args.Action);

        ShowSpawnedEntityComponent? component = null;

        if (!HasComp<ShowSpawnedEntityComponent>(action)) return;
        if (!Resolve(action, ref component)) return;

        component.Rotation = args.Rotation;

        Dirty(action, component);
    }
}
