using System.Linq;
using System.Threading.Tasks;
using Content.Server.CMU14.Round;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RuCM.Kaban;

namespace Content.Server._RuCM.Kaban;

public sealed partial class KabanSystem
{
    private sealed record ForceSettlement(string Server, Guid Id, string Attempt, bool Used, long At, string Reason);
    private readonly Queue<ForceSettlement> _forceSettlements = new();
    private KabanRoundRequest? _forceActive;
    private Guid _forceConsidered;
    private int _forceGeneration;
    private Action<KabanForceChoice?>? _forceWaiting;
    private DateTime _forceDeadline;
    private bool _forceSaving;
    private DateTime _forceRetry;
    private readonly HashSet<string> _forceLoadedMaps = new();

    private bool ForcingEnabled => Enabled && _ready && _cfg.GetCVar(KabanCVars.Forcing);
    public bool HasPendingForcing => _forceActive != null || _forceWaiting != null;

    public bool TryBeginForcing(Action<KabanForceChoice?> continuation)
    {
        if (!ForcingEnabled || !_ticker.CanUpdateMap() || _forceConsidered == _lobby || _forceSettlements.Count != 0
            || EntityManager.System<CMUPresetVoteSystem>().LockedPresets.Count > 0)
            return false;
        _forceConsidered = _lobby;
        _forceLoadedMaps.Clear();
        _forceWaiting = continuation;
        _forceDeadline = DateTime.UtcNow.AddSeconds(5);
        var generation = ++_forceGeneration;
        _ = ClaimForcing(_cfg.GetCVar(KabanCVars.ServerId), _lobby.ToString(), generation);
        return true;
    }

    private async Task ClaimForcing(string server, string attempt, int generation)
    {
        KabanRoundRequest? request = null;
        try
        {
            // No active/in-flight settlement survives TryBeginForcing's gate. Recover uncertain replies from earlier lobbies.
            await _db.KabanRecoverForce(server, DateTime.UtcNow.Ticks);
            request = await _db.KabanClaimForce(server, attempt, DateTime.UtcNow.Ticks);
        }
        catch (Exception e) { _log.Error($"Round forcing unavailable; ordinary voting continues: {e}"); }
        _completed.Enqueue(() =>
        {
            var payload = request == null ? null : KabanEconomy.ForcePayload(request);
            var valid = generation == _forceGeneration && _forceWaiting != null && ForcingEnabled
                && _ticker.CanUpdateMap() && EntityManager.System<CMUPresetVoteSystem>().LockedPresets.Count == 0;
            if (request != null && (!valid || payload == null
                || !EntityManager.System<AuRoundSystem>().KabanCatalog(fresh: true).Any(payload.Choice.Matches)))
            {
                _forceSettlements.Enqueue(new(server, request.Id, attempt, false, DateTime.UtcNow.Ticks, "late-or-incompatible"));
                request = null;
            }
            if (generation != _forceGeneration)
                return;
            var next = _forceWaiting;
            _forceWaiting = null;
            _forceActive = request;
            next?.Invoke(request == null ? null : payload?.Choice);
        });
    }

    public void CancelForcing(string reason)
    {
        _forceGeneration++;
        _forceWaiting = null;
        FinishForcing(false, reason);
    }

    private void FinishForcing(bool used, string reason)
    {
        if (_forceActive is not { } request || KabanEconomy.ForcePayload(request) is not { } payload)
            return;
        _forceActive = null;
        _forceSettlements.Enqueue(new(payload.Server, request.Id, payload.Attempt, used, DateTime.UtcNow.Ticks, reason));
    }

    private void UpdateForcing()
    {
        if (_forceWaiting != null && DateTime.UtcNow >= _forceDeadline)
        {
            var next = _forceWaiting;
            CancelForcing("claim-timeout");
            next(null);
        }
        var round = EntityManager.System<AuRoundSystem>();
        if (_forceActive != null && round.KabanSelectionOpen
            && (!ForcingEnabled || EntityManager.System<CMUPresetVoteSystem>().LockedPresets.Count > 0))
            round.StopVoteSequence();
        if (!_forceSaving && _forceSettlements.TryPeek(out var item) && DateTime.UtcNow >= _forceRetry)
        {
            _forceSaving = true;
            _ = SaveForcing(item);
        }
    }

    private async Task SaveForcing(ForceSettlement item)
    {
        try
        {
            await _db.KabanFinishForce(item.Server, item.Id, item.Attempt, item.Used, item.At, item.Reason);
            _completed.Enqueue(() => { _forceSettlements.Dequeue(); _forceSaving = false; });
        }
        catch (Exception e)
        {
            _log.Error($"Round forcing settlement deferred: {e}");
            _completed.Enqueue(() => { _forceSaving = false; _forceRetry = DateTime.UtcNow.AddSeconds(30); });
        }
    }

    private void ForcingRunLevel(GameRunLevelChangedEvent ev)
    {
        if (ev.New != GameRunLevel.InRound || _forceActive == null)
            return;
        var payload = KabanEconomy.ForcePayload(_forceActive);
        var round = EntityManager.System<AuRoundSystem>();
        var success = payload != null && round.KabanSelectionMatches(payload.Choice)
            && round.SelectedPreset?.ID == _ticker.CurrentPreset?.ID
            && round.GetSelectedPlanet() is { } planet && _forceLoadedMaps.Contains(planet.MapId)
            && (round.ResolveGovforShip(_ticker.CurrentPreset) is not { Length: > 0 } gov || _forceLoadedMaps.Contains(gov))
            && (round.ResolveOpforShip(_ticker.CurrentPreset) is not { Length: > 0 } op || _forceLoadedMaps.Contains(op));
        FinishForcing(success, success ? $"round:{_ticker.RoundId}" : "round-start-mismatch");
        round.CompleteKabanSelection();
    }
}
