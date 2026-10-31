using Robust.Shared.Console;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Client.CMU14.Lighting;

internal enum CinematicDebugView : byte
{
    Final, Native, Direct, Indirect, Emissive, Core, Inner, Outer, Reflections, Materials, Atmosphere, Occlusion, Bloom, Contacts,
}

/// <summary>Client-local diagnostic switches. No readback, replication or gameplay light changes.</summary>
internal sealed class CinematicLightingControls
{
    public CinematicDebugView View;
    public bool Direct = true;
    public bool Indirect = true;
    public bool Reflections = true;
    public bool Glow = true;
    public bool Atmosphere = true;
    public bool Contacts = true;
    public int EnabledMask => (Direct ? 1 : 0) | (Indirect ? 2 : 0) | (Reflections ? 4 : 0)
        | (Glow ? 8 : 0) | (Atmosphere ? 16 : 0) | (Contacts ? 32 : 0);

    public void Reset()
    {
        View = CinematicDebugView.Final;
        Direct = Indirect = Reflections = Glow = Atmosphere = Contacts = true;
    }
}

public sealed partial class CinematicLightingDebugCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IClyde _clyde = default!;
    public string Command => "cmu_cinematic_debug";
    public string Description => Loc.GetString("cmu-cinematic-debug-description");
    public string Help => Loc.GetString("cmu-cinematic-debug-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 2 && args[0] == "quality" && int.TryParse(args[1], out var quality)
            && quality is >= 0 and <= 3)
        {
            _config.SetCVar(CCVars.CMUCinematicLighting, quality);
            return;
        }
        if (args.Length == 3 && args[0] == "window" && int.TryParse(args[1], out var width)
            && int.TryParse(args[2], out var height) && width is >= 640 and <= 3840 && height is >= 480 and <= 2160)
        {
            _clyde.MainWindow.Size = new Vector2i(width, height);
            return;
        }
        var controls = _entities.System<CinematicLightingSystem>().Controls;
        if (args.Length == 1 && args[0] is "all" or "off" or "final")
        {
            controls.Reset();
            return;
        }
        if (args.Length == 1 && Enum.TryParse<CinematicDebugView>(args[0], true, out var view))
        {
            controls.View = view;
            return;
        }
        if (args.Length == 2 && args[0] is "enable" or "disable")
        {
            var enabled = args[0] == "enable";
            switch (args[1])
            {
                case "direct": controls.Direct = enabled; return;
                case "indirect": controls.Indirect = enabled; return;
                case "reflections": controls.Reflections = enabled; return;
                case "glow": controls.Glow = enabled; return;
                case "atmosphere": controls.Atmosphere = enabled; return;
                case "contacts": controls.Contacts = enabled; return;
            }
        }
        shell.WriteError(Help);
    }
}
