using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.ImperialStore;

public abstract class SharedImperialStoreSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImperialStoreComponent, ActivatableUIOpenAttemptEvent>(OnStoreOpenAttempt);
    }

    private void OnStoreOpenAttempt(EntityUid uid, ImperialStoreComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!component.OwnerOnly) return;
        if (component.AccountOwner == args.User) return;

        args.Cancel();

        if (!_timing.IsFirstTimePredicted) return;
        if (_timing.CurTick == component.LastOpenTick) return;

        component.LastOpenTick = _timing.CurTick;

        _popupSystem.PopupClient(Loc.GetString(component.CannotAccessStoreText, ("store", uid)), args.User, args.User);
    }
}
