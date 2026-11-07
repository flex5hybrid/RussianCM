namespace Content.Server._RuCM.Balance;

/// <summary>History of completed rounds; selecting a mode or restarting a lobby never advances it.</summary>
public sealed class RuDistressRotationState
{
    public const string Distress = "DistressSignal";
    public int Misses { get; private set; }
    public bool LastWasDistress { get; private set; }

    public void RecordCompleted(string? preset, bool distressEligible)
    {
        LastWasDistress = preset == Distress;
        if (LastWasDistress)
            Misses = 0;
        else if (distressEligible && Misses < int.MaxValue)
            Misses++;
    }

    public static bool IsUnattendedServerVote(bool serverInitiated, IReadOnlyList<int> votes)
    {
        if (!serverInitiated)
            return false;
        foreach (var count in votes)
        {
            if (count != 0)
                return false;
        }
        return true;
    }

    public bool IsGuaranteed(bool eligible, int guaranteeAfter)
        => eligible && !LastWasDistress && guaranteeAfter > 0 && Misses >= guaranteeAfter;

    public double GetWeight(double baseWeight, double growth, double maximumMultiplier)
    {
        if (!double.IsFinite(baseWeight) || baseWeight <= 0)
            return 0;
        growth = double.IsFinite(growth) ? Math.Max(0, growth) : 0;
        maximumMultiplier = double.IsFinite(maximumMultiplier) ? Math.Max(1, maximumMultiplier) : 1;
        return baseWeight * Math.Min(maximumMultiplier, Math.Pow(1 + growth, Misses));
    }
}
