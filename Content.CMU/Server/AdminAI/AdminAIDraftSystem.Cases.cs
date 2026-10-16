using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.CMU14.AdminAI;

public sealed partial class AdminAIDraftSystem
{
    [Dependency] private IAdminLogManager _logs = default!;
    private int _contextOperations;
    internal Action<ICommonSession, AdminAICaseState>? CaseDelivery;

    private bool CanInvestigate(ICommonSession session) => Enabled && _config.GetCVar(AdminAICVars.Investigation)
        && Authorized(session) && _admins.HasAdminFlag(session, AdminFlags.Logs)
        && _config.GetCVar(AdminAICVars.ContextMaxLogs) is >= 1 and <= 128
        && _config.GetCVar(AdminAICVars.EvidenceMaxChars) is >= 4096 and <= 32768
        && _config.GetCVar(AdminAICVars.ContextWindowMinutes) is >= 1 and <= 30;

    internal void HandleContext(AdminAIContextRequest request, ICommonSession session)
    {
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (!CanInvestigate(session))
        {
            if (_timing.RealTime >= _nextDenialLog)
            { _nextDenialLog = _timing.RealTime + TimeSpan.FromSeconds(1); _audit.Info($"AI context denied actor={session.UserId}"); }
            return;
        }
        if (_shuttingDown) return;
        var view = _store.Get(session.UserId.UserId, request.Channel.UserId, request.ViewId, request.SelectionId,
            _round.CurrentRoundId, _timing.RealTime, true);
        if (view == null || request.Round != view.Round || request.AdditionalPlayers == null || request.AdditionalPlayers.Length > 3
            || request.AdditionalPlayers.Any(player => player == Guid.Empty)) return;
        if (_rateLimit.CountAction(session, RateKey) != RateLimitStatus.Allowed)
        { DeliverCase(session, view, "rate-limited"); return; }
        if (_contextOperations >= 1) { DeliverCase(session, view, "busy"); return; }
        var maxLogs = _config.GetCVar(AdminAICVars.ContextMaxLogs); var maxChars = _config.GetCVar(AdminAICVars.EvidenceMaxChars);
        var maxWindow = _config.GetCVar(AdminAICVars.ContextWindowMinutes);
        if (maxLogs is < 1 or > 128 || maxChars is < 4096 or > 32768 || maxWindow is < 1 or > 30
            || request.BeforeMinutes < 0 || request.AfterMinutes < 0 || request.BeforeMinutes > maxWindow
            || request.AfterMinutes > maxWindow || request.BeforeMinutes + request.AfterMinutes is < 1
            || request.BeforeMinutes + request.AfterMinutes > maxWindow)
        { DeliverCase(session, view, "invalid"); return; }
        var infrastructure = _inference.Snapshot();
        if (!infrastructure.Enabled || infrastructure.State != AIResourceState.Normal || infrastructure.Health != AIWorkerHealth.Ready)
        { DeliverCase(session, view, "paused"); return; }
        var knowledge = GetKnowledge();
        if (knowledge == null) { DeliverCase(session, view, "loading"); return; }
        var ticket = _store.Begin(view, _timing.RealTime, 800);
        if (ticket == null) return;
        var query = new AIContextQuery(ticket.Round, ticket.ComplaintUtc.AddMinutes(-request.BeforeMinutes),
            ticket.ComplaintUtc.AddMinutes(request.AfterMinutes), new[] { ticket.Channel }.Concat(request.AdditionalPlayers).Distinct().ToArray(), maxLogs, maxChars);
        if (!AICaseAnalyzer.Valid(query)) { _store.Cancel(view); return; }
        var labels = new[] { "claim", "selection", "count", "participants", "confirmed", "unknown", "truncated", "questions", "draft", "sources" }
            .ToDictionary(key => key, key => Loc.GetString("cmu-admin-ai-report-" + key));
        _contextOperations++; Send(session, view, AdminAIDraftStatus.Working); DeliverCase(session, view, "working");
        _audit.Info($"AI context requested actor={session.UserId} case={ticket.CaseId} round={ticket.Round} request={ticket.Id} logs={maxLogs}");
        _ = Task.Run(async () =>
        {
            AICaseReport? report = null; var status = "unavailable"; Task<System.Collections.Generic.List<SharedAdminLog>>? operation = null;
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ticket.Token); cancel.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                operation = AIContextCollector.Read(_logs, query, cancel.Token);
                var rows = await operation.WaitAsync(cancel.Token).ConfigureAwait(false);
                var sources = knowledge.Search(ticket.Question, 1200);
                report = AICaseAnalyzer.Build(query, rows.Select(row => new AIContextRow(row.Id, row.Date, row.Type.ToString(), row.Message, row.Players)), ticket.Question, sources, labels);
                status = "ready";
            }
            catch (OperationCanceledException) { status = "expired"; }
            catch (Exception) { status = "unavailable"; }
            var completed = report; var resultStatus = status;
            _tasks.RunOnMainThread(() =>
            {
                try { CompleteCase(session, ticket, completed, resultStatus); }
                catch (Exception) { _audit.Warning("AI evidence delivery failed; ordinary ahelp remains available."); }
            });
            // A backend ignoring cancellation cannot free the physical query slot and start overlapping work.
            if (operation != null) { try { await operation.ConfigureAwait(false); } catch (Exception) { } }
            _tasks.RunOnMainThread(() => _contextOperations--);
        });
    }

    private void CompleteCase(ICommonSession original, AIDraftTicket ticket, AICaseReport? report, string status)
    {
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (_shuttingDown || !_players.TryGetSessionById(new(ticket.Owner), out var current) || !ReferenceEquals(current, original)) return;
        if (!CanInvestigate(current)) { _store.Revoke(ticket.Owner); SendReset(current); return; }
        if (!_store.IsCurrent(ticket, _round.CurrentRoundId, _timing.RealTime, true)) return;
        if (_inference.Snapshot().State != AIResourceState.Normal) { report = null; status = "paused"; }
        var view = _store.Complete(ticket, _round.CurrentRoundId, _timing.RealTime, true,
            report == null ? AIInferenceStatus.Unavailable : AIInferenceStatus.Success, "");
        if (view == null) return;
        view.LastResult = null; view.Report = report; view.ReportId = ticket.Id;
        view.ReportExpires = _timing.RealTime + TimeSpan.FromSeconds(_config.GetCVar(AdminAICVars.DraftTtlSeconds));
        _audit.Info($"AI context completed actor={current.UserId} case={ticket.CaseId} round={ticket.Round} request={ticket.Id} result={status} rows={report?.Evidence.Length ?? 0}");
        Send(current, view); if (report == null) DeliverCase(current, view, status);
    }

    private void SendCase(ICommonSession session, AIDraftSessionStore.View view)
    {
        if (view.Report != null && view.LastSentReport != view.ReportId && CanInvestigate(session)) DeliverCase(session, view, "ready");
        else if (view.Report == null && view.LastSentReport != Guid.Empty) DeliverCase(session, view, "empty");
    }
    private void DeliverCase(ICommonSession session, AIDraftSessionStore.View view, string status)
    {
        // Never send log-derived bytes to Adminhelp without Logs or to an ordinary player's channel.
        if (!CanInvestigate(session) || view.Owner != session.UserId.UserId) return;
        var report = view.Report;
        var evidence = report == null ? Array.Empty<AdminAIEvidence>() : report.Evidence.Select(row =>
            new AdminAIEvidence(view.Round, row.Id, row.Date.Ticks, row.Type, row.Message)).ToArray();
        var state = new AdminAICaseState(new(view.Channel), view.SelectionId, view.Id, view.ReportId, status,
            report?.Text ?? "", evidence, report == null ? 0 : Math.Max(0, (int)(view.ReportExpires - _timing.RealTime).TotalSeconds));
        if (CaseDelivery != null) CaseDelivery(session, state); else RaiseNetworkEvent(state, session);
        view.LastSentReport = report == null ? Guid.Empty : view.ReportId;
    }
}
