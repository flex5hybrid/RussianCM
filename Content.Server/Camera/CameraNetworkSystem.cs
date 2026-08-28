using System.Numerics;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.SurveillanceCamera;
using Content.Shared.Camera;
using Content.Shared._RMC14.Camera;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Camera;

public sealed class CameraNetworkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private readonly Dictionary<ProtoId<CameraNetworkPrototype>, HashSet<EntityUid>> _members = [];
    private readonly Dictionary<ProtoId<CameraNetworkPrototype>, HashSet<EntityUid>> _receivers = [];
    private readonly Dictionary<EntityUid,
        Dictionary<ProtoId<CameraNetworkPrototype>, HashSet<EntityUid>>> _runtimeGrants = [];
    private readonly Dictionary<EntityUid, HashSet<(EntityUid Receiver, ProtoId<CameraNetworkPrototype> Network)>>
        _grantsBySource = [];
    private readonly Dictionary<EntityUid, EntityUid> _pendingMarkerReceivers = [];
    private readonly Dictionary<EntityUid, TimeSpan> _mobileMarkerUpdates = [];
    private readonly HashSet<EntityUid> _legacyMembers = [];
    private readonly HashSet<EntityUid> _legacyReceivers = [];
    private readonly Dictionary<(EntityUid Receiver, EntityUid Viewer), MapViewSubscription> _mapViewSubscriptions = [];
    private readonly Dictionary<(EntityUid Grid, ICommonSession Session), MapGridViewSubscription>
        _mapGridViewSubscriptions = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CameraNetworkMemberComponent, ComponentStartup>(OnMemberStartup);
        SubscribeLocalEvent<CameraNetworkMemberComponent, ComponentShutdown>(OnMemberShutdown);
        SubscribeLocalEvent<CameraNetworkReceiverComponent, ComponentStartup>(OnReceiverStartup);
        SubscribeLocalEvent<CameraNetworkReceiverComponent, ComponentShutdown>(OnReceiverShutdown);
        SubscribeLocalEvent<CameraNetworkReceiverComponent, CameraNetworkGrantRequestEvent>(OnGrantRequest);
        SubscribeLocalEvent<RMCCameraComponent, RMCLegacyCameraMapInitEvent>(OnLegacyCameraMapInit);
        SubscribeLocalEvent<RMCCameraComputerComponent, RMCLegacyCameraComputerMapInitEvent>(OnLegacyComputerMapInit);
        SubscribeLocalEvent<RMCCameraComponent, RMCLegacyCameraIdChangedEvent>(OnLegacyCameraIdChanged);
        SubscribeLocalEvent<CameraMapMarkerComponent, ComponentStartup>(OnMarkerStartup);
        SubscribeLocalEvent<CameraMapMarkerComponent, ComponentShutdown>(OnMarkerShutdown);
        SubscribeLocalEvent<CameraMapMarkerComponent, MoveEvent>(OnMarkerMove);
        SubscribeLocalEvent<CameraMapMarkerComponent, EntityRenamedEvent>(OnMarkerRenamed);
        SubscribeLocalEvent<CameraMapMarkerComponent, PowerChangedEvent>(OnMarkerPowerChanged);
        SubscribeLocalEvent<CameraMapMarkerComponent, EntityPausedEvent>(OnMarkerPaused);
        SubscribeLocalEvent<CameraMapMarkerComponent, EntityUnpausedEvent>(OnMarkerUnpaused);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnMemberStartup(Entity<CameraNetworkMemberComponent> ent, ref ComponentStartup args)
    {
        IndexMember(ent.Owner, ent.Comp.Networks);
        NotifyReceivers(ent.Comp.Networks, ent.Owner);
    }

    private void OnMemberShutdown(Entity<CameraNetworkMemberComponent> ent, ref ComponentShutdown args)
    {
        UnindexMember(ent.Owner, ent.Comp.Networks);
        NotifyReceivers(ent.Comp.Networks, ent.Owner);
    }

    private void OnReceiverStartup(Entity<CameraNetworkReceiverComponent> ent, ref ComponentStartup args)
    {
        IndexReceiver(ent.Owner, GetEffectiveNetworks(ent.Owner, ent.Comp));
    }

    private void OnReceiverShutdown(Entity<CameraNetworkReceiverComponent> ent, ref ComponentShutdown args)
    {
        _pendingMarkerReceivers.Remove(ent.Owner);
        ClearMapViewSubscriptions(ent.Owner);
        UnindexReceiver(ent.Owner, GetEffectiveNetworks(ent.Owner, ent.Comp));
        CleanupReceiverGrants(ent.Owner);
    }

    private void OnGrantRequest(Entity<CameraNetworkReceiverComponent> ent, ref CameraNetworkGrantRequestEvent args)
    {
        if (args.Grant)
            GrantNetwork(ent.Owner, args.Network, args.Source);
        else
            RevokeNetwork(ent.Owner, args.Network, args.Source);
    }

    private void OnLegacyCameraMapInit(Entity<RMCCameraComponent> ent, ref RMCLegacyCameraMapInitEvent args)
    {
        if (HasComp<CameraNetworkMemberComponent>(ent))
            return;

        var member = EnsureComp<CameraNetworkMemberComponent>(ent);
        member.SourceKinds = CameraSourceKinds.Rmc;
        SetMemberNetworks(ent, ValidateLegacyNetworks(ent.Owner, ent.Comp.Id));
        _legacyMembers.Add(ent.Owner);
    }

    private void OnLegacyComputerMapInit(Entity<RMCCameraComputerComponent> ent, ref RMCLegacyCameraComputerMapInitEvent args)
    {
        if (HasComp<CameraNetworkReceiverComponent>(ent))
            return;

        var receiver = EnsureComp<CameraNetworkReceiverComponent>(ent);
        receiver.SupportedSources = CameraSourceKinds.Rmc;
        SetReceiverNetworks(ent, ValidateLegacyNetworks(ent.Owner, ent.Comp.ProtoIds));
        _legacyReceivers.Add(ent.Owner);
    }

    private void OnLegacyCameraIdChanged(Entity<RMCCameraComponent> ent, ref RMCLegacyCameraIdChangedEvent args)
    {
        if (!_legacyMembers.Contains(ent.Owner) || !TryComp(ent, out CameraNetworkMemberComponent? member))
            return;

        SetMemberNetworks(ent, ValidateLegacyNetworks(ent.Owner, args.NewId));
    }

    private HashSet<ProtoId<CameraNetworkPrototype>> ValidateLegacyNetworks(
        EntityUid entity,
        IEnumerable<EntProtoId> legacyIds)
    {
        var networks = new HashSet<ProtoId<CameraNetworkPrototype>>();
        foreach (var legacyId in legacyIds)
        {
            if (_prototypeManager.HasIndex<CameraNetworkPrototype>(legacyId))
            {
                networks.Add(new ProtoId<CameraNetworkPrototype>(legacyId.Id));
                continue;
            }

            Log.Warning($"RMC camera prototype '{MetaData(entity).EntityPrototype?.ID ?? "unknown"}' has invalid legacy camera network '{legacyId}'.");
        }

        return networks;
    }

    private HashSet<ProtoId<CameraNetworkPrototype>> ValidateLegacyNetworks(EntityUid entity, EntProtoId? legacyId)
    {
        return legacyId == null ? [] : ValidateLegacyNetworks(entity, [legacyId.Value]);
    }

    private void OnMarkerStartup(Entity<CameraMapMarkerComponent> ent, ref ComponentStartup args)
    {
        QueueMarkerChange(ent);
    }

    private void OnMarkerShutdown(Entity<CameraMapMarkerComponent> ent, ref ComponentShutdown args)
    {
        _mobileMarkerUpdates.Remove(ent.Owner);
        QueueMarkerChange(ent);
    }

    private void OnMarkerMove(Entity<CameraMapMarkerComponent> ent, ref MoveEvent args)
    {
        QueueMarkerChange(ent);
    }

    private void OnMarkerRenamed(Entity<CameraMapMarkerComponent> ent, ref EntityRenamedEvent args)
    {
        QueueMarkerChange(ent);
    }

    private void OnMarkerPowerChanged(Entity<CameraMapMarkerComponent> ent, ref PowerChangedEvent args)
    {
        QueueMarkerChange(ent);
    }

    private void OnMarkerPaused(Entity<CameraMapMarkerComponent> ent, ref EntityPausedEvent args)
    {
        QueueMarkerChange(ent);
    }

    private void OnMarkerUnpaused(Entity<CameraMapMarkerComponent> ent, ref EntityUnpausedEvent args)
    {
        QueueMarkerChange(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (marker, due) in _mobileMarkerUpdates.ToArray())
        {
            if (due > _timing.CurTime)
                continue;

            _mobileMarkerUpdates.Remove(marker);
            QueueAffectedReceivers(marker);
        }

        var pendingMarkerReceivers = _pendingMarkerReceivers.ToArray();
        // Preserve marker changes queued synchronously by receiver event handlers for the next update.
        _pendingMarkerReceivers.Clear();

        foreach (var (receiver, marker) in pendingMarkerReceivers)
            NotifyReceiver(receiver, marker, CameraReceiverChangeKind.Marker);
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent args)
    {
        var entity = args.Entity.Owner;
        _legacyMembers.Remove(entity);
        _legacyReceivers.Remove(entity);
        _pendingMarkerReceivers.Remove(entity);
        ClearMapViewSubscriptions(entity);
        ClearMapViewSubscriptionsForViewer(entity);
        CleanupSourceGrants(entity);

        if (TryComp(entity, out CameraNetworkReceiverComponent? receiver))
        {
            UnindexReceiver(entity, GetEffectiveNetworks(entity, receiver));
            CleanupReceiverGrants(entity);
        }
    }

    public HashSet<EntityUid> GetAccessibleCameras(Entity<CameraNetworkReceiverComponent> receiver)
    {
        var cameras = new HashSet<EntityUid>();

        foreach (var network in GetEffectiveNetworks(receiver.Owner, receiver.Comp))
        {
            if (!_members.TryGetValue(network, out var members))
                continue;

            foreach (var member in members)
            {
                if (CanAccess(receiver.Owner, member))
                    cameras.Add(member);
            }
        }

        return cameras;
    }

    public CameraMapUiState BuildMapState(EntityUid receiver)
    {
        EntityUid? consoleGrid = null;
        if (TryComp(receiver, out TransformComponent? receiverTransform))
            consoleGrid = receiverTransform.GridUid ?? receiverTransform.MapUid;

        if (!TryComp(receiver, out CameraNetworkReceiverComponent? receiverComponent))
            return new CameraMapUiState(GetNetEntity(consoleGrid), []);

        var grouped = new Dictionary<EntityUid, List<(EntityUid Camera, Vector2 Position, string Name, CameraMapMarkerStatus Status)>>();
        foreach (var camera in GetAccessibleCameras((receiver, receiverComponent)))
        {
            if (TerminatingOrDeleted(camera)
                || Paused(camera)
                || !TryComp(camera, out CameraMapMarkerComponent? marker)
                || !marker.Visible
                || !TryComp(camera, out TransformComponent? xform))
            {
                continue;
            }

            var grid = xform.GridUid ?? xform.MapUid;
            if (grid == null || TerminatingOrDeleted(grid.Value))
                continue;

            if (!grouped.TryGetValue(grid.Value, out var markers))
            {
                // Colony and standalone maps are not necessarily registered as station grids,
                // so NavMapSystem never sees StationGridAddedEvent for them. Generate the
                // geometry lazily when an accessible camera exposes such a grid in the UI.
                if (TryComp<MapGridComponent>(grid.Value, out var mapGrid))
                    _navMap.EnsureNavMapIfMissing((grid.Value, mapGrid));

                markers = [];
                grouped.Add(grid.Value, markers);
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            var position = Vector2.Transform(worldPosition, _transform.GetInvWorldMatrix(grid.Value));
            var status = IsAvailable(camera)
                    ? CameraMapMarkerStatus.Active
                    : CameraMapMarkerStatus.Inactive;
            markers.Add((camera, position, Name(camera), status));
        }

        var grids = grouped
            .OrderBy(pair => pair.Key != consoleGrid)
            .ThenBy(pair => Name(pair.Key), StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Id)
            .Select(pair => new CameraMapGridUiData(
                GetNetEntity(pair.Key),
                Name(pair.Key),
                pair.Value
                    .OrderBy(marker => marker.Name, StringComparer.Ordinal)
                    .ThenBy(marker => marker.Camera.Id)
                    .Select(marker => new CameraMapMarkerUiData(
                        GetNetEntity(marker.Camera), marker.Position, marker.Name, marker.Status))
                    .ToList()))
            .ToList();

        return new CameraMapUiState(GetNetEntity(consoleGrid), grids);
    }

    public void SyncMapViewSubscriptions(EntityUid receiver, EntityUid viewer, CameraMapUiState state)
    {
        if (!TryComp(viewer, out ActorComponent? actor))
        {
            ClearMapViewSubscriptions(receiver, viewer);
            return;
        }

        var key = (receiver, viewer);
        if (_mapViewSubscriptions.TryGetValue(key, out var existing) && existing.Session != actor.PlayerSession)
        {
            ReleaseMapViewSubscription(existing);
            existing = null;
        }

        var desired = new HashSet<EntityUid>();
        foreach (var gridData in state.Grids)
        {
            if (state.ConsoleGrid == gridData.Grid)
                continue;

            if (TryGetEntity(gridData.Grid, out var grid) && grid is { } gridUid && !TerminatingOrDeleted(gridUid))
                desired.Add(gridUid);
        }

        existing ??= new MapViewSubscription(actor.PlayerSession);

        foreach (var grid in existing.Grids.Except(desired).ToArray())
        {
            existing.Grids.Remove(grid);
            ReleaseMapGrid(grid, existing.Session);
        }

        foreach (var grid in desired.Except(existing.Grids))
        {
            existing.Grids.Add(grid);
            AcquireMapGrid(grid, existing.Session);
        }

        if (existing.Grids.Count == 0)
            _mapViewSubscriptions.Remove(key);
        else
            _mapViewSubscriptions[key] = existing;
    }

    public void ClearMapViewSubscriptions(EntityUid receiver, EntityUid viewer)
    {
        var key = (receiver, viewer);
        if (_mapViewSubscriptions.Remove(key, out var subscription))
            ReleaseMapViewSubscription(subscription);
    }

    public void ClearMapViewSubscriptions(EntityUid receiver)
    {
        foreach (var (key, subscription) in _mapViewSubscriptions.ToArray())
        {
            if (key.Receiver != receiver)
                continue;

            _mapViewSubscriptions.Remove(key);
            ReleaseMapViewSubscription(subscription);
        }
    }

    public void ClearMapViewSubscriptionsForViewer(EntityUid viewer)
    {
        foreach (var (key, subscription) in _mapViewSubscriptions.ToArray())
        {
            if (key.Viewer != viewer)
                continue;

            _mapViewSubscriptions.Remove(key);
            ReleaseMapViewSubscription(subscription);
        }
    }

    private void AcquireMapGrid(EntityUid grid, ICommonSession session)
    {
        var key = (grid, session);
        if (_mapGridViewSubscriptions.TryGetValue(key, out var subscription))
        {
            subscription.Count++;
            return;
        }

        // ViewSubscriberSystem stores a global set rather than source-aware
        // subscriptions. Use our own view entity so closing a camera UI can
        // never remove a subscription owned by another feature.
        var view = Spawn(null, new EntityCoordinates(grid, Vector2.Zero));
        _mapGridViewSubscriptions[key] = new MapGridViewSubscription(view);
        _viewSubscriber.AddViewSubscriber(view, session);
    }

    private void ReleaseMapGrid(EntityUid grid, ICommonSession session)
    {
        var key = (grid, session);
        if (!_mapGridViewSubscriptions.TryGetValue(key, out var subscription))
            return;

        if (subscription.Count > 1)
        {
            subscription.Count--;
            return;
        }

        _mapGridViewSubscriptions.Remove(key);
        _viewSubscriber.RemoveViewSubscriber(subscription.View, session);
        QueueDel(subscription.View);
    }

    private void ReleaseMapViewSubscription(MapViewSubscription subscription)
    {
        foreach (var grid in subscription.Grids)
            ReleaseMapGrid(grid, subscription.Session);
    }

    public bool CanAccess(EntityUid receiver, EntityUid camera)
    {
        if (!TryComp(receiver, out CameraNetworkReceiverComponent? receiverComponent)
            || !TryComp(camera, out CameraNetworkMemberComponent? memberComponent)
            || (receiverComponent.SupportedSources & memberComponent.SourceKinds) == CameraSourceKinds.None)
        {
            return false;
        }

        foreach (var network in GetEffectiveNetworks(receiver, receiverComponent))
        {
            // ComponentShutdown leaves the component readable while its lifecycle
            // callback runs. The index is therefore the authoritative indicator
            // that this source is still a live member of the logical network.
            if (memberComponent.Networks.Contains(network)
                && _members.TryGetValue(network, out var members)
                && members.Contains(camera))
                return true;
        }

        return false;
    }

    public bool SetMapVisibility(EntityUid camera, bool visible)
    {
        if (!TryComp(camera, out CameraMapMarkerComponent? marker)
            || marker.Visible == visible)
        {
            return false;
        }

        marker.Visible = visible;
        QueueMarkerChange((camera, marker));
        return true;
    }

    public bool IsMapVisible(EntityUid camera)
    {
        return TryComp(camera, out CameraMapMarkerComponent? marker) && marker.Visible;
    }

    public bool AddNetwork(EntityUid member, ProtoId<CameraNetworkPrototype> network)
    {
        if (!TryComp(member, out CameraNetworkMemberComponent? component)
            || !component.Networks.Add(network))
        {
            return false;
        }

        IndexMember(member, [network]);
        NotifyReceivers([network], member);
        return true;
    }

    public bool RemoveNetwork(EntityUid member, ProtoId<CameraNetworkPrototype> network)
    {
        if (!TryComp(member, out CameraNetworkMemberComponent? component)
            || !component.Networks.Remove(network))
        {
            return false;
        }

        UnindexMember(member, [network]);
        NotifyReceivers([network], member);
        return true;
    }

    public bool SetMemberNetworks(EntityUid member, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        return SetMemberNetworksBatch(
            new Dictionary<EntityUid, IReadOnlyCollection<ProtoId<CameraNetworkPrototype>>>
            {
                [member] = networks.ToHashSet(),
            });
    }

    public bool SetMemberNetworksBatch(
        IReadOnlyDictionary<EntityUid, IReadOnlyCollection<ProtoId<CameraNetworkPrototype>>> updates)
    {
        var changed = new List<(
            EntityUid Member,
            CameraNetworkMemberComponent Component,
            HashSet<ProtoId<CameraNetworkPrototype>> Updated)>();
        var affected = new HashSet<ProtoId<CameraNetworkPrototype>>();

        foreach (var (member, networks) in updates)
        {
            if (!TryComp(member, out CameraNetworkMemberComponent? component))
                return false;

            var updated = networks.ToHashSet();
            if (component.Networks.SetEquals(updated))
                continue;

            affected.UnionWith(component.Networks);
            affected.UnionWith(updated);
            changed.Add((member, component, updated));
        }

        if (changed.Count == 0)
            return false;

        foreach (var (member, component, _) in changed)
            UnindexMember(member, component.Networks);

        foreach (var (member, component, updated) in changed)
        {
            component.Networks = updated;
            IndexMember(member, component.Networks);
        }

        NotifyReceivers(affected, changed.Count == 1 ? changed[0].Member : null);
        return true;
    }

    public IReadOnlyCollection<EntityUid> GetNetworkMembers(ProtoId<CameraNetworkPrototype> network)
    {
        return _members.TryGetValue(network, out var members)
            ? members.ToArray()
            : Array.Empty<EntityUid>();
    }

    public bool SetReceiverNetworks(EntityUid receiver, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        if (!TryComp(receiver, out CameraNetworkReceiverComponent? component))
            return false;

        var updated = networks.ToHashSet();
        if (component.Networks.SetEquals(updated))
            return false;

        var oldEffective = GetEffectiveNetworks(receiver, component);
        component.Networks = updated;
        var newEffective = GetEffectiveNetworks(receiver, component);
        UpdateReceiverIndex(receiver, oldEffective, newEffective);
        if (!oldEffective.SetEquals(newEffective))
            NotifyAuthorizationChanged(receiver);
        return true;
    }

    public HashSet<ProtoId<CameraNetworkPrototype>> GetEffectiveNetworks(EntityUid receiver)
    {
        return TryComp(receiver, out CameraNetworkReceiverComponent? component)
            ? GetEffectiveNetworks(receiver, component)
            : [];
    }

    public bool GrantNetwork(EntityUid receiver, ProtoId<CameraNetworkPrototype> network, EntityUid source)
    {
        if (!TryComp(receiver, out CameraNetworkReceiverComponent? component))
            return false;

        var effectiveNetworks = GetEffectiveNetworks(receiver, component);
        if (!_runtimeGrants.TryGetValue(receiver, out var grants))
        {
            grants = [];
            _runtimeGrants.Add(receiver, grants);
        }

        if (!grants.TryGetValue(network, out var sources))
        {
            sources = [];
            grants.Add(network, sources);
        }

        if (!sources.Add(source))
            return false;

        AddSourceGrant(source, receiver, network);
        if (effectiveNetworks.Add(network))
        {
            IndexReceiver(receiver, [network]);
            NotifyAuthorizationChanged(receiver);
        }

        return true;
    }

    public bool RevokeNetwork(EntityUid receiver, ProtoId<CameraNetworkPrototype> network, EntityUid source)
    {
        if (!TryComp(receiver, out CameraNetworkReceiverComponent? component))
            return false;

        return RevokeNetwork(receiver, component, network, source);
    }

    private bool RevokeNetwork(
        EntityUid receiver,
        CameraNetworkReceiverComponent component,
        ProtoId<CameraNetworkPrototype> network,
        EntityUid source)
    {
        if (!_runtimeGrants.TryGetValue(receiver, out var grants)
            || !grants.TryGetValue(network, out var sources)
            || !sources.Remove(source))
        {
            return false;
        }

        RemoveSourceGrant(source, receiver, network);
        if (sources.Count != 0)
            return true;

        grants.Remove(network);
        if (grants.Count == 0)
            _runtimeGrants.Remove(receiver);

        if (component.Networks.Contains(network))
            return true;

        UnindexReceiver(receiver, [network]);
        NotifyAuthorizationChanged(receiver);
        return true;
    }

    private void IndexMember(EntityUid member, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        foreach (var network in networks)
        {
            if (!_members.TryGetValue(network, out var members))
            {
                members = [];
                _members.Add(network, members);
            }

            members.Add(member);
        }
    }

    private void UnindexMember(EntityUid member, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        foreach (var network in networks)
        {
            if (!_members.TryGetValue(network, out var members))
                continue;

            members.Remove(member);
            if (members.Count == 0)
                _members.Remove(network);
        }
    }

    private void IndexReceiver(EntityUid receiver, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        foreach (var network in networks)
        {
            if (!_receivers.TryGetValue(network, out var receivers))
            {
                receivers = [];
                _receivers.Add(network, receivers);
            }

            receivers.Add(receiver);
        }
    }

    private void UnindexReceiver(EntityUid receiver, IEnumerable<ProtoId<CameraNetworkPrototype>> networks)
    {
        foreach (var network in networks)
        {
            if (!_receivers.TryGetValue(network, out var receivers))
                continue;

            receivers.Remove(receiver);
            if (receivers.Count == 0)
                _receivers.Remove(network);
        }
    }

    private void UpdateReceiverIndex(
        EntityUid receiver,
        IEnumerable<ProtoId<CameraNetworkPrototype>> oldNetworks,
        IEnumerable<ProtoId<CameraNetworkPrototype>> newNetworks)
    {
        UnindexReceiver(receiver, oldNetworks);
        IndexReceiver(receiver, newNetworks);
    }

    private HashSet<ProtoId<CameraNetworkPrototype>> GetEffectiveNetworks(
        EntityUid receiver,
        CameraNetworkReceiverComponent component)
    {
        var networks = new HashSet<ProtoId<CameraNetworkPrototype>>(component.Networks);
        if (_runtimeGrants.TryGetValue(receiver, out var grants))
            networks.UnionWith(grants.Keys);

        return networks;
    }

    private void AddSourceGrant(EntityUid source, EntityUid receiver, ProtoId<CameraNetworkPrototype> network)
    {
        if (!_grantsBySource.TryGetValue(source, out var grants))
        {
            grants = [];
            _grantsBySource.Add(source, grants);
        }

        grants.Add((receiver, network));
    }

    private void RemoveSourceGrant(EntityUid source, EntityUid receiver, ProtoId<CameraNetworkPrototype> network)
    {
        if (!_grantsBySource.TryGetValue(source, out var grants))
            return;

        grants.Remove((receiver, network));
        if (grants.Count == 0)
            _grantsBySource.Remove(source);
    }

    private void CleanupSourceGrants(EntityUid source)
    {
        if (!_grantsBySource.Remove(source, out var grants))
            return;

        foreach (var (receiver, network) in grants)
        {
            if (TryComp(receiver, out CameraNetworkReceiverComponent? component))
                RevokeNetwork(receiver, component, network, source);
            else
                RemoveRuntimeGrant(receiver, network, source);
        }
    }

    private void CleanupReceiverGrants(EntityUid receiver)
    {
        if (!_runtimeGrants.Remove(receiver, out var grants))
            return;

        foreach (var (network, sources) in grants)
        {
            foreach (var source in sources)
                RemoveSourceGrant(source, receiver, network);
        }
    }

    private void RemoveRuntimeGrant(EntityUid receiver, ProtoId<CameraNetworkPrototype> network, EntityUid source)
    {
        if (!_runtimeGrants.TryGetValue(receiver, out var grants)
            || !grants.TryGetValue(network, out var sources)
            || !sources.Remove(source))
        {
            return;
        }

        if (sources.Count == 0)
            grants.Remove(network);
        if (grants.Count == 0)
            _runtimeGrants.Remove(receiver);
    }

    private void NotifyReceivers(
        IEnumerable<ProtoId<CameraNetworkPrototype>> networks,
        EntityUid? member,
        CameraReceiverChangeKind kind = CameraReceiverChangeKind.MemberList)
    {
        var notified = new HashSet<EntityUid>();
        foreach (var network in networks)
        {
            if (!_receivers.TryGetValue(network, out var receivers))
                continue;

            foreach (var receiver in receivers)
            {
                if (notified.Add(receiver))
                    NotifyReceiver(receiver, member, kind);
            }
        }
    }

    private bool IsAvailable(EntityUid camera)
    {
        if (TerminatingOrDeleted(camera) || MetaData(camera).EntityPaused)
            return false;

        if (TryComp(camera, out SurveillanceCameraComponent? surveillance) && !surveillance.Active)
            return false;

        return !TryComp(camera, out ApcPowerReceiverComponent? power) || power.Powered;
    }

    private void QueueMarkerChange(Entity<CameraMapMarkerComponent> marker)
    {
        if (marker.Comp.Mobile)
        {
            _mobileMarkerUpdates.TryAdd(marker.Owner, _timing.CurTime + marker.Comp.UpdateInterval);
            return;
        }

        QueueAffectedReceivers(marker.Owner);
    }

    private void QueueAffectedReceivers(EntityUid marker)
    {
        if (!TryComp(marker, out CameraNetworkMemberComponent? member))
            return;

        foreach (var network in member.Networks)
        {
            if (!_receivers.TryGetValue(network, out var receivers))
                continue;

            foreach (var receiver in receivers)
                _pendingMarkerReceivers.TryAdd(receiver, marker);
        }
    }

    private void NotifyAuthorizationChanged(EntityUid receiver)
    {
        var ev = new CameraReceiverChangedEvent(CameraReceiverChangeKind.Authorization);
        RaiseLocalEvent(receiver, ref ev);
    }

    private void NotifyReceiver(EntityUid receiver, EntityUid? member, CameraReceiverChangeKind kind)
    {
        var ev = new CameraReceiverChangedEvent(kind, member);
        RaiseLocalEvent(receiver, ref ev);
    }
}

internal sealed class MapViewSubscription(ICommonSession session)
{
    public ICommonSession Session { get; } = session;
    public HashSet<EntityUid> Grids { get; } = [];
}

internal sealed class MapGridViewSubscription(EntityUid view)
{
    public EntityUid View { get; } = view;
    public int Count { get; set; } = 1;
}
