using Content.Server._RMC14.Humanoid; // RuMC edit
using Content.Server.Access.Systems;
using Content.Shared._RMC14.Marines;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines;

public sealed partial class MarineSystem : SharedMarineSystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawning, after: [typeof(RMCHumanoidSystem)]); // RuMC edit
    }

    private void OnPlayerSpawning(PlayerSpawnCompleteEvent args) // RuMC edit
    {
        if (args.JobId is not { } jobId)
            return;

        if (!_prototypes.TryIndex<JobPrototype>(jobId, out var job) || !job.IsCM)
            return;

        if (!HasComp<MarineComponent>(args.Mob)) // RuMC edit
            return;

        SpriteSpecifier? icon = null;
        if (job.HasIcon && _prototypes.TryIndex(job.Icon, out var jobIcon))
            icon = jobIcon.Icon;

        MakeMarine(args.Mob, icon);

        if (!_idCard.TryFindIdCard(args.Mob, out var card)) // RuMC edit
            return;

        _idCard.TryChangeOriginalOwner(card, args.Mob); // RuMC edit
    }
}
