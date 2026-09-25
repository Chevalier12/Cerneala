using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneCollisionStreamingTests
{
    [Theory]
    [InlineData("move")]
    [InlineData("ray")]
    [InlineData("overlap")]
    public void UnpreparedFarRegionThrowsWithoutLoadingMissingColliders(string operation)
    {
        using Fixture fixture = new();
        if (operation == "overlap") { fixture.Actor.X = 2020; }
        Assert.Equal(0, fixture.Loads);
        Assert.Empty(fixture.Map.LogicalChildren);

        Exception? failure = Record.Exception(() =>
        {
            switch (operation)
            {
                case "move": fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)); break;
                case "ray": fixture.Scene.CollisionWorld.Raycast(new(2000, 5), Vector2.UnitX, 100); break;
                case "overlap": fixture.Scene.CollisionWorld.Overlap(fixture.Actor.Collider!); break;
            }
        });

        SceneCollisionRegionNotReadyException missing = Assert.IsType<SceneCollisionRegionNotReadyException>(failure);
        Assert.Equal(EntryId(202), missing.EntryId);
        Assert.Equal(0, fixture.Loads);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Equal(operation == "overlap" ? 2020 : 2000, fixture.Actor.X);
    }

    [Fact]
    public async Task PreparedRegionsSharePayloadsAndLastReleaseRemovesCollisionGeometry()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D first = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(first.IsReady);
        Assert.Equal(1, fixture.Loads);
        Collider2D wall = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
        MoveCollisionResult2D move = fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0));
        Assert.InRange(move.Travel.X, 9.9999f, 10);
        Assert.Same(fixture.Map, move.Collision!.Entity);
        Assert.Same(wall, move.Collision.Collider);
        Assert.Equal(2000, fixture.Actor.X);

        using SceneCollisionRegion2D second = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2010, 0, 40, 10));
        first.Dispose();
        Assert.True(second.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.Equal(0, fixture.Releases);
        second.Dispose();
        Assert.False(first.IsReady);
        Assert.False(second.IsReady);
        Assert.Equal(1, fixture.Releases);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.False(wall.IsAttached);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
    }

    [Fact]
    public async Task CollisionBoundsCanDifferFromVisualBoundsAndNullMeansNoCollisionData()
    {
        using Fixture fixture = new();
        fixture.SetModel(ModelWithCollider(new(TileColliderShape2D.Box, width: 10, height: 10, offsetX: -2980), (500, 1)));
        using (SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 5, 100, 0)))
        {
            Assert.True(region.IsReady);
            Assert.Equal(1, fixture.Loads);
            Assert.Contains(fixture.Scene.CollisionWorld.Raycast(new(2000, 5), Vector2.UnitX, 100),
                hit => ReferenceEquals(hit.Entity, fixture.Map));
        }
        fixture.SetModel(ModelWithCollider(null, (500, 1)));
        using SceneCollisionRegion2D empty = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(empty.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
    }

    [Fact]
    public async Task PreparationIncludesTheNarrowPhaseContactFringe()
    {
        using Fixture fixture = new();
        fixture.Surface.ViewBox = new(10000, 10000, 100, 100);
        fixture.SetModel(ModelWithCollider(new(TileColliderShape2D.Box, width: 1, height: 1, offsetX: 1.000005f), (0, 1)));
        Assert.Equal(0, fixture.Loads);
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(0, 0, 1, 1));
        Assert.True(region.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.Single(fixture.Scene.CollisionWorld.Raycast(new(0, 0.5f), Vector2.UnitX, 1));
    }

    [Fact]
    public async Task UnrelatedFailedMapDoesNotFailTheRequestedPreparation()
    {
        using Fixture fixture = new();
        TileMapSource2D backing = TileMapSource2D.FromModel(Model((600, 1)));
        TileMap2D unrelated = TileMap2D.FromSource(new TileMapSource2D(backing.Catalog,
            (_, _, _) => ValueTask.FromException<SceneSpatialLease2D<TileMapChunkData2D>>(
                new IOException("unrelated source failure"))));
        fixture.Scene.Children.Add(unrelated);
        await Assert.ThrowsAsync<IOException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(6000, 0, 100, 10)));
        Assert.Empty(unrelated.LogicalChildren);

        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(region.IsReady);
        Assert.Same(fixture.Map,
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision!.Entity);
    }

    [Fact]
    public async Task CancelledPreparationReleasesALatePayloadWithoutMovingOrAttachingIt()
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new();
        fixture.LoaderOverride = (_, _, _) => new(completion.Task);
        using CancellationTokenSource cancellation = new();
        Task<SceneCollisionRegion2D> preparing =
            fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10), cancellation.Token).AsTask();
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        cancellation.Cancel();
        fixture.DrainUntil(() => preparing.IsCompleted);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await preparing);
        fixture.Tick();
        TileMapCatalog2D catalog = fixture.Source.Catalog;
        completion.SetResult(await fixture.CreateLeaseAsync(catalog, Assert.Single(catalog.Chunks)));
        fixture.DrainUntil(() => fixture.Releases == 1);
        Assert.Equal(2000, fixture.Actor.X);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Equal(1, fixture.Loads);
    }

    [Fact]
    public async Task DetachInvalidatesPreparedRegionsAndTheyDoNotResurrectOnReattach()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        fixture.Root.VisualChildren.Remove(fixture.Surface);
        Assert.False(region.IsReady);
        Assert.Equal(1, fixture.Releases);
        fixture.Root.VisualChildren.Add(fixture.Surface);
        fixture.Tick();
        Assert.False(region.IsReady);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
    }

    [Fact]
    public async Task ANewPayloadVersionCannotUseOldCollisionCoverageWhileLoading()
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new();
        fixture.LoaderOverride = (catalog, info, token) => info.Spatial.Version == 1
            ? fixture.CreateLeaseAsync(catalog, info, token) : new(completion.Task);
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Collider2D before = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
        fixture.SetModel(Model((202, 2)));
        Assert.False(region.IsReady);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        TileMapCatalog2D catalog = fixture.Source.Catalog;
        completion.SetResult(await fixture.CreateLeaseAsync(catalog, Assert.Single(catalog.Chunks)));
        fixture.DrainUntil(() => region.IsReady);
        Collider2D after = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
        Assert.NotSame(before, after);
        Assert.False(before.IsAttached);
        Assert.Equal(2, fixture.Loads);
    }

    [Fact]
    public void OffCameraSimulatedActorsPinNearbyTerrainWithoutPinningTheGap()
    {
        using Fixture fixture = new();
        fixture.SetModel(Model((202, 1), (502, 1), (1002, 1)));
        fixture.Actor.X = 2015;
        fixture.Actor.Collider!.IsSimulated = true;
        Sprite2D farActor = new()
        {
            X = 10015,
            Collider = new BoxCollider2D { Width = 10, Height = 10, IsSimulated = true }
        };
        fixture.Scene.Children.Add(farActor);
        fixture.Tick();
        fixture.DrainUntil(() => fixture.Map.LogicalChildren.Count == 2);
        Assert.Equal(2, fixture.Loads);
        Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider, new(50, 0)).Collision);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.Raycast(new(5000, 5), Vector2.UnitX, 100));

        fixture.Actor.Collider.IsSimulated = false;
        farActor.Collider!.IsSimulated = false;
        fixture.Tick();
        fixture.DrainUntil(() => fixture.Map.LogicalChildren.Count == 0);
        Assert.Equal(2, fixture.Releases);
        Assert.Equal(2, fixture.Scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
    }

    [Fact]
    public void EmptyQueryFiltersAndExplicitHiddenMapsDoNotRequireUnavailableData()
    {
        using Fixture fixture = new();
        Assert.Empty(fixture.Scene.CollisionWorld.Raycast(new(2000, 5), Vector2.UnitX, 100, new(collisionMask: 0)));
        fixture.Map.IsVisible = false;
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        Assert.Equal(0, fixture.Loads);
    }

    [Fact]
    public async Task RequiredLoadFailurePropagatesAndANewExplicitPreparationCanRetry()
    {
        using Fixture fixture = new();
        int attempts = 0;
        fixture.LoaderOverride = (catalog, info, token) => ++attempts == 1
            ? ValueTask.FromException<SceneSpatialLease2D<TileMapChunkData2D>>(new IOException("required terrain failure"))
            : fixture.CreateLeaseAsync(catalog, info, token);
        IOException failure = await Assert.ThrowsAsync<IOException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10)));
        Assert.Equal("required terrain failure", failure.Message);
        Assert.Equal(1, fixture.Loads);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        fixture.Map.Refresh();
        using SceneCollisionRegion2D retry = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(retry.IsReady);
        Assert.Equal(2, fixture.Loads);
    }

    [Fact]
    public async Task InvalidOrAlreadyCancelledPreparationDoesNotStartLoading()
    {
        using Fixture fixture = new();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(0, 0, -1, 1)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10), new CancellationToken(true)));
        Assert.Equal(0, fixture.Loads);
        Assert.Empty(fixture.Map.LogicalChildren);
        fixture.Dispose();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10)));
    }

    [Fact]
    public async Task RegionCoordinatesUseTheSameTransformedSceneSpaceAsCollisionQueries()
    {
        using Fixture fixture = new();
        fixture.Scene.Children.Remove(fixture.Map);
        Scene2D transformed = new() { ScaleX = 2, TranslateX = 1000 };
        transformed.Children.Add(fixture.Map);
        fixture.Scene.Children.Add(transformed);
        fixture.Actor.X = 5000;
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(5000, 0, 100, 10));
        Assert.True(region.IsReady);
        Assert.Equal(1, fixture.Loads);
        MoveCollisionResult2D move = fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(100, 0));
        Assert.InRange(move.Travel.X, 29.9999f, 30);
        Assert.Equal(5000, fixture.Actor.X);
    }

    [Fact]
    public async Task WorkerDisposalInvalidatesTheLeaseAndRelaysTreeRetirement()
    {
        using Fixture fixture = new();
        SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Task disposing = Task.Run(region.Dispose);
        fixture.DrainUntil(() => disposing.IsCompleted);
        await disposing;
        fixture.Tick();
        Assert.False(region.IsReady);
        Assert.Equal(1, fixture.Releases);
        Assert.Empty(fixture.Map.LogicalChildren);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishedCatalogRetiresRemovedOrRevisedPayloadsBeforeUnrelatedLoadingCompletes(bool reviseSameId)
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> replacement =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new();
        fixture.SetModel(Model((202, 1), (400, 1)));
        using SceneCollisionRegion2D wallRegion = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        using SceneCollisionRegion2D keeperRegion = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(4000, 0, 100, 10));
        Assert.Equal(2, fixture.Map.LogicalChildren.Count);
        Collider2D oldWall = Assert.IsAssignableFrom<Collider2D>(fixture.Map.LogicalChildren[0]);
        fixture.LoaderOverride = (catalog, info, token) => info.Spatial.Id == EntryId(600)
            ? new(replacement.Task) : fixture.CreateLeaseAsync(catalog, info, token);
        fixture.SetModel(reviseSameId
            ? Model((202, 2), (400, 1), (600, 1))
            : Model((400, 1), (600, 1)));
        Task<SceneCollisionRegion2D> farPreparation =
            fixture.Scene.CollisionWorld.PrepareRegionAsync(new(6000, 0, 100, 10)).AsTask();
        fixture.Tick();
        Assert.False(oldWall.IsAttached);
        Assert.True(keeperRegion.IsReady);
        Assert.False(farPreparation.IsCompleted);
        Assert.True(fixture.Releases >= 1);
        if (!reviseSameId)
        {
            Assert.True(wallRegion.IsReady);
            Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        }
        else
        {
            Assert.True(wallRegion.IsReady);
            Assert.Contains(fixture.Map.LogicalChildren, node => node is Collider2D collider && !ReferenceEquals(collider, oldWall));
        }

        TileMapCatalog2D catalog = fixture.Source.Catalog;
        TileMapChunkInfo2D pending = Assert.Single(catalog.Chunks.Where(info => info.Spatial.Id == EntryId(600)));
        replacement.SetResult(await fixture.CreateLeaseAsync(catalog, pending));
        fixture.DrainUntil(() => farPreparation.IsCompleted);
        using SceneCollisionRegion2D farRegion = await farPreparation;
        Assert.True(farRegion.IsReady);
        Assert.Equal(reviseSameId ? 3 : 2, fixture.Map.LogicalChildren.Count);
    }

    [Fact]
    public async Task WorkerPublicationCannotReturnStaleCollisionResultsBeforeTheUiRelayAppliesIt()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Task publication = Task.Run(() => fixture.SetModel(Model()));
        Assert.True(SpinWait.SpinUntil(() => publication.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.True(publication.IsCompletedSuccessfully, publication.Exception?.ToString());
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        Assert.False(region.IsReady);
        Assert.Equal(1, fixture.Loads);
        fixture.Tick();
        Assert.True(region.IsReady);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Equal(1, fixture.Releases);
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        Assert.Equal(2000, fixture.Actor.X);
    }

    private static string EntryId(int cellX) => $"grid:{cellX}:0:1:1";

    private static TileMap2DModel Model(params (int CellX, long Version)[] chunks) =>
        ModelWithCollider(new(TileColliderShape2D.Box, width: 10, height: 10), chunks);

    private static TileMap2DModel ModelWithCollider(
        TileColliderDescriptor2D? collider,
        params (int CellX, long Version)[] chunks) =>
        new("terrain", new DrawSize(10, 10),
            [new TileSet2D("wall", new ResourceId<ImageResource>("CollisionAtlas"),
                [new TileDefinition2D(1, new(0, 0, 10, 10), collider: collider)])],
            chunks.Select(chunk => new TileChunk2D(new(chunk.CellX, 0), 1, 1,
                [new TileCell2D(1)], version: chunk.Version)));

    private sealed class Fixture : IDisposable
    {
        private readonly Dictionary<TileMapCatalog2D, TileMapSource2D> backings = new();

        internal Fixture()
        {
            TileMapSource2D initial = TileMapSource2D.FromModel(Model((202, 1)));
            backings.Add(initial.Catalog, initial);
            Source = new TileMapSource2D(initial.Catalog, (catalog, info, token) =>
            {
                Loads++;
                return LoaderOverride is null
                    ? CreateLeaseAsync(catalog, info, token)
                    : LoaderOverride(catalog, info, token);
            });
            Map = TileMap2D.FromSource(Source);
            Scene.Children.Add(Map);
            Scene.Children.Add(Actor);
            Surface.Scene = Scene;
            Surface.Resources.SetResource(new ResourceId<ImageResource>("CollisionAtlas"),
                new ImageResource("collision-atlas.png"));
            Root.SetImageLoader(new InlineImageLoader());
            Root.VisualChildren.Add(Surface);
            Tick();
        }

        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal TileMap2D Map { get; }
        internal TileMapSource2D Source { get; }
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal Sprite2D Actor { get; } = new() { X = 2000, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        internal Func<TileMapCatalog2D, TileMapChunkInfo2D, CancellationToken,
            ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>>? LoaderOverride { get; set; }
        internal int Loads, Releases;

        internal void SetModel(TileMap2DModel model)
        {
            TileMapSource2D backing = TileMapSource2D.FromModel(model);
            lock (backings) { backings.Add(backing.Catalog, backing); }
            Source.SetCatalog(backing.Catalog);
        }

        internal async ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> CreateLeaseAsync(
            TileMapCatalog2D catalog, TileMapChunkInfo2D info, CancellationToken token = default)
        {
            TileMapSource2D backing;
            lock (backings) { backing = backings[catalog]; }
            SceneSpatialLease2D<TileMapChunkData2D> acquired = await backing.LoadAsync(info.Spatial, token);
            return new(acquired.Value, _ =>
            {
                acquired.Dispose();
                Interlocked.Increment(ref Releases);
            });
        }

        internal void DrainUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            return done();
        }, TimeSpan.FromSeconds(5)),
            $"Collision preparation did not finish within five seconds: " +
            $"mapChildren={Map.LogicalChildren.Count}, loads={Loads}, " +
            $"collisionInterests={Scene.CollisionWorld.GetSpatialCollisionInterest().Count}.");

        internal void Tick()
        {
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
            Root.ProcessFrame();
        }

        public void Dispose() => Root.VisualChildren.Remove(Surface);
    }

    private sealed record InlineImage(int Width, int Height) : IDrawImage;

    private sealed class InlineImageLoader : IImageLoader, IAsyncImageLoader
    {
        public IDrawImage Load(string path) => new InlineImage(10, 10);
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
    }
}
