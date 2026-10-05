using System;
using System.Collections.Generic;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RuCM.Qualifications;

[Serializable, NetSerializable]
public enum QualificationAction
{
    View, Complete, Certify, Note, Suspend, ConfirmSuspension, Restore, Revoke, Grant,
    SaveRole, SaveDefinition, SaveInstructor, SaveManagement, SaveCommandJobs, CorrectProgress,
    MigrationPreview, MigrationExecute, SaveMigrationSettings, ResetRecruit
}

[Serializable, NetSerializable]
public sealed class QualificationEuiState : EuiStateBase
{
    public QualificationView View { get; }
    public QualificationEuiState(QualificationView view) { View = view; }
}

[Serializable, NetSerializable]
public sealed class QualificationEuiRequest : EuiMessageBase
{
    public QualificationAction Action { get; }
    public QualificationRequest Request { get; }
    public QualificationEuiRequest(QualificationAction action, QualificationRequest request) { Action = action; Request = request; }
}

[Serializable, NetSerializable]
public enum QualificationUiKey { Main }

[Serializable, NetSerializable]
public sealed class QualificationBoundRequest : BoundUserInterfaceMessage
{
    public QualificationAction Action { get; }
    public QualificationRequest Request { get; }
    public QualificationBoundRequest(QualificationAction action, QualificationRequest request) { Action = action; Request = request; }
}

[Serializable, NetSerializable]
public sealed class QualificationBoundView : BoundUserInterfaceMessage
{
    public QualificationView View { get; }
    public QualificationBoundView(QualificationView view) { View = view; }
}

// Engine-serialized contracts. Only server persistence/API adapters use JSON.
[Serializable, NetSerializable]
public sealed class QualificationRequest
{
    public Guid RequestId { get; set; }
    public Guid Target { get; set; }
    public string TargetName { get; set; } = "";
    public string Qualification { get; set; } = "";
    public string Item { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid Suspension { get; set; }
    public long Revision { get; set; }
    public string Payload { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public QualificationConfiguration? Configuration { get; set; }
}

[Serializable, NetSerializable]
public sealed class QualificationConfiguration
{
    public RoleRequirement? Role { get; set; }
    public QualificationDefinition? Definition { get; set; }
    public InstructorAccreditation? Instructor { get; set; }
    public HashSet<Guid>? Management { get; set; }
    public HashSet<string>? OfficerJobs { get; set; }
    public HashSet<string>? CommandingOfficerJobs { get; set; }
    public Dictionary<string, HashSet<string>>? MigrationGroups { get; set; }
    public Dictionary<string, string>? TrackerAliases { get; set; }
    public HashSet<Guid>? Roster { get; set; }
}

[Serializable, NetSerializable]
public sealed class QualificationMigrationPreview
{
    public int AccountsScanned { get; set; }
    public int EligibleAccounts { get; set; }
    public Dictionary<string, int> Counts { get; set; } = new();
    public int Records { get; set; }
}

[Serializable, NetSerializable]
public sealed class QualificationView
{
    public Guid ResponseId { get; set; }
    public QualificationStore Store { get; set; } = new();
    public Guid Viewer { get; set; }
    public Guid Target { get; set; }
    public bool Management { get; set; }
    public bool Instructor { get; set; }
    public bool InstructorOnDuty { get; set; }
    public bool TargetOnline { get; set; }
    public bool TargetSynthetic { get; set; }
    public bool Officer { get; set; }
    public bool CommandingOfficer { get; set; }
    public bool Administrator { get; set; }
    public bool Available { get; set; }
    public bool Enforcing { get; set; }
    public string Error { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public QualificationMigrationPreview? Preview { get; set; }
    public System.Collections.Generic.Dictionary<Guid, string> OnlinePlayers { get; set; } = new();
    public Dictionary<Guid, string> AccountNames { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> Jobs { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, double> Metrics { get; set; } = new();
}
