using Robust.Shared.Configuration;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Server-only local assistant configuration; automatic replies are separately opt-in.</summary>
[CVarDefs]
public static class AdminAICVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("cmu.admin_ai.enabled", false, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SocketPath =
        CVarDef.Create("cmu.admin_ai.socket_path", "/run/rucmu-ai/llama.sock", CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> TimeoutSeconds =
        CVarDef.Create("cmu.admin_ai.request_timeout_seconds", 45, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxInputChars =
        CVarDef.Create("cmu.admin_ai.max_input_chars", 4096, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxOutputTokens =
        CVarDef.Create("cmu.admin_ai.max_output_tokens", 256, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxOutputChars =
        CVarDef.Create("cmu.admin_ai.max_output_chars", 4096, CVar.SERVERONLY | CVar.ARCHIVE);

    private const CVar Flags = CVar.SERVERONLY | CVar.ARCHIVE;
    public static readonly CVarDef<bool> SafeFAQ = CVarDef.Create("cmu.admin_ai.safe_faq", false, Flags);
    public static readonly CVarDef<bool> Clarification = CVarDef.Create("cmu.admin_ai.clarification", false, Flags);
    public static readonly CVarDef<bool> Investigation = CVarDef.Create("cmu.admin_ai.investigation", false, Flags);
    public static readonly CVarDef<int> ContextMaxLogs = CVarDef.Create("cmu.admin_ai.context_max_logs", 64, Flags);
    public static readonly CVarDef<int> EvidenceMaxChars = CVarDef.Create("cmu.admin_ai.evidence_max_chars", 24000, Flags);
    public static readonly CVarDef<int> ContextWindowMinutes = CVarDef.Create("cmu.admin_ai.context_window_minutes", 15, Flags);
    public static readonly CVarDef<int> DraftOutputTokens = CVarDef.Create("cmu.admin_ai.draft_output_tokens", 128, Flags);
    public static readonly CVarDef<int> MaxConcurrentRequests = CVarDef.Create("cmu.admin_ai.max_concurrent_requests", 1, Flags);
    public static readonly CVarDef<int> QueueCapacity = CVarDef.Create("cmu.admin_ai.queue_capacity", 8, Flags);
    public static readonly CVarDef<int> QueueTtlSeconds = CVarDef.Create("cmu.admin_ai.queue_ttl_seconds", 30, Flags);
    public static readonly CVarDef<int> DegradedInputChars = CVarDef.Create("cmu.admin_ai.degraded_input_chars", 1024, Flags);
    public static readonly CVarDef<int> DegradedOutputTokens = CVarDef.Create("cmu.admin_ai.degraded_output_tokens", 64, Flags);
    public static readonly CVarDef<bool> GovernorEnabled = CVarDef.Create("cmu.admin_ai.governor_enabled", true, Flags);
    public static readonly CVarDef<int> PollSeconds = CVarDef.Create("cmu.admin_ai.poll_seconds", 2, Flags);
    public static readonly CVarDef<int> RecoverySeconds = CVarDef.Create("cmu.admin_ai.recovery_seconds", 30, Flags);
    public static readonly CVarDef<int> MaxSampleAgeSeconds = CVarDef.Create("cmu.admin_ai.max_sample_age_seconds", 5, Flags);
    public static readonly CVarDef<float> CpuDegraded = CVarDef.Create("cmu.admin_ai.cpu_psi_degraded", 10f, Flags);
    public static readonly CVarDef<float> CpuPaused = CVarDef.Create("cmu.admin_ai.cpu_psi_paused", 25f, Flags);
    public static readonly CVarDef<float> MemoryDegraded = CVarDef.Create("cmu.admin_ai.memory_psi_degraded", 1f, Flags);
    public static readonly CVarDef<float> MemoryPaused = CVarDef.Create("cmu.admin_ai.memory_psi_paused", 5f, Flags);
    public static readonly CVarDef<float> IoDegraded = CVarDef.Create("cmu.admin_ai.io_psi_degraded", 5f, Flags);
    public static readonly CVarDef<float> IoPaused = CVarDef.Create("cmu.admin_ai.io_psi_paused", 15f, Flags);
    public static readonly CVarDef<float> SimulationDegraded = CVarDef.Create("cmu.admin_ai.tick_budget_degraded", 0.8f, Flags);
    public static readonly CVarDef<float> SimulationPaused = CVarDef.Create("cmu.admin_ai.tick_budget_paused", 1.2f, Flags);
    public static readonly CVarDef<string> CgroupPaths = CVarDef.Create("cmu.admin_ai.cgroup_paths", "", Flags);
    public static readonly CVarDef<int> ContextTtlSeconds = CVarDef.Create("cmu.admin_ai.context_ttl_seconds", 300, Flags);
    public static readonly CVarDef<int> DraftTtlSeconds = CVarDef.Create("cmu.admin_ai.draft_ttl_seconds", 120, Flags);
    public static readonly CVarDef<int> MaxConversations = CVarDef.Create("cmu.admin_ai.max_conversations", 128, Flags);
    public static readonly CVarDef<int> MaxViews = CVarDef.Create("cmu.admin_ai.max_private_views", 32, Flags);
    public static readonly CVarDef<int> ContextMessages = CVarDef.Create("cmu.admin_ai.context_messages", 8, Flags);
    public static readonly CVarDef<int> ContextMessageChars = CVarDef.Create("cmu.admin_ai.context_message_chars", 512, Flags);
    public static readonly CVarDef<int> DraftCooldownSeconds = CVarDef.Create("cmu.admin_ai.draft_cooldown_seconds", 5, Flags);

    public static readonly CVarDef<bool>[] RuntimeBools = { Enabled, GovernorEnabled, SafeFAQ, Clarification, Investigation };
    public static readonly CVarDef<bool>[] InfrastructureBools = { Enabled, GovernorEnabled };
    public static readonly CVarDef<int>[] InfrastructureInts = { TimeoutSeconds, MaxInputChars, MaxOutputTokens, MaxOutputChars,
        MaxConcurrentRequests, QueueCapacity, QueueTtlSeconds, DegradedInputChars, DegradedOutputTokens,
        PollSeconds, RecoverySeconds, MaxSampleAgeSeconds };
    public static readonly CVarDef<int>[] RuntimeInts = { TimeoutSeconds, MaxInputChars, MaxOutputTokens, MaxOutputChars,
        MaxConcurrentRequests, QueueCapacity, QueueTtlSeconds, DegradedInputChars, DegradedOutputTokens,
        PollSeconds, RecoverySeconds, MaxSampleAgeSeconds, ContextTtlSeconds, DraftTtlSeconds, MaxConversations,
        MaxViews, ContextMessages, ContextMessageChars, DraftCooldownSeconds, DraftOutputTokens,
        ContextMaxLogs, EvidenceMaxChars, ContextWindowMinutes };
    public static readonly CVarDef<float>[] RuntimeFloats = { CpuDegraded, CpuPaused, MemoryDegraded, MemoryPaused,
        IoDegraded, IoPaused, SimulationDegraded, SimulationPaused };
    public static readonly CVarDef<string>[] RuntimeStrings = { SocketPath, CgroupPaths };

    public static AIQueueOptions ReadQueue(IConfigurationManager config) => new(config.GetCVar(QueueCapacity),
        config.GetCVar(QueueTtlSeconds), config.GetCVar(TimeoutSeconds), config.GetCVar(MaxInputChars),
        config.GetCVar(DegradedInputChars), config.GetCVar(DegradedOutputTokens));

    public static AIGovernorOptions ReadGovernor(IConfigurationManager config) => new(config.GetCVar(GovernorEnabled),
        config.GetCVar(CpuDegraded), config.GetCVar(CpuPaused), config.GetCVar(MemoryDegraded), config.GetCVar(MemoryPaused),
        config.GetCVar(IoDegraded), config.GetCVar(IoPaused), config.GetCVar(SimulationDegraded), config.GetCVar(SimulationPaused),
        config.GetCVar(RecoverySeconds), config.GetCVar(PollSeconds), config.GetCVar(MaxSampleAgeSeconds));

    public static AIDraftContextOptions ReadContext(IConfigurationManager config) => new(config.GetCVar(MaxConversations),
        config.GetCVar(MaxViews), config.GetCVar(ContextMessages), config.GetCVar(ContextMessageChars),
        config.GetCVar(ContextTtlSeconds), config.GetCVar(DraftTtlSeconds), config.GetCVar(DraftCooldownSeconds));

    /// <summary>Reads a fresh snapshot; invalid values always disable it.</summary>
    public static AIInferenceOptions Read(IConfigurationManager config)
    {
        var options = new AIInferenceOptions(
            config.GetCVar(Enabled), config.GetCVar(SocketPath), config.GetCVar(TimeoutSeconds),
            config.GetCVar(MaxInputChars), config.GetCVar(MaxOutputTokens), config.GetCVar(MaxOutputChars));
        return options.IsValid() ? options : options with { Enabled = false };
    }
}
