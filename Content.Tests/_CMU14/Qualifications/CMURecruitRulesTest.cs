using System.IO;
using System.Xml.Linq;
using Content.Tests._RuCM.Guidebook;
using NUnit.Framework;

namespace Content.Tests.CMU14.Qualifications;

[TestFixture]
public sealed class CMURecruitRulesTest
{
    [TestCase("Content.CMU/Resources/ServerInfo/Guidebook/CMU14/Rules/Gamemodes/AU14GamemodeRulesInsurgency.xml")]
    [TestCase("Resources/ServerInfo/Guidebook/_RuCM/GOVFOR/TrainingQualifications.xml")]
    [TestCase("Resources/ServerInfo/Guidebook/_RuCM/GOVFOR/RecruitCourse/Introduction.xml")]
    [TestCase("Resources/ServerInfo/Guidebook/_RuCM/GOVFOR/InstructorHandbook/Work.xml")]
    public void PublishedRulesProhibitRecruitTasksOutsideTheBaseWithoutCommandOrEmergencyExceptions(string path)
    {
        var text = XDocument.Load(GOVFORRegulationsTests.Find(path)).Root!.Value;
        Assert.That(text, Does.Contain("вне базы"));
        Assert.That(text, Does.Contain("FOB"));
        Assert.That(text, Does.Contain("корабл"));
        Assert.That(text, Does.Contain("чрезвычайны"));
        Assert.That(text, Does.Contain("правил сервера"));
    }

    [Test]
    public void TrainingRegulationsCannotAuthorizeEmergencyDeployment()
    {
        foreach (var path in new[]
        {
            "Resources/RuCM/GOVFOR/Regulations/TrainingQualifications.md",
            "Resources/ServerInfo/Guidebook/_RuCM/GOVFOR/TrainingQualifications.xml"
        })
        {
            var text = File.ReadAllText(GOVFORRegulationsTests.Find(path));
            Assert.That(text, Does.Not.Contain("вправе разрешить их развёртывание"));
            Assert.That(text, Does.Not.Contain("при чрезвычайном развёртывании"));
        }
    }
}
