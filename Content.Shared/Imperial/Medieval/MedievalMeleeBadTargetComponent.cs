namespace Content.Shared.MedievalMeleeResource.Components
{
    [RegisterComponent]
    public sealed partial class MedievalMeleeBadTargetComponent : Component
    {
        [DataField]
        public float BreakMultiplier = 5f;

        [DataField]
        public string SafeToHitGroup = "nothing";
    }
}
