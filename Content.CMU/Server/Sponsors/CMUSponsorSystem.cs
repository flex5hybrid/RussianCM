using System.Text.Json;
using Content.Server._RMC14.Figurines;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Database;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Dogtags;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.CMU14.Sponsors;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Sponsors;

public sealed class CMUSponsorSystem : EntitySystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LinkAccountManager _link = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IComponentFactory _components = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private SharedUniformAccessorySystem _accessories = default!;

    private readonly HashSet<NetUserId> _saving = [];
    private readonly Dictionary<Guid, string> _approvedDescriptions = [];

    public override void Initialize()
    {
        SubscribeNetworkEvent<CMUSponsorSaveSettingsEvent>(OnSaveSettings);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawn);
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostAttached);
        SubscribeLocalEvent<CMUSponsorEngravingComponent, ExaminedEvent>(OnEngravingExamined);
        SubscribeLocalEvent<PatronFigurineComponent, ExaminedEvent>(OnFigurineExamined);
        _link.PatronUpdated += OnPatronUpdated;
        _link.PatronsReloaded += ReloadApprovals;
        ReloadApprovals();
    }

    public override void Shutdown()
    {
        _link.PatronUpdated -= OnPatronUpdated;
        _link.PatronsReloaded -= ReloadApprovals;
        base.Shutdown();
    }

    private async void OnSaveSettings(CMUSponsorSaveSettingsEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (ev.Settings == null || !_saving.Add(session.UserId))
            return;
        var success = false;
        try
        {
            success = await _link.SaveSponsorSettings(session.UserId, ev.Settings);
        }
        catch (Exception e)
        {
            Log.Error($"Error saving sponsor preferences for {session.UserId}: {e}");
        }
        finally
        {
            _saving.Remove(session.UserId);
            RaiseNetworkEvent(new CMUSponsorSaveResultEvent(success), session);
        }
    }

    private void OnGhostAttached(Entity<GhostComponent> ent, ref PlayerAttachedEvent args)
    {
        if (TryComp(ent, out ActorComponent? actor))
            ApplyGhost(ent, _link.GetConnectedPatron(actor.PlayerSession.UserId));
    }

    private void OnPatronUpdated((NetUserId Id, Content.Shared._RMC14.LinkAccount.SharedRMCPatronFull Patron) update)
    {
        var ghosts = EntityQueryEnumerator<GhostComponent, ActorComponent>();
        while (ghosts.MoveNext(out var uid, out _, out var actor))
        {
            if (actor.PlayerSession.UserId == update.Id)
                ApplyGhost(uid, update.Patron);
        }
    }

    private void ApplyGhost(EntityUid ghost, Content.Shared._RMC14.LinkAccount.SharedRMCPatronFull? patron)
    {
        if (HasComp<RMCAdminGhostComponent>(ghost))
            return;
        var appearance = EnsureComp<CMUSponsorGhostComponent>(ghost);
        appearance.State = patron is { Tier.GhostColor: true, SponsorSettings: { } settings }
            && CMUSponsorCatalog.Ghosts.TryGetValue(settings.Ghost, out var state) ? state : "animated";
        Dirty(ghost, appearance);
    }

    private void OnSpawn(PlayerSpawnCompleteEvent ev) => ApplySpawnCosmetics(ev.Mob, ev.Player.UserId);

    public void ApplySpawnCosmetics(EntityUid mob, NetUserId user)
    {
        if (!HasComp<MarineComponent>(mob) || !HasComp<BodyComponent>(mob) || HasComp<XenoComponent>(mob))
            return;
        var patron = _link.GetConnectedPatron(user);
        if (patron?.SponsorSettings is not { } settings)
            return;
        if (patron.Tier is { NamedItems: true })
        {
            if (settings.Inscription.Length > 0 && _inventory.TryGetSlotEntity(mob, "id", out var id)
                && HasComp<TakeableTagsComponent>(id))
            {
                var engraving = EnsureComp<CMUSponsorEngravingComponent>(id.Value);
                engraving.Text = settings.Inscription;
            }
            if (CMUSponsorCatalog.Patches.ContainsKey(settings.Patch))
            {
                var patch = Spawn(CMUSponsorCatalog.PatchPrototype(settings.Patch), Transform(mob).Coordinates);
                if (!_accessories.TryInsertToValidSlot(patch, mob))
                    Deliver(mob, patch);
            }
        }

        if (CMUSponsorCatalog.CanUseCape(patron.Tier, settings.Cape))
        {
            var cape = Spawn($"CMUSponsorCape{settings.Cape}", Transform(mob).Coordinates);
            var cosmetics = Comp<CMUSponsorCapeComponent>(cape);
            if (CMUSponsorCatalog.CanCustomizeCape(patron.Tier))
            {
                cosmetics.Trim = settings.Trim;
                cosmetics.Emblem = settings.Emblem;
                Dirty(cape, cosmetics);
            }
            // Neck slots may already hold role equipment; leave it in place and deliver to storage/hands.
            if (!_inventory.TryEquip(mob, cape, "neck", silent: true))
                Deliver(mob, cape);
        }

        if (patron.Tier is { Priority: 1 } && patron.CustomItem.Length > 0
            && _prototypes.TryIndex<EntityPrototype>(patron.CustomItem, out var prototype)
            && prototype.TryComp(out CMUSponsorCustomItemComponent? approved, _components))
        {
            if (approved.Owner == user.UserId)
                Deliver(mob, Spawn(patron.CustomItem, Transform(mob).Coordinates));
        }
    }

    private void Deliver(EntityUid user, EntityUid item)
    {
        var slots = _inventory.GetSlotEnumerator(user, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } bag && TryComp(bag, out StorageComponent? storage)
                && _storage.Insert(bag, item, out _, storageComp: storage))
                return;
        }
        _hands.TryPickupAnyHand(user, item, false);
        // Spawned at the player's feet, so failure never leaves an item stranded in nullspace.
    }

    private void OnEngravingExamined(Entity<CMUSponsorEngravingComponent> ent, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("cmu-sponsor-inscription-examine", ("text", ent.Comp.Text)));
    }

    private void OnFigurineExamined(Entity<PatronFigurineComponent> ent, ref ExaminedEvent args)
    {
        if (Guid.TryParse(ent.Comp.Id, out var owner) && _approvedDescriptions.TryGetValue(owner, out var text))
            args.PushText(text);
    }

    public async void ReloadApprovals()
    {
        try
        {
            var preferences = await _db.GetAllCMUSponsorPreferences();
            _approvedDescriptions.Clear();
            foreach (var row in preferences)
            {
                if (row.ApprovedFigurineDescription.Length > 0)
                    _approvedDescriptions[row.PlayerId] = row.ApprovedFigurineDescription;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error loading approved figurine descriptions: {e}");
        }
    }
}

/// <summary>Only host-approved prototypes marked for a specific owner can be issued as custom rewards.</summary>
[RegisterComponent]
public sealed partial class CMUSponsorCustomItemComponent : Component
{
    [DataField(required: true)]
    public Guid Owner;
}
