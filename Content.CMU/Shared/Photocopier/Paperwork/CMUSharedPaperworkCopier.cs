using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Photocopier.Paperwork;

[Serializable, NetSerializable]
public enum CMUPaperworkCopierUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum CMUPaperworkCopierStage : byte
{
    Idle,
    Scanning,
    Printing,
    Cooldown,
}

[Serializable, NetSerializable]
public sealed class CMUPaperworkCopierUiState : BoundUserInterfaceState
{
    public bool CanPrint { get; }
    public bool IsPowered { get; }
    public CMUPaperworkCopierStage Stage { get; }

    public float BusyRemaining { get; }
    public float BusyTotal { get; }

    public string? InsertedPaperName { get; }
    public string? InsertedPaperContent { get; }
    public ProtoId<CMUPaperworkFormPrototype>? ChosenForm { get; }
    public HashSet<CMUPaperworkFormCategory> AllowedFormCategories { get; }

    public bool IsPaperInserted => InsertedPaperName != null;

    public CMUPaperworkCopierUiState(
        bool canPrint,
        bool isPowered,
        CMUPaperworkCopierStage stage,
        float busyRemaining,
        float busyTotal,
        string? insertedPaperName,
        string? insertedPaperContent,
        ProtoId<CMUPaperworkFormPrototype>? chosenForm,
        HashSet<CMUPaperworkFormCategory> allowedFormCategories)
    {
        CanPrint = canPrint;
        IsPowered = isPowered;
        Stage = stage;
        BusyRemaining = busyRemaining;
        BusyTotal = busyTotal;
        InsertedPaperName = insertedPaperName;
        InsertedPaperContent = insertedPaperContent;
        ChosenForm = chosenForm;
        AllowedFormCategories = allowedFormCategories;
    }
}

[Serializable, NetSerializable]
public sealed class CMUPaperworkCopierChoseFormMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<CMUPaperworkFormPrototype> PaperworkForm;

    public CMUPaperworkCopierChoseFormMessage(ProtoId<CMUPaperworkFormPrototype> paperworkForm)
    {
        PaperworkForm = paperworkForm;
    }
}

[Serializable, NetSerializable]
public sealed class CMUPaperworkCopierPrintMessage : BoundUserInterfaceMessage
{
    public readonly CMUPaperworkCopierMode Mode;

    public CMUPaperworkCopierPrintMessage(CMUPaperworkCopierMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public enum CMUPaperworkCopierMode : byte
{
    Copy,
    Print,
}

/// <summary>
/// Категории
/// </summary>
[Serializable, NetSerializable]
public enum CMUPaperworkFormCategory : byte
{
    OA,
    UPP,
    TWE,
    UN,
    WeY,
    CLF,
    Colony,
    Global,
}

public static class CMUPaperworkTextSubstitutions
{
    public static string ApplyBase(string text, string documentName, TimeSpan roundDuration, string? stationName)
    {
        text = text.Replace("DOCUMENT NAME", documentName);
        text = text.Replace("{{HOUR.MINUTE.SECOND}}", roundDuration.ToString("hh\\:mm\\:ss"));
        text = text.Replace("{{DAY.MONTH.YEAR}}", DateTime.UtcNow.AddHours(3).ToString("dd.MM") + ".2710");

        if (stationName != null)
            text = text.Replace("STATION XX-00", stationName);

        return text;
    }
}
