using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Typed read-only context adapter. The coordinator checks authenticated permissions before and after this call.</summary>
public static class AIContextCollector
{
    public static Task<List<SharedAdminLog>> Read(IAdminLogManager logs, AIContextQuery query, CancellationToken cancellation)
    {
        if (!AICaseAnalyzer.Valid(query))
            throw new ArgumentException("Invalid bounded context query.");

        return logs.All(new LogFilter
        {
            Round = query.Round,
            After = query.After,
            Before = query.Before,
            AnyPlayers = query.Players,
            IncludePlayers = true,
            IncludeNonPlayers = false,
            Limit = query.MaxLogs + 1,
            DateOrder = DateOrder.Ascending,
            CancellationToken = cancellation,
        });
    }
}
