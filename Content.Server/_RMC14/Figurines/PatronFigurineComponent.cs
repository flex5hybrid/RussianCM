namespace Content.Server._RMC14.Figurines;

[RegisterComponent]
[Access(typeof(FigurineSystem), typeof(Content.Server.CMU14.Sponsors.CMUSponsorSystem))] // CMU14
public sealed partial class PatronFigurineComponent : Component
{
    [DataField(required: true)]
    public string Id;
}
