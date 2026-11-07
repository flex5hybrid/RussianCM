using Content.Server._RuCM.Balance;
using Content.Server.CMU14.Threats;
using Content.Shared.CMU14.Threats;

namespace Content.IntegrationTests._RuCM.Balance;

[TestFixture]
public sealed class RuRotationRulesTest
{
    [Test]
    public void FifthEligibleSelectionIsGuaranteedAndIneligibleRoundsDoNotAdvanceHistory()
    {
        var state = new RuDistressRotationState();
        for (var i = 0; i < 4; i++)
        {
            Assert.That(state.IsGuaranteed(true, 4), Is.False);
            state.RecordCompleted("ColonyFall", true);
            var misses = state.Misses;
            state.RecordCompleted("ColonyShift", false);
            Assert.That(state.Misses, Is.EqualTo(misses));
        }

        Assert.That(state.IsGuaranteed(true, 4), Is.True);
        Assert.That(state.IsGuaranteed(false, 4), Is.False);
        Assert.That(state.IsGuaranteed(true, 0), Is.False, "zero disables the guarantee");
        state.RecordCompleted(RuDistressRotationState.Distress, false);
        Assert.That(state.Misses, Is.Zero, "a conducted manual Distress also resets history");
        Assert.That(state.LastWasDistress, Is.True);
        Assert.That(state.IsGuaranteed(true, 4), Is.False);
    }

    [Test]
    public void WeightGrowsToACapWithoutAdvancingHistory()
    {
        var state = new RuDistressRotationState();
        var previous = state.GetWeight(3, 0.25, 1.75);
        for (var i = 0; i < 20; i++)
        {
            state.RecordCompleted("ColonyFall", true);
            var weight = state.GetWeight(3, 0.25, 1.75);
            Assert.That(weight, Is.GreaterThanOrEqualTo(previous).And.LessThanOrEqualTo(3 * 1.75));
            var misses = state.Misses;
            Assert.That(state.GetWeight(3, 0.25, 1.75), Is.EqualTo(weight));
            Assert.That(state.Misses, Is.EqualTo(misses));
            previous = weight;
        }
        Assert.That(previous, Is.EqualTo(3 * 1.75));
        state.RecordCompleted(RuDistressRotationState.Distress, true);
        Assert.That(state.GetWeight(3, 0.25, 1.75), Is.EqualTo(3));
    }

    [TestCase(true, 0, 0, true)]
    [TestCase(true, 1, 0, false)]
    [TestCase(true, 2, 2, false)]
    [TestCase(false, 0, 0, false)]
    public void ExplicitVotesAndPlayerInitiatedBallotsRetainPriority(bool server, int first, int second, bool automatic)
    {
        Assert.That(RuDistressRotationState.IsUnattendedServerVote(server, new[] { first, second }), Is.EqualTo(automatic));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    [TestCase(9)]
    [TestCase(20)]
    [TestCase(50)]
    [TestCase(100)]
    [TestCase(200)]
    public void ForceBudgetIsBoundedAndOptionalMinimumKeepsHumanSeats(int players)
    {
        var defaults = new ThreatPrototype();
        var tuned = new ThreatPrototype { RoundPlayerBudgetUp = true, MinimumPlayerBudget = 3 };
        var original = ThreatVoteSelection.CalculateThreatPlayerBudget(players, defaults);
        var budget = ThreatVoteSelection.CalculateThreatPlayerBudget(players, tuned);
        Assert.That(budget, Is.InRange(0, players));
        Assert.That(budget, Is.GreaterThanOrEqualTo(original));
        Assert.That(budget, Is.LessThanOrEqualTo(Math.Max(original + 1, Math.Min(3, players / 2))));
        Assert.That(ThreatVoteSelection.CalculateThreatPlayerBudget(players, new ThreatPrototype()),
            Is.EqualTo(ThreatVoteSelection.CalculateThreatPlayerBudget(players, defaults.ThreatRatio)),
            "other threats retain the existing formula");
    }

    [Test]
    public void UpwardRoundingAndMinimumHandleSmallPopulations()
    {
        var threat = new ThreatPrototype { RoundPlayerBudgetUp = true, MinimumPlayerBudget = 3 };
        Assert.That(ThreatVoteSelection.CalculateThreatPlayerBudget(9, threat),
            Is.GreaterThan(ThreatVoteSelection.CalculateThreatPlayerBudget(9, threat.ThreatRatio)));
        Assert.That(ThreatVoteSelection.CalculateThreatPlayerBudget(6, threat), Is.EqualTo(3));
        Assert.That(ThreatVoteSelection.CalculateThreatPlayerBudget(0, threat), Is.Zero);
    }
}
