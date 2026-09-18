using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMapPresentationStreamingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevisedPaletteIsValidatedAgainstAnAlreadyResidentAtlasBeforeDrawing(bool delayed)
    {
        using Fixture fixture = new(empty: true, declareAtlasSizes: false);
        fixture.Scene.Children.Add(new Sprite2D
        {
            Image = new(new ResourceId<ImageResource>("near")), X = 50, Y = 30, Width = 100, Height = 10
        });
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TileMapSource2D source = new(fixture.Catalog, (_, info, token) =>
        {
            fixture.Loads++;
            return info.Spatial.Version != 2 ? ValueTask.FromResult(fixture.Acquire(info))
                : LoadPayloadAfterAsync(delayed ? completion.Task : Task.CompletedTask, fixture, info, token);
        });
        fixture.Map.Source = source;
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        TestImage atlas = fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        DrawSpriteBatch first = fixture.Record().Single(command => command.Kind == DrawCommandKind.DrawSpriteBatch).SpriteBatch!;
        TileMapChunkInfo2D original = fixture.Catalog.Chunks[0];
        TileMapCatalog2D Revision(long version) => new("terrain",
            [new TileMapChunkInfo2D(new(original.Spatial.Id, original.Spatial.Bounds, original.Spatial.CollisionBounds, version: version),
                original.Cells!.Value, original.TileIds, original.Images, original.ExpandedColliderCount)], new(10, 10));

        source.SetCatalog(Revision(2));
        if (delayed)
        {
            fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
            completion.SetResult(true);
        }
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Error);
        Assert.IsType<ArgumentException>(fixture.Surface.PresentationError);
        Assert.Null(fixture.Map.PreparationError); // Atlas geometry is not collision-data readiness.
        Assert.Single(fixture.Cast());
        Assert.Empty(fixture.Record().Where(command => command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch));
        Assert.Single(fixture.Loader.Requests);
        Assert.False(atlas.Disposed);

        source.SetCatalog(Revision(3));
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        DrawSpriteBatch recovered = fixture.Record().Single(command => command.Kind == DrawCommandKind.DrawSpriteBatch).SpriteBatch!;
        Assert.NotSame(first, recovered);
        Assert.Same(atlas, recovered.Image);
        Assert.Single(fixture.Loader.Requests);
        Assert.Equal(3, fixture.Loads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidWarmPaletteNeverBuildsAndBecomesAnExplicitErrorWhenRequired(bool delayed)
    {
        using Fixture fixture = new(empty: true, includeWarm: true, declareAtlasSizes: false);
        fixture.Scene.Children.Add(new Sprite2D
        {
            Image = new(new ResourceId<ImageResource>("near")), X = 50, Y = 30, Width = 100, Height = 10
        });
        TileMapChunkInfo2D near = fixture.Catalog.Chunks[0], oldWarm = fixture.Catalog.Chunks[1];
        TileMapChunkInfo2D warm = new(oldWarm.Spatial, oldWarm.Cells!.Value, oldWarm.TileIds, near.Images,
            oldWarm.ExpandedColliderCount, dataResidencyBytes: 4096);
        TileMapCatalog2D catalog = new("terrain", [near, warm], new(10, 10));
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int warmLoads = 0;
        fixture.Map.Source = new(catalog, (_, info, token) =>
        {
            if (info.Spatial.Id == "near") { return ValueTask.FromResult(fixture.Acquire(info)); }
            Interlocked.Increment(ref warmLoads);
            return LoadPayloadAfterAsync(delayed ? completion.Task : Task.CompletedTask, fixture, info, token);
        });
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => Volatile.Read(ref warmLoads) == 1);
        completion.TrySetResult(true);
        fixture.PumpUntil(() => fixture.Map.GetDiagnosticsSnapshot().PendingDataChunks == 0 &&
            fixture.Map.GetDiagnosticsSnapshot().WarmPendingChunks == 0);
        for (int frame = 0; frame < 32; frame++)
        {
            fixture.Root.ProcessFrame();
            fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChunks);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmTilesPrepared);
        }
        Assert.Equal(1, warmLoads);
        Assert.Single(fixture.Map.LogicalChildren);
        fixture.Surface.ViewBox = new(110, 0, 100, 100);
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Error);
        Assert.IsType<ArgumentException>(fixture.Surface.PresentationError);
        Assert.Empty(fixture.Record().Where(command => command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch));
        Assert.Single(fixture.Loader.Requests);
    }

    private static async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> LoadPayloadAfterAsync(
        Task completion, Fixture fixture, TileMapChunkInfo2D info, CancellationToken token)
    {
        await completion.WaitAsync(token).ConfigureAwait(false);
        return new(CreatePayload(info, "near", sourceX: 20), _ => Interlocked.Increment(ref fixture.Releases));
    }

    private static TileMapChunkData2D CreatePayload(TileMapChunkInfo2D info, string atlas, float sourceX) =>
        new(new TileChunk2D(new(info.Cells!.Value.X, 0), 1, 1, [new TileCell2D(info.TileIds.Single())], version: info.Spatial.Version),
            [new TileSet2D(info.Spatial.Id, new(atlas),
                [new TileDefinition2D(info.TileIds.Single(), new(sourceX, 0, 10, 10),
                    collider: new(TileColliderShape2D.Box, width: 10, height: 10))])]);

    [Fact]
    public void ColdDataAndAtlasWithholdTheWholeSceneWithoutBlockingCollisionPreparation()
    {
        TaskCompletionSource<bool> data = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new(delayNear: data);
        fixture.Scene.Children.Add(new Sprite2D { Image = new(new TestImage()), X = 30, Width = 10, Height = 10 });
        Assert.Empty(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.Single(fixture.Loader.Requests);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());

        data.SetResult(true);
        fixture.PumpUntil(() => fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks == 1);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.Single(fixture.Cast());
        Assert.Empty(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Assert.Single(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public async Task OffCameraCollisionInterestRetainsDataButReleasesGraphicalResources()
    {
        using Fixture fixture = new();
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        TestImage atlas = fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld.PrepareRegionAsync(new(0, 0, 10, 10)).AsTask();
        fixture.PumpUntil(() => preparing.IsCompleted);
        using SceneCollisionRegion2D region = await preparing;
        fixture.Surface.ViewBox = new(5000, 0, 100, 100);
        fixture.PumpUntil(() => atlas.Disposed);
        Assert.True(region.IsReady);
        Assert.Single(fixture.Cast());
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
        Assert.Equal(0, fixture.Releases);
        region.Dispose();
        Assert.Equal(1, fixture.Releases);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void LeavingTheCameraCancelsPendingDataAndReleasesLateResultsAcross32Cycles()
    {
        using Fixture fixture = new(empty: true);
        for (int iteration = 0; iteration < 32; iteration++)
        {
            TaskCompletionSource<bool> delayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int previousImages = fixture.Loader.Requests.Count;
            fixture.Map.Source = new(fixture.Catalog, (_, info, _) => LoadDelayedChunkAsync(delayed.Task, fixture, info));
            fixture.Surface.ViewBox = new(0, 0, 100, 100);
            fixture.Root.ProcessFrame();
            fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
            fixture.Surface.ViewBox = new(5000, 0, 100, 100);
            fixture.Root.ProcessFrame();
            fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            delayed.SetResult(true);
            fixture.PumpUntil(() => Volatile.Read(ref fixture.Releases) == iteration + 1);
            Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Empty(fixture.Map.LogicalChildren);
            foreach (PendingImage request in fixture.Loader.Requests.Skip(previousImages))
            {
                TestImage late = request.Complete();
                fixture.PumpUntil(() => late.Disposed);
            }
            Assert.Equal(iteration + 1, fixture.Loader.Requests.Count);
        }
    }

    [Fact]
    public void FailedRequiredDataPublishesErrorWithoutRetryStormAndExplicitRefreshRecovers()
    {
        using Fixture fixture = new(empty: true);
        IOException failure = new("chunk unavailable");
        bool failed = true;
        int loads = 0;
        fixture.Map.Source = new(fixture.Catalog, (_, info, _) =>
        {
            loads++;
            return failed ? ValueTask.FromException<SceneSpatialLease2D<TileMapChunkData2D>>(failure)
                : ValueTask.FromResult(fixture.Acquire(info));
        });
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Error);
        Assert.Same(failure, fixture.Surface.PresentationError);
        for (int frame = 0; frame < 32; frame++) { fixture.Root.ProcessFrame(); fixture.Record(); }
        Assert.Equal(1, loads);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
        failed = false;
        fixture.Map.Refresh();
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Assert.Equal(2, loads);
        Assert.Single(fixture.Cast());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1048577L)]
    [InlineData(long.MaxValue)]
    public void UnknownOrOverBudgetOptionalDataDoesNotStartLoading(long? charge)
    {
        using Fixture fixture = new(warmCharge: charge, includeWarm: true);
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        for (int frame = 0; frame < 32; frame++) { fixture.Root.ProcessFrame(); fixture.Record(); }
        Assert.Equal(1, fixture.Loads);
        Assert.Single(fixture.Loader.Requests);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmDataBytes);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
    }

    [Fact]
    public void BudgetedWarmDataAndAtlasArePreparedWithoutDrawingAndRetiredOutsideTheWarmRegion()
    {
        using Fixture fixture = new(includeWarm: true, warmCharge: 4096);
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 2);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
        Assert.Equal(2, fixture.Loads);
        Assert.Equal(4096, fixture.Map.GetDiagnosticsSnapshot().WarmDataBytes);
        Assert.InRange(fixture.Map.GetDiagnosticsSnapshot().WarmChargedBytes, 4096, 1048576);
        TestImage optional = fixture.Loader.Requests[1].Complete();
        fixture.PumpUntil(() => fixture.Map.GetDiagnosticsSnapshot().WarmChunks == 1);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
        fixture.Surface.ViewBox = new(5000, 0, 100, 100);
        fixture.PumpUntil(() => optional.Disposed && fixture.Releases == 2);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().ResidentDataChunks);
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmChargedBytes);
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void RequiredPreparationDoesNotGetDroppedBecauseItsDeclaredDataChargeExceedsTheWarmBudget()
    {
        using Fixture fixture = new(requiredCharge: long.MaxValue);
        fixture.PumpUntil(() => fixture.Loader.Requests.Count == 1);
        fixture.Loader.Requests[0].Complete();
        fixture.PumpUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
        Assert.Single(fixture.Cast());
        Assert.Equal(0, fixture.Map.GetDiagnosticsSnapshot().WarmDataBytes);
    }

    private static async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> LoadDelayedChunkAsync(
        Task<bool> completion, Fixture fixture, TileMapChunkInfo2D info)
    {
        await completion.ConfigureAwait(false);
        return fixture.Acquire(info);
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly UIRoot Root = new(100, 100);
        internal readonly Scene2D Scene = new();
        internal readonly RenderSurface2D Surface = new() { ViewBox = new(0, 0, 100, 100) };
        internal readonly TileMap2D Map = new();
        internal readonly Loader Loader = new();
        internal readonly TileMapCatalog2D Catalog;
        internal int Loads, Releases;
        private int frame;
        private readonly TaskCompletionSource<bool>? delayedData;
        private readonly TileSet2D[] palettes;

        internal Fixture(TaskCompletionSource<bool>? delayNear = null,
            bool empty = false, bool includeWarm = false, long? warmCharge = 4096, long? requiredCharge = 512,
            bool declareAtlasSizes = true)
        {
            delayedData = delayNear;
            TileColliderDescriptor2D box = new(TileColliderShape2D.Box, width: 10, height: 10);
            TileSet2D near = new("near", new("near"), [new TileDefinition2D(1, new(0, 0, 10, 10), collider: box)]);
            TileSet2D warm = new("warm", new("warm"), [new TileDefinition2D(2, new(0, 0, 10, 10), collider: box)]);
            palettes = [near, warm];
            TileMapChunkInfo2D first = new(new("near", new(0, 0, 10, 10), new DrawRect(0, 0, 10, 10)),
                new TileMapBounds2D(0, 0, 1, 1), [1], [new(near.AtlasResourceId)], 1, dataResidencyBytes: requiredCharge);
            TileMapChunkInfo2D second = new(new("warm", new(110, 0, 10, 10), new DrawRect(110, 0, 10, 10)),
                new TileMapBounds2D(11, 0, 1, 1), [2], [new(warm.AtlasResourceId)], 1, dataResidencyBytes: warmCharge);
            Catalog = new("terrain", includeWarm ? [first, second] : [first], new(10, 10),
                imageSizes: declareAtlasSizes ? new Dictionary<string, DrawSize> { ["near"] = new(10, 10), ["warm"] = new(10, 10) } : null);
            if (!empty)
            {
                Map.Source = new(Catalog, async (_, info, _) =>
                {
                    Loads++;
                    if (delayNear is not null) { await delayNear.Task.ConfigureAwait(false); }
                    return Acquire(info);
                });
            }
            Root.SetImageLoader(Loader);
            Surface.Resources.SetResource(new ResourceId<ImageResource>("near"), new ImageResource("near.png"));
            Surface.Resources.SetResource(new ResourceId<ImageResource>("warm"), new ImageResource("warm.png"));
            Scene.Children.Add(Map);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Root.ProcessFrame();
        }

        internal SceneSpatialLease2D<TileMapChunkData2D> Acquire(TileMapChunkInfo2D info) =>
            new(new TileMapChunkData2D(new TileChunk2D(new(info.Cells!.Value.X, 0), 1, 1,
                [new TileCell2D(info.TileIds.Single())], version: info.Spatial.Version),
                [palettes.Single(set => set.Id == info.Spatial.Id)]),
                _ => Interlocked.Increment(ref Releases));
        internal CollisionHit2D[] Cast() => Scene.CollisionWorld.Raycast(new(-10, 5), Vector2.UnitX, 30);
        internal DrawCommandList Record()
        {
            // Camera interest belongs to the frame update, not command recording.
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(frame++ * 16));
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, new(0, 0, 100, 100));
            return commands;
        }
        internal void PumpUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            Record();
            return done();
        }, TimeSpan.FromSeconds(5)), $"Required presentation or retirement did not finish: state={Surface.PresentationState}, loads={Loads}, releases={Releases}, images={Loader.Requests.Count}, data={Map.GetDiagnosticsSnapshot().ResidentDataChunks}, pending={Map.GetDiagnosticsSnapshot().PendingDataChunks}, warm={Map.GetDiagnosticsSnapshot().WarmChunks}.");
        public void Dispose()
        {
            Root.VisualChildren.Remove(Surface);
            delayedData?.TrySetResult(true);
            foreach (PendingImage image in Loader.Requests)
            {
                if (!image.Completion.Task.IsCompleted) { image.Complete(); }
            }
            Task retired = Root.ImageResourceCache!.DisposeAsync().AsTask();
            Assert.True(SpinWait.SpinUntil(() => retired.IsCompleted, TimeSpan.FromSeconds(5)));
            Assert.True(retired.IsCompletedSuccessfully, retired.Exception?.ToString());
        }
    }

    private sealed class Loader : IAsyncImageLoader
    {
        internal int SynchronousLoads;
        internal readonly List<PendingImage> Requests = [];
        public IDrawImage Load(string path) { SynchronousLoads++; throw new InvalidOperationException("Synchronous tile image loading is forbidden."); }
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            PendingImage pending = new();
            Requests.Add(pending);
            return new(pending.Completion.Task);
        }
    }

    private sealed class PendingImage
    {
        internal readonly TaskCompletionSource<IDrawImage> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TestImage Complete()
        {
            TestImage image = new();
            Completion.SetResult(image);
            return image;
        }
    }

    private sealed class TestImage : IDrawImage, IDisposable
    {
        private int disposed;
        internal bool Disposed => Volatile.Read(ref disposed) != 0;
        public int Width => 10;
        public int Height => 10;
        public void Dispose() => Interlocked.Exchange(ref disposed, 1);
    }
}
