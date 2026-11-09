using System.Numerics;
using Content.Shared.CMU14.HumanAnimation;
using NUnit.Framework;

namespace Content.CMU.Tests.HumanAnimation;

[TestFixture]
public sealed class CMUHumanAnimationTest
{
    [TestCase(false)]
    [TestCase(true)]
    public void UnconsciousOrDeadBodyCannotPlayVoluntaryActions(bool down)
    {
        Assert.That(CMUHumanAnimation.Select(false, down, true, true, true, true, true, true),
            Is.EqualTo(CMUHumanAnimationKind.None));
    }

    [Test]
    public void ProneMovementOverridesRunAimAndWork()
    {
        Assert.That(CMUHumanAnimation.Select(true, true, true, true, true, true, true, true),
            Is.EqualTo(CMUHumanAnimationKind.Crawl));
        Assert.That(CMUHumanAnimation.Select(true, true, false, true, true, true, true, true),
            Is.EqualTo(CMUHumanAnimationKind.None));
    }

    [Test]
    public void AimingKeepsTheWeaponSteadyInsteadOfSwingingTheArms()
    {
        var kind = CMUHumanAnimation.Select(true, false, true, true, true, true, false, false);
        Assert.That(kind, Is.EqualTo(CMUHumanAnimationKind.CombatWalk));
        for (var i = 0; i < 12; i++)
            Assert.That(CMUHumanAnimation.Sample(kind, i / 12f, 0).Action.X, Is.Zero);
    }

    [Test]
    public void WorkingAnimationEndsWhenTheActionEnds()
    {
        Assert.That(CMUHumanAnimation.Select(true, false, false, false, false, false, false, true),
            Is.EqualTo(CMUHumanAnimationKind.Interact));
        Assert.That(CMUHumanAnimation.Select(true, false, false, false, false, false, false, false),
            Is.EqualTo(CMUHumanAnimationKind.Idle));
    }

    [Test]
    public void MovementLoopsMeetAtTheirSeam()
    {
        foreach (var kind in new[] { CMUHumanAnimationKind.Walk, CMUHumanAnimationKind.Run,
                     CMUHumanAnimationKind.HeavyWalk, CMUHumanAnimationKind.CombatWalk,
                     CMUHumanAnimationKind.Limp, CMUHumanAnimationKind.Crawl })
        {
            var start = CMUHumanAnimation.Sample(kind, 0, 0);
            var end = CMUHumanAnimation.Sample(kind, 1, 0);
            Assert.That(Vector4.Distance(start.Gait, end.Gait), Is.LessThan(0.00001f), kind.ToString());
            Assert.That(Vector4.Distance(start.Action, end.Action), Is.LessThan(0.00001f), kind.ToString());
        }
    }

    [Test]
    public void CombatAndPostureActionsLeaveNoPermanentDeformation()
    {
        foreach (var kind in new[] { CMUHumanAnimationKind.Melee, CMUHumanAnimationKind.Shoot,
                     CMUHumanAnimationKind.Fall, CMUHumanAnimationKind.GetUp })
        {
            var end = CMUHumanAnimation.Sample(kind, 0, 0, 1);
            Assert.That(end.Gait.Length(), Is.LessThan(0.00001f), kind.ToString());
            Assert.That(end.Action.Length(), Is.LessThan(0.00001f), kind.ToString());
        }
    }

    [Test]
    public void MissingLegOverridesOtherWalkingStyles()
    {
        Assert.That(CMUHumanAnimation.Select(true, false, true, true, true, true, true, false),
            Is.EqualTo(CMUHumanAnimationKind.Limp));
    }
}
