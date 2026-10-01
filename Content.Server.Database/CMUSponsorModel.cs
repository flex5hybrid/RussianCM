// CMU14: persistent sponsor cosmetic choices and moderator approvals.
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Content.Server.Database;

/// <summary>Separate from RMCPatron so removing a subscription preserves cosmetic preferences and approvals.</summary>
[Table("cmu_sponsor_preferences")]
public sealed class CMUSponsorPreferences
{
    [Key]
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = default!;
    public string Settings { get; set; } = "{}";
    public string ApprovedFigurineDescription { get; set; } = "";
    public string CustomItem { get; set; } = "";
}
