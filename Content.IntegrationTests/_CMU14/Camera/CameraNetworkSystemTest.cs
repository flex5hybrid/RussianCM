using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Camera;
using Content.Server.Camera;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Power;
using Content.Server.Power.Components;
using Content.Server._RMC14.Camera;
using Content.Server.SurveillanceCamera;
using Content.Server.Wires;
using Content.Shared._RMC14.Camera;
using Content.Shared.Camera;
using Content.Shared.GameTicking;
using Content.Shared.Item;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Pinpointer;
using Content.Shared.SurveillanceCamera;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Content.IntegrationTests._CMU14.Camera;

[TestFixture]
public sealed class CameraNetworkSystemTest
{
    private const string NetworkA = "CMUTestCameraNetworkA";
    private const string NetworkB = "CMUTestCameraNetworkB";

    [Test]
    public void RmcEditorContractCarriesOpaqueIdsRevisionMembershipAndErrors()
    {
        var runtime = (ProtoId<CameraNetworkPrototype>) "CMURuntimeCameraNetwork10N1";
        var camera = new NetEntity(20);
        var editor = new RMCCameraNetworkEditorUiState(
            7,
            [new RMCCameraNetworkEditorNetworkUiData(
                runtime,
                "Landing pad",
                RMCCameraNetworkEditorOrigin.Owned,
                false)],
            [new RMCCameraNetworkEditorCameraUiData(camera, "North hall", [runtime])]);
        var save = new RMCCameraNetworkEditorSaveCameraBuiMsg(
            7,
            camera,
            "North hall 2",
            [runtime]);

        Assert.Multiple(() =>
        {
            Assert.That(editor.Revision, Is.EqualTo(7));
            Assert.That(editor.Networks.Single().Id, Is.EqualTo(runtime));
            Assert.That(editor.Cameras.Single().Networks, Is.EquivalentTo(new[] { runtime }));
            Assert.That(save.Revision, Is.EqualTo(7));
            Assert.That(save.Camera, Is.EqualTo(camera));
            Assert.That(new RMCCameraNetworkEditorResultBuiMsg(
                    RMCCameraNetworkEditorError.StaleRevision,
                    8).Error,
                Is.EqualTo(RMCCameraNetworkEditorError.StaleRevision));
        });
    }

    [Test]
    public async Task MemberNetworksBatchUpdatesIndexesBeforeNotification()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var first = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);
                var second = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);

                try
                {
                    entMan.AddComponent(receiver, new CameraNetworkReceiverChangedProbeComponent
                    {
                        FirstMember = first,
                        SecondMember = second,
                        ExpectedNetwork = NetworkB,
                    });

                    var updates = new Dictionary<EntityUid,
                        IReadOnlyCollection<ProtoId<CameraNetworkPrototype>>>
                    {
                        [first] = [(ProtoId<CameraNetworkPrototype>) NetworkB],
                        [second] = [(ProtoId<CameraNetworkPrototype>) NetworkB],
                    };

                    Assert.That(networks.SetMemberNetworksBatch(updates), Is.True);
                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);

                    Assert.Multiple(() =>
                    {
                        Assert.That(probe.Events, Is.EqualTo(1));
                        Assert.That(probe.SawBothUpdated, Is.True);
                        Assert.That(networks.GetNetworkMembers(NetworkA), Does.Not.Contain(first));
                        Assert.That(networks.GetNetworkMembers(NetworkA), Does.Not.Contain(second));
                        Assert.That(networks.GetNetworkMembers(NetworkB), Does.Contain(first));
                        Assert.That(networks.GetNetworkMembers(NetworkB), Does.Contain(second));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(first);
                    entMan.DeleteEntity(second);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorCreatesRenamesAndDeletesConsoleOwnedNetwork()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestRmcShipCamera", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    var initial = rmc.BuildEditorState((consoleUid, console));

                    Assert.That(rmc.TryCreateEditorNetwork(
                        (consoleUid, console), actor, initial.Revision, "  Landing pad  ", out var createError), Is.True);
                    Assert.That(createError, Is.EqualTo(RMCCameraNetworkEditorError.None));

                    var createdState = rmc.BuildEditorState((consoleUid, console));
                    var created = createdState.Networks.Single(network =>
                        network.Origin == RMCCameraNetworkEditorOrigin.Owned);
                    Assert.Multiple(() =>
                    {
                        Assert.That(created.Name, Is.EqualTo("Landing pad"));
                        Assert.That(created.Id.ToString(),
                            Does.StartWith($"CMURuntimeCameraNetwork{consoleUid.Id}N"));
                        Assert.That(networks.GetEffectiveNetworks(consoleUid), Does.Contain(created.Id));
                    });

                    Assert.That(rmc.TryRenameEditorNetwork(
                        (consoleUid, console), actor, createdState.Revision, created.Id, "Flight deck", out var renameError), Is.True);
                    Assert.That(renameError, Is.EqualTo(RMCCameraNetworkEditorError.None));
                    Assert.That(rmc.BuildEditorState((consoleUid, console)).Networks
                        .Single(network => network.Id == created.Id).Name, Is.EqualTo("Flight deck"));

                    Assert.That(networks.SetMemberNetworks(camera, [created.Id]), Is.True);
                    var renamedState = rmc.BuildEditorState((consoleUid, console));
                    Assert.That(rmc.TryDeleteEditorNetwork(
                        (consoleUid, console), actor, renamedState.Revision, created.Id, out var deleteError), Is.True);

                    var deletedState = rmc.BuildEditorState((consoleUid, console));
                    Assert.Multiple(() =>
                    {
                        Assert.That(deleteError, Is.EqualTo(RMCCameraNetworkEditorError.None));
                        Assert.That(deletedState.Networks.Select(network => network.Id), Does.Not.Contain(created.Id));
                        Assert.That(networks.GetEffectiveNetworks(consoleUid), Does.Not.Contain(created.Id));
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Does.Not.Contain(created.Id));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorOwnedNetworkIsIsolatedBetweenConsoles()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var firstUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var secondUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var first = entMan.GetComponent<RMCCameraComputerComponent>(firstUid);
                    var second = entMan.GetComponent<RMCCameraComputerComponent>(secondUid);
                    Assert.That(rmc.TryCreateEditorNetwork(
                        (firstUid, first), actor, 0, "Console one", out _), Is.True);
                    var created = rmc.BuildEditorState((firstUid, first)).Networks
                        .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned);

                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.GetEffectiveNetworks(firstUid), Does.Contain(created.Id));
                        Assert.That(networks.GetEffectiveNetworks(secondUid), Does.Not.Contain(created.Id));
                        Assert.That(rmc.BuildEditorState((secondUid, second)).Networks.Select(network => network.Id),
                            Does.Not.Contain(created.Id));
                        Assert.That(rmc.BuildAvailableNetworks(secondUid).Select(network => network.Id),
                            Does.Not.Contain(created.Id));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(firstUid);
                    entMan.DeleteEntity(secondUid);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorAliasesAndHidesSeededNetworkLocally()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var firstUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var secondUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var first = entMan.GetComponent<RMCCameraComputerComponent>(firstUid);
                    var second = entMan.GetComponent<RMCCameraComputerComponent>(secondUid);
                    Assert.That(rmc.TryRenameEditorNetwork(
                        (firstUid, first), actor, 0, NetworkA, "Local engineering", out _), Is.True);
                    var renamed = rmc.BuildEditorState((firstUid, first));
                    Assert.That(rmc.TrySetSeededNetworkHidden(
                        (firstUid, first), actor, renamed.Revision, NetworkA, true, out _), Is.True);

                    Assert.Multiple(() =>
                    {
                        Assert.That(rmc.BuildAvailableNetworks(firstUid).Select(network => network.Id),
                            Does.Not.Contain((ProtoId<CameraNetworkPrototype>) NetworkA));
                        Assert.That(rmc.BuildAvailableNetworks(secondUid).Single(network => network.Id == NetworkA).Name,
                            Is.EqualTo("cmu-test-camera-network-a"));
                        Assert.That(rmc.BuildEditorState((firstUid, first)).Networks
                            .Single(network => network.Id == NetworkA).Name, Is.EqualTo("Local engineering"));
                        Assert.That(rmc.BuildEditorState((firstUid, first)).Networks
                            .Single(network => network.Id == NetworkA).Hidden, Is.True);
                    });

                    var hidden = rmc.BuildEditorState((firstUid, first));
                    Assert.That(rmc.TrySetSeededNetworkHidden(
                        (firstUid, first), actor, hidden.Revision, NetworkA, false, out _), Is.True);
                    Assert.That(rmc.BuildAvailableNetworks(firstUid).Single(network => network.Id == NetworkA).Name,
                        Is.EqualTo("Local engineering"));
                    Assert.That(rmc.BuildEditorState((secondUid, second)).Revision, Is.Zero);
                }
                finally
                {
                    entMan.DeleteEntity(firstUid);
                    entMan.DeleteEntity(secondUid);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorRejectsDuplicateInvalidAndStaleNetworkChanges()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    Assert.Multiple(() =>
                    {
                        Assert.That(rmc.TryCreateEditorNetwork(
                            (consoleUid, console), actor, 0, "  ", out var emptyError), Is.False);
                        Assert.That(emptyError, Is.EqualTo(RMCCameraNetworkEditorError.InvalidName));
                        Assert.That(rmc.TryCreateEditorNetwork(
                            (consoleUid, console), actor, 0, new string('x', 49), out var longError), Is.False);
                        Assert.That(longError, Is.EqualTo(RMCCameraNetworkEditorError.InvalidName));
                    });

                    var seededName = rmc.BuildEditorState((consoleUid, console)).Networks
                        .Single(network => network.Id == NetworkA).Name;
                    Assert.That(rmc.TryCreateEditorNetwork(
                        (consoleUid, console), actor, 0, seededName.ToUpperInvariant(), out var duplicateError), Is.False);
                    Assert.That(duplicateError, Is.EqualTo(RMCCameraNetworkEditorError.DuplicateName));

                    Assert.That(rmc.TryCreateEditorNetwork(
                        (consoleUid, console), actor, 0, "Valid", out _), Is.True);
                    var owned = rmc.BuildEditorState((consoleUid, console)).Networks
                        .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned);
                    Assert.That(rmc.TryRenameEditorNetwork(
                        (consoleUid, console), actor, 0, owned.Id, "Stale overwrite", out var staleError), Is.False);

                    Assert.Multiple(() =>
                    {
                        Assert.That(staleError, Is.EqualTo(RMCCameraNetworkEditorError.StaleRevision));
                        Assert.That(rmc.BuildEditorState((consoleUid, console)).Revision, Is.EqualTo(1));
                        Assert.That(rmc.BuildEditorState((consoleUid, console)).Networks
                            .Single(network => network.Id == owned.Id).Name, Is.EqualTo("Valid"));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorRuntimeGrantIsSelectableButNotEditable()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var source = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    Assert.That(networks.GrantNetwork(consoleUid, NetworkB, source), Is.True);
                    var editor = rmc.BuildEditorState((consoleUid, console));

                    Assert.Multiple(() =>
                    {
                        Assert.That(rmc.BuildAvailableNetworks(consoleUid).Select(network => network.Id),
                            Does.Contain((ProtoId<CameraNetworkPrototype>) NetworkB));
                        Assert.That(editor.Networks.Select(network => network.Id),
                            Does.Not.Contain((ProtoId<CameraNetworkPrototype>) NetworkB));
                        Assert.That(rmc.TryRenameEditorNetwork(
                            (consoleUid, console), actor, editor.Revision, NetworkB, "No", out var renameError), Is.False);
                        Assert.That(renameError, Is.EqualTo(RMCCameraNetworkEditorError.InvalidNetwork));
                        Assert.That(rmc.TryDeleteEditorNetwork(
                            (consoleUid, console), actor, editor.Revision, NetworkB, out var deleteError), Is.False);
                        Assert.That(deleteError, Is.EqualTo(RMCCameraNetworkEditorError.InvalidNetwork));
                        Assert.That(rmc.TrySetSeededNetworkHidden(
                            (consoleUid, console), actor, editor.Revision, NetworkB, true, out var hideError), Is.False);
                        Assert.That(hideError, Is.EqualTo(RMCCameraNetworkEditorError.InvalidNetwork));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(actor);
                    entMan.DeleteEntity(source);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorListsUnassignedWallCamerasAcrossGridsButExcludesEquipment()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var maps = entMan.System<SharedMapSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                maps.CreateMap(out var mapId);
                var firstGrid = maps.CreateGridEntity(mapId).Owner;
                var secondGrid = maps.CreateGridEntity(mapId).Owner;
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer",
                    new EntityCoordinates(firstGrid, Vector2.Zero));
                var first = entMan.SpawnEntity("CMUTestEditableRmcCameraA",
                    new EntityCoordinates(firstGrid, Vector2.Zero));
                var unassigned = entMan.SpawnEntity("CMUTestEditableRmcCameraB",
                    new EntityCoordinates(secondGrid, Vector2.Zero));
                var item = entMan.SpawnEntity("CMUTestEditableRmcItemCamera", MapCoordinates.Nullspace);
                var mortar = entMan.SpawnEntity("CMUTestEditableRmcMortarCamera", MapCoordinates.Nullspace);
                var nonSurveillance = entMan.SpawnEntity("CMUTestEditableRmcNonSurveillance", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.SetMemberNetworks(unassigned, []), Is.True);
                    var editor = rmc.BuildEditorState((consoleUid,
                        entMan.GetComponent<RMCCameraComputerComponent>(consoleUid)));

                    Assert.Multiple(() =>
                    {
                        Assert.That(rmc.GetEditableCameras(), Is.EquivalentTo(new[] { first, unassigned }));
                        Assert.That(editor.Cameras.Select(camera => camera.Camera),
                            Is.EquivalentTo(new[] { entMan.GetNetEntity(first), entMan.GetNetEntity(unassigned) }));
                        Assert.That(editor.Cameras.Single(camera => camera.Camera == entMan.GetNetEntity(unassigned)).Networks,
                            Is.Empty);
                        Assert.That(editor.Cameras.Select(camera => camera.Camera),
                            Does.Not.Contain(entMan.GetNetEntity(item)));
                        Assert.That(editor.Cameras.Select(camera => camera.Camera),
                            Does.Not.Contain(entMan.GetNetEntity(mortar)));
                        Assert.That(editor.Cameras.Select(camera => camera.Camera),
                            Does.Not.Contain(entMan.GetNetEntity(nonSurveillance)));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(first);
                    entMan.DeleteEntity(unassigned);
                    entMan.DeleteEntity(item);
                    entMan.DeleteEntity(mortar);
                    entMan.DeleteEntity(nonSurveillance);
                    entMan.DeleteEntity(firstGrid);
                    entMan.DeleteEntity(secondGrid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorSavesZeroOneAndMultipleMembershipsAtomically()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    var netCamera = entMan.GetNetEntity(camera);
                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, netCamera,
                        "Camera zero", [], out var zeroError), Is.True);
                    Assert.That(zeroError, Is.EqualTo(RMCCameraNetworkEditorError.None));
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks, Is.Empty);

                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 1, netCamera,
                        "Camera one", [NetworkA], out _), Is.True);
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { NetworkA }));

                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 2, netCamera,
                        "Camera many", [NetworkA, NetworkB], out _), Is.True);
                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Is.EquivalentTo(new[] { NetworkA, NetworkB }));
                        Assert.That(rmc.BuildEditorState((consoleUid, console)).Revision, Is.EqualTo(3));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorPreservesHiddenForeignAndGrantedMemberships()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var firstUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var secondUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var first = entMan.GetComponent<RMCCameraComputerComponent>(firstUid);
                    var second = entMan.GetComponent<RMCCameraComputerComponent>(secondUid);
                    Assert.That(rmc.TryCreateEditorNetwork((firstUid, first), actor, 0, "First owned", out _), Is.True);
                    var firstOwned = rmc.BuildEditorState((firstUid, first)).Networks
                        .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned).Id;
                    Assert.That(rmc.TrySetSeededNetworkHidden((firstUid, first), actor, 1, NetworkB, true, out _), Is.True);
                    Assert.That(rmc.TryCreateEditorNetwork((secondUid, second), actor, 0, "Foreign owned", out _), Is.True);
                    var foreign = rmc.BuildEditorState((secondUid, second)).Networks
                        .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned).Id;
                    Assert.That(networks.SetMemberNetworks(camera, [NetworkA, NetworkB, foreign]), Is.True);

                    Assert.That(rmc.TrySaveEditorCamera((firstUid, first), actor, 2, entMan.GetNetEntity(camera),
                        "Preserved", [firstOwned], out _), Is.True);

                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { (ProtoId<CameraNetworkPrototype>) NetworkB, foreign, firstOwned }));
                }
                finally
                {
                    entMan.DeleteEntity(firstUid);
                    entMan.DeleteEntity(secondUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorReassignsCameraAfterItsLastNetworkWasRemoved()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    Assert.That(networks.SetMemberNetworks(camera, []), Is.True);
                    Assert.That(rmc.BuildEditorState((consoleUid, console)).Cameras.Select(entry => entry.Camera),
                        Does.Contain(entMan.GetNetEntity(camera)));

                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, entMan.GetNetEntity(camera),
                        "Returned camera", [NetworkA], out _), Is.True);
                    rmc.RebuildComputerCameras(consoleUid, console);
                    Assert.Multiple(() =>
                    {
                        Assert.That(console.CameraIds, Does.Contain(entMan.GetNetEntity(camera)));
                        Assert.That(networks.GetNetworkMembers(NetworkA), Does.Contain(camera));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorRenameUpdatesListMapAndActiveFeedName()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var maps = entMan.System<SharedMapSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                maps.CreateMap(out var mapId);
                var grid = maps.CreateGridEntity(mapId);
                maps.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                var firstConsoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer",
                    new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
                var secondConsoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer",
                    new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA",
                    new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
                var duplicate = entMan.SpawnEntity("CMUTestEditableRmcCameraB",
                    new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var first = entMan.GetComponent<RMCCameraComputerComponent>(firstConsoleUid);
                    var second = entMan.GetComponent<RMCCameraComputerComponent>(secondConsoleUid);
                    rmc.RebuildComputerCameras(firstConsoleUid, first);
                    rmc.RebuildComputerCameras(secondConsoleUid, second);
                    Assert.That(rmc.TrySelectCamera((firstConsoleUid, first), camera), Is.True);
                    Assert.That(rmc.TrySelectCamera((secondConsoleUid, second), camera), Is.True);

                    Assert.That(rmc.TrySaveEditorCamera((firstConsoleUid, first), actor, 0,
                        entMan.GetNetEntity(camera), "Renamed camera", [NetworkA], out _), Is.True);
                    networks.Update(0f);

                    Assert.That(rmc.TrySaveEditorCamera((firstConsoleUid, first), actor, 1,
                        entMan.GetNetEntity(duplicate), "Renamed camera", [NetworkA], out _), Is.True,
                        "camera display names need not be unique");
                    networks.Update(0f);

                    var firstState = rmc.BuildBuiState((firstConsoleUid, first));
                    var secondState = rmc.BuildBuiState((secondConsoleUid, second));
                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.GetComponent<MetaDataComponent>(camera).EntityName, Is.EqualTo("Renamed camera"));
                        Assert.That(first.CameraNames, Does.Contain("Renamed camera"));
                        Assert.That(second.CameraNames, Does.Contain("Renamed camera"));
                        Assert.That(firstState.Map.Grids.SelectMany(cameraGrid => cameraGrid.Markers)
                            .Single(marker => marker.Camera == entMan.GetNetEntity(camera)).Name, Is.EqualTo("Renamed camera"));
                        Assert.That(secondState.Map.Grids.SelectMany(cameraGrid => cameraGrid.Markers)
                            .Single(marker => marker.Camera == entMan.GetNetEntity(camera)).Name, Is.EqualTo("Renamed camera"));
                        Assert.That(rmc.GetComputerCameraName((firstConsoleUid, first), camera, out var activeName), Is.True);
                        Assert.That(activeName, Is.EqualTo("Renamed camera"));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(firstConsoleUid);
                    entMan.DeleteEntity(secondConsoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(duplicate);
                    entMan.DeleteEntity(actor);
                    entMan.DeleteEntity(grid.Owner);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorRejectsDeletedPausedInactiveAndForgedCameras()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var meta = entMan.System<MetaDataSystem>();
                var surveillance = entMan.System<SurveillanceCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var deleted = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var paused = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var inactive = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    var deletedNet = entMan.GetNetEntity(deleted);
                    entMan.DeleteEntity(deleted);
                    meta.SetEntityPaused(paused, true);
                    surveillance.SetActive(inactive, false,
                        entMan.GetComponent<SurveillanceCameraComponent>(inactive));

                    Assert.Multiple(() =>
                    {
                        Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, deletedNet,
                            "Deleted", [NetworkA], out var deletedError), Is.False);
                        Assert.That(deletedError, Is.EqualTo(RMCCameraNetworkEditorError.MissingCamera));
                        Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, entMan.GetNetEntity(paused),
                            "Paused", [NetworkA], out var pausedError), Is.False);
                        Assert.That(pausedError, Is.EqualTo(RMCCameraNetworkEditorError.MissingCamera));
                        Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, entMan.GetNetEntity(inactive),
                            "Inactive", [NetworkA], out var inactiveError), Is.False);
                        Assert.That(inactiveError, Is.EqualTo(RMCCameraNetworkEditorError.MissingCamera));
                        Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, new NetEntity(int.MaxValue),
                            "Forged", [NetworkA], out var forgedError), Is.False);
                        Assert.That(forgedError, Is.EqualTo(RMCCameraNetworkEditorError.MissingCamera));
                        Assert.That(rmc.BuildEditorState((consoleUid, console)).Revision, Is.Zero);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    if (entMan.EntityExists(paused))
                        entMan.DeleteEntity(paused);
                    entMan.DeleteEntity(inactive);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcEditorRequiresActorSpecificOpenUiAndConsoleAccess()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var ui = entMan.System<SharedUserInterfaceSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestEditableRmcBuiComputer", MapCoordinates.Nullspace);
                var secureUid = entMan.SpawnEntity("CMUTestSecureEditableRmcBuiComputer", MapCoordinates.Nullspace);
                var openedActor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var otherActor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    Assert.That(ui.TryOpenUi(consoleUid, RMCCameraUiKey.Key, openedActor), Is.True);
                    Assert.That(ui.TryOpenUi(secureUid, RMCCameraUiKey.Key, openedActor), Is.True);

                    entMan.EventBus.RaiseLocalEvent(consoleUid,
                        new RMCCameraNetworkEditorCreateBuiMsg(0, "Forged other actor")
                            { Actor = otherActor, UiKey = RMCCameraUiKey.Key });
                    Assert.That(rmc.BuildEditorState((consoleUid,
                        entMan.GetComponent<RMCCameraComputerComponent>(consoleUid))).Revision, Is.Zero);

                    entMan.EventBus.RaiseLocalEvent(secureUid,
                        new RMCCameraNetworkEditorCreateBuiMsg(0, "No access")
                            { Actor = openedActor, UiKey = RMCCameraUiKey.Key });
                    Assert.That(rmc.BuildEditorState((secureUid,
                        entMan.GetComponent<RMCCameraComputerComponent>(secureUid))).Revision, Is.Zero);

                    entMan.EventBus.RaiseLocalEvent(consoleUid,
                        new RMCCameraNetworkEditorCreateBuiMsg(0, "Accepted")
                            { Actor = openedActor, UiKey = RMCCameraUiKey.Key });
                    Assert.That(rmc.BuildEditorState((consoleUid,
                        entMan.GetComponent<RMCCameraComputerComponent>(consoleUid))).Revision, Is.EqualTo(1));
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(secureUid);
                    entMan.DeleteEntity(openedActor);
                    entMan.DeleteEntity(otherActor);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorStaleCommandCannotPartiallyRenameOrReassign()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    Assert.That(rmc.TryCreateEditorNetwork((consoleUid, console), actor, 0, "Advance", out _), Is.True);
                    var beforeName = entMan.GetComponent<RMCCameraComponent>(camera).NameOverride;
                    var beforeNetworks = entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks.ToHashSet();

                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, 0, entMan.GetNetEntity(camera),
                        "Must not apply", [NetworkB], out var error), Is.False);
                    Assert.Multiple(() =>
                    {
                        Assert.That(error, Is.EqualTo(RMCCameraNetworkEditorError.StaleRevision));
                        Assert.That(entMan.GetComponent<RMCCameraComponent>(camera).NameOverride, Is.EqualTo(beforeName));
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Is.EquivalentTo(beforeNetworks));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorConsoleShutdownRemovesOwnedMemberships()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                Assert.That(rmc.TryCreateEditorNetwork((consoleUid, console), actor, 0, "Owned", out _), Is.True);
                var owned = rmc.BuildEditorState((consoleUid, console)).Networks
                    .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned).Id;
                Assert.That(networks.SetMemberNetworks(camera, [NetworkA, owned]), Is.True);

                entMan.DeleteEntity(consoleUid);

                Assert.Multiple(() =>
                {
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { NetworkA }));
                    Assert.That(networks.GetNetworkMembers(owned), Is.Empty);
                });
                entMan.DeleteEntity(camera);
                entMan.DeleteEntity(actor);
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorCameraShutdownRefreshesOpenEditor()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var ui = entMan.System<SharedUserInterfaceSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestEditableRmcBuiComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                Assert.That(networks.SetMemberNetworks(camera, []), Is.True);
                Assert.That(ui.TryOpenUi(consoleUid, RMCCameraUiKey.Key, actor), Is.True);
                var interfaceComponent = entMan.GetComponent<UserInterfaceComponent>(consoleUid);
                var before = (RMCCameraBuiState) interfaceComponent.States[RMCCameraUiKey.Key];
                Assert.That(before.Editor.Cameras.Select(entry => entry.Camera),
                    Does.Contain(entMan.GetNetEntity(camera)));
                var netCamera = entMan.GetNetEntity(camera);

                entMan.DeleteEntity(camera);

                var after = (RMCCameraBuiState) interfaceComponent.States[RMCCameraUiKey.Key];
                Assert.That(after.Editor.Cameras.Select(entry => entry.Camera), Does.Not.Contain(netCamera));
                entMan.DeleteEntity(consoleUid);
                entMan.DeleteEntity(actor);
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorDeletingSelectedSubnetDisconnectsFeedAndRemoteGridSubscriptions()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                    Assert.That(rmc.TryCreateEditorNetwork((consoleUid, console), actor, 0, "Temporary", out _), Is.True);
                    var editor = rmc.BuildEditorState((consoleUid, console));
                    var owned = editor.Networks.Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned).Id;
                    Assert.That(rmc.TrySaveEditorCamera((consoleUid, console), actor, editor.Revision,
                        entMan.GetNetEntity(camera), "Temporary camera", [owned], out _), Is.True);
                    Assert.That(rmc.TrySelectNetwork((consoleUid, console), owned), Is.True);
                    rmc.RebuildComputerCameras(consoleUid, console);
                    Assert.That(rmc.TrySelectCamera((consoleUid, console), camera), Is.True);

                    var beforeDelete = rmc.BuildEditorState((consoleUid, console));
                    Assert.That(rmc.TryDeleteEditorNetwork((consoleUid, console), actor, beforeDelete.Revision,
                        owned, out _), Is.True);

                    Assert.Multiple(() =>
                    {
                        Assert.That(console.CurrentCamera, Is.Null);
                        Assert.That(console.CameraIds, Does.Not.Contain(entMan.GetNetEntity(camera)));
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Does.Not.Contain(owned));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(consoleUid);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(actor);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorTwoOpenViewersReceiveSameAcceptedRevision()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var ui = entMan.System<SharedUserInterfaceSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestEditableRmcBuiComputer", MapCoordinates.Nullspace);
                var first = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var second = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                Assert.That(ui.TryOpenUi(consoleUid, RMCCameraUiKey.Key, first), Is.True);
                Assert.That(ui.TryOpenUi(consoleUid, RMCCameraUiKey.Key, second), Is.True);

                entMan.EventBus.RaiseLocalEvent(consoleUid,
                    new RMCCameraNetworkEditorCreateBuiMsg(0, "Shared revision")
                        { Actor = first, UiKey = RMCCameraUiKey.Key });

                var component = entMan.GetComponent<UserInterfaceComponent>(consoleUid);
                var state = (RMCCameraBuiState) component.States[RMCCameraUiKey.Key];
                Assert.Multiple(() =>
                {
                    Assert.That(state.Editor.Revision, Is.EqualTo(1));
                    Assert.That(component.Actors[RMCCameraUiKey.Key], Is.EquivalentTo(new[] { first, second }));
                });
                entMan.DeleteEntity(consoleUid);
                entMan.DeleteEntity(first);
                entMan.DeleteEntity(second);
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcEditorRoundRestartCleanupRestoresSeedState()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var consoleUid = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestEditableRmcCameraA", MapCoordinates.Nullspace);
                var actor = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var console = entMan.GetComponent<RMCCameraComputerComponent>(consoleUid);
                Assert.That(rmc.TryCreateEditorNetwork((consoleUid, console), actor, 0, "Round local", out _), Is.True);
                var owned = rmc.BuildEditorState((consoleUid, console)).Networks
                    .Single(network => network.Origin == RMCCameraNetworkEditorOrigin.Owned).Id;
                Assert.That(networks.SetMemberNetworks(camera, [NetworkA, owned]), Is.True);

                entMan.EventBus.RaiseEvent(EventSource.Local, new RoundRestartCleanupEvent());

                var state = rmc.BuildEditorState((consoleUid, console));
                Assert.Multiple(() =>
                {
                    Assert.That(state.Revision, Is.Zero);
                    Assert.That(state.Networks.Select(network => network.Id),
                        Is.EquivalentTo(new[] { (ProtoId<CameraNetworkPrototype>) NetworkA,
                            (ProtoId<CameraNetworkPrototype>) NetworkB }));
                    Assert.That(networks.GetEffectiveNetworks(consoleUid), Does.Not.Contain(owned));
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { NetworkA }));
                });
                entMan.DeleteEntity(consoleUid);
                entMan.DeleteEntity(camera);
                entMan.DeleteEntity(actor);
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public void RmcBuiStateRetainsLogicalNetworkIdsNamesAndSelection()
    {
        var state = new RMCCameraBuiState(
            new CameraMapUiState(null, []),
            [
                new CameraNetworkUiData((ProtoId<CameraNetworkPrototype>) NetworkA, "cmu-test-camera-network-a"),
                new CameraNetworkUiData((ProtoId<CameraNetworkPrototype>) NetworkB, "cmu-test-camera-network-b"),
            ],
            (ProtoId<CameraNetworkPrototype>) NetworkB);
        var message = new RMCCameraNetworkBuiMsg((ProtoId<CameraNetworkPrototype>) NetworkA);

        Assert.Multiple(() =>
        {
            Assert.That(state.Networks.Select(network => network.Id), Is.EqualTo(new[]
            {
                (ProtoId<CameraNetworkPrototype>) NetworkA,
                (ProtoId<CameraNetworkPrototype>) NetworkB,
            }));
            Assert.That(state.Networks.Select(network => network.Name), Is.EqualTo(new[]
            {
                "cmu-test-camera-network-a",
                "cmu-test-camera-network-b",
            }));
            Assert.That(state.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkB));
            Assert.That(message.Network, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkA));
        });
    }

    [Test]
    public async Task RmcBuildsLocalizedAvailableNetworksInStableOrder()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var computer = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    var state = rmc.BuildBuiState((computer, console));

                    Assert.Multiple(() =>
                    {
                        Assert.That(state.Networks.Select(network => network.Id), Is.EqualTo(new[]
                        {
                            (ProtoId<CameraNetworkPrototype>) NetworkA,
                            (ProtoId<CameraNetworkPrototype>) NetworkB,
                        }));
                        Assert.That(state.Networks.Select(network => network.Name), Is.EqualTo(new[]
                        {
                            "cmu-test-camera-network-a",
                            "cmu-test-camera-network-b",
                        }));
                        Assert.That(console.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkA));
                        Assert.That(state.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkA));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcSelectingAuthorizedNetworkFiltersCamerasAndMap()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var computer = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", new EntityCoordinates(grid, Vector2.Zero));
                var cameraA = entMan.SpawnEntity("CMUTestRmcShipCamera", new EntityCoordinates(grid, Vector2.One));
                var cameraB = entMan.SpawnEntity("CMUTestRmcCameraB", new EntityCoordinates(grid, new Vector2(2, 2)));
                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(cameraA);
                    entMan.AddComponent<CameraMapMarkerComponent>(cameraB);
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);

                    Assert.That(rmc.TrySelectCamera((computer, console), cameraA), Is.True);
                    Assert.That(rmc.TrySelectNetwork((computer, console), NetworkB), Is.True);
                    var state = rmc.BuildBuiState((computer, console));

                    Assert.Multiple(() =>
                    {
                        Assert.That(console.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkB));
                        Assert.That(console.CurrentCamera, Is.Null);
                        Assert.That(console.CameraIds, Is.EqualTo(new[] { entMan.GetNetEntity(cameraB) }));
                        Assert.That(rmc.TrySelectCamera((computer, console), cameraA), Is.False);
                        Assert.That(state.Map.Grids.SelectMany(cameraGrid => cameraGrid.Markers)
                                .Select(marker => marker.Camera),
                            Is.EqualTo(new[] { entMan.GetNetEntity(cameraB) }));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(cameraA);
                    entMan.DeleteEntity(cameraB);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcRuntimeGrantedNetworkAppearsSelectsAndShowsItsCameras()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestRmcCameraB", new EntityCoordinates(grid, Vector2.One));
                var granter = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    Assert.That(networks.GrantNetwork(computer, NetworkB, granter), Is.True);

                    Assert.That(rmc.TrySelectNetwork((computer, console), NetworkB), Is.True);
                    var state = rmc.BuildBuiState((computer, console));

                    Assert.Multiple(() =>
                    {
                        Assert.That(state.Networks.Select(network => network.Id), Does.Contain((ProtoId<CameraNetworkPrototype>) NetworkB));
                        Assert.That(console.CameraIds, Does.Contain(entMan.GetNetEntity(camera)));
                        Assert.That(state.Map.Grids.SelectMany(cameraGrid => cameraGrid.Markers).Select(marker => marker.Camera),
                            Does.Contain(entMan.GetNetEntity(camera)));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverComponent>(computer).Networks,
                            Does.Not.Contain((ProtoId<CameraNetworkPrototype>) NetworkB));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(granter);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcRemovingSelectedNetworkMembershipClearsGloballyAccessibleCamera()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var session = server.PlayerMan.Sessions.Single();
                var previousAttached = session.AttachedEntity;
                var computer = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestRmcShipCamera", MapCoordinates.Nullspace);
                var watcher = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    server.PlayerMan.SetAttachedEntity(session, watcher);
                    entMan.EventBus.RaiseLocalEvent(computer,
                        new BoundUIOpenedEvent(RMCCameraUiKey.Key, computer, watcher));
                    Assert.That(rmc.TrySelectCamera((computer, console), camera), Is.True);
                    var watcherComp = entMan.GetComponent<RMCCameraWatcherComponent>(watcher);
                    Assert.That(watcherComp.Overrides, Does.Contain(entMan.GetNetEntity(camera)));
                    Assert.That(session.ViewSubscriptions, Does.Contain(camera));
                    Assert.That(networks.SetMemberNetworks(camera, [NetworkB]), Is.True);

                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(computer, camera), Is.True);
                        Assert.That(console.CameraIds, Does.Not.Contain(entMan.GetNetEntity(camera)));
                        Assert.That(console.CurrentCamera, Is.Null);
                        Assert.That(watcherComp.Overrides, Is.Empty);
                        Assert.That(session.ViewSubscriptions, Does.Not.Contain(camera));
                    });
                }
                finally
                {
                    server.PlayerMan.SetAttachedEntity(session, previousAttached);
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(watcher);
                }
            });
        }
        finally { await pair.CleanReturnAsync(); }
    }

    [Test]
    public async Task StandardMonitorSubscribesViewerToEveryVisibleCameraGrid()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        try
        {
            if (!server.ProtoMan.HasIndex<CameraNetworkPrototype>(NetworkA))
                await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var session = server.PlayerMan.Sessions.Single();
                var previousAttached = session.AttachedEntity;
                mapSystem.CreateMap(out var mapId);
                var consoleGrid = mapSystem.CreateGridEntity(mapId);
                var remoteGrid = mapSystem.CreateGridEntity(mapId);
                entMan.System<SharedTransformSystem>().SetLocalPosition(remoteGrid, new Vector2(50, 50));
                mapSystem.SetTile(consoleGrid.Owner, consoleGrid.Comp, Vector2i.Zero, new Tile(1));
                mapSystem.SetTile(remoteGrid.Owner, remoteGrid.Comp, Vector2i.Zero, new Tile(1));
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor",
                    new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard",
                    new EntityCoordinates(remoteGrid.Owner, Vector2.Zero));
                var viewer = entMan.SpawnEntity(null, new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    server.PlayerMan.SetAttachedEntity(session, viewer);

                    monitors.AfterOpenUserInterface(monitor, viewer);

                    Assert.That(session.ViewSubscriptions.Any(view =>
                        entMan.TryGetComponent(view, out TransformComponent? viewTransform) &&
                        viewTransform.GridUid == remoteGrid.Owner), Is.True);

                    entMan.EventBus.RaiseLocalEvent(monitor,
                        new BoundUIClosedEvent(SurveillanceCameraMonitorUiKey.Key, monitor, viewer));
                    Assert.That(session.ViewSubscriptions.Any(view =>
                        entMan.TryGetComponent(view, out TransformComponent? viewTransform) &&
                        viewTransform.GridUid == remoteGrid.Owner), Is.False);
                }
                finally
                {
                    server.PlayerMan.SetAttachedEntity(session, previousAttached);
                    entMan.DeleteEntity(viewer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(consoleGrid);
                    entMan.DeleteEntity(remoteGrid);
                }
            });
        }
        finally { await pair.CleanReturnAsync(); }
    }

    [Test]
    public async Task StandardMonitorReplicatesRemoteNavMapGeometryToClient()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;
        var monitor = EntityUid.Invalid;
        var camera = EntityUid.Invalid;
        var viewer = EntityUid.Invalid;
        var consoleGrid = EntityUid.Invalid;
        var remoteGrid = EntityUid.Invalid;
        var remoteGridNet = NetEntity.Invalid;
        CameraMapUiState? cameraMap = null;
        CameraNavMapControl? control = null;
        EntityUid? previousAttached = null;
        try
        {
            if (!server.ProtoMan.HasIndex<CameraNetworkPrototype>(NetworkA))
                await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var session = server.PlayerMan.Sessions.Single();
                previousAttached = session.AttachedEntity;
                mapSystem.CreateMap(out var mapId);
                var consoleGridEntity = mapSystem.CreateGridEntity(mapId);
                var remoteGridEntity = mapSystem.CreateGridEntity(mapId);
                consoleGrid = consoleGridEntity.Owner;
                remoteGrid = remoteGridEntity.Owner;
                entMan.System<SharedTransformSystem>().SetLocalPosition(remoteGridEntity, new Vector2(50, 50));
                mapSystem.SetTile(consoleGridEntity.Owner, consoleGridEntity.Comp, Vector2i.Zero, new Tile(1));
                mapSystem.SetTile(remoteGridEntity.Owner, remoteGridEntity.Comp, Vector2i.Zero, new Tile(1));

                var navMap = entMan.AddComponent<NavMapComponent>(remoteGrid);
                var chunk = new NavMapChunk(Vector2i.Zero);
                chunk.TileData[0] = SharedNavMapSystem.FloorMask;
                navMap.Chunks[Vector2i.Zero] = chunk;

                monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor",
                    new EntityCoordinates(consoleGrid, Vector2.Zero));
                camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard",
                    new EntityCoordinates(remoteGrid, Vector2.Zero));
                viewer = entMan.SpawnEntity(null, new EntityCoordinates(consoleGrid, Vector2.Zero));
                entMan.AddComponent<CameraMapMarkerComponent>(camera);
                server.PlayerMan.SetAttachedEntity(session, viewer);
                remoteGridNet = entMan.GetNetEntity(remoteGrid);
                cameraMap = monitors.BuildUiState((monitor,
                    entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor))).CameraMap;
            });

            await client.WaitAssertion(() =>
            {
                Assert.That(client.EntMan.TryGetEntity(remoteGridNet, out _), Is.False);
                control = new CameraNavMapControl();
                control.SetState(cameraMap!, null);
                Assert.Multiple(() =>
                {
                    Assert.That(control.MapUid, Is.Null);
                    Assert.That(control.GridBindingReady, Is.False);
                });
            });

            await server.WaitAssertion(() =>
            {
                var monitors = server.EntMan.System<SurveillanceCameraMonitorSystem>();
                monitors.AfterOpenUserInterface(monitor, viewer);
            });

            await pair.RunTicksSync(10);

            await client.WaitAssertion(() =>
            {
                var entMan = client.EntMan;
                Assert.That(entMan.TryGetEntity(remoteGridNet, out var clientGrid), Is.True);
                Assert.That(clientGrid, Is.Not.Null);
                var navMap = entMan.GetComponent<NavMapComponent>(clientGrid.Value);
                Assert.Multiple(() =>
                {
                    Assert.That(control!.RefreshSelectedGridBinding(), Is.True);
                    Assert.That(control.MapUid, Is.EqualTo(clientGrid));
                    Assert.That(control.GridBindingReady, Is.True);
                    Assert.That(navMap.Chunks, Contains.Key(Vector2i.Zero));
                    Assert.That(navMap.Chunks[Vector2i.Zero].TileData[0],
                        Is.EqualTo(SharedNavMapSystem.FloorMask));
                });
            });
        }
        finally
        {
            await server.WaitAssertion(() =>
            {
                var session = server.PlayerMan.Sessions.Single();
                server.PlayerMan.SetAttachedEntity(session, previousAttached);
                foreach (var uid in new[] { viewer, camera, monitor, consoleGrid, remoteGrid })
                {
                    if (server.EntMan.EntityExists(uid))
                        server.EntMan.DeleteEntity(uid);
                }
            });
            await pair.CleanReturnAsync();
        }
    }

    [Test]
    public async Task LoadedZLevelGridHasGeneratedNavMapGeometry()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var ticker = entMan.System<GameTicker>();
                var mapSystem = entMan.System<SharedMapSystem>();
                var mapPrototype = server.ProtoMan.Index<GameMapPrototype>("CMUTestCameraZMap");
                var options = DeserializationOptions.Default with { InitializeMaps = true };

                ticker.LoadGameMap(mapPrototype, out _, options);

                EntityUid? upperMap = null;
                var maps = entMan.EntityQueryEnumerator<MapComponent, MetaDataComponent>();
                while (maps.MoveNext(out var mapUid, out _, out var metadata))
                {
                    if (metadata.EntityName == "CMU Test Camera Z Map [1]")
                    {
                        upperMap = mapUid;
                        break;
                    }
                }

                Assert.That(upperMap, Is.Not.Null);
                var upperMapId = entMan.GetComponent<MapComponent>(upperMap.Value).MapId;
                var upperGrid = mapSystem.GetAllGrids(upperMapId).Single();

                Assert.Multiple(() =>
                {
                    Assert.That(entMan.HasComponent<NavMapComponent>(upperGrid.Owner), Is.True);
                    Assert.That(entMan.GetComponent<NavMapComponent>(upperGrid.Owner).Chunks, Is.Not.Empty);
                });
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task ColonyCameraGridGetsGeneratedNavMapGeometry()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId);
                mapSystem.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver",
                    new EntityCoordinates(grid.Owner, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA",
                    new EntityCoordinates(grid.Owner, Vector2.Zero));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    Assert.That(entMan.HasComponent<NavMapComponent>(grid.Owner), Is.False);

                    var state = networks.BuildMapState(receiver);
                    var navMap = entMan.GetComponent<NavMapComponent>(grid.Owner);

                    Assert.Multiple(() =>
                    {
                        Assert.That(state.Grids.Single().Grid, Is.EqualTo(entMan.GetNetEntity(grid.Owner)));
                        Assert.That(navMap.Chunks, Contains.Key(Vector2i.Zero));
                        Assert.That(navMap.Chunks[Vector2i.Zero].TileData[0],
                            Is.EqualTo(SharedNavMapSystem.FloorMask));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcMonitorSubscribesViewerToEveryVisibleCameraGrid()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        try
        {
            if (!server.ProtoMan.HasIndex<CameraNetworkPrototype>(NetworkA))
                await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                _ = entMan.System<RMCCameraSystem>();
                var session = server.PlayerMan.Sessions.Single();
                var previousAttached = session.AttachedEntity;
                mapSystem.CreateMap(out var mapId);
                var consoleGrid = mapSystem.CreateGridEntity(mapId);
                var remoteGrid = mapSystem.CreateGridEntity(mapId);
                entMan.System<SharedTransformSystem>().SetLocalPosition(remoteGrid, new Vector2(50, 50));
                mapSystem.SetTile(consoleGrid.Owner, consoleGrid.Comp, Vector2i.Zero, new Tile(1));
                mapSystem.SetTile(remoteGrid.Owner, remoteGrid.Comp, Vector2i.Zero, new Tile(1));
                var monitor = entMan.SpawnEntity("CMUTestRmcMapComputer",
                    new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestRmcShipCamera",
                    new EntityCoordinates(remoteGrid.Owner, Vector2.Zero));
                var viewer = entMan.SpawnEntity(null, new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    server.PlayerMan.SetAttachedEntity(session, viewer);

                    entMan.EventBus.RaiseLocalEvent(monitor,
                        new BoundUIOpenedEvent(RMCCameraUiKey.Key, monitor, viewer));

                    Assert.That(session.ViewSubscriptions.Any(view =>
                        entMan.TryGetComponent(view, out TransformComponent? viewTransform) &&
                        viewTransform.GridUid == remoteGrid.Owner), Is.True);

                    entMan.EventBus.RaiseLocalEvent(monitor,
                        new BoundUIClosedEvent(RMCCameraUiKey.Key, monitor, viewer));
                    server.RunTicks(1);
                    Assert.That(session.ViewSubscriptions.Any(view =>
                        entMan.TryGetComponent(view, out TransformComponent? viewTransform) &&
                        viewTransform.GridUid == remoteGrid.Owner), Is.False);
                }
                finally
                {
                    server.PlayerMan.SetAttachedEntity(session, previousAttached);
                    entMan.DeleteEntity(viewer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(consoleGrid);
                    entMan.DeleteEntity(remoteGrid);
                }
            });
        }
        finally { await pair.CleanReturnAsync(); }
    }

    [Test]
    public async Task CameraMapSubscriptionsDoNotRemoveAnExistingGridView()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        try
        {
            if (!server.ProtoMan.HasIndex<CameraNetworkPrototype>(NetworkA))
                await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var viewSubscribers = entMan.System<ViewSubscriberSystem>();
                var session = server.PlayerMan.Sessions.Single();
                var previousAttached = session.AttachedEntity;
                mapSystem.CreateMap(out var mapId);
                var consoleGrid = mapSystem.CreateGridEntity(mapId);
                var remoteGrid = mapSystem.CreateGridEntity(mapId);
                entMan.System<SharedTransformSystem>().SetLocalPosition(remoteGrid, new Vector2(50, 50));
                mapSystem.SetTile(consoleGrid.Owner, consoleGrid.Comp, Vector2i.Zero, new Tile(1));
                mapSystem.SetTile(remoteGrid.Owner, remoteGrid.Comp, Vector2i.Zero, new Tile(1));
                var firstMonitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor",
                    new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));
                var secondMonitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor",
                    new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard",
                    new EntityCoordinates(remoteGrid.Owner, Vector2.Zero));
                var viewer = entMan.SpawnEntity(null, new EntityCoordinates(consoleGrid.Owner, Vector2.Zero));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    server.PlayerMan.SetAttachedEntity(session, viewer);
                    viewSubscribers.AddViewSubscriber(remoteGrid.Owner, session);

                    monitors.AfterOpenUserInterface(firstMonitor, viewer);
                    monitors.AfterOpenUserInterface(secondMonitor, viewer);
                    entMan.EventBus.RaiseLocalEvent(firstMonitor,
                        new BoundUIClosedEvent(SurveillanceCameraMonitorUiKey.Key, firstMonitor, viewer));

                    Assert.That(session.ViewSubscriptions.Any(view =>
                        view != remoteGrid.Owner &&
                        entMan.TryGetComponent(view, out TransformComponent? viewTransform) &&
                        viewTransform.GridUid == remoteGrid.Owner), Is.True);

                    entMan.EventBus.RaiseLocalEvent(secondMonitor,
                        new BoundUIClosedEvent(SurveillanceCameraMonitorUiKey.Key, secondMonitor, viewer));

                    Assert.That(session.ViewSubscriptions, Does.Contain(remoteGrid.Owner));
                }
                finally
                {
                    viewSubscribers.RemoveViewSubscriber(remoteGrid.Owner, session);
                    server.PlayerMan.SetAttachedEntity(session, previousAttached);
                    entMan.DeleteEntity(viewer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(firstMonitor);
                    entMan.DeleteEntity(secondMonitor);
                    entMan.DeleteEntity(consoleGrid);
                    entMan.DeleteEntity(remoteGrid);
                }
            });
        }
        finally { await pair.CleanReturnAsync(); }
    }

    [Test]
    public async Task RmcRejectingUnavailableNetworkKeepsSelectionAndCameras()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var computer = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                var cameraA = entMan.SpawnEntity("CMUTestRmcShipCamera", MapCoordinates.Nullspace);
                var cameraB = entMan.SpawnEntity("CMUTestRmcCameraB", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    Assert.That(rmc.TrySelectNetwork((computer, console), NetworkB), Is.True);
                    var previousCameras = console.CameraIds.ToList();

                    Assert.That(rmc.TrySelectNetwork((computer, console), "CMUTestUnknownCameraNetwork"), Is.False);
                    Assert.Multiple(() =>
                    {
                        Assert.That(console.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkB));
                        Assert.That(console.CameraIds, Is.EqualTo(previousCameras));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverComponent>(computer).Networks,
                            Is.EquivalentTo(new[] { NetworkA, NetworkB }));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(cameraA);
                    entMan.DeleteEntity(cameraB);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcFallsBackToFirstAvailableNetworkWhenActiveNetworkIsRemoved()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var computer = entMan.SpawnEntity("CMUTestDualNetworkRmcComputer", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    Assert.That(rmc.TrySelectNetwork((computer, console), NetworkB), Is.True);
                    Assert.That(networks.SetReceiverNetworks(computer, [NetworkA]), Is.True);

                    var state = rmc.BuildBuiState((computer, console));
                    Assert.Multiple(() =>
                    {
                        Assert.That(console.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkA));
                        Assert.That(state.ActiveNetwork, Is.EqualTo((ProtoId<CameraNetworkPrototype>) NetworkA));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [TestPrototypes]
    private const string Prototypes = """
        - type: cameraNetwork
          id: CMUTestCameraNetworkA
          name: cmu-test-camera-network-a
          configurable: true

        - type: cameraNetwork
          id: CMUTestCameraNetworkB
          name: cmu-test-camera-network-b
          configurable: false

        - type: gameMap
          id: CMUTestCameraZMap
          mapName: CMU Test Camera Z Map
          mapPath: /Maps/Test/empty.yml
          minPlayers: 0
          mapsAbove:
          - /Maps/Test/empty.yml
          stations:
            Empty:
              stationProto: StandardNanotrasenStation
              components: []

        - type: entity
          id: CMUTestCameraStandard
          components:
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
            sourceKinds: Standard

        - type: entity
          id: CMUTestCameraRmc
          components:
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestCameraStandardA
          components:
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Standard

        - type: entity
          id: CMUTestCameraStandardB
          components:
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkB]
            sourceKinds: Standard

        - type: entity
          id: CMUTestCameraReceiver
          components:
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA]
            supportedSources: Standard

        - type: entity
          id: CMUTestSurveillanceCameraGeneralReceiver
          components:
          - type: CameraNetworkReceiver
            networks: [SurveillanceCameraGeneral]
            supportedSources: Standard

        - type: entity
          id: CMUTestCameraDynamicReceiver
          components:
          - type: CameraNetworkReceiver
            supportedSources: Standard

        - type: entity
          id: CMUTestSurveillanceMonitor
          components:
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA]
            supportedSources: Standard
          - type: SurveillanceCameraMonitor
          - type: Eye

        - type: entity
          id: CMUTestSurveillanceMonitorDualNetwork
          components:
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
            supportedSources: Standard
          - type: SurveillanceCameraMonitor
          - type: Eye

        - type: entity
          id: CMUTestSurveillanceCameraStandard
          name: CMU Test Surveillance Camera
          components:
          - type: SurveillanceCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Standard

        - type: entity
          id: CMUTestSurveillanceCameraRmc
          components:
          - type: SurveillanceCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestConstructedSurveillanceCamera
          components:
          - type: SurveillanceCamera
            setupAvailableNetworks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]

        - type: entity
          id: CMUTestPanelGatedSurveillanceCamera
          parent: CMUSurveillanceCameraColonyCMB
          components:
          - type: WiresPanel
            openDelay: 0

        - type: entity
          id: CMUTestConstructedSurveillanceCameraNetworked
          components:
          - type: SurveillanceCamera
            setupAvailableNetworks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkB]
            sourceKinds: Standard

        - type: entity
          id: CMUTestUnnetworkedSurveillanceCamera
          components:
          - type: SurveillanceCamera

        - type: entity
          id: CMUTestUnnetworkedItemSurveillanceCamera
          components:
          - type: Item
          - type: SurveillanceCamera

        - type: entity
          id: CMUTestLegacyRmcCamera
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA

        - type: entity
          id: CMUTestLegacyRmcComputer
          components:
          - type: RMCCameraComputer
            protoIds: [CMUTestCameraNetworkA]

        - type: entity
          id: CMUTestExplicitRmcCamera
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkB]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestExplicitRmcComputer
          components:
          - type: RMCCameraComputer
            protoIds: [CMUTestCameraNetworkA]
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkB]
            supportedSources: Rmc

        - type: entity
          id: CMUTestUnknownLegacyRmcCamera
          components:
          - type: RMCCamera
            id: CMUTestUnknownCameraNetwork

        - type: entity
          id: CMUTestUnknownLegacyRmcComputer
          components:
          - type: RMCCameraComputer
            protoIds: [CMUTestUnknownCameraNetwork]

        - type: entity
          id: CMUTestRmcMapComputer
          components:
          - type: RMCCameraComputer
            protoIds: []
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA]
            supportedSources: Rmc

        - type: entity
          id: CMUTestDualNetworkRmcComputer
          components:
          - type: RMCCameraComputer
            protoIds: []
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
            supportedSources: Rmc

        - type: entity
          id: CMUTestEditableRmcBuiComputer
          components:
          - type: UserInterface
            interfaces:
              enum.RMCCameraUiKey.Key:
                type: RMCCameraBui
                interactionRange: 0
          - type: RMCCameraComputer
            protoIds: []
          - type: CameraNetworkReceiver
            networks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
            supportedSources: Rmc

        - type: entity
          parent: CMUTestEditableRmcBuiComputer
          id: CMUTestSecureEditableRmcBuiComputer
          components:
          - type: AccessReader
            access:
            - [CMUAccessYautjaSecure]

        - type: entity
          id: CMUTestRmcShipCamera
          name: CMU Test RMC Ship Camera
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestEditableRmcCameraA
          name: Editable RMC camera A
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
            nameOverride: Old camera A
          - type: SurveillanceCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc
          - type: CameraMapMarker
          - type: Eye

        - type: entity
          id: CMUTestEditableRmcCameraB
          name: Editable RMC camera B
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkB
            nameOverride: Old camera B
          - type: SurveillanceCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkB]
            sourceKinds: Rmc
          - type: CameraMapMarker
          - type: Eye

        - type: entity
          id: CMUTestEditableRmcItemCamera
          name: Editable RMC item camera
          components:
          - type: Item
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: SurveillanceCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestEditableRmcMortarCamera
          name: Editable RMC mortar camera
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: SurveillanceCamera
          - type: MortarCamera
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestEditableRmcNonSurveillance
          name: Editable RMC non-surveillance source
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestRmcMortarCamera
          name: CMU Test RMC Mortar Camera
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkA
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestRmcCameraB
          name: CMU Test RMC Camera B
          components:
          - type: RMCCamera
            id: CMUTestCameraNetworkB
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkB]
            sourceKinds: Rmc

        - type: entity
          id: CMUTestStatefulSurveillanceCamera
          components:
          - type: SurveillanceCamera
            id: Preserved camera id
            nameSet: true
            networkSet: true
            setupAvailableNetworks: [CMUTestCameraNetworkA, CMUTestCameraNetworkB]
          - type: CameraNetworkMember
            networks: [CMUTestCameraNetworkA]
            sourceKinds: Standard
          - type: CameraMapMarker
            visible: false
            mobile: true
            updateInterval: 2
        """;

    [Test]
    public async Task LegacyRmcFieldsCreateLogicalMembershipAndReceiver()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                _ = entMan.System<RMCCameraSystem>();
                var camera = entMan.SpawnEntity("CMUTestLegacyRmcCamera", MapCoordinates.Nullspace);
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);

                try
                {
                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    var receiver = entMan.GetComponent<CameraNetworkReceiverComponent>(computer);
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);

                    Assert.Multiple(() =>
                    {
                        Assert.That(member.Networks, Is.EquivalentTo(new[] { NetworkA }));
                        Assert.That(member.SourceKinds, Is.EqualTo(CameraSourceKinds.Rmc));
                        Assert.That(receiver.Networks, Is.EquivalentTo(new[] { NetworkA }));
                        Assert.That(receiver.SupportedSources, Is.EqualTo(CameraSourceKinds.Rmc));
                        Assert.That(console.CameraIds, Does.Contain(entMan.GetNetEntity(camera)));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(computer);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task ExplicitRmcComponentsOverrideLegacyFields()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestExplicitRmcCamera", MapCoordinates.Nullspace);
                var computer = entMan.SpawnEntity("CMUTestExplicitRmcComputer", MapCoordinates.Nullspace);
                try
                {
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { NetworkB }));
                    Assert.That(entMan.GetComponent<CameraNetworkReceiverComponent>(computer).Networks,
                        Is.EquivalentTo(new[] { NetworkB }));
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(computer);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task LegacyCameraIdChangeUpdatesOnlyBridgeCreatedMembership()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var camera = entMan.SpawnEntity("CMUTestLegacyRmcCamera", MapCoordinates.Nullspace);
                try
                {
                    rmc.SetCameraId(camera, NetworkB, entMan.GetComponent<RMCCameraComponent>(camera));
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                        Is.EquivalentTo(new[] { NetworkB }));
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task UnknownLegacyNetworkIsSkipped()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestUnknownLegacyRmcCamera", MapCoordinates.Nullspace);
                var computer = entMan.SpawnEntity("CMUTestUnknownLegacyRmcComputer", MapCoordinates.Nullspace);
                try
                {
                    Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks, Is.Empty);
                    Assert.That(entMan.GetComponent<CameraNetworkReceiverComponent>(computer).Networks, Is.Empty);
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(computer);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcSelectionRejectsStandardAndUnauthorizedSources()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var standard = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);
                var rmcCamera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    Assert.That(rmc.TrySelectCamera((computer, console), standard), Is.False);
                    Assert.That(rmc.TrySelectCamera((computer, console), rmcCamera), Is.True);
                    Assert.That(networks.SetReceiverNetworks(computer, [NetworkB]), Is.True);
                    Assert.That(rmc.TrySelectCamera((computer, console), rmcCamera), Is.False);
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(standard);
                    entMan.DeleteEntity(rmcCamera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RmcSelectionRejectsUnavailableSource(bool deleted)
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);
                try
                {
                    if (deleted)
                        entMan.DeleteEntity(camera);
                    else
                        entMan.System<MetaDataSystem>().SetEntityPaused(camera, true);

                    Assert.That(rmc.TrySelectCamera((computer,
                            entMan.GetComponent<RMCCameraComputerComponent>(computer)), camera),
                        Is.False);
                    Assert.That(entMan.GetComponent<RMCCameraComputerComponent>(computer).CurrentCamera, Is.Null);
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    if (!entMan.Deleted(camera))
                        entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task FinalNetworkLossDisconnectsRmcWatchers()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    Assert.That(rmc.TrySelectCamera((computer, console), camera), Is.True);
                    Assert.That(networks.SetReceiverNetworks(computer, [NetworkB]), Is.True);
                    Assert.That(console.CurrentCamera, Is.Null);
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RmcRejectsClickAfterStateCameraLosesAuthorization()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestRmcShipCamera", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    rmc.RebuildComputerCameras(computer, console);
                    Assert.That(console.CameraIds, Does.Contain(entMan.GetNetEntity(camera)));

                    Assert.That(networks.SetReceiverNetworks(computer, [NetworkB]), Is.True);
                    Assert.That(rmc.TrySelectCamera((computer, console), camera), Is.False);
                    Assert.That(console.CurrentCamera, Is.Null);
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task RemovedRmcCameraClearsCurrentSelection()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var rmc = entMan.System<RMCCameraSystem>();
                var computer = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);
                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    Assert.That(rmc.TrySelectCamera((computer, console), camera), Is.True);
                    var netCamera = entMan.GetNetEntity(camera);

                    entMan.DeleteEntity(camera);

                    Assert.Multiple(() =>
                    {
                        Assert.That(console.CurrentCamera, Is.Null);
                        Assert.That(console.CameraIds, Does.Not.Contain(netCamera));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    if (!entMan.Deleted(camera))
                        entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task TwoLzGrantersRequireTwoRevokes()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestLegacyRmcComputer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);
                var firstGranter = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var secondGranter = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                try
                {
                    networks.SetReceiverNetworks(receiver, []);
                    var firstGrant = new CameraNetworkGrantRequestEvent(NetworkA, firstGranter, true);
                    var secondGrant = new CameraNetworkGrantRequestEvent(NetworkA, secondGranter, true);
                    entMan.EventBus.RaiseLocalEvent(receiver, ref firstGrant);
                    entMan.EventBus.RaiseLocalEvent(receiver, ref secondGrant);
                    Assert.That(networks.CanAccess(receiver, camera), Is.True);

                    var firstRevoke = new CameraNetworkGrantRequestEvent(NetworkA, firstGranter, false);
                    entMan.EventBus.RaiseLocalEvent(receiver, ref firstRevoke);
                    Assert.That(networks.CanAccess(receiver, camera), Is.True);

                    var secondRevoke = new CameraNetworkGrantRequestEvent(NetworkA, secondGranter, false);
                    entMan.EventBus.RaiseLocalEvent(receiver, ref secondRevoke);
                    Assert.That(networks.CanAccess(receiver, camera), Is.False);
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(firstGranter);
                    entMan.DeleteEntity(secondGranter);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task StandardMonitorSwitchesWithoutDeviceNetwork()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    var selected = monitors.TrySelectCamera(
                        new Entity<SurveillanceCameraMonitorComponent>(
                            monitor,
                            entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)),
                        camera);

                    Assert.Multiple(() =>
                    {
                        Assert.That(selected, Is.True);
                        Assert.That(entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor).ActiveCamera,
                            Is.EqualTo(camera));
                        Assert.That(entMan.GetComponent<SurveillanceCameraComponent>(camera).ActiveMonitors,
                            Does.Contain(monitor));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task StandardMonitorStateContainsAuthorizedMapAndEntityCameraList()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitorDualNetwork", new EntityCoordinates(grid, Vector2.Zero));
                var authorizedCamera = entMan.SpawnEntity(
                    "CMUTestSurveillanceCameraStandard",
                    new EntityCoordinates(grid, Vector2.One));
                var authorizedSecondCamera = entMan.SpawnEntity(
                    "CMUTestCameraStandardB",
                    new EntityCoordinates(grid, new Vector2(2, 2)));
                var unauthorizedCamera = entMan.SpawnEntity(
                    "CMUTestSurveillanceCameraRmc",
                    new EntityCoordinates(grid, new Vector2(3, 3)));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(authorizedCamera);
                    entMan.AddComponent<CameraMapMarkerComponent>(authorizedSecondCamera);
                    entMan.AddComponent<CameraMapMarkerComponent>(unauthorizedCamera);

                    var state = monitors.BuildUiState((monitor,
                        entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)));

                    Assert.Multiple(() =>
                    {
                        Assert.That(state.Networks.Select(network => network.Id),
                            Is.EqualTo(new[]
                            {
                                (ProtoId<CameraNetworkPrototype>) NetworkA,
                                (ProtoId<CameraNetworkPrototype>) NetworkB,
                            }));
                        Assert.That(state.Networks.All(network => !string.IsNullOrEmpty(network.Name)), Is.True);
                        Assert.That(state.CameraList, Has.Count.EqualTo(1));
                        Assert.That(state.CameraList[0].Camera, Is.EqualTo(entMan.GetNetEntity(authorizedCamera)));
                        Assert.That(state.CameraList[0].Name, Is.Not.Empty);
                        Assert.That(state.CameraMap.Grids.SelectMany(cameraGrid => cameraGrid.Markers)
                                .Select(marker => marker.Camera),
                            Is.EquivalentTo(new[]
                            {
                                entMan.GetNetEntity(authorizedCamera),
                                entMan.GetNetEntity(authorizedSecondCamera),
                            }));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(authorizedCamera);
                    entMan.DeleteEntity(authorizedSecondCamera);
                    entMan.DeleteEntity(unauthorizedCamera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RmcBuiStateContainsMapButKeepsFullCameraList()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var cameras = entMan.System<RMCCameraSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var computer = entMan.SpawnEntity("CMUTestRmcMapComputer", new EntityCoordinates(grid, Vector2.Zero));
                var shipCamera = entMan.SpawnEntity("CMUTestRmcShipCamera", new EntityCoordinates(grid, Vector2.One));
                var mortarCamera = entMan.SpawnEntity("CMUTestRmcMortarCamera", new EntityCoordinates(grid, new Vector2(2, 2)));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(shipCamera);
                    cameras.RebuildComputerCameras(computer);

                    var state = cameras.BuildBuiState((computer,
                        entMan.GetComponent<RMCCameraComputerComponent>(computer)));
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);

                    Assert.Multiple(() =>
                    {
                        Assert.That(console.CameraIds, Is.EquivalentTo(new[]
                        {
                            entMan.GetNetEntity(shipCamera),
                            entMan.GetNetEntity(mortarCamera),
                        }));
                        Assert.That(state.Map.Grids.SelectMany(cameraGrid => cameraGrid.Markers)
                                .Select(marker => marker.Camera),
                            Is.EquivalentTo(new[] { entMan.GetNetEntity(shipCamera) }));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(shipCamera);
                    entMan.DeleteEntity(mortarCamera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task StandardMonitorRejectsRmcSource()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraRmc", MapCoordinates.Nullspace);

                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(monitors.TrySelectCamera((monitor,
                                entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)), camera),
                            Is.False);
                        Assert.That(entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor).ActiveCamera,
                            Is.Null);
                        Assert.That(entMan.GetComponent<SurveillanceCameraComponent>(camera).ActiveMonitors,
                            Does.Not.Contain(monitor));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task StandardMonitorRejectsPausedAndDeletedSource()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    entMan.System<MetaDataSystem>().SetEntityPaused(camera, true);
                    Assert.That(monitors.TrySelectCamera((monitor,
                            entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)), camera),
                        Is.False);

                    entMan.System<MetaDataSystem>().SetEntityPaused(camera, false);
                    entMan.DeleteEntity(camera);
                    Assert.That(monitors.TrySelectCamera((monitor,
                            entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)), camera),
                        Is.False);
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    if (!entMan.Deleted(camera))
                        entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task StandardMonitorRejectsInactiveSource()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var cameras = entMan.System<SurveillanceCameraSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    cameras.SetActive(camera, false);

                    Assert.That(monitors.TrySelectCamera((monitor,
                            entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)), camera),
                        Is.False);
                    Assert.That(entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor).ActiveCamera, Is.Null);
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task FinalNetworkLossDisconnectsStandardMonitor()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(monitors.TrySelectCamera((monitor,
                            entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor)), camera),
                        Is.True);

                    Assert.That(networks.SetReceiverNetworks(monitor, []), Is.True);

                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor).ActiveCamera,
                            Is.Null);
                        Assert.That(entMan.GetComponent<SurveillanceCameraComponent>(camera).ActiveMonitors,
                            Does.Not.Contain(monitor));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MemberShutdownDisconnectsStandardMonitor()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    var monitorComponent = entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor);
                    Assert.That(monitors.TrySelectCamera((monitor, monitorComponent), camera), Is.True);

                    entMan.RemoveComponent<CameraNetworkMemberComponent>(camera);

                    Assert.Multiple(() =>
                    {
                        Assert.That(monitorComponent.ActiveCamera, Is.Null);
                        Assert.That(entMan.GetComponent<SurveillanceCameraComponent>(camera).ActiveMonitors,
                            Does.Not.Contain(monitor));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task StandardMonitorRejectsClickAfterStateCameraLosesAuthorization()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var monitors = entMan.System<SurveillanceCameraMonitorSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                var monitor = entMan.SpawnEntity("CMUTestSurveillanceMonitor", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestSurveillanceCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    var monitorComponent = entMan.GetComponent<SurveillanceCameraMonitorComponent>(monitor);
                    var state = monitors.BuildUiState((monitor, monitorComponent));
                    Assert.That(state.CameraList.Select(data => data.Camera), Does.Contain(entMan.GetNetEntity(camera)));

                    Assert.That(networks.SetReceiverNetworks(monitor, []), Is.True);
                    Assert.That(monitors.TrySelectCamera((monitor, monitorComponent), camera), Is.False);
                    Assert.That(monitorComponent.ActiveCamera, Is.Null);
                }
                finally
                {
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task UnnetworkedSurveillanceCameraRemainsOutsideCameraScope()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestUnnetworkedSurveillanceCamera", MapCoordinates.Nullspace);

                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.HasComponent<CameraNetworkMemberComponent>(camera), Is.False);
                        Assert.That(entMan.HasComponent<CameraMapMarkerComponent>(camera), Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task UnnetworkedItemSurveillanceCameraStaysOutOfStandardNetwork()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestUnnetworkedItemSurveillanceCamera", MapCoordinates.Nullspace);

                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.HasComponent<ItemComponent>(camera), Is.True);
                        Assert.That(entMan.HasComponent<CameraNetworkMemberComponent>(camera), Is.False);
                        Assert.That(entMan.HasComponent<CameraMapMarkerComponent>(camera), Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task ConstructedCameraSelectionHonorsAllowlistAndConfigurableFlag()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var cameras = entMan.System<SurveillanceCameraSystem>();
                var camera = entMan.SpawnEntity("CMUTestConstructedSurveillanceCamera", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);

                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(cameras.TrySetNetwork((camera, component), NetworkA), Is.True);
                        Assert.That(cameras.TrySetNetwork((camera, component), NetworkB), Is.False);
                        Assert.That(cameras.TrySetNetwork((camera, component), "CMUTestCameraNetworkUnlisted"), Is.False);
                    });

                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    Assert.Multiple(() =>
                    {
                        Assert.That(component.NetworkSet, Is.True);
                        Assert.That(member.Networks, Is.EquivalentTo(new[] { NetworkA }));
                        Assert.That(member.SourceKinds, Is.EqualTo(CameraSourceKinds.Standard));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task ClosedCameraPanelHidesSetupVerbAndRejectsNetworkRequest()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestPanelGatedSurveillanceCamera", MapCoordinates.Nullspace);
                var user = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);

                try
                {
                    var verbs = new GetVerbsEvent<AlternativeVerb>(user, camera, null, null, true, true, true, []);
                    entMan.EventBus.RaiseLocalEvent(camera, verbs, true);
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(1)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetName("closed-panel-name")
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(verbs.Verbs.Any(verb => verb.Text == Loc.GetString("surveillance-camera-setup")), Is.False);
                        Assert.That(component.NameSet, Is.False);
                        Assert.That(component.NetworkSet, Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(user);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task OpeningCameraPanelEnablesSetupVerbAndNetworkRequest()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestPanelGatedSurveillanceCamera", MapCoordinates.Nullspace);
                var user = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
                var screwdriver = entMan.SpawnEntity("Screwdriver", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                var panel = entMan.GetComponent<WiresPanelComponent>(camera);

                try
                {
                    var interaction = new InteractUsingEvent(
                        user,
                        screwdriver,
                        camera,
                        entMan.GetComponent<TransformComponent>(camera).Coordinates);
                    entMan.EventBus.RaiseLocalEvent(camera, interaction);
                    Assert.That(interaction.Handled, Is.True);
                    server.RunTicks(1);
                    Assert.That(panel.Open, Is.True);

                    var verbs = new GetVerbsEvent<AlternativeVerb>(user, camera, null, null, true, true, true, []);
                    entMan.EventBus.RaiseLocalEvent(camera, verbs, true);
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(1)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(verbs.Verbs.Any(verb => verb.Text == Loc.GetString("surveillance-camera-setup")), Is.True);
                        Assert.That(component.NetworkSet, Is.True);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(screwdriver);
                    entMan.DeleteEntity(user);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task OpenCameraSetupAllowsRepeatedNameAndNetworkChanges()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var wires = entMan.System<WiresSystem>();
                var ui = entMan.System<UserInterfaceSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId);
                var camera = entMan.SpawnEntity("CMUTestPanelGatedSurveillanceCamera",
                    new EntityCoordinates(grid, Vector2.Zero));
                var monitor = entMan.SpawnEntity("CMUMonitorCameraColonyGovforSpy",
                    new EntityCoordinates(grid, Vector2.One));
                var user = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                var panel = entMan.GetComponent<WiresPanelComponent>(camera);
                var monitorComponent = entMan.GetComponent<RMCCameraComputerComponent>(monitor);

                try
                {
                    Assert.That(wires.TogglePanel(camera, panel, true, user), Is.True);

                    var verbs = new GetVerbsEvent<AlternativeVerb>(user, camera, null, null, true, true, true, []);
                    entMan.EventBus.RaiseLocalEvent(camera, verbs, true);
                    verbs.Verbs.Single(verb => verb.Text == Loc.GetString("surveillance-camera-setup")).Act!();

                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetName("camera-alpha")
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(0)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.That(ui.IsUiOpen(camera, SurveillanceCameraSetupUiKey.Camera, user), Is.True);
                    Assert.That(ui.TryGetUiState<SurveillanceCameraLogicalNetworkSetupBoundUiState>(
                        camera, SurveillanceCameraSetupUiKey.Camera, out var state), Is.True);
                    Assert.That(rmc.TrySelectNetwork((monitor, monitorComponent),
                        "CMUSurveillanceCameraColonyCMB"), Is.True);
                    rmc.RebuildComputerCameras(monitor, monitorComponent);
                    var alphaState = rmc.BuildBuiState((monitor, monitorComponent));
                    Assert.Multiple(() =>
                    {
                        Assert.That(component.CameraId, Is.EqualTo("camera-alpha"));
                        Assert.That(entMan.GetComponent<MetaDataComponent>(camera).EntityName,
                            Is.EqualTo("camera-alpha"));
                        Assert.That(component.NameSet, Is.True);
                        Assert.That(component.NetworkSet, Is.True);
                        Assert.That(state!.NameDisabled, Is.False);
                        Assert.That(state.NetworkDisabled, Is.False);
                        Assert.That(monitorComponent.CameraNames, Does.Contain("camera-alpha"));
                        Assert.That(alphaState.Map.Grids.SelectMany(gridState => gridState.Markers)
                            .Single(marker => marker.Camera == entMan.GetNetEntity(camera)).Name,
                            Is.EqualTo("camera-alpha"));
                    });

                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetName("camera-bravo")
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(1)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.That(rmc.TrySelectNetwork((monitor, monitorComponent),
                        "CMUSurveillanceCameraColonyGOVFOR"), Is.True);
                    rmc.RebuildComputerCameras(monitor, monitorComponent);
                    var bravoState = rmc.BuildBuiState((monitor, monitorComponent));

                    Assert.Multiple(() =>
                    {
                        Assert.That(component.CameraId, Is.EqualTo("camera-bravo"));
                        Assert.That(entMan.GetComponent<MetaDataComponent>(camera).EntityName,
                            Is.EqualTo("camera-bravo"));
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Is.EquivalentTo(new[] { "CMUSurveillanceCameraColonyGOVFOR" }));
                        Assert.That(ui.IsUiOpen(camera, SurveillanceCameraSetupUiKey.Camera, user), Is.True);
                        Assert.That(monitorComponent.CameraNames, Does.Contain("camera-bravo"));
                        Assert.That(bravoState.Map.Grids.SelectMany(gridState => gridState.Markers)
                            .Single(marker => marker.Camera == entMan.GetNetEntity(camera)).Name,
                            Is.EqualTo("camera-bravo"));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(user);
                    entMan.DeleteEntity(monitor);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task ClosingCameraPanelClosesSetupUiAndRejectsStaleNetworkRequest()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var wires = entMan.System<WiresSystem>();
                var ui = entMan.System<UserInterfaceSystem>();
                var camera = entMan.SpawnEntity("CMUSurveillanceCameraColonyCMB", MapCoordinates.Nullspace);
                var user = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                var panel = entMan.GetComponent<WiresPanelComponent>(camera);

                try
                {
                    Assert.That(wires.TogglePanel(camera, panel, true, user), Is.True);

                    var verbs = new GetVerbsEvent<AlternativeVerb>(user, camera, null, null, true, true, true, []);
                    entMan.EventBus.RaiseLocalEvent(camera, verbs, true);
                    verbs.Verbs.Single(verb => verb.Text == Loc.GetString("surveillance-camera-setup")).Act!();
                    Assert.That(ui.IsUiOpen(camera, SurveillanceCameraSetupUiKey.Camera, user), Is.True);

                    Assert.That(wires.TogglePanel(camera, panel, false, user), Is.True);
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(1)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetName("stale-panel-name")
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(ui.IsUiOpen(camera, SurveillanceCameraSetupUiKey.Camera, user), Is.False);
                        Assert.That(component.NameSet, Is.False);
                        Assert.That(component.NetworkSet, Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(user);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task SpoofedSetupIndexDoesNotChangeMembership()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestConstructedSurveillanceCameraNetworked", MapCoordinates.Nullspace);
                var component = entMan.GetComponent<SurveillanceCameraComponent>(camera);

                try
                {
                    entMan.EventBus.RaiseLocalEvent(camera, new SurveillanceCameraSetupSetNetwork(component.AvailableNetworks.Count)
                    {
                        UiKey = SurveillanceCameraSetupUiKey.Camera,
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(component.NetworkSet, Is.False);
                        Assert.That(entMan.GetComponent<CameraNetworkMemberComponent>(camera).Networks,
                            Is.EquivalentTo(new[] { NetworkB }));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public void RouterSetupStateStillUsesDeviceFrequencies()
    {
        var state = new SurveillanceCameraSetupBoundUiState(
            "router",
            42,
            ["CMUTestRouterFrequency"],
            nameDisabled: true,
            networkDisabled: false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Network, Is.EqualTo(42));
            Assert.That(state.Networks, Is.EquivalentTo(new[] { "CMUTestRouterFrequency" }));
        });
    }

    [Test]
    public async Task ExplicitCameraStateIsPreservedOneToOne()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var camera = entMan.SpawnEntity("CMUTestStatefulSurveillanceCamera", MapCoordinates.Nullspace);

                try
                {
                    var surveillance = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    var marker = entMan.GetComponent<CameraMapMarkerComponent>(camera);
                    entMan.System<SurveillanceCameraSystem>().SetActive(camera, false, surveillance);
                    var availableNetworks = new List<ProtoId<CameraNetworkPrototype>>(surveillance.AvailableNetworks);
                    var memberNetworks = new HashSet<ProtoId<CameraNetworkPrototype>>(member.Networks);
                    var updateInterval = marker.UpdateInterval;

                    server.RunTicks(3);

                    Assert.Multiple(() =>
                    {
                        Assert.That(surveillance.Active, Is.False);
                        Assert.That(surveillance.CameraId, Is.EqualTo("Preserved camera id"));
                        Assert.That(surveillance.NameSet, Is.True);
                        Assert.That(surveillance.NetworkSet, Is.True);
                        Assert.That(surveillance.AvailableNetworks, Is.EqualTo(availableNetworks));
                        Assert.That(member.Networks, Is.EquivalentTo(memberNetworks));
                        Assert.That(member.SourceKinds, Is.EqualTo(CameraSourceKinds.Standard));
                        Assert.That(marker.Visible, Is.False);
                        Assert.That(marker.Mobile, Is.True);
                        Assert.That(marker.UpdateInterval, Is.EqualTo(updateInterval));
                        Assert.That(surveillance.ActiveViewers, Is.Empty);
                        Assert.That(surveillance.ActiveMonitors, Is.Empty);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task HiddenMarkerRemainsListAccessible()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId);
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent(camera, new CameraMapMarkerComponent { Visible = false });

                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(receiver, camera), Is.True);
                        Assert.That(networks.GetAccessibleCameras(new Entity<CameraNetworkReceiverComponent>(
                                receiver, entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))),
                            Does.Contain(camera));
                        var hiddenState = networks.BuildMapState(receiver);
                        Assert.That(hiddenState.Grids.SelectMany(grid => grid.Markers), Is.Empty);

                        networks.SetMapVisibility(camera, true);
                        var visibleState = networks.BuildMapState(receiver);
                        var visibleMarker = visibleState.Grids.SelectMany(grid => grid.Markers).Single();
                        Assert.That(visibleMarker.Status, Is.EqualTo(CameraMapMarkerStatus.Active));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MapVisibilityDoesNotAffectListOrVideoAccess()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId);
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);

                    Assert.That(networks.SetMapVisibility(camera, false), Is.True);
                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(receiver, camera), Is.True);
                        Assert.That(networks.GetAccessibleCameras(new Entity<CameraNetworkReceiverComponent>(
                                receiver, entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))),
                            Does.Contain(camera));
                        Assert.That(networks.BuildMapState(receiver).Grids
                            .SelectMany(gridState => gridState.Markers), Is.Empty);
                    });

                    Assert.That(networks.SetMapVisibility(camera, true), Is.True);
                    Assert.That(networks.BuildMapState(receiver).Grids.Single().Markers.Single().Camera,
                        Is.EqualTo(entMan.GetNetEntity(camera)));
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [TestCase("CMUSurveillanceCameraColonyCMB", "CMUMonitorCameraColonyCMB")]
    [TestCase("RMCSurveillanceCameraAlmayer", "RMCMonitorCameraAlmayer")]
    public async Task CmuRmcMapWireAndMarkerLifecycle(
        string cameraPrototype,
        string receiverPrototype)
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var transforms = entMan.System<SharedTransformSystem>();
                var maps = entMan.System<SharedMapSystem>();
                maps.CreateMap(out var mapId);
                var firstGrid = maps.CreateGridEntity(mapId).Owner;
                var secondGrid = maps.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity(receiverPrototype, new EntityCoordinates(firstGrid, Vector2.Zero));
                var camera = entMan.SpawnEntity(cameraPrototype, new EntityCoordinates(firstGrid, Vector2.Zero));

                try
                {
                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    networks.SetReceiverNetworks(receiver, member.Networks);
                    var marker = networks.BuildMapState(receiver).Grids.SelectMany(grid => grid.Markers).Single();
                    Assert.That(marker.Camera, Is.EqualTo(entMan.GetNetEntity(camera)), "created marker");

                    transforms.SetCoordinates(camera, new EntityCoordinates(secondGrid, Vector2.Zero));
                    marker = networks.BuildMapState(receiver).Grids.SelectMany(grid => grid.Markers).Single();
                    Assert.That(networks.BuildMapState(receiver).Grids.Single().Grid,
                        Is.EqualTo(entMan.GetNetEntity(secondGrid)), "moved grid");

                    var wires = entMan.GetComponent<WiresComponent>(camera);
                    var mapWire = wires.WiresList.Single(wire => wire.Action is CameraMapVisibilityWireAction);
                    var action = (CameraMapVisibilityWireAction) mapWire.Action!;
                    var surveillance = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                    Assert.That(action.Cut(receiver, mapWire, surveillance), Is.True);
                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.BuildMapState(receiver).Grids.SelectMany(grid => grid.Markers), Is.Empty,
                            "MAP cut hides marker");
                        Assert.That(networks.GetAccessibleCameras((receiver,
                            entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))), Does.Contain(camera),
                            "MAP cut keeps list access");
                    });

                    Assert.That(action.Mend(receiver, mapWire, surveillance), Is.True);
                    Assert.That(networks.BuildMapState(receiver).Grids.SelectMany(grid => grid.Markers).Single().Camera,
                        Is.EqualTo(entMan.GetNetEntity(camera)), "MAP mend restores marker");

                    entMan.DeleteEntity(camera);
                    Assert.That(networks.BuildMapState(receiver).Grids.SelectMany(grid => grid.Markers), Is.Empty,
                        "deleted camera has no marker");
                }
                finally
                {
                    if (entMan.EntityExists(camera))
                        entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(firstGrid);
                    entMan.DeleteEntity(secondGrid);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [TestCase("CMUSurveillanceCameraColonyCMB", "CMUMonitorCameraColonyCMB")]
    [TestCase("RMCSurveillanceCameraAlmayer", "RMCMonitorCameraAlmayer")]
    public async Task CmuRmcCameraPowerWireAndApcStateControlAvailability(
        string cameraPrototype,
        string receiverPrototype)
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);
        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var cameras = entMan.System<SurveillanceCameraSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var maps = entMan.System<SharedMapSystem>();
                maps.CreateMap(out var mapId);
                var grid = maps.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity(receiverPrototype, new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity(cameraPrototype, new EntityCoordinates(grid, Vector2.Zero));

                try
                {
                    var surveillance = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    networks.SetReceiverNetworks(receiver, member.Networks);
                    cameras.SetActive(camera, true, surveillance);
                    if (entMan.TryGetComponent(receiver, out RMCCameraComputerComponent? computer))
                    {
                        Assert.That(rmc.TrySelectNetwork((receiver, computer), member.Networks.Single()), Is.True);
                        rmc.RebuildComputerCameras(receiver, computer);
                        Assert.That(rmc.TrySelectCamera((receiver, computer), camera), Is.True, "powered selection");
                    }

                    var wires = entMan.GetComponent<WiresComponent>(camera);
                    var powerWire = wires.WiresList.Single(wire => wire.Action is PowerWireAction);
                    var powerAction = (PowerWireAction) powerWire.Action!;
                    var power = entMan.GetComponent<ApcPowerReceiverComponent>(camera);
                    power.Powered = true;

                    Assert.That(powerAction.Cut(receiver, powerWire), Is.True);
                    Assert.That(power.PowerDisabled, Is.True, "POWER cut disables power receiver");
                    power.Powered = false;
                    var lostPower = new PowerChangedEvent(false, 0f);
                    entMan.EventBus.RaiseLocalEvent(camera, ref lostPower);

                    var inactiveMarker = networks.BuildMapState(receiver).Grids
                        .SelectMany(gridState => gridState.Markers).Single();
                    Assert.Multiple(() =>
                    {
                        Assert.That(surveillance.Active, Is.False, "power loss disables camera");
                        Assert.That(inactiveMarker.Status, Is.EqualTo(CameraMapMarkerStatus.Inactive));
                        if (entMan.TryGetComponent(receiver, out RMCCameraComputerComponent? inactiveComputer))
                            Assert.That(rmc.TrySelectCamera((receiver, inactiveComputer), camera), Is.False,
                                "inactive camera cannot be selected");
                    });

                    Assert.That(powerAction.Mend(receiver, powerWire), Is.True);
                    Assert.That(power.PowerDisabled, Is.False, "POWER mend enables power receiver");
                    power.Powered = true;
                    var restoredPower = new PowerChangedEvent(true, power.Load);
                    entMan.EventBus.RaiseLocalEvent(camera, ref restoredPower);

                    var activeMarker = networks.BuildMapState(receiver).Grids
                        .SelectMany(gridState => gridState.Markers).Single();
                    Assert.Multiple(() =>
                    {
                        Assert.That(surveillance.Active, Is.True, "restored APC power enables camera");
                        Assert.That(activeMarker.Status, Is.EqualTo(CameraMapMarkerStatus.Active));
                        if (entMan.TryGetComponent(receiver, out RMCCameraComputerComponent? activeComputer))
                        {
                            rmc.RebuildComputerCameras(receiver, activeComputer);
                            Assert.That(rmc.TrySelectCamera((receiver, activeComputer), camera), Is.True,
                                "restored camera can be selected");
                        }
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally { server.Dispose(); }
    }

    [Test]
    public async Task SelectedRmcCameraPowerLossClearsFeedAndViewerOverride()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Fresh = true,
            Destructive = true,
        });
        var server = pair.Server;
        try
        {
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var rmc = entMan.System<RMCCameraSystem>();
                var session = server.PlayerMan.Sessions.Single();
                var previousAttached = session.AttachedEntity;
                var computer = entMan.SpawnEntity("RMCMonitorCameraAlmayer", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("RMCSurveillanceCameraAlmayer", MapCoordinates.Nullspace);
                var watcher = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    var console = entMan.GetComponent<RMCCameraComputerComponent>(computer);
                    var member = entMan.GetComponent<CameraNetworkMemberComponent>(camera);
                    networks.SetReceiverNetworks(computer, member.Networks);
                    rmc.RebuildComputerCameras(computer, console);
                    server.PlayerMan.SetAttachedEntity(session, watcher);
                    entMan.EventBus.RaiseLocalEvent(computer,
                        new BoundUIOpenedEvent(RMCCameraUiKey.Key, computer, watcher));
                    Assert.That(rmc.TrySelectCamera((computer, console), camera), Is.True);

                    var watcherComp = entMan.GetComponent<RMCCameraWatcherComponent>(watcher);
                    Assert.That(watcherComp.Overrides, Does.Contain(entMan.GetNetEntity(camera)));
                    Assert.That(session.ViewSubscriptions, Does.Contain(camera));

                    var surveillance = entMan.GetComponent<SurveillanceCameraComponent>(camera);
                    entMan.System<SurveillanceCameraSystem>().SetActive(camera, false, surveillance);
                    var powerLost = new PowerChangedEvent(false, 0f);
                    entMan.EventBus.RaiseLocalEvent(camera, ref powerLost);
                    networks.Update(0f);

                    Assert.Multiple(() =>
                    {
                        Assert.That(console.CurrentCamera, Is.Null);
                        Assert.That(watcherComp.Overrides, Is.Empty);
                        Assert.That(session.ViewSubscriptions, Does.Not.Contain(camera));
                    });
                }
                finally
                {
                    server.PlayerMan.SetAttachedEntity(session, previousAttached);
                    entMan.DeleteEntity(computer);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(watcher);
                }
            });
        }
        finally { await pair.CleanReturnAsync(); }
    }

    [Test]
    public async Task UnmarkedCameraVisibilityActionIsSafe()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);

                try
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.SetMapVisibility(camera, false), Is.False);
                        Assert.That(networks.IsMapVisible(camera), Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task InactiveMarkerIsIncludedButNotActive()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    entMan.AddComponent<SurveillanceCameraComponent>(camera);
                    entMan.System<SurveillanceCameraSystem>().SetActive(camera, false);

                    var marker = networks.BuildMapState(receiver).Grids.Single().Markers.Single();
                    Assert.Multiple(() =>
                    {
                        Assert.That(marker.Camera, Is.EqualTo(entMan.GetNetEntity(camera)));
                        Assert.That(marker.Active, Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task PausedMarkerIsNotExposedInMapState()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    entMan.System<MetaDataSystem>().SetEntityPaused(camera, true);

                    Assert.That(networks.BuildMapState(receiver).Grids, Is.Empty);
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MapStateDoesNotAttachNetworkedComponentToGrid()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                mapSystem.CreateMap(out var mapId);
                var firstGridEntity = mapSystem.CreateGridEntity(mapId);
                var secondGridEntity = mapSystem.CreateGridEntity(mapId);
                var firstGrid = firstGridEntity.Owner;
                var secondGrid = secondGridEntity.Owner;
                mapSystem.SetTile(firstGridEntity.Owner, firstGridEntity.Comp, Vector2i.Zero, new Tile(1));
                mapSystem.SetTile(secondGridEntity.Owner, secondGridEntity.Comp, Vector2i.Zero, new Tile(1));
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(firstGrid, Vector2.Zero));

                try
                {
                    _ = networks.BuildMapState(receiver);
                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.HasComponent<CameraMapMarkerComponent>(firstGrid), Is.False);
                        Assert.That(entMan.HasComponent<CameraMapMarkerComponent>(secondGrid), Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(firstGrid);
                    entMan.DeleteEntity(secondGrid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MobileMovementCoalescesReceiverChangeEvents()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var timing = server.ResolveDependency<IGameTiming>();
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var marker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent(marker, new CameraMapMarkerComponent { Mobile = true });
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    var transforms = entMan.System<SharedTransformSystem>();
                    transforms.SetCoordinates(marker, new EntityCoordinates(grid, new Vector2(2, 1)));
                    transforms.SetCoordinates(marker, new EntityCoordinates(grid, new Vector2(3, 1)));

                    timing.CurTick += (uint) Math.Ceiling(TimeSpan.FromSeconds(0.25).TotalSeconds / timing.TickPeriod.TotalSeconds);
                    networks.Update(0f);

                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.That(probe.Events, Is.EqualTo(1));
                    Assert.That(probe.LastKind, Is.EqualTo(CameraReceiverChangeKind.Marker));
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(marker);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }

    }

    [TestCase(MarkerLifecycleChange.Rename)]
    [TestCase(MarkerLifecycleChange.GridTransfer)]
    [TestCase(MarkerLifecycleChange.PowerLoss)]
    [TestCase(MarkerLifecycleChange.MarkerRemoval)]
    public async Task StaticMarkerLifecycleChangesQueueReceiverRefresh(MarkerLifecycleChange change)
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var maps = entMan.System<SharedMapSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                maps.CreateMap(out var mapId);
                var firstGrid = maps.CreateGridEntity(mapId).Owner;
                var secondGrid = maps.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(firstGrid, Vector2.Zero));
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(firstGrid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(camera);
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    networks.Update(0f);
                    entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver).Events = 0;

                    switch (change)
                    {
                        case MarkerLifecycleChange.Rename:
                            entMan.System<MetaDataSystem>().SetEntityName(camera, "Renamed camera");
                            break;
                        case MarkerLifecycleChange.GridTransfer:
                            entMan.System<SharedTransformSystem>().SetCoordinates(camera,
                                new EntityCoordinates(secondGrid, Vector2.One));
                            break;
                        case MarkerLifecycleChange.PowerLoss:
                            var power = new PowerChangedEvent(false, 0f);
                            entMan.EventBus.RaiseLocalEvent(camera, ref power);
                            break;
                        case MarkerLifecycleChange.MarkerRemoval:
                            entMan.RemoveComponent<CameraMapMarkerComponent>(camera);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(change), change, null);
                    }

                    networks.Update(0f);
                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.Multiple(() =>
                    {
                        Assert.That(probe.Events, Is.EqualTo(1));
                        Assert.That(probe.LastKind, Is.EqualTo(CameraReceiverChangeKind.Marker));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(firstGrid);
                    entMan.DeleteEntity(secondGrid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task QueuedMarkerChangeIsDroppedWhenReceiverShutsDown()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var marker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    entMan.AddComponent<CameraMapMarkerComponent>(marker);
                    entMan.RemoveComponent<CameraNetworkReceiverComponent>(receiver);

                    networks.Update(0f);

                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.That(probe.Events, Is.Zero);
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(marker);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task QueuedMarkerFlushAllowsReentrantReceiverShutdown()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var firstReceiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var secondReceiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.One));
                var marker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, new Vector2(2, 1)));

                try
                {
                    entMan.AddComponent(firstReceiver, new CameraNetworkReceiverChangedProbeComponent
                    {
                        RemoveReceiverOnMarker = true,
                    });
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(secondReceiver);
                    entMan.AddComponent<CameraMapMarkerComponent>(marker);

                    Assert.DoesNotThrow(() => networks.Update(0f));

                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(firstReceiver).Events, Is.EqualTo(1));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(secondReceiver).Events, Is.EqualTo(1));
                        Assert.That(entMan.HasComponent<CameraNetworkReceiverComponent>(firstReceiver), Is.False);
                    });
                }
                finally
                {
                    entMan.DeleteEntity(firstReceiver);
                    entMan.DeleteEntity(secondReceiver);
                    entMan.DeleteEntity(marker);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task QueuedMarkerChangeIsDroppedWhenReceiverTerminates()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var probes = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var marker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    entMan.AddComponent<CameraMapMarkerComponent>(marker);
                    entMan.DeleteEntity(receiver);

                    Assert.That(probes.TerminatingReceivers, Does.Contain(receiver));
                    Assert.DoesNotThrow(() => networks.Update(0f));
                }
                finally
                {
                    entMan.DeleteEntity(marker);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task ReentrantMarkerChangeIsFlushedOnNextUpdate()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var mapSystem = entMan.System<SharedMapSystem>();
                mapSystem.CreateMap(out var mapId);
                var grid = mapSystem.CreateGridEntity(mapId).Owner;
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(grid, Vector2.Zero));
                var firstMarker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, Vector2.One));
                var secondMarker = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(grid, new Vector2(2, 1)));

                try
                {
                    entMan.AddComponent(receiver, new CameraNetworkReceiverChangedProbeComponent
                    {
                        MarkerToQueue = secondMarker,
                    });
                    entMan.AddComponent<CameraMapMarkerComponent>(firstMarker);

                    networks.Update(0f);
                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.That(probe.Events, Is.EqualTo(1));

                    networks.Update(0f);
                    Assert.That(probe.Events, Is.EqualTo(2));
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(firstMarker);
                    entMan.DeleteEntity(secondMarker);
                    entMan.DeleteEntity(grid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MapStateContainsOnlyAuthorizedVisibleMarkersAcrossGrids()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var mapSystem = entMan.System<SharedMapSystem>();
                var networks = entMan.System<CameraNetworkSystem>();
                mapSystem.CreateMap(out var mapId);
                var firstGridEntity = mapSystem.CreateGridEntity(mapId);
                var secondGridEntity = mapSystem.CreateGridEntity(mapId);
                var firstGrid = firstGridEntity.Owner;
                var secondGrid = secondGridEntity.Owner;
                entMan.System<SharedTransformSystem>().SetLocalPosition(secondGrid, new Vector2(50, 50));
                var firstTiles = new List<(Vector2i GridIndices, Tile Tile)>
                {
                    (Vector2i.Zero, new Tile(1)),
                    (new Vector2i(1, 0), new Tile(1)),
                    (Vector2i.One, new Tile(1)),
                };

                for (var y = 1; y <= 3; y++)
                {
                    firstTiles.Add((new Vector2i(2, y), new Tile(1)));
                }

                for (var x = 3; x <= 6; x++)
                {
                    firstTiles.Add((new Vector2i(x, 3), new Tile(1)));
                }

                for (var y = 4; y <= 7; y++)
                {
                    firstTiles.Add((new Vector2i(6, y), new Tile(1)));
                }

                var secondTiles = new List<(Vector2i GridIndices, Tile Tile)>();
                for (var x = -4; x <= 1; x++)
                {
                    secondTiles.Add((new Vector2i(x, 0), new Tile(1)));
                }

                for (var y = 1; y <= 5; y++)
                {
                    secondTiles.Add((new Vector2i(-4, y), new Tile(1)));
                }

                secondTiles.Add((Vector2i.One, new Tile(1)));
                mapSystem.SetTiles(firstGridEntity.Owner, firstGridEntity.Comp, firstTiles);
                mapSystem.SetTiles(secondGridEntity.Owner, secondGridEntity.Comp, secondTiles);
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", new EntityCoordinates(firstGrid, Vector2.Zero));
                var firstCamera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(firstGrid, new Vector2(2, 3)));
                var secondCamera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(secondGrid, new Vector2(-4, 5)));
                var unmarkedCamera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(firstGrid, Vector2.One));
                var hiddenCamera = entMan.SpawnEntity("CMUTestCameraStandardA", new EntityCoordinates(firstGrid, new Vector2(6, 7)));
                var otherNetworkCamera = entMan.SpawnEntity("CMUTestCameraStandardB", new EntityCoordinates(secondGrid, Vector2.One));

                try
                {
                    entMan.AddComponent<CameraMapMarkerComponent>(firstCamera);
                    entMan.AddComponent<CameraMapMarkerComponent>(secondCamera);
                    entMan.AddComponent(hiddenCamera, new CameraMapMarkerComponent { Visible = false });
                    entMan.AddComponent<CameraMapMarkerComponent>(otherNetworkCamera);

                    var state = networks.BuildMapState(receiver);

                    Assert.Multiple(() =>
                    {
                        Assert.That(state.ConsoleGrid, Is.EqualTo(entMan.GetNetEntity(firstGrid)));
                        Assert.That(state.Grids, Has.Count.EqualTo(2));
                        Assert.That(state.Grids[0].Grid, Is.EqualTo(entMan.GetNetEntity(firstGrid)));
                        Assert.That(state.Grids.SelectMany(grid => grid.Markers).Select(marker => marker.Camera),
                            Is.EquivalentTo(new[] { entMan.GetNetEntity(firstCamera), entMan.GetNetEntity(secondCamera) }));
                        Assert.That(state.Grids.Single(grid => grid.Grid == entMan.GetNetEntity(firstGrid)).Markers.Single().Position,
                            Is.EqualTo(new Vector2(2, 3)));
                        Assert.That(state.Grids.Single(grid => grid.Grid == entMan.GetNetEntity(secondGrid)).Markers.Single().Position,
                            Is.EqualTo(new Vector2(-4, 5)));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(firstCamera);
                    entMan.DeleteEntity(secondCamera);
                    entMan.DeleteEntity(unmarkedCamera);
                    entMan.DeleteEntity(hiddenCamera);
                    entMan.DeleteEntity(otherNetworkCamera);
                    entMan.DeleteEntity(firstGrid);
                    entMan.DeleteEntity(secondGrid);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MemberRegistrationAndSourceKindGateAccess()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var standardCamera = entMan.SpawnEntity("CMUTestCameraStandard", MapCoordinates.Nullspace);
                var rmcCamera = entMan.SpawnEntity("CMUTestCameraRmc", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.CanAccess(receiver, standardCamera), Is.True);
                    Assert.That(networks.CanAccess(receiver, rmcCamera), Is.False);
                    Assert.That(
                        networks.GetAccessibleCameras(new Entity<CameraNetworkReceiverComponent>(
                            receiver,
                            entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))),
                        Is.EquivalentTo(new[] { standardCamera }));

                    networks.RemoveNetwork(standardCamera, NetworkA);
                    Assert.That(networks.CanAccess(receiver, standardCamera), Is.False);
                    Assert.That(
                        networks.GetAccessibleCameras(new Entity<CameraNetworkReceiverComponent>(
                            receiver,
                            entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))),
                        Does.Not.Contain(standardCamera));
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(standardCamera);
                    entMan.DeleteEntity(rmcCamera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MultipleMembershipReturnsEachCameraOnce()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraStandard", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.SetReceiverNetworks(receiver, [NetworkA, NetworkB]), Is.True);

                    var accessible = networks.GetAccessibleCameras(
                        new Entity<CameraNetworkReceiverComponent>(
                            receiver,
                            entMan.GetComponent<CameraNetworkReceiverComponent>(receiver)));

                    Assert.Multiple(() =>
                    {
                        Assert.That(accessible, Does.Contain(camera));
                        Assert.That(accessible.Count, Is.EqualTo(1));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task RuntimeGrantsAreSourceAwareAndReferenceCounted()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraDynamicReceiver", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraStandard", MapCoordinates.Nullspace);
                var firstSource = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var secondSource = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.CanAccess(receiver, camera), Is.False);
                    Assert.That(networks.GrantNetwork(receiver, NetworkA, firstSource), Is.True);
                    Assert.That(networks.GrantNetwork(receiver, NetworkA, secondSource), Is.True);
                    Assert.That(networks.CanAccess(receiver, camera), Is.True);

                    Assert.That(networks.RevokeNetwork(receiver, NetworkA, firstSource), Is.True);
                    Assert.That(networks.CanAccess(receiver, camera), Is.True);

                    Assert.That(networks.RevokeNetwork(receiver, NetworkA, secondSource), Is.True);
                    Assert.That(networks.CanAccess(receiver, camera), Is.False);
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(firstSource);
                    entMan.DeleteEntity(secondSource);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task FinalRuntimeGrantRevokeNotifiesAndRemovesAccess()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraDynamicReceiver", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);
                var source = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.That(networks.GrantNetwork(receiver, NetworkA, source), Is.True);
                    entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver).Events = 0;

                    Assert.That(networks.RevokeNetwork(receiver, NetworkA, source), Is.True);

                    var probe = entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(receiver);
                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(receiver, camera), Is.False);
                        Assert.That(probe.Events, Is.EqualTo(1));
                        Assert.That(probe.LastKind, Is.EqualTo(CameraReceiverChangeKind.Authorization));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                    entMan.DeleteEntity(source);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task DeletingGrantSourceRevokesOnlyItsGrant()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraDynamicReceiver", MapCoordinates.Nullspace);
                var cameraA = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);
                var cameraB = entMan.SpawnEntity("CMUTestCameraStandardB", MapCoordinates.Nullspace);
                var sourceA = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                var sourceB = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.GrantNetwork(receiver, NetworkA, sourceA), Is.True);
                    Assert.That(networks.GrantNetwork(receiver, NetworkB, sourceB), Is.True);
                    Assert.That(networks.CanAccess(receiver, cameraA), Is.True);
                    Assert.That(networks.CanAccess(receiver, cameraB), Is.True);

                    entMan.DeleteEntity(sourceA);

                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(receiver, cameraA), Is.False);
                        Assert.That(networks.CanAccess(receiver, cameraB), Is.True);
                        Assert.That(networks.GetEffectiveNetworks(receiver), Does.Not.Contain(NetworkA));
                        Assert.That(networks.GetEffectiveNetworks(receiver), Does.Contain(NetworkB));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(cameraA);
                    entMan.DeleteEntity(cameraB);
                    entMan.DeleteEntity(sourceB);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MemberShutdownRemovesIndexedAccess()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                var receiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.CanAccess(receiver, camera), Is.True);
                    entMan.RemoveComponent<CameraNetworkMemberComponent>(camera);

                    Assert.Multiple(() =>
                    {
                        Assert.That(networks.CanAccess(receiver, camera), Is.False);
                        Assert.That(
                            networks.GetAccessibleCameras(new Entity<CameraNetworkReceiverComponent>(
                                receiver,
                                entMan.GetComponent<CameraNetworkReceiverComponent>(receiver))),
                            Does.Not.Contain(camera));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(receiver);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    [Test]
    public async Task MemberMutationNotifiesOnlyReceiversSharingAffectedNetwork()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await LoadPrototypes(server);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                var networks = entMan.System<CameraNetworkSystem>();
                _ = entMan.System<CameraNetworkReceiverChangedProbeSystem>();
                var networkAReceiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var networkBReceiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var unsharedReceiver = entMan.SpawnEntity("CMUTestCameraReceiver", MapCoordinates.Nullspace);
                var camera = entMan.SpawnEntity("CMUTestCameraStandardA", MapCoordinates.Nullspace);

                try
                {
                    Assert.That(networks.SetReceiverNetworks(networkBReceiver, [NetworkB]), Is.True);
                    Assert.That(networks.SetReceiverNetworks(unsharedReceiver, []), Is.True);
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(networkAReceiver);
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(networkBReceiver);
                    entMan.AddComponent<CameraNetworkReceiverChangedProbeComponent>(unsharedReceiver);

                    Assert.That(networks.SetMemberNetworks(camera, [NetworkB]), Is.True);

                    Assert.Multiple(() =>
                    {
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(networkAReceiver).Events,
                            Is.EqualTo(1));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(networkBReceiver).Events,
                            Is.EqualTo(1));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(unsharedReceiver).Events,
                            Is.Zero);
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(networkAReceiver).LastKind,
                            Is.EqualTo(CameraReceiverChangeKind.MemberList));
                        Assert.That(entMan.GetComponent<CameraNetworkReceiverChangedProbeComponent>(networkBReceiver).LastKind,
                            Is.EqualTo(CameraReceiverChangeKind.MemberList));
                    });
                }
                finally
                {
                    entMan.DeleteEntity(networkAReceiver);
                    entMan.DeleteEntity(networkBReceiver);
                    entMan.DeleteEntity(unsharedReceiver);
                    entMan.DeleteEntity(camera);
                }
            });
        }
        finally
        {
            server.Dispose();
        }
    }

    private static async Task LoadPrototypes(RobustIntegrationTest.IntegrationInstance server)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        server.ProtoMan.LoadString(Prototypes, changed: changed);
        await server.WaitPost(() => server.ProtoMan.ReloadPrototypes(changed));
    }
}

