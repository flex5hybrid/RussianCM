using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.CMU14.Photocopier.Paperwork;

[Prototype("cmuPaperworkForm")]
public sealed partial class CMUPaperworkFormPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public CMUPaperworkFormCategory Category { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public ResPath Text = default!;

    [DataField(required: true)]
    public EntProtoId PaperPrototype = default!;
}
