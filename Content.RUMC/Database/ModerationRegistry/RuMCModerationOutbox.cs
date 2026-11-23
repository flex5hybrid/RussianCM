using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("rumc_moderation_outbox")]
[Index(nameof(WebhookId), nameof(AvailableAt), Name = "IX_rumc_moderation_outbox_webhook_id_available_at")]
public sealed class RuMCModerationOutbox
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public required string WebhookId { get; set; }
    public required string Payload { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public Guid? LeaseId { get; set; }
}
