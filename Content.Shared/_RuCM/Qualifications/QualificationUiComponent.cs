namespace Content.Shared._RuCM.Qualifications;

/// <summary>Private, per-player BUI host installed only through the public UI API.</summary>
[RegisterComponent]
public sealed partial class QualificationUiComponent : Component;

public sealed record RuCMQualificationGrantedEvent(Guid Player, string Qualification, Guid Actor);
public sealed record RuCMQualificationSuspendedEvent(Guid Player, string Qualification, Guid Actor);
public sealed record RuCMQualificationRestoredEvent(Guid Player, string Qualification, Guid Actor);
public sealed record RuCMChecklistItemCompletedEvent(Guid Player, string Qualification, Guid Actor);
