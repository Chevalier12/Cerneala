using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMap2DDrainContractTests
{
    [Fact]
    public async Task AttachedDisposeIsRejectedWithoutConsumingTerminalMapDisposal()
    {
        TileMap2D map = new();
        IAsyncDisposable terminal = Assert.IsAssignableFrom<IAsyncDisposable>(map);
        Scene2D scene = new();
        scene.Children.Add(map);
        SceneSimulationContext2D? context = new(scene);
        try
        {
            Assert.Throws<InvalidOperationException>(() => terminal.DisposeAsync());
            context.Update();
            scene.Children.Remove(map);
            Assert.DoesNotContain(map, scene.Children);
            context.Dispose();
            context = null;

            Task completed = terminal.DisposeAsync().AsTask();
            Scene2D retryScene = new();
            Assert.Throws<ObjectDisposedException>(() =>
            {
                retryScene.Children.Add(map);
                using SceneSimulationContext2D rejectedContext = new(retryScene);
            });
            await completed.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            context?.Dispose();
        }
    }

    [Fact]
    public async Task DisposeDrainsLateAndResidentReleaseFaultsAcrossRetiredGenerations()
    {
        TileMap2D map = new();
        IAsyncDisposable terminal = Assert.IsAssignableFrom<IAsyncDisposable>(map);
        (TileMapCatalog2D catalog, TileMapChunkInfo2D chunk) = CreateCatalog();
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> delayedA =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releasedA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loadsA = 0;
        int loadsB = 0;
        TileMapSource2D sourceA = new(catalog, (_, _, _) =>
        {
            Interlocked.Increment(ref loadsA);
            return new ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>(delayedA.Task);
        });
        TileMapSource2D sourceB = new(catalog, (_, info, _) =>
        {
            Interlocked.Increment(ref loadsB);
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(CreateData(info), _ =>
                throw new IOException("generation B resident release")));
        });
        map.Source = sourceA;
        Scene2D scene = new();
        scene.Children.Add(map);
        SceneSimulationContext2D? context = new(scene);
        SceneCollisionRegion2D? regionB = null;
        using CancellationTokenSource cancelA = new();
        Task firstDrain;
        Task repeatedDrain;
        try
        {
            Task<SceneCollisionRegion2D> preparingA = scene.CollisionWorld
                .PrepareRegionAsync(new DrawRect(0, 0, 10, 10), cancelA.Token).AsTask();
            PumpUntil(context, () => Volatile.Read(ref loadsA) > 0);

            scene.Children.Remove(map);
            Assert.DoesNotContain(map, scene.Children);
            cancelA.Cancel();
            map.Source = sourceB;
            scene.Children.Add(map);

            Task<SceneCollisionRegion2D> preparingB = scene.CollisionWorld
                .PrepareRegionAsync(new DrawRect(0, 0, 10, 10)).AsTask();
            PumpUntil(context, () => preparingB.IsCompleted && map.GetDiagnosticsSnapshot().ResidentDataChunks == 1);
            Assert.True(preparingB.IsCompletedSuccessfully);
            // The result is already complete; reading it on the UI owner thread
            // preserves affinity for the following scene mutations. No blocking wait occurs.
#pragma warning disable xUnit1031
            regionB = preparingB.GetAwaiter().GetResult();
#pragma warning restore xUnit1031
            Assert.Equal(1, loadsB);
            Assert.IsAssignableFrom<Collider2D>(Assert.Single(map.LogicalChildren));

            delayedA.SetResult(new SceneSpatialLease2D<TileMapChunkData2D>(CreateData(chunk), _ =>
            {
                releasedA.TrySetResult();
                throw new IOException("generation A late release");
            }));
            PumpUntil(context, () => releasedA.Task.IsCompleted);

            scene.Children.Remove(map);
            Assert.DoesNotContain(map, scene.Children);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Empty(map.LogicalChildren);
            regionB.Dispose();
            regionB = null;
            Assert.False(preparingA.IsCompletedSuccessfully);
            context.Dispose();
            context = null;

            firstDrain = terminal.DisposeAsync().AsTask();
            repeatedDrain = terminal.DisposeAsync().AsTask();
        }
        finally
        {
            regionB?.Dispose();
            context?.Dispose();
        }

        Exception? firstFailure = await Record.ExceptionAsync(() => firstDrain);
        Exception? repeatedFailure = await Record.ExceptionAsync(() => repeatedDrain);
        Assert.NotNull(firstFailure);
        Assert.NotNull(repeatedFailure);
        Assert.Contains("generation A late release", firstFailure.ToString(), StringComparison.Ordinal);
        Assert.Contains("generation B resident release", firstFailure.ToString(), StringComparison.Ordinal);
        Assert.Contains("generation A late release", repeatedFailure.ToString(), StringComparison.Ordinal);
        Assert.Contains("generation B resident release", repeatedFailure.ToString(), StringComparison.Ordinal);
    }

    private static (TileMapCatalog2D Catalog, TileMapChunkInfo2D Chunk) CreateCatalog()
    {
        SceneSpatialEntry2D spatial = new("near", new(0, 0, 10, 10), new DrawRect(0, 0, 10, 10));
        TileMapChunkInfo2D chunk = new(spatial, new TileMapBounds2D(0, 0, 1, 1), [1],
            [new(new ResourceId<ImageResource>("NeverDecodedAtlas"))], 1, dataResidencyBytes: 512);
        return (new TileMapCatalog2D("terrain", [chunk], new DrawSize(10, 10)), chunk);
    }

    private static TileMapChunkData2D CreateData(TileMapChunkInfo2D info)
    {
        TileColliderDescriptor2D shape = new(TileColliderShape2D.Box, width: 10, height: 10);
        return new(new TileChunk2D(new(info.Cells!.Value.X, 0), 1, 1,
                [new TileCell2D(1)], version: info.Spatial.Version),
            [new TileSet2D("palette", new("NeverDecodedAtlas"),
                [new TileDefinition2D(1, new(0, 0, 10, 10), collider: shape)])]);
    }

    private static void PumpUntil(SceneSimulationContext2D context, Func<bool> done) =>
        Assert.True(SpinWait.SpinUntil(() =>
        {
            context.Update();
            return done();
        }, TimeSpan.FromSeconds(5)), "Map operation did not complete on its owner thread.");
}
