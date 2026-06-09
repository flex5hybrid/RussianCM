using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization; // RuMC edit

namespace Content.Client.Lobby.UI;

internal sealed class LobbyHighJobPreviewEntry
{
    public LobbyHighJobPreviewEntry(JobPrototype job, IReadOnlyList<string> gamemodeLabels)
    {
        Job = job;
        GamemodeLabels = gamemodeLabels;
    }

    public JobPrototype Job { get; }
    public IReadOnlyList<string> GamemodeLabels { get; }

    public string JobName => LobbyHighJobPreview.GetDisplayJobName(Job);

    public string DisplayName
    {
        get
        {
            if (GamemodeLabels.Count == 0)
                return JobName;

            return $"{string.Join("+", GamemodeLabels)} / {JobName}";
        }
    }

    public string Signature => $"{Job.ID}:{string.Join("+", GamemodeLabels)}";
}

internal static class LobbyHighJobPreview
{
    private static readonly string[] HiddenFactionSuffixes =
    {
        " (GOVFOR)",
        " (OPFOR)"
    };

    private static readonly (string Key, string Label)[] Gamemodes =
    {
        ("Insurgency", "rmc-lobby-gamemode-label-insurgency"), // RuMC edit
        ("ColonyFall", "rmc-lobby-gamemode-label-colony-fall"),
        ("DistressSignal", "rmc-lobby-gamemode-label-distress-signal"),
        ("Jailbreak", "rmc-lobby-gamemode-label-jailbreak")
    };

    public static string GetDisplayJobName(JobPrototype job)
    {
        var name = !string.IsNullOrWhiteSpace(job.SpawnMenuRoleName)
            ? (IoCManager.Resolve<ILocalizationManager>().TryGetString(job.SpawnMenuRoleName, out var loc) ? loc : job.SpawnMenuRoleName)
            : job.LocalizedName;
        return TrimHiddenFactionSuffix(name);
    }

    public static string GetLocalizedJobName(JobPrototype job) // RuMC edit
    {
        return TrimHiddenFactionSuffix(job.LocalizedName);
    }

    public static List<LobbyHighJobPreviewEntry> GetHighPriorityJobs(
        HumanoidCharacterProfile profile,
        IPrototypeManager prototypeManager)
    {
        var jobOrder = new List<string>();
        var jobs = new Dictionary<string, JobPrototype>();
        var gamemodeLabels = new Dictionary<string, List<string>>();

        foreach (var (gamemode, label) in Gamemodes)
        {
            foreach (var (jobId, priority) in profile.GetJobPrioritiesForGamemode(gamemode))
            {
                if (priority != JobPriority.High ||
                    !prototypeManager.TryIndex(jobId, out JobPrototype? job))
                {
                    continue;
                }

                if (!jobs.ContainsKey(job.ID))
                {
                    jobOrder.Add(job.ID);
                    jobs[job.ID] = job;
                    gamemodeLabels[job.ID] = new List<string>();
                }

                var labels = gamemodeLabels[job.ID];
                var localizedLabel = Loc.GetString(label); // RuMC edit
                if (!labels.Contains(localizedLabel))
                    labels.Add(localizedLabel);
            }
        }

        var entries = new List<LobbyHighJobPreviewEntry>();
        foreach (var jobId in jobOrder)
        {
            entries.Add(new LobbyHighJobPreviewEntry(jobs[jobId], gamemodeLabels[jobId]));
        }

        return entries;
    }

    public static string GetSignature(IReadOnlyList<LobbyHighJobPreviewEntry> entries)
    {
        if (entries.Count == 0)
            return string.Empty;

        return string.Join("|", entries.Select(entry => entry.Signature));
    }

    private static string TrimHiddenFactionSuffix(string name)
    {
        var trimmed = name.TrimEnd();
        foreach (var suffix in HiddenFactionSuffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(0, trimmed.Length - suffix.Length).TrimEnd();
        }

        return name;
    }
}
