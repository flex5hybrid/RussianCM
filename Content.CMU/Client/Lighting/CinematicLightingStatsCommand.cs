using Robust.Shared.Console;

namespace Content.Client.CMU14.Lighting;

/// <summary>Diagnostic counters only. The CPU interval is not a GPU timer.</summary>
public sealed partial class CinematicLightingStatsCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entities = default!;
    public string Command => "cmu_cinematic_stats";
    public string Description => Loc.GetString("cmu-cinematic-stats-description");
    public string Help => Loc.GetString("cmu-cinematic-stats-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var stats = _entities.System<CinematicLightingSystem>().LastFrame;
        shell.WriteLine(Loc.GetString("cmu-cinematic-stats-result", ("quality", stats.Quality),
            ("sources", stats.Sources), ("beams", stats.AtmosphereSources), ("edges", stats.OccluderEdges),
            ("rebuilds", stats.MeshRebuilds), ("targets", stats.Targets),
            ("memory", (stats.TargetBytes / 1048576d).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            ("cpu", stats.CpuMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));
    }
}
