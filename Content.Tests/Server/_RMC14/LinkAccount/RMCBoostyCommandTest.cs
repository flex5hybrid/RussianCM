using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Administration;
using Content.Shared.Administration;
using NUnit.Framework;

namespace Content.Tests.Server._RMC14.LinkAccount;

[TestFixture]
public sealed class RMCBoostyCommandTest
{
    [Test]
    public void CommandRequiresHostFlag()
    {
        var attribute = typeof(RMCBoostyCommand)
            .GetCustomAttributes(typeof(AdminCommandAttribute), inherit: false)
            .Cast<AdminCommandAttribute>()
            .Single();

        Assert.That(attribute.Flags, Is.EqualTo(AdminFlags.Host));
    }
}
