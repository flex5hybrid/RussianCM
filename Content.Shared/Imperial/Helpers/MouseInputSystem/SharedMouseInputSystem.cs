using Content.Shared.Imperial.MouseInput.Events;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.MouseInput;


public abstract class SharedMouseInputSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;


    private TimeSpan _bandwidth = TimeSpan.FromSeconds(0.5f);
    private Dictionary<EntityUid, (EntityCoordinates, TimeSpan, TimeSpan)> _cache = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<MousePositionResponseEvent>(OnMousePositionResponse);
    }

    private void OnMousePositionResponse(MousePositionResponseEvent args, EntitySessionEventArgs session)
    {
        if (session.SenderSession.AttachedEntity == null) return;

        var coords = GetCoordinates(args.Coordinates);
        var performer = GetEntity(args.Performer);

        UpdateCache(session.SenderSession.AttachedEntity.Value, coords);
        RaiseLocalEvent(performer, new MousePositionRefreshEvent()
        {
            MousePosition = coords,
            Player = session.SenderSession.AttachedEntity.Value
        });
    }

    #region Public API

    public virtual void CreateMousePositionRequest(EntityUid player, EntityUid? performer = null, TimeSpan? bandwidth = null)
    {
    }

    #endregion

    #region Helpers

    protected bool TryGetCache(EntityUid player, out EntityCoordinates? coords)
    {
        coords = null;

        if (!_cache.TryGetValue(player, out var cache)) return false;
        if (cache.Item3 + cache.Item2 < _timing.CurTime) return false;

        coords = cache.Item1;

        return true;
    }

    protected void UpdateCache(EntityUid player, EntityCoordinates coords, TimeSpan? bandwidth = null)
    {
        if (_cache.TryGetValue(player, out var cache))
        {
            if (!coords.IsValid(EntityManager)) return;

            _cache[player] = (
                coords,
                bandwidth ?? cache.Item2,
                _timing.CurTime
            );

            return;
        }

        _cache.Add(player, (coords, bandwidth ?? _bandwidth, TimeSpan.Zero));
    }

    #endregion
}
