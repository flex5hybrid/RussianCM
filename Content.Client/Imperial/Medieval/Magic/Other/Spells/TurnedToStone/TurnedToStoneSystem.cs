using Content.Client.Interactable.Components;
using Content.Client.Popups;
using Content.Shared.Imperial.Medieval.Magic.TurnedToStone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Medieval.Magic.TurnedToStone;


public sealed partial class TurnedToStoneSystem : SharedTurnedToStoneSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;


    private ShaderInstance? _shader;
    private Dictionary<EntityUid, ShaderInstance?> _cachedShaders = new();


    public override void Initialize()
    {
        base.Initialize();

        _shader = _prototypeManager.Index<ShaderPrototype>("TurnedToStone").InstanceUnique();
    }

    protected override void OnStartup(EntityUid uid, TurnedToStoneComponent component, ComponentStartup args)
    {
        base.OnStartup(uid, component, args);

        if (!CanTurnToStone(uid)) return;
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent)) return;

        if (TryComp<InteractionOutlineComponent>(uid, out var interactionOutlineComponent))
            interactionOutlineComponent.OnMouseLeave(uid);

        _popupSystem.PopupEntity(Loc.GetString("turned-to-stone"), uid, uid, Shared.Popups.PopupType.LargeCaution);
        _cachedShaders.TryAdd(uid, spriteComponent.PostShader?.Duplicate());

        spriteComponent.Color = Color.White;
        spriteComponent.PostShader = _shader;
    }

    protected override void OnShutdown(EntityUid uid, TurnedToStoneComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component, args);

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent)) return;

        _cachedShaders.TryGetValue(uid, out var shader);

        spriteComponent.PostShader = shader;

        _cachedShaders.Remove(uid);
    }
}
