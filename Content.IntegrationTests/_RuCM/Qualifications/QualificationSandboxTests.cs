using Content.Shared._RuCM.Qualifications;
using Robust.Client;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests._RuCM.Qualifications;

/// <summary>Real loader checks, including the production whitelist and IL verification.</summary>
public sealed class QualificationSandboxTests
{
    [Test]
    public async Task ClientAndSharedAssembliesPassRealSandboxChecks()
    {
        using var client = new RobustIntegrationTest.ClientIntegrationInstance(new RobustIntegrationTest.ClientIntegrationOptions
        {
            ContentStart = false,
            ContentAssemblies = [],
            Options = new GameControllerOptions
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
                PrototypeDirectory = new ResPath("/RuCMQualificationSandboxPrototypes"),
                MountOptions = new MountOptions(dirMounts: ["../../RobustToolbox/Resources"], zipMounts: []),
            },
        });
        await client.WaitIdleAsync();
        await client.CheckSandboxed(typeof(Content.Client._RuCM.Qualifications.QualificationEui).Assembly);
        await client.CheckSandboxed(typeof(QualificationStore).Assembly);
    }
}
