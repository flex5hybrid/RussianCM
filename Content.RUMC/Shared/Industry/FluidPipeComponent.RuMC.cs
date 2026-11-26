namespace Content.Shared.CMU14.Fluids;

// RuMC: the liquid pipe going up.
public sealed partial class FluidPipeComponent
{
    // A pipe that goes up: it also passes its liquid to the pipe in the same place on the level above.
    [DataField]
    public bool Up;
}
