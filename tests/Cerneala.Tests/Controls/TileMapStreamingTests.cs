using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMapStreamingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeadlessRegionsLoadAndRetireGridAndFreePayloadsAcross32Cycles(bool free)
    {
        using Fixture fixture = new(free);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast(0));
        Assert.Equal(0, fixture.Loads);
        fixture.Start();
        fixture.Pump();
        Assert.Equal(0, fixture.Loads);
        Assert.Empty(fixture.Map.LogicalChildren);
        for (int iteration = 0; iteration < 32; iteration++)
        {
            int x = iteration % 2 == 0 ? 0 : 1000;
            Task<SceneCollisionRegion2D> task = fixture.Prepare(x);
            fixture.PumpUntil(() => task.IsCompleted);
            using SceneCollisionRegion2D region = await task;
            Assert.True(region.IsReady);
            Assert.Equal(iteration + 1, fixture.Loads);
            Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Same(fixture.Map, fixture.Cast(x).Single().Entity);
            Collider2D collider = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
            Assert.Null(collider.Root);
            Assert.Null(collider.Surface);
            region.Dispose();
            Assert.Equal(iteration + 1, fixture.Releases);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Empty(fixture.Map.LogicalChildren);
            Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast(x));
        }
        for (int collection = 0; collection < 3; collection++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.Equal(32, fixture.Payloads.Count);
        Assert.All(fixture.Payloads, payload => Assert.False(payload.IsAlive, "A retired map must not keep its loaded payload alive."));
        GC.KeepAlive(fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnIndependentRegionDoesNotWaitForAnotherChunksUnfinishedLoad(bool cancelOnWorker)
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> delayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new();
        fixture.Load = (info, _) => info.Spatial.Bounds.X == 0 ? new(delayed.Task) : fixture.Acquire(info);
        fixture.Start();
        using CancellationTokenSource cancellation = new();
        Task<SceneCollisionRegion2D> first = fixture.Prepare(0, cancellation.Token);
        Task<SceneCollisionRegion2D> second = fixture.Prepare(1000);
        fixture.PumpUntil(() => second.IsCompleted);
        using SceneCollisionRegion2D ready = await second;
        Assert.False(first.IsCompleted);
        Assert.True(ready.IsReady);
        Assert.Single(fixture.Cast(1000));
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast(0));
        Task cancellationWork = cancelOnWorker ? Task.Run(cancellation.Cancel) : Task.CompletedTask;
        if (!cancelOnWorker) { cancellation.Cancel(); }
        Assert.True(SpinWait.SpinUntil(() => cancellationWork.IsCompleted && first.IsCompleted, TimeSpan.FromSeconds(5)));
        await cancellationWork;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first);
        if (cancelOnWorker) { Assert.True(fixture.Context!.Relay.HasPendingWork); }
        delayed.SetResult(new(fixture.CreateData(fixture.Catalog.Chunks[0]),
            _ => Interlocked.Increment(ref fixture.Releases)));
        // Cancelled preparation may finish before worker-side region disposal
        // reaches its live owner. Retirement still requires that owner's loop.
        fixture.PumpUntil(() => Volatile.Read(ref fixture.Releases) == 1);
        Assert.True(ready.IsReady);
    }

    [Fact]
    public async Task AnotherRegionsFailureDoesNotPoisonReadyTerrain()
    {
        using Fixture fixture = new();
        fixture.Load = (info, _) => info.Spatial.Bounds.X == 0
            ? ValueTask.FromException<SceneSpatialLease2D<TileMapChunkData2D>>(new IOException("unavailable near chunk"))
            : fixture.Acquire(info);
        fixture.Start();
        Task<SceneCollisionRegion2D> first = fixture.Prepare(0);
        fixture.PumpUntil(() => first.IsCompleted);
        await Assert.ThrowsAsync<IOException>(async () => await first);
        Task<SceneCollisionRegion2D> second = fixture.Prepare(1000);
        fixture.PumpUntil(() => second.IsCompleted);
        using SceneCollisionRegion2D ready = await second;
        Assert.True(ready.IsReady);
        Assert.Single(fixture.Cast(1000));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CatalogRemovalIsAuthoritativeWithoutAFrameOrQueryTimeLoad(bool worker)
    {
        using Fixture fixture = new();
        fixture.Start();
        Task<SceneCollisionRegion2D> task = fixture.Prepare(0);
        fixture.PumpUntil(() => task.IsCompleted);
        using SceneCollisionRegion2D region = await task;
        TileMapCatalog2D empty = new("terrain", [], fixture.Catalog.TileSize);
        if (worker)
        {
            Task publication = Task.Run(() => fixture.Source.SetCatalog(empty));
            Assert.True(SpinWait.SpinUntil(() => publication.IsCompleted, TimeSpan.FromSeconds(5)));
            await publication;
            Assert.False(region.IsReady);
            Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast(0));
            fixture.Pump();
        }
        else { fixture.Source.SetCatalog(empty); }
        Assert.True(region.IsReady);
        Assert.Empty(fixture.Cast(0));
        Assert.Equal(1, fixture.Loads);
        Assert.Equal(1, fixture.Releases);
        Assert.Empty(fixture.Map.LogicalChildren);
    }

    [Fact]
    public async Task ContextDisposalRetiresLatePayloadsWithoutAnotherOwnerPumpAcross32Cycles()
    {
        for (int iteration = 0; iteration < 32; iteration++)
        {
            using Fixture fixture = new();
            TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> delayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.Load = (_, _) => new(delayed.Task);
            fixture.Start();
            Task<SceneCollisionRegion2D> task = fixture.Prepare(0);
            fixture.Dispose();
            delayed.SetResult(new(fixture.CreateData(fixture.Catalog.Chunks[0]),
                _ => Interlocked.Increment(ref fixture.Releases)));
            Assert.True(SpinWait.SpinUntil(() => task.IsCompleted && Volatile.Read(ref fixture.Releases) == 1, TimeSpan.FromSeconds(5)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Empty(fixture.Map.LogicalChildren);
        }
    }

    [Fact]
    public async Task ReentrantSourceReplacementDuringReleaseDoesNotRetireTheNewOwnersPayload()
    {
        using Fixture fixture = new();
        fixture.Start();
        Task<SceneCollisionRegion2D> task = fixture.Prepare(0);
        fixture.PumpUntil(() => task.IsCompleted);
        using SceneCollisionRegion2D region = await task;
        int replacementLoads = 0, replacementReleases = 0;
        TileMapSource2D replacement = new(fixture.Catalog, (_, info, _) =>
        {
            replacementLoads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(
                fixture.CreateData(info), _ => replacementReleases++));
        });
        fixture.OnRelease = () => fixture.Map.Source = replacement;
        fixture.Source.SetCatalog(new("terrain", [], fixture.Catalog.TileSize));
        fixture.PumpUntil(() => region.IsReady);
        Assert.Same(replacement, fixture.Map.Source);
        Assert.Equal(1, replacementLoads);
        Assert.Equal(0, replacementReleases);
        Assert.Single(fixture.Cast(0));
        fixture.OnRelease = null;
        region.Dispose();
        Assert.Equal(1, replacementReleases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleaseFailuresRemainObservableAndDoNotRetainSiblingPayloads(bool throughContext)
    {
        using Fixture fixture = new();
        fixture.Start();
        Task<SceneCollisionRegion2D> task = fixture.Scene.CollisionWorld.PrepareRegionAsync(new(0, 0, 1010, 10)).AsTask();
        fixture.PumpUntil(() => task.IsCompleted);
        using SceneCollisionRegion2D region = await task;
        fixture.OnRelease = () => throw new IOException("release failed");
        Exception? failure = Record.Exception(throughContext ? fixture.Dispose : region.Dispose);
        Assert.NotNull(failure);
        AggregateException aggregate = Assert.IsType<AggregateException>(failure);
        Assert.Contains(aggregate.Flatten().InnerExceptions, error => error is IOException { Message: "release failed" });
        Assert.Equal(2, fixture.Releases);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Scene2D Scene = new();
        internal readonly TileMap2D Map = new();
        internal readonly TileMapCatalog2D Catalog;
        internal readonly TileMapSource2D Source;
        internal SceneSimulationContext2D? Context;
        internal Func<TileMapChunkInfo2D, CancellationToken, ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>>? Load;
        internal Action? OnRelease;
        internal int Loads, Releases;
        internal readonly List<WeakReference> Payloads = [];
        private readonly bool free;
        private readonly TileColliderDescriptor2D shape = new(TileColliderShape2D.Box, width: 10, height: 10);

        internal Fixture(bool free = false)
        {
            this.free = free;
            TileMapChunkInfo2D[] chunks = [Info("near", 0), Info("far", 1000)];
            Catalog = new("terrain", chunks, free ? null : new DrawSize(10, 10));
            Source = new(Catalog, (_, info, token) =>
            {
                Loads++;
                return Load is null ? Acquire(info) : Load(info, token);
            });
            Map.Source = Source;
            Scene.Children.Add(Map);
        }

        private TileMapChunkInfo2D Info(string id, int x)
        {
            SceneSpatialEntry2D spatial = new(id, new(x, 0, 10, 10), new DrawRect(x, 0, 10, 10));
            return free ? new(spatial, 1, [new(new Cerneala.UI.Resources.ResourceId<Cerneala.UI.Resources.ImageResource>("NeverDecodedAtlas"))], 1, dataResidencyBytes: 512)
                : new(spatial, new TileMapBounds2D(x / 10, 0, 1, 1), [1],
                    [new(new Cerneala.UI.Resources.ResourceId<Cerneala.UI.Resources.ImageResource>("NeverDecodedAtlas"))], 1, dataResidencyBytes: 512);
        }

        internal TileMapChunkData2D CreateData(TileMapChunkInfo2D info) => free
            ? new([new Tile(new(new Cerneala.UI.Resources.ResourceId<Cerneala.UI.Resources.ImageResource>("NeverDecodedAtlas")),
                shape, info.Spatial.Bounds.X, 0, 10, 10)])
            : new(new TileChunk2D(new(info.Cells!.Value.X, 0), 1, 1, [new TileCell2D(1)], version: info.Spatial.Version),
                [new TileSet2D("palette", new("NeverDecodedAtlas"),
                    [new TileDefinition2D(1, new(0, 0, 10, 10), collider: shape)])]);

        internal ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> Acquire(TileMapChunkInfo2D info)
        {
            TileMapChunkData2D data = CreateData(info);
            Payloads.Add(new(data));
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(data, _ =>
            {
                Interlocked.Increment(ref Releases);
                OnRelease?.Invoke();
            }));
        }
        internal void Start() => Context = new(Scene);
        internal IReadOnlyList<CollisionHit2D> Cast(int x) => Scene.CollisionWorld.Raycast(new(x - 10, 5), Vector2.UnitX, 30);
        internal Task<SceneCollisionRegion2D> Prepare(int x, CancellationToken token = default) =>
            Scene.CollisionWorld.PrepareRegionAsync(new(x, 0, 10, 10), token).AsTask();
        internal void Pump() => Context!.Update();
        internal void PumpUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Pump();
            return done();
        }, TimeSpan.FromSeconds(5)), "Tile chunk preparation did not complete on its owner.");
        public void Dispose() => Context?.Dispose();
    }
}
