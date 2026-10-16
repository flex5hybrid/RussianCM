using Content.Server.CMU14.AdminAI;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture]
public sealed class AdminAIConfigurationTests
{
    [Test]
    public void ConfigurationIsServerOnlyAndDisabledByDefault()
    {
        Assert.That(AdminAICVars.Enabled.DefaultValue, Is.False);
        Assert.That(AdminAICVars.SafeFAQ.DefaultValue, Is.False);
        Assert.That(AdminAICVars.Clarification.DefaultValue, Is.False);
        Assert.That(AdminAICVars.Investigation.DefaultValue, Is.False);
        Assert.That(AdminAICVars.DraftOutputTokens.DefaultValue, Is.EqualTo(128));
        Assert.That(AdminAICVars.GovernorEnabled.DefaultValue, Is.True);
        Assert.That(AdminAICVars.MaxConcurrentRequests.DefaultValue, Is.EqualTo(1));
        foreach (var definition in AdminAICVars.RuntimeBools.Cast<CVarDef>().Concat(AdminAICVars.RuntimeInts)
                     .Concat(AdminAICVars.RuntimeFloats).Concat(AdminAICVars.RuntimeStrings))
        {
            Assert.That(definition.Flags.HasFlag(CVar.SERVERONLY), Is.True);
            Assert.That((definition.Flags & (CVar.REPLICATED | CVar.NOTIFY)), Is.EqualTo(CVar.NONE));
        }
    }
}
