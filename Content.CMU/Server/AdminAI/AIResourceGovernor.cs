using System;

namespace Content.Server.CMU14.AdminAI;

public enum AIResourceState : byte { Normal, Degraded, Paused }

public sealed record AILoadSample(TimeSpan CollectedAt, double CpuSomeAvg10, double MemoryFullAvg10,
    double IoFullAvg10, double SimulationBudgetRatio);

public sealed record AIGovernorOptions(bool Enabled = true, double CpuDegraded = 10, double CpuPaused = 25,
    double MemoryDegraded = 1, double MemoryPaused = 5, double IoDegraded = 5, double IoPaused = 15,
    double SimulationDegraded = 0.8, double SimulationPaused = 1.2, int RecoverySeconds = 30,
    int PollSeconds = 2, int MaxSampleAgeSeconds = 5)
{
    public bool IsValid() => Pair(CpuDegraded, CpuPaused, 100) && Pair(MemoryDegraded, MemoryPaused, 100)
        && Pair(IoDegraded, IoPaused, 100) && Pair(SimulationDegraded, SimulationPaused, 10)
        && RecoverySeconds is >= 5 and <= 300 && PollSeconds is >= 1 and <= 10
        && MaxSampleAgeSeconds >= PollSeconds * 2 && MaxSampleAgeSeconds <= 60;

    private static bool Pair(double low, double high, double max) => double.IsFinite(low) && double.IsFinite(high)
        && low > 0 && low < high && high <= max;
}

/// <summary>Fast protection, slow recovery. No raw CPU percentages or invented load measurements.</summary>
public sealed class AIResourceGovernor(AIGovernorOptions options)
{
    private TimeSpan? _healthySince;
    public AIResourceState State { get; private set; } = AIResourceState.Paused;
    public string Reason { get; private set; } = "metrics-unavailable";

    public AIResourceState Observe(AILoadSample? sample, TimeSpan now)
    {
        if (!options.IsValid())
            return Set(AIResourceState.Paused, "invalid-configuration");
        if (!options.Enabled)
            return Set(AIResourceState.Normal, "governor-disabled-by-operator");
        if (sample == null || sample.CollectedAt > now || now - sample.CollectedAt > TimeSpan.FromSeconds(options.MaxSampleAgeSeconds)
            || !Percent(sample.CpuSomeAvg10) || !Percent(sample.MemoryFullAvg10) || !Percent(sample.IoFullAvg10)
            || !double.IsFinite(sample.SimulationBudgetRatio) || sample.SimulationBudgetRatio < 0)
            return Set(AIResourceState.Paused, "metrics-unavailable");

        if (sample.CpuSomeAvg10 >= options.CpuPaused || sample.MemoryFullAvg10 >= options.MemoryPaused
            || sample.IoFullAvg10 >= options.IoPaused || sample.SimulationBudgetRatio >= options.SimulationPaused)
            return Set(AIResourceState.Paused, "load-high");

        if (sample.CpuSomeAvg10 >= options.CpuDegraded || sample.MemoryFullAvg10 >= options.MemoryDegraded
            || sample.IoFullAvg10 >= options.IoDegraded || sample.SimulationBudgetRatio >= options.SimulationDegraded)
        {
            if (State == AIResourceState.Paused)
                return Set(AIResourceState.Paused, "load-not-recovered");
            return Set(AIResourceState.Degraded, "load-elevated");
        }

        var recovered = sample.CpuSomeAvg10 < options.CpuDegraded * 0.75
            && sample.MemoryFullAvg10 < options.MemoryDegraded * 0.75 && sample.IoFullAvg10 < options.IoDegraded * 0.75
            && sample.SimulationBudgetRatio < options.SimulationDegraded * 0.75;
        if (!recovered) { _healthySince = null; return State; }
        if (State == AIResourceState.Normal) return State;
        if (_healthySince == null || now < _healthySince) _healthySince = now;
        if (now - _healthySince < TimeSpan.FromSeconds(options.RecoverySeconds)) return State;
        _healthySince = now;
        State = State == AIResourceState.Paused ? AIResourceState.Degraded : AIResourceState.Normal;
        Reason = "load-recovered";
        return State;
    }

    private AIResourceState Set(AIResourceState state, string reason)
    {
        State = state; Reason = reason; _healthySince = null; return State;
    }

    private static bool Percent(double value) => double.IsFinite(value) && value is >= 0 and <= 100;
}
