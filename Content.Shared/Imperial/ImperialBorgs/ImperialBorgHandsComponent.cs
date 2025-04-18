using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.GameStates;

namespace Content.Shared.Borgs.Imperial;

[RegisterComponent, NetworkedComponent]
public sealed partial class BorgHandsImperialSecurityComponent : Component // Компонент для СБ борга Империала
{
    // Этот компонент не содержит никаких данных, он просто служит маркером.
}

public sealed partial class BorgHandsImperialSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgHandsImperialSecurityComponent, PickupAttemptEvent>(OnPickupAttempt);
    }


    public void OnPickupAttempt(EntityUid uid, BorgHandsImperialSecurityComponent component, PickupAttemptEvent args)
    {
        if (_tagSystem.HasAnyTag(args.Item, "SecurityBorgItem")) return;

        args.Cancel();
    }

}