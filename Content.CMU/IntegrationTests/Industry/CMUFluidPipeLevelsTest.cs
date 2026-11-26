using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Fluids;
using Content.Server.CMU14.ZLevelBuilding;
using Content.Shared.CMU14.Fluids;
using Content.Shared.CMU14.ZLevelBuilding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.CMU.Industry;

// The liquid pipes that go through the floor: one going up from a level joins the pipe over it, and one going down over a pipe
// going up joins it once, not twice.
public sealed class CMUFluidPipeLevelsTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    [Test]
    public async Task PipesGoingUpAndDownJoinTheLevels()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var maps = Server.System<SharedMapSystem>();
            var transform = Server.System<SharedTransformSystem>();
            var building = Server.System<ZLevelBuildingSystem>();
            var fluids = Server.System<FluidSystem>();
            SEntMan.EnsureComponent<ZBuildableMapComponent>(map.MapUid);

            // The colony's level has a grid of its own on it; the level under it is a map that is a grid itself.
            var upper = SSpawnAtPosition("RUMCSteelPipe", map.GridCoords);
            var world = transform.GetWorldPosition(upper);
            Assert.That(building.EnsureNeighborLevel(map.MapUid, -1, map.Grid.Owner, world, out var below, out var belowGrid), Is.True);
            var grid = SComp<MapGridComponent>(belowGrid);
            var tile = maps.WorldToTile(belowGrid, grid, world);
            maps.SetTile(belowGrid, grid, tile, map.Tile.Tile);
            var lower = SSpawnAtPosition("RUMCSteelPipeUp", maps.GridTileToLocal(belowGrid, grid, tile));

            var lowerPipe = SComp<FluidPipeComponent>(lower);
            var upperPipe = SComp<FluidPipeComponent>(upper);
            lowerPipe.Kind = "Water";
            lowerPipe.Amount = 60;
            fluids.Update(0.5f);
            Assert.That(upperPipe.Amount, Is.GreaterThan(0), "A pipe going up must pass its liquid to the pipe over it.");
            Assert.That(lowerPipe.Amount + upperPipe.Amount, Is.EqualTo(60).Within(0.001f));
            Assert.That(lowerPipe.Neighbours.Count(other => other == upper), Is.EqualTo(1));

            // A pipe going down over the pipe going up: the two are joined once.
            SEntMan.DeleteEntity(upper);
            var down = SSpawnAtPosition("RUMCSteelPipeDown", map.GridCoords);
            fluids.Update(0.5f);
            var downPipe = SComp<FluidPipeComponent>(down);
            Assert.That(lowerPipe.Neighbours.Count(other => other == down), Is.EqualTo(1));
            Assert.That(downPipe.Neighbours.Count(other => other == lower), Is.EqualTo(1));

            SEntMan.DeleteEntity(below);
        });
    }
}
