using System.Linq;
using Content.Shared._RMC14.LinkAccount;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Sponsors;

/// <summary>Account-wide choices survive subscription loss. Entitlements are checked when applying them.</summary>
[Serializable, NetSerializable]
public sealed record CMUSponsorSettings(
    string Ghost = "",
    string Inscription = "",
    string Patch = "",
    string Cape = "",
    string Trim = "",
    string Emblem = "",
    string FigurineDescription = "",
    bool PublicRecognition = true,
    int? GhostColor = null);

/// <summary>Allowlisted cosmetics. These choices never select arbitrary entity prototypes.</summary>
public static class CMUSponsorCatalog
{
    public const int InscriptionLimit = 60;
    public const int DescriptionLimit = 200;

    public static readonly IReadOnlyDictionary<string, string> Ghosts = new Dictionary<string, string>
    {
        ["flame"] = "ghost_Heat0",
        ["frost"] = "ghost_Cold0",
        ["spark"] = "ghost_Shock0",
        ["glow"] = "ghost_Radiation0",
        ["tattered"] = "ghost_Explosion0",
    };

    public static readonly IReadOnlyDictionary<string, string> Patches = new Dictionary<string, string>
    {
        ["peace"] = "peace_patch",
        ["branch"] = "olive_branch_patch",
        ["skull"] = "merc_patch",
    };

    public static readonly string[] Trims = ["gold", "silver", "red"];

    public static string PatchPrototype(string choice) => "CMUSponsorPatch" + char.ToUpperInvariant(choice[0]) + choice[1..];

    public static bool CanUseCapes(SharedRMCPatronTier? tier) => tier is { Figurines: true, Priority: <= 4 };
    public static bool CanCustomizeCape(SharedRMCPatronTier? tier) => tier is { LobbyMessage: true, Priority: <= 3 };

    public static bool CanUseCape(SharedRMCPatronTier? tier, string cape)
    {
        return CanUseCapes(tier)
            && int.TryParse(cape, out var number)
            && number is >= 1 and <= 16
            && cape == number.ToString("D2")
            && (number <= 8 || CanCustomizeCape(tier));
    }

    public static bool TryValidate(SharedRMCPatronTier? tier, CMUSponsorSettings current,
        CMUSponsorSettings requested, out CMUSponsorSettings validated)
    {
        validated = current;
        // Keep locked saved choices, but reject attempts to select a new perk above the current tier.
        if (requested.GhostColor != current.GhostColor && requested.GhostColor != null && tier?.GhostColor != true)
            return false;
        if (!Choice(requested.Ghost, current.Ghost, tier?.GhostColor == true, Ghosts.ContainsKey)
            || !Text(requested.Inscription, current.Inscription, tier?.NamedItems == true, InscriptionLimit)
            || !Choice(requested.Patch, current.Patch, tier?.NamedItems == true, Patches.ContainsKey)
            || !Choice(requested.Cape, current.Cape, CanUseCapes(tier), c => CanUseCape(tier, c))
            || !Choice(requested.Trim, current.Trim, CanCustomizeCape(tier), c => Trims.Contains(c))
            || !Choice(requested.Emblem, current.Emblem, CanCustomizeCape(tier), Patches.ContainsKey)
            || !Text(requested.FigurineDescription, current.FigurineDescription, tier?.Figurines == true, DescriptionLimit))
            return false;

        validated = requested with
        {
            Inscription = requested.Inscription.Trim(),
            FigurineDescription = requested.FigurineDescription.Trim(),
            // Alpha is fixed so cosmetics cannot make observers invisible.
            GhostColor = requested.GhostColor is { } color ? (int)((uint)color & 0x00FFFFFF | 0x88000000) : null,
        };
        return true;
    }

    private static bool Choice(string? requested, string current, bool allowed, Func<string, bool> valid)
    {
        return requested != null && (requested == current || requested.Length == 0 || allowed && valid(requested));
    }

    private static bool Text(string? requested, string current, bool allowed, int limit)
    {
        return requested != null && requested.Length <= limit && !requested.Any(char.IsControl)
            && (requested == current || requested.Length == 0 || allowed);
    }
}
