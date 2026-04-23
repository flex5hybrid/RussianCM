using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Humanoid;

namespace Content.Server._RMC14.Synth;

public sealed class SynthSystem : SharedSynthSystem
{
    private const string GrenadeTag = "Grenade";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TagComponent, UseInHandEvent>(OnTaggedUseInHand, before: [typeof(TriggerSystem)]);
    }

    protected override void MakeSynth(Entity<SynthComponent> ent)
    {
        base.MakeSynth(ent);

        // Remove DNA and Fingerprint components if present
        RemComp<Content.Shared.Forensics.Components.DnaComponent>(ent.Owner);
        RemComp<Content.Shared.Forensics.Components.FingerprintComponent>(ent.Owner);

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable))
            _damageable.SetDamageModifierSetId(ent.Owner, ent.Comp.NewDamageModifier, damageable);

        if (TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream)) // These TryComps are so tests don't fail
        {
            // This makes it so the synth doesn't take bloodloss damage.
            _bloodstream.SetBloodLossThreshold((ent, bloodstream), 0f);
            _bloodstream.ChangeBloodReagent((ent, bloodstream), ent.Comp.NewBloodReagent);
        }

        var repOverrideComp = EnsureComp<RMCHumanoidRepresentationOverrideComponent>(ent);
        if (!ent.Comp.HideGeneration)
        {
            repOverrideComp.Age = ent.Comp.Generation;
            repOverrideComp.Species = ent.Comp.SpeciesName;

        }
        Dirty(ent, repOverrideComp);

        // If UseHumanHealthIcons is true, use the same health icons as a human
        if (ent.Comp.UseHumanHealthIcons)
        {
            ent.Comp.HealthIconOverrides = new()
            {
                [RMCHealthIconTypes.Healthy] = "CMHealthIconHealthy",
                [RMCHealthIconTypes.DeadDefib] = "CMHealthIconDeadDefib",
                [RMCHealthIconTypes.DeadClose] = "CMHealthIconDeadClose",
                [RMCHealthIconTypes.DeadAlmost] = "CMHealthIconDeadAlmost",
                [RMCHealthIconTypes.DeadDNR] = "CMHealthIconDeadDNR",
                [RMCHealthIconTypes.Dead] = "CMHealthIconDead",
                [RMCHealthIconTypes.HCDead] = "CMHealthIconHCDead",
            };
        }

        if (!HasComp<BodyComponent>(ent.Owner))
            return;

        var organComps = _body.GetBodyOrganEntityComps<OrganComponent>(ent.Owner);

        foreach (var organ in organComps)
        {
            Del(organ); // Synths do not metabolize chems or breathe
        }

        var headSlots = _body.GetBodyChildrenOfType(ent, BodyPartType.Head);

        foreach (var part in headSlots)
        {
            if (!ent.Comp.ChangeBrain)
                return;
            var newBrain = SpawnNextToOrDrop(ent.Comp.NewBrain, ent);
            _body.AddOrganToFirstValidSlot(part.Id, newBrain);
        }
    }

    private void OnTaggedUseInHand(Entity<TagComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SynthComponent>(args.User))
            return;

        if (!_tags.HasTag(ent.Owner, GrenadeTag))
            return;

        if (!HasComp<OnUseTimerTriggerComponent>(ent.Owner))
            return;

        DoSynthUnableToUsePopup(args.User, ent.Owner);
        args.Handled = true;
    }
}
