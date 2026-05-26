using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.RoundEnd;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._RuMC14.Rules;

/// <summary>
/// Automatically starts a round end vote
/// 120 minutes after it begins.
/// If rejected, retries the vote every 60 minutes.
/// </summary>
public sealed class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(120);
    private static readonly TimeSpan RetryDelay   = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan VoteDuration = TimeSpan.FromSeconds(30);

    private CancellationTokenSource? _timerToken;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CancelTimer();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        CancelTimer();
        ScheduleVote(InitialDelay);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CancelTimer();
    }

    private void CancelTimer()
    {
        _timerToken?.Cancel();
        _timerToken?.Dispose();
        _timerToken = null;
    }

    private void ScheduleVote(TimeSpan delay)
    {
        _timerToken = new CancellationTokenSource();
        Timer.Spawn(delay, StartRoundEndVote, _timerToken.Token);
    }

    private void StartRoundEndVote()
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var options = new VoteOptions
        {
            Title = Loc.GetString("ui-vote-round-end-title"),
            Options =
            {
                (Loc.GetString("ui-vote-round-end-yes"), "yes"),
                (Loc.GetString("ui-vote-round-end-no"),  "no"),
            },
            Duration = VoteDuration,
        };
        options.SetInitiatorOrServer(null);

        var vote = _voteManager.CreateVote(options);

        vote.OnFinished += (_, _) =>
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
                return;

            var votesYes = vote.VotesPerOption["yes"];
            var votesNo  = vote.VotesPerOption["no"];

            if (votesYes > votesNo)
            {
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-round-end-succeeded"));
                _roundEnd.EndRound();
            }
            else
            {
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-round-end-failed"));
                ScheduleVote(RetryDelay);
            }
        };
    }
}
