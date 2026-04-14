using Robust.Shared.GameObjects;

namespace Content.Shared.RuMC14.Predator;

[RegisterComponent]
public sealed partial class PredatorComponent : Component
{
    [DataField]
    public PredatorColor Color = PredatorColor.Tan;
}

public enum PredatorColor
{
    Tan,
    Green,
    Purple,
    Blue,
    Red,
    Black
}
