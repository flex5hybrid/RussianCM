using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.DonorCapes;

[Prototype("rmcDonorCape")]
public sealed partial class RMCDonorCapePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public LocId Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public EntProtoId Item { get; private set; } = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

    [DataField]
    public SpriteSpecifier? Preview { get; private set; }

    [DataField]
    public int Number { get; private set; }

    [DataField]
    public int RequiredPriority { get; private set; } = 4;
}
