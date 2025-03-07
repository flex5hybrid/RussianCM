
using System.Linq;
using Content.Client.Imperial.TargetOverlay;
using Content.Shared.Imperial.Medieval.Magic.MedievalTargetOverlayOnAiming;
using Content.Shared.Imperial.Medieval.Magic.Overlays;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Medieval.Magic.MedievalTargetOverlayOnAiming;


public sealed partial class MedievalTargetOverlayOnAimingSystem : EntitySystem
{
    [Dependency] private readonly TargetOverlaySystem _targetOverlaySystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalTargetOverlayOnAimingComponent, MedievalActionStartTargetingEvent>(OnStartTargeting);
        SubscribeLocalEvent<MedievalTargetOverlayOnAimingComponent, MedievalActionStopTargetingEvent>(OnStopTargeting);
    }

    private void OnStartTargeting(EntityUid uid, MedievalTargetOverlayOnAimingComponent component, MedievalActionStartTargetingEvent args)
    {
        _targetOverlaySystem.StartTargeting(
            args.ActionOwner,
            uid,
            component.MaxTargetCount,
            GetComponentsNames(component.WhiteListComponents),
            GetComponentsNames(component.BlackListComponents)
        );
    }

    private void OnStopTargeting(EntityUid uid, MedievalTargetOverlayOnAimingComponent component, MedievalActionStopTargetingEvent args)
    {
        _targetOverlaySystem.StopTargeting(args.ActionOwner);
    }

    #region Helpers

    private HashSet<string> GetComponentsNames(ComponentRegistry comps) => comps.Select(comp => comp.Key).ToHashSet();

    #endregion
}
