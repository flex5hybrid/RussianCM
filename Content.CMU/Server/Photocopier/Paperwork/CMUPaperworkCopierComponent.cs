using Content.Shared.CMU14.Photocopier.Paperwork;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Photocopier.Paperwork;

[RegisterComponent]
public sealed partial class CMUPaperworkCopierComponent : Component
{
    /// <summary>
    /// Слот для копируемого документа
    /// </summary>
    [DataField(required: true)]
    public ItemSlot PaperSlot = new();

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/scanning.ogg");

    [ViewVariables]
    [DataField]
    public Queue<CMUPaperworkCopierPrintout> PrintingQueue { get; private set; } = new();

    [ViewVariables]
    [DataField]
    public float UseTimeoutRemaining;

    [ViewVariables]
    [DataField]
    public float UseTimeout = 2.6f;

    [DataField]
    public float ScanningTimeRemaining;

    [ViewVariables]
    public float ScanningTime = 12.1f;

    [DataField]
    public float PrintingTimeRemaining;

    [ViewVariables]
    public float PrintingTime = 2.3f;

    /// <summary>
    /// Категории в принтере
    /// </summary>
    [DataField(required: true)]
    public HashSet<CMUPaperworkFormCategory> AllowedFormCategories = default!;

    [DataField]
    public ProtoId<CMUPaperworkFormPrototype>? ChosenPaper = null;
}

[DataDefinition]
public sealed partial class CMUPaperworkCopierPrintout
{
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField(required: true)]
    public string Content { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId PrototypeId { get; private set; } = default!;

    [DataField]
    public string? StampState { get; private set; }

    [DataField]
    public List<StampDisplayInfo> StampedBy { get; private set; } = new();

    public CMUPaperworkCopierPrintout(string content, string name, string prototypeId, string? stampState = null, List<StampDisplayInfo>? stampedBy = null)
    {
        Content = content;
        Name = name;
        PrototypeId = prototypeId;
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
    }
}