public enum MarkerLifecycleChange : byte
{
    Rename,
    GridTransfer,
    PowerLoss,
    MarkerRemoval,
}

[RegisterComponent]
public sealed partial class CameraNetworkReceiverChangedProbeComponent : Component
{
    public int Events;
    public CameraReceiverChangeKind? LastKind;
    public bool RemoveReceiverOnMarker;
    public EntityUid? MarkerToQueue;
    public EntityUid? FirstMember;
    public EntityUid? SecondMember;
    public ProtoId<CameraNetworkPrototype>? ExpectedNetwork;
    public bool SawBothUpdated;
}

public sealed class CameraNetworkReceiverChangedProbeSystem : EntitySystem
{
    public readonly HashSet<EntityUid> TerminatingReceivers = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CameraNetworkReceiverChangedProbeComponent, CameraReceiverChangedEvent>(OnChanged);
        SubscribeLocalEvent<CameraNetworkReceiverChangedProbeComponent, EntityTerminatingEvent>(OnTerminating);
    }

    private void OnChanged(
        Entity<CameraNetworkReceiverChangedProbeComponent> ent,
        ref CameraReceiverChangedEvent args)
    {
        ent.Comp.Events++;
        ent.Comp.LastKind = args.Kind;

        if (ent.Comp.FirstMember is { } first &&
            ent.Comp.SecondMember is { } second &&
            ent.Comp.ExpectedNetwork is { } expected &&
            EntityManager.TryGetComponent(first, out CameraNetworkMemberComponent? firstMember) &&
            EntityManager.TryGetComponent(second, out CameraNetworkMemberComponent? secondMember))
        {
            ent.Comp.SawBothUpdated = firstMember.Networks.Contains(expected) &&
                                      secondMember.Networks.Contains(expected);
        }

        if (args.Kind == CameraReceiverChangeKind.Marker && ent.Comp.RemoveReceiverOnMarker)
            EntityManager.RemoveComponent<CameraNetworkReceiverComponent>(ent.Owner);

        if (args.Kind == CameraReceiverChangeKind.Marker && ent.Comp.MarkerToQueue is { } marker)
        {
            ent.Comp.MarkerToQueue = null;
            EntityManager.AddComponent<CameraMapMarkerComponent>(marker);
        }
    }

    private void OnTerminating(
        Entity<CameraNetworkReceiverChangedProbeComponent> ent,
        ref EntityTerminatingEvent args)
    {
        TerminatingReceivers.Add(ent.Owner);
    }
}
