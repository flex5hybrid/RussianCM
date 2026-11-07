using System.Linq;
using Content.Server._RuCM.Balance;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Presets;
using Content.Shared._RuCM.Balance;
using Content.Shared.GameTicking;
using Robust.Shared.Random;

namespace Content.Server.CMU14.Round;

// CMU14: RuCM automatically resolves only server ballots with no effective votes.
public sealed partial class CMUPresetVoteSystem
{
    [Dependency] private IRobustRandom _rotationRandom = default!;

    private readonly RuDistressRotationState _rotation = new();
    private bool _roundDistressEligible;
    private string? _startedRotationPreset;
    private bool _rotationRoundStarted;

    private void InitializeRotation()
    {
        SubscribeLocalEvent<RoundStartingEvent>(_ => _roundDistressEligible = IsDistressEligible());
    }

    private bool IsDistressEligible()
    {
        var players = _ticker.ReadyPlayerCount();
        return _prototypes.TryIndex<GamePresetPrototype>(RuDistressRotationState.Distress, out var preset)
            && players >= _ticker.GetMinimumPlayerCount(preset)
            && players <= (preset.MaxPlayers ?? int.MaxValue);
    }

    private void TrackRotation(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.InRound && ev.Old != GameRunLevel.InRound)
        {
            _startedRotationPreset = _ticker.CurrentPreset?.ID;
            _rotationRoundStarted = true;
        }
        else if (ev.New == GameRunLevel.PostRound && _rotationRoundStarted)
        {
            _rotation.RecordCompleted(_startedRotationPreset, _roundDistressEligible);
            _rotationRoundStarted = false;
        }
        else if (ev.New == GameRunLevel.PreRoundLobby)
        {
            _rotationRoundStarted = false;
        }
    }

    /// <summary>Called only for an unattended server ballot. The offered set retains admin and paid constraints.</summary>
    public string? PickAutomaticPreset(IEnumerable<string> offered)
    {
        var players = _ticker.ReadyPlayerCount();
        var candidates = offered.Where(id => _prototypes.TryIndex<GamePresetPrototype>(id, out var preset)
            && players >= _ticker.GetMinimumPlayerCount(preset)
            && players <= (preset.MaxPlayers ?? int.MaxValue)).ToList();
        if (candidates.Count == 0)
            return null;

        if ((_rotation.LastWasDistress || _rotationRoundStarted && _startedRotationPreset == RuDistressRotationState.Distress)
            && candidates.Count > 1)
            candidates.Remove(RuDistressRotationState.Distress);

        var eligible = candidates.Contains(RuDistressRotationState.Distress);
        if (_rotation.IsGuaranteed(eligible, _cfg.GetCVar(RuBalanceCVars.DistressGuaranteeAfter)))
            return RuDistressRotationState.Distress;

        var distressWeight = _rotation.GetWeight(
            _cfg.GetCVar(RuBalanceCVars.DistressBaseWeight),
            _cfg.GetCVar(RuBalanceCVars.DistressWeightGrowth),
            _cfg.GetCVar(RuBalanceCVars.DistressMaximumMultiplier));
        var weights = candidates.Select(id => id == RuDistressRotationState.Distress ? distressWeight : 1d).ToArray();
        var sum = weights.Sum();
        if (sum <= 0)
            return null;

        var ticket = _rotationRandom.NextDouble() * sum;
        for (var i = 0; i < candidates.Count; i++)
        {
            ticket -= weights[i];
            if (ticket < 0)
                return candidates[i];
        }

        return candidates[^1];
    }
}
