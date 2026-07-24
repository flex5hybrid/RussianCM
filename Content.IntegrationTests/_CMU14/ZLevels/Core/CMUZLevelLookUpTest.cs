using Content.Shared._CMU14.ZLevels.Core.Components;
using Content.Shared._CMU14.ZLevels.Core.EntitySystems;
using Robust.Server;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Content.IntegrationTests._CMU14.ZLevels.Core;

[TestFixture]
public sealed class CMUZLevelLookUpTest
{
    [Test]
    public async Task FirstToggleImmediatelyEnablesLookUp()
    {
        var options = new RobustIntegrationTest.ServerIntegrationOptions
        {
            ContentStart = true,
            FailureLogLevel = LogLevel.Fatal,
            Options = new ServerOptions
            {
                LoadConfigAndUserData = false,
                LoadContentResources = true,
            },
            ContentAssemblies =
            [
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Server.Entry.EntryPoint).Assembly,
            ],
        };

        using var server = new RobustIntegrationTest.ServerIntegrationInstance(options);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var viewer = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            try
            {
                var viewerComp = entMan.EnsureComponent<CMUZLevelViewerComponent>(viewer);
                var toggle = new CMUToggleZLevelLookUpAction();

                entMan.EventBus.RaiseLocalEvent(viewer, toggle);

                Assert.Multiple(() =>
                {
                    Assert.That(toggle.Handled, Is.True);
                    Assert.That(viewerComp.LookUp, Is.True,
                        "The first toggle must use the original two-state normal/look-up behavior.");
                });
            }
            finally
            {
                entMan.DeleteEntity(viewer);
            }
        });
    }
}
