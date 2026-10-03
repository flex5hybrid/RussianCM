using Robust.Shared.Configuration;

namespace Content.Shared._RuCM.Qualifications;

[CVarDefs]
public sealed class QualificationCVars
{
    public static readonly CVarDef<bool> Enabled = CVarDef.Create("rucm.qualifications.enabled", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Enforce = CVarDef.Create("rucm.qualifications.enforce", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> FailOpen = CVarDef.Create("rucm.qualifications.fail_open", true, CVar.SERVERONLY);
    public static readonly CVarDef<string> Connection = CVarDef.Create("rucm.qualifications.connection", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
    public static readonly CVarDef<string> ServerId = CVarDef.Create("rucm.qualifications.server_id", "RussianCM", CVar.SERVERONLY);
}
