using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Imperial.PhaseSpace;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared.Imperial.Medieval.Magic.MedievalSpellTeleportEffect;


public abstract partial class SharedMedievalSpellTeleportEffectSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalSpellTeleportEffectComponent, MedievalBeforeCastSpellEvent>(OnBeforeCast);
        SubscribeLocalEvent<MedievalSpellTeleportEffectComponent, MedievalAfterCastSpellEvent>(OnAfterCast);
    }

    protected virtual void OnBeforeCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalBeforeCastSpellEvent args)
    {
        var origin = _transformSystem.GetMapCoordinates(args.Performer);
        var target = _transformSystem.ToMapCoordinates(args.Target);

        if (component.CheckOccluded && !_examineSystem.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            _popupSystem.PopupClient(Loc.GetString("dash-ability-cant-see"), args.Performer, args.Performer);
            _actionsSystem.ClearCooldown(uid);

            args.Cancelled = true;

            return;
        }

        EnsureComp<PhaseSpaceFadeDistortionComponent>(args.Performer);
    }

    protected virtual void OnAfterCast(EntityUid uid, MedievalSpellTeleportEffectComponent component, ref MedievalAfterCastSpellEvent args)
    {
        EnsureComp<PhaseSpaceFadeDistortionComponent>(args.Performer);
    }
}
