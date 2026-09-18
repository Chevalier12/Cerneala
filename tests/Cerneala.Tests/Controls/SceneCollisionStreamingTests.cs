using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

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
        Assert.Equal(0, fixture.Items.RealizedItemCount);

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
        Assert.Equal("wall", missing.EntryId);
        Assert.Equal(0, fixture.Loads);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(operation == "overlap" ? 2020 : 2000, fixture.Actor.X);
    }

    [Fact]
    public async Task PreparedRegionsSharePayloadsAndLastReleaseRemovesCollisionGeometry()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D first = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(first.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.True(fixture.Items.TryGetRealizedNode("wall", out SceneNode2D? wall));
        MoveCollisionResult2D move = fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0));
        Assert.InRange(move.Travel.X, 9.9999f, 10);
        Assert.Same(wall, move.Collision!.Entity);
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
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.False(wall!.IsAttached);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
    }

    [Fact]
    public async Task CollisionBoundsCanDifferFromVisualBoundsAndNullMeansNoCollisionData()
    {
        using Fixture fixture = new();
        fixture.Source.SetEntries([new("wall", new(5000, 0, 10, 10), new DrawRect(2020, 0, 10, 10))]);
        using (SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 5, 100, 0)))
        {
            Assert.True(region.IsReady);
            Assert.Equal(1, fixture.Loads);
            Assert.Contains(fixture.Scene.CollisionWorld.Raycast(new(2000, 5), Vector2.UnitX, 100),
                hit => hit.Entity != fixture.Actor);
        }
        fixture.Source.SetEntries([new("wall", new(5000, 0, 10, 10), collisionBounds: null)]);
        using SceneCollisionRegion2D empty = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(empty.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
    }

    [Fact]
    public async Task PreparationIncludesTheNarrowPhaseContactFringe()
    {
        using Fixture fixture = new();
        fixture.Source.SetEntries([new("wall", new(2020, 0, 1, 1), new DrawRect(1.000005f, 0, 1, 1))]);
        fixture.Items.Templates[0] = new ContentTemplate<string>("fringe", null, 0,
            _ => new Sprite2D { X = 1.000005f, Collider = new BoxCollider2D { Width = 1, Height = 1 } });
        Assert.Equal(0, fixture.Loads);
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(0, 0, 1, 1));
        Assert.True(region.IsReady);
        Assert.Equal(1, fixture.Loads);
        Assert.Single(fixture.Scene.CollisionWorld.Raycast(new(0, 0.5f), Vector2.UnitX, 1));
    }

    [Fact]
    public async Task UnrelatedFailedSourceDoesNotFailTheRequestedPreparation()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new((_, _) => new(completion.Task));
        SceneItems2D unrelated = new()
        {
            ItemsSource = new SceneSpatialSource2D<object>([new("other", new(0, 0, 10, 10), new DrawRect(6000, 0, 10, 10))],
                (_, _) => ValueTask.FromException<SceneSpatialLease2D<object>>(new IOException("unrelated source failure")))
        };
        fixture.Scene.Children.Add(unrelated);
        Assert.True(unrelated.Preparation.IsFaulted);
        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10)).AsTask();
        completion.SetResult(new("wall"));
        fixture.DrainUntil(() => preparing.IsCompleted);
        using SceneCollisionRegion2D region = await preparing;
        Assert.True(region.IsReady);
        Assert.IsType<IOException>(unrelated.PreparationError);
    }

    [Fact]
    public async Task CancelledPreparationReleasesALatePayloadWithoutMovingOrAttachingIt()
    {
        int released = 0;
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new((_, _) => new(completion.Task));
        using CancellationTokenSource cancellation = new();
        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10), cancellation.Token).AsTask();
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        cancellation.Cancel();
        fixture.DrainUntil(() => preparing.IsCompleted);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await preparing);
        fixture.Tick();
        completion.SetResult(new("wall", _ => Interlocked.Increment(ref released)));
        fixture.DrainUntil(() => Volatile.Read(ref released) == 1);
        Assert.Equal(2000, fixture.Actor.X);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
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
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
    }

    [Fact]
    public async Task ANewPayloadVersionCannotUseOldCollisionCoverageWhileLoading()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new((entry, _) => entry.Version == 1
            ? ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)) : new(completion.Task));
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(fixture.Items.TryGetRealizedNode("wall", out SceneNode2D? before));
        fixture.Source.SetEntries([new("wall", new(2020, 0, 10, 10), version: 2)]);
        Assert.False(region.IsReady);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        completion.SetResult(new("wall"));
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.True(fixture.Items.Preparation.IsCompletedSuccessfully);
        Assert.True(region.IsReady);
        Assert.True(fixture.Items.TryGetRealizedNode("wall", out SceneNode2D? after));
        Assert.NotSame(before, after);
        Assert.False(before!.IsAttached);
        Assert.Equal(2, fixture.Loads);
    }

    [Fact]
    public void OffCameraSimulatedPayloadAutomaticallyPinsNearbyTerrainWithoutPinningTheGap()
    {
        using Fixture fixture = new();
        fixture.Source.SetEntries(
        [
            new("wall", new(1900, 0, 200, 100)),
            new("gap", new(5000, 0, 200, 100)),
            new("far", new(9900, 0, 200, 100))
        ]);
        fixture.Items.Templates[0] = new ContentTemplate<string>("terrain", null, 0,
            context => new Sprite2D
            {
                X = context.Data == "wall" ? 2020 : context.Data == "far" ? 10020 : 5020,
                Collider = new BoxCollider2D { Width = 10, Height = 10 }
            });
        fixture.Scene.Children.Remove(fixture.Actor);
        SceneSpatialSource2D<object> npcs = new(
            [new("near", new(2000, 0, 10, 10), isSimulated: true), new("far", new(10000, 0, 10, 10), isSimulated: true)],
            (entry, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)));
        SceneItems2D actors = new() { ItemsSource = npcs };
        actors.Templates.Add(new ContentTemplate<string>("actor", null, 0,
            context => context.Data == "near" ? fixture.Actor : new Sprite2D
            {
                X = 10000, Collider = new BoxCollider2D { Width = 10, Height = 10 }
            }));
        fixture.Scene.Children.Add(actors);
        fixture.Tick();
        Assert.Equal(2, fixture.Items.RealizedItemCount);
        Assert.Equal(2, fixture.Loads);
        Assert.False(fixture.Items.TryGetRealizedNode("gap", out _));
        Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.Raycast(new(5000, 5), Vector2.UnitX, 100));
        npcs.SetEntries([]);
        fixture.Tick();
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(2, fixture.Releases);
        Assert.Equal(0, fixture.Scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
    }

    [Fact]
    public void EmptyQueryFiltersAndExplicitHiddenSourcesDoNotRequireUnavailableData()
    {
        using Fixture fixture = new();
        Assert.Empty(fixture.Scene.CollisionWorld.Raycast(new(2000, 5), Vector2.UnitX, 100, new(collisionMask: 0)));
        fixture.Items.IsVisible = false;
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        Assert.Equal(0, fixture.Loads);
    }

    [Fact]
    public async Task RequiredLoadFailurePropagatesAndANewExplicitPreparationCanRetry()
    {
        int attempts = 0;
        using Fixture fixture = new((entry, _) => ++attempts == 1
            ? ValueTask.FromException<SceneSpatialLease2D<object>>(new IOException("required terrain failure"))
            : ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)));
        IOException failure = await Assert.ThrowsAsync<IOException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10)));
        Assert.Equal("required terrain failure", failure.Message);
        Assert.Equal(1, fixture.Loads);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
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
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        fixture.Dispose();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10)));
    }

    [Fact]
    public async Task RegionCoordinatesUseTheSameTransformedSceneSpaceAsCollisionQueries()
    {
        using Fixture fixture = new();
        fixture.Scene.Children.Remove(fixture.Items);
        Scene2D transformed = new() { ScaleX = 2, TranslateX = 1000 };
        transformed.Children.Add(fixture.Items);
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
        Assert.Equal(0, fixture.Items.RealizedItemCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishedCatalogRetiresRemovedOrRevisedPayloadsBeforeUnrelatedLoadingCompletes(bool reviseSameId)
    {
        Dictionary<string, int> releases = new();
        TaskCompletionSource<SceneSpatialLease2D<object>> replacement = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new((entry, _) => entry.Id == "replacement" || entry.Version == 2
            ? new(replacement.Task)
            : ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id,
                _ => releases[entry.Id] = releases.GetValueOrDefault(entry.Id) + 1)));
        fixture.Items.Templates[0] = new ContentTemplate<string>("positions", null, 0,
            context => new Sprite2D
            {
                X = context.Data == "wall" ? 2020 : context.Data == "keeper" ? 4000 : 0,
                Collider = new BoxCollider2D { Width = 10, Height = 10 }
            });
        fixture.Source.SetEntries([new("wall", new(2020, 0, 10, 10)), new("keeper", new(4000, 0, 10, 10), isSimulated: true)]);
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Assert.True(fixture.Items.TryGetRealizedNode("wall", out SceneNode2D? removed));
        Assert.True(fixture.Items.TryGetRealizedNode("keeper", out SceneNode2D? keeper));

        try
        {
            fixture.Source.SetEntries(
            [
                new("keeper", new(4000, 0, 10, 10), isSimulated: true),
                new(reviseSameId ? "wall" : "replacement", new(0, 0, 10, 10), version: reviseSameId ? 2 : 1)
            ]);
            Assert.False(fixture.Items.Preparation.IsCompleted);
            Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
            Assert.False(removed!.IsAttached);
            Assert.False(fixture.Items.TryGetRealizedNode("wall", out _));
            Assert.True(region.IsReady);
            Assert.Equal(1, releases.GetValueOrDefault("wall"));
            Assert.Equal(0, releases.GetValueOrDefault("keeper"));
            Assert.True(fixture.Items.TryGetRealizedNode("keeper", out SceneNode2D? retained));
            Assert.Same(keeper, retained);
            Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
                fixture.Scene.CollisionWorld.Raycast(new(-10, 5), Vector2.UnitX, 30));
            Assert.Equal(2000, fixture.Actor.X);

            replacement.SetResult(new("replacement"));
            fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
            Assert.True(fixture.Items.Preparation.IsCompletedSuccessfully);
            Assert.Equal(2, fixture.Items.RealizedItemCount);
            Assert.Equal(3, fixture.Loads);
            Assert.Same(keeper, fixture.Items.TryGetRealizedNode("keeper", out retained) ? retained : null);
            Assert.Equal(1, releases.GetValueOrDefault("wall"));
            fixture.Dispose();
            Assert.Equal(1, releases.GetValueOrDefault("keeper"));
        }
        finally { replacement.TrySetCanceled(); }
    }

    [Fact]
    public async Task WorkerPublicationCannotReturnStaleCollisionResultsBeforeTheUiRelayAppliesIt()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = await fixture.Scene.CollisionWorld.PrepareRegionAsync(new(2000, 0, 100, 10));
        Task publication = Task.Run(() => fixture.Source.SetEntries([]));
        Assert.True(SpinWait.SpinUntil(() => publication.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.True(publication.IsCompletedSuccessfully, publication.Exception?.ToString());
        Assert.Throws<SceneCollisionRegionNotReadyException>(() =>
            fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)));
        Assert.False(region.IsReady);
        Assert.Equal(1, fixture.Loads);
        fixture.Tick();
        Assert.True(region.IsReady);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(1, fixture.Releases);
        Assert.Null(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(50, 0)).Collision);
        Assert.Equal(2000, fixture.Actor.X);
    }

    private sealed class Fixture : IDisposable
    {
        internal Fixture(Func<SceneSpatialEntry2D, CancellationToken, ValueTask<SceneSpatialLease2D<object>>>? load = null)
        {
            Source = new SceneSpatialSource2D<object>([new("wall", new(2020, 0, 10, 10))], (entry, token) =>
            {
                Loads++;
                return load is not null ? load(entry, token)
                    : ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id, _ => Releases++));
            });
            Items.Templates.Add(new ContentTemplate<string>("wall", null, 0,
                _ => new Sprite2D { X = 2020, Collider = new BoxCollider2D { Width = 10, Height = 10 } }));
            Items.ItemsSource = Source;
            Scene.Children.Add(Items);
            Scene.Children.Add(Actor);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Tick();
        }

        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal SceneItems2D Items { get; } = new();
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal Sprite2D Actor { get; } = new() { X = 2000, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        internal SceneSpatialSource2D<object> Source { get; }
        internal int Loads, Releases;
        internal void DrainUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            return done();
        }, TimeSpan.FromSeconds(5)), "Collision preparation did not finish within five seconds.");
        internal void Tick()
        {
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
            Root.ProcessFrame();
        }
        public void Dispose() => Root.VisualChildren.Remove(Surface);
    }
}
