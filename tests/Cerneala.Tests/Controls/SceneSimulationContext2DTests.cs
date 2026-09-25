using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Relay;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneSimulationContext2DTests
{
    [Fact]
    public void ExplicitPreparationSharesAndRetiresCollisionsWithoutAttachingAnyUi()
    {
        using Fixture fixture = new();
        fixture.Context.Update();
        Assert.Equal(0, fixture.Loads);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
        using SceneCollisionRegion2D first = fixture.Prepare();
        using SceneCollisionRegion2D second = fixture.Prepare();
        Assert.True(first.IsReady);
        Assert.True(second.IsReady);
        Assert.Equal(1, fixture.Loads);
        Collider2D wall = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
        Assert.Null(wall.Root);
        Assert.Null(wall.Surface);
        Assert.False(wall.IsAttached);
        Assert.False(wall.IsInitialized);
        Assert.Null(wall.ElementId);
        Assert.Same(fixture.Context, wall.SimulationContext);
        Assert.InRange(fixture.Cast().Travel.X, 9.9999f, 10);
        Assert.Equal(2000, fixture.Actor.X);
        first.Dispose();
        Assert.Equal(0, fixture.Releases);
        second.Dispose();
        Assert.Equal(1, fixture.Releases);
        Assert.Null(wall.SimulationContext);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.False(first.IsReady);
        Assert.False(second.IsReady);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
    }

    [Fact]
    public async Task WorkerCompletionPublishesMapCollidersOnlyOnTheSimulationOwner()
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new((_, _) => new(completion.Task));
        int ownerThread = Environment.CurrentManagedThreadId;
        int publicationThread = -1, loadedEvents = 0;
        fixture.Map.LogicalChildren.Changed += (_, _) => publicationThread = Environment.CurrentManagedThreadId;
        fixture.Map.Loaded += (_, _) => loadedEvents++;
        Task<SceneCollisionRegion2D> preparing = fixture.BeginPrepare();
        Assert.False(preparing.IsCompleted);
        Worker(() => completion.SetResult(Fixture.NewLease()));
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
        fixture.PumpUntil(() => preparing.IsCompleted);
        using SceneCollisionRegion2D region = await preparing;
        Assert.True(region.IsReady);
        Collider2D wall = Assert.IsAssignableFrom<Collider2D>(Assert.Single(fixture.Map.LogicalChildren));
        Assert.Equal(ownerThread, publicationThread);
        Assert.Equal(0, loadedEvents);
        Assert.Same(fixture.Context, wall.SimulationContext);
        Assert.Null(wall.Root);
        Assert.NotNull(fixture.Cast().Collision);
    }

    [Fact]
    public void WorkerCatalogPublicationNeverPermitsStaleQueriesBeforeThePump()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = fixture.Prepare();
        Worker(() => fixture.Source.SetCatalog(Fixture.Catalog()));
        Assert.False(region.IsReady);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Cast());
        Assert.Equal(1, fixture.Loads);
        fixture.Context.Update();
        Assert.True(region.IsReady);
        Assert.Null(fixture.Cast().Collision);
        Assert.Equal(1, fixture.Releases);
    }

    [Fact]
    public void OffThreadMutationsQueriesAndLifecycleAreRejectedBeforeChangingState()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D region = fixture.Prepare();
        int children = fixture.Scene.Children.Count;
        int templates = fixture.Items.Templates.Count;
        Action[] operations =
        [
            () => fixture.Actor.X = 5000,
            () => fixture.Actor.Collider!.Enabled = false,
            () => fixture.Scene.Children.Clear(),
            () => fixture.Scene.Children.Remove(fixture.Items),
            () => fixture.Scene.Children.Add(new Sprite2D()),
            () => fixture.Scene.LogicalChildren.Remove(fixture.Actor),
            () => fixture.Items.Templates.Clear(),
            () => fixture.Items.Refresh(),
            () => fixture.Items.ItemsSource = new[] { "replacement" },
            () => fixture.Cast(),
            () => fixture.Scene.CollisionWorld.GetDiagnosticsSnapshot(),
            () => fixture.Context.Update(),
            () => fixture.Context.Dispose()
        ];
        foreach (Action operation in operations)
        {
            Exception? failure = null;
            Worker(() => failure = Record.Exception(operation));
            Assert.IsType<InvalidOperationException>(failure);
        }
        Assert.False(fixture.Context.IsDisposed);
        Assert.Equal(children, fixture.Scene.Children.Count);
        Assert.Equal(templates, fixture.Items.Templates.Count);
        Assert.Equal(2000, fixture.Actor.X);
        Assert.True(fixture.Actor.Collider!.Enabled);
        Assert.True(region.IsReady);
        Assert.NotNull(fixture.Cast().Collision);
    }

    [Fact]
    public void ASecondOwnerIsRejectedBeforeEitherTreeOrSurfacePropertyChanges()
    {
        using Fixture fixture = new();
        Assert.Throws<InvalidOperationException>(() => new SceneSimulationContext2D(fixture.Scene));
        Scene2D parent = new();
        Assert.Throws<InvalidOperationException>(() => parent.Children.Add(fixture.Scene));
        Assert.Empty(parent.Children);
        Assert.Empty(parent.LogicalChildren);
        RenderSurface2D surface = new();
        Assert.Throws<InvalidOperationException>(() => surface.Scene = fixture.Scene);
        Assert.Null(surface.Scene);
        Assert.Empty(surface.LogicalChildren);
        UIRoot root = new(100, 100);
        Assert.Throws<InvalidOperationException>(() => root.VisualChildren.Add(fixture.Scene));
        Assert.Empty(root.VisualChildren);
        Assert.Same(fixture.Context, fixture.Scene.SimulationContext);
        Assert.Null(fixture.Scene.Root);
    }

    [Fact]
    public async Task ExplicitDisposalAllowsUiThenHeadlessTransferWithoutRevivingOldLeases()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D old = fixture.Prepare();
        fixture.Context.Dispose();
        Assert.False(old.IsReady);
        Assert.Null(fixture.Actor.SimulationContext);
        Assert.Equal(1, fixture.Releases);
        RenderSurface2D surface = new() { Scene = fixture.Scene, ViewBox = new(0, 0, 100, 100) };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(surface);
        try
        {
            root.ProcessFrame();
            Assert.Same(root.Relay, fixture.Scene.SimulationContext!.Relay);
            Assert.NotSame(fixture.Context, fixture.Scene.SimulationContext);
            Assert.Throws<InvalidOperationException>(() => fixture.Scene.SimulationContext.Update());
            Assert.Throws<InvalidOperationException>(() => fixture.Scene.SimulationContext.Dispose());
            Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld.PrepareRegionAsync(Fixture.Region).AsTask();
            Assert.True(SpinWait.SpinUntil(() => { root.ProcessFrame(); return preparing.IsCompleted; }, TimeSpan.FromSeconds(5)));
            using SceneCollisionRegion2D attached = await preparing;
            Assert.True(attached.IsReady);
            Assert.False(old.IsReady);
            Assert.NotNull(fixture.Cast().Collision);
        }
        finally { root.VisualChildren.Remove(surface); surface.Scene = null; }
        using SceneSimulationContext2D next = new(fixture.Scene);
        Task<SceneCollisionRegion2D> preparingFresh = fixture.Scene.CollisionWorld.PrepareRegionAsync(Fixture.Region).AsTask();
        Assert.True(SpinWait.SpinUntil(() => { next.Update(); return preparingFresh.IsCompleted; }, TimeSpan.FromSeconds(5)));
        using SceneCollisionRegion2D fresh = await preparingFresh;
        Assert.True(fresh.IsReady);
        Assert.False(old.IsReady);
        Assert.Null(fixture.Actor.Root);
        Assert.Same(next, fixture.Actor.SimulationContext);
    }

    [Fact]
    public void ContextDisposalCancelsPreparationAndRetiresLateLoadsWithoutAnotherPump()
    {
        for (int iteration = 0; iteration < 32; iteration++)
        {
            int released = 0;
            TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using Fixture fixture = new((_, _) => new(completion.Task));
            Task<SceneCollisionRegion2D> preparing = fixture.BeginPrepare();
            fixture.Context.Dispose();
            completion.SetResult(Fixture.NewLease(release: _ => Interlocked.Increment(ref released)));
            Assert.True(SpinWait.SpinUntil(() => preparing.IsCompleted && Volatile.Read(ref released) == 1, TimeSpan.FromSeconds(5)));
            Assert.True(preparing.IsCanceled, preparing.Exception?.ToString());
            Assert.Empty(fixture.Map.LogicalChildren);
            Assert.Null(fixture.Items.SimulationContext);
            Assert.Throws<ObjectDisposedException>(() => fixture.Context.Update());
        }
    }

    [Fact]
    public void RegionWorkerDisposalUsesTheSameRelayAndInvalidatesImmediately()
    {
        using Fixture fixture = new();
        SceneCollisionRegion2D region = fixture.Prepare();
        Worker(region.Dispose);
        Assert.False(region.IsReady);
        fixture.Context.Update();
        Assert.Equal(1, fixture.Releases);
        Assert.Empty(fixture.Map.LogicalChildren);
    }

    [Fact]
    public async Task ADisposedContextsQueuedCatalogCallbackCannotPublishIntoItsReplacement()
    {
        using Fixture fixture = new();
        using SceneCollisionRegion2D old = fixture.Prepare();
        Worker(() => fixture.Source.SetCatalog(Fixture.Catalog(Fixture.Info("wall", 2020, version: 2))));
        fixture.Context.Dispose();
        using SceneSimulationContext2D next = new(fixture.Scene);
        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld.PrepareRegionAsync(Fixture.Region).AsTask();
        Assert.True(SpinWait.SpinUntil(() => { next.Update(); return preparing.IsCompleted; }, TimeSpan.FromSeconds(5)));
        using SceneCollisionRegion2D region = await preparing;
        Assert.True(region.IsReady);
        Assert.Equal(2, fixture.Loads);
        fixture.Context.Relay.Drain();
        Assert.Equal(2, fixture.Loads);
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Same(next, fixture.Map.SimulationContext);
        Assert.False(old.IsReady);
    }

    [Fact]
    public async Task PumpKeepsTheExistingBoundedSnapshotAndContinuationOwnerContract()
    {
        using SceneSimulationContext2D context = new(new Scene2D(), new UiRelayOptions { MaxCallbacksPerUpdate = 2 });
        List<int> order = [];
        context.Relay.Post(() => { order.Add(1); context.Relay.Post(() => order.Add(4)); });
        context.Relay.Post(() => order.Add(2));
        context.Relay.Post(() => order.Add(3));
        Assert.Empty(order);
        context.Update();
        Assert.Equal([1, 2], order);
        context.Update();
        Assert.Equal([1, 2, 3, 4], order);
        Task continuation = context.Relay.InvokeAsync(async () =>
        {
            await Task.Yield();
            Assert.True(context.Relay.CheckAccess());
            order.Add(5);
            Assert.Throws<InvalidOperationException>(() => context.Update());
        });
        context.Update();
        Assert.False(continuation.IsCompleted);
        Assert.Equal([1, 2, 3, 4], order);
        Assert.True(SpinWait.SpinUntil(() => { context.Update(); return continuation.IsCompleted; }, TimeSpan.FromSeconds(5)));
        await continuation;
        Assert.Equal([1, 2, 3, 4, 5], order);
    }

    [Fact]
    public void TypedBindingRebindsToTheOwnerAndStopsAtHeadlessDetach()
    {
        Scene2D scene = new();
        Sprite2D actor = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        scene.Children.Add(actor);
        ObservableValue<float> x = new(10);
        using UiPropertyBinding<float> binding = BindingOperations.BindOneWay(actor, Sprite2D.XProperty, x);
        using SceneSimulationContext2D context = new(scene);
        Worker(() => x.Value = 20);
        Assert.Equal(10, actor.X);
        context.Update();
        Assert.Equal(20, actor.X);
        Assert.Single(scene.CollisionWorld.Raycast(new(0, 5), Vector2.UnitX, 40));
        Worker(() => x.Value = 30);
        scene.Children.Remove(actor);
        context.Update();
        Assert.Equal(20, actor.X);
        Worker(() => x.Value = 40);
        Assert.Equal(20, actor.X);
        scene.Children.Add(actor);
        context.Update();
        Assert.Equal(40, actor.X);
        Assert.Same(context, actor.Collider!.SimulationContext);
    }

    [Fact]
    public void GeneratedDataBindingsAndConditionalValuesUseTheOwnerWithoutUiActivation()
    {
        Model model = new();
        Scene2D scene = new() { DataContext = model };
        Sprite2D actor = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        scene.Children.Add(actor);
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(actor, actor, Sprite2D.XProperty,
            GeneratedMarkup.ObserveDataPath(actor, new MarkupDataPathSegment(nameof(Model.X), value => ((Model)value!).X)),
            BindingMode.OneWay, value => (float)value!, "headless X");
        MarkupObservation blocked = GeneratedMarkup.ObserveDataPath(actor,
            new MarkupDataPathSegment(nameof(Model.Blocked), value => ((Model)value!).Blocked));
        using IDisposable condition = GeneratedMarkup.AttachConditions(actor, [blocked],
            [new MarkupConditionRule(0, () => blocked.Value is true,
                [new MarkupConditionalValue(actor.Collider, Collider2D.EnabledProperty, false, UiPropertyValueSource.MarkupConditional)])]);
        using SceneSimulationContext2D context = new(scene);
        Worker(() => { model.X = 25; model.Blocked = true; });
        context.Update();
        Assert.Equal(25, actor.X);
        Assert.False(actor.Collider!.Enabled);
        Assert.Empty(scene.CollisionWorld.Raycast(new(0, 5), Vector2.UnitX, 50));
        Assert.Null(actor.Root);
        scene.Children.Remove(actor);
        Assert.Equal(0, model.Subscribers);
        Worker(() => { model.X = 40; model.Blocked = false; });
        context.Update();
        Assert.Equal(25, actor.X);
        Assert.False(actor.Collider.Enabled);
    }

    [Fact]
    public void AnExplicitForeignBindingRelayIsRejectedBeforeContextAttachment()
    {
        using SceneSimulationContext2D other = new(new Scene2D());
        Scene2D scene = new();
        Sprite2D actor = new();
        scene.Children.Add(actor);
        using UiPropertyBinding<float> binding = BindingOperations.BindOneWay(actor, Sprite2D.XProperty, new ObservableValue<float>(10), other.Relay);
        Assert.Throws<InvalidOperationException>(() => new SceneSimulationContext2D(scene));
        Assert.Null(scene.SimulationContext);
        Assert.Null(actor.SimulationContext);
        Assert.Same(scene, actor.LogicalParent);
    }

    [Fact]
    public void NestedSimulatedCollidersRetainOnlyNearbyTerrainWithoutAnyViewport()
    {
        using Fixture fixture = new();
        fixture.Source.SetCatalog(Fixture.Catalog(Fixture.Info("wall", 2020), Fixture.Info("gap", 5000)));
        Sprite2D npc = new()
        {
            X = 2020, // The simulated collider's actual bounds overlap the private wall chunk.
            Collider = new BoxCollider2D { Width = 10, Height = 10, IsSimulated = true }
        };
        SceneItems2D actors = new() { ItemsSource = new[] { "npc" } };
        actors.Templates.Add(new ContentTemplate<string>("npc", null, 0, _ => npc));
        Scene2D group = new();
        group.Children.Add(actors);
        SceneItems2D outer = new() { ItemsSource = new[] { "group" } };
        outer.Templates.Add(new ContentTemplate<string>("group", null, 0, _ => group));
        fixture.Scene.Children.Add(outer);
        fixture.PumpUntil(() => fixture.Map.LogicalChildren.Count == 1);
        Assert.Same(fixture.Context, actors.SimulationContext);
        Assert.Equal(1, actors.RealizedItemCount);
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Equal(1, fixture.Loads);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => fixture.Scene.CollisionWorld.Raycast(new(5000, 5), Vector2.UnitX, 100));
        fixture.Scene.Children.Remove(outer);
        fixture.PumpUntil(() => fixture.Map.LogicalChildren.Count == 0);
        Assert.Equal(0, actors.RealizedItemCount);
        Assert.Empty(fixture.Map.LogicalChildren);
        Assert.Equal(1, fixture.Releases);
    }

    [Fact]
    public async Task RequiredFailureIsReportedAndExplicitPreparationCanRetry()
    {
        int attempts = 0;
        using Fixture fixture = new((_, _) => ++attempts == 1
            ? ValueTask.FromException<SceneSpatialLease2D<TileMapChunkData2D>>(new IOException("headless terrain"))
            : ValueTask.FromResult(Fixture.NewLease()));
        Task<SceneCollisionRegion2D> first = fixture.BeginPrepare();
        fixture.PumpUntil(() => first.IsCompleted);
        await Assert.ThrowsAsync<IOException>(async () => await first);
        fixture.Context.Update();
        Assert.Equal(1, attempts);
        using SceneCollisionRegion2D retry = fixture.Prepare();
        Assert.True(retry.IsReady);
        Assert.Equal(2, attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AClosedRegionDoesNotKeepItsFormerSceneAlive(bool invalidateThroughContext)
    {
        (SceneCollisionRegion2D region, WeakReference scene) = CreateClosedRegion(invalidateThroughContext);
        for (int iteration = 0; iteration < 3; iteration++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.False(region.IsReady);
        Assert.False(scene.IsAlive, "A closed region must not retain its former context/scene graph.");
        GC.KeepAlive(region);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (SceneCollisionRegion2D, WeakReference) CreateClosedRegion(bool invalidateThroughContext)
    {
        using Fixture fixture = new();
        SceneCollisionRegion2D region = fixture.Prepare();
        WeakReference scene = new(fixture.Scene);
        if (!invalidateThroughContext) { region.Dispose(); }
        fixture.Context.Dispose();
        return (region, scene);
    }

    [Fact]
    public void WorkerPublishedRetirementErrorsRemainObservableOnTheOwner()
    {
        using Fixture fixture = new((_, _) => ValueTask.FromResult(
            Fixture.NewLease(release: _ => throw new IOException("release failed"))));
        using SceneCollisionRegion2D region = fixture.Prepare();
        Worker(() => fixture.Source.SetCatalog(Fixture.Catalog()));
        AggregateException failure = Assert.Throws<AggregateException>(() => fixture.Context.Update());
        Assert.Contains(failure.Flatten().InnerExceptions, error => error is IOException { Message: "release failed" });
        Assert.Empty(fixture.Map.LogicalChildren);
    }

    [Fact]
    public void AFailingNodeBindingCleanupDoesNotKeepItsPayloadOrSiblingOwnership()
    {
        using Fixture fixture = new();
        SceneNode2D wall = fixture.AddCollectionWall();
        using SceneCollisionRegion2D region = fixture.Prepare();
        wall.Bindings.Add(new ThrowingLifetime());
        AggregateException failure = Assert.Throws<AggregateException>(() => fixture.Context.Dispose());
        Assert.Contains(failure.Flatten().InnerExceptions, error => error is IOException { Message: "binding cleanup" });
        Assert.False(region.IsReady);
        Assert.True(fixture.Context.IsDisposed);
        Assert.Null(wall.LogicalParent);
        Assert.Null(wall.SimulationContext);
        Assert.Null(fixture.Actor.SimulationContext);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(1, fixture.Releases);
        Assert.Empty(fixture.Items.LogicalChildren);
    }

    [Fact]
    public async Task PermanentCollidersAndInMemoryMapAdaptersDoNotRequireUiServices()
    {
        Scene2D scene = new();
        Sprite2D actor = new() { X = 2000, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        TileColliderDescriptor2D shape = new(TileColliderShape2D.Box, width: 10, height: 10);
        TileMap2D map = TileMap2D.FromModel(new("terrain", new DrawSize(10, 10),
            [new TileSet2D("palette", new("NotLoadedAtlas"), [new TileDefinition2D(1, new(0, 0, 10, 10), collider: shape)])],
            [new TileChunk2D(new(202, 0), 1, 1, [new TileCell2D(1)])]));
        scene.Children.Add(map);
        scene.Children.Add(actor);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => scene.CollisionWorld.MoveAndCollide(actor.Collider!, new(50, 0)));
        using SceneSimulationContext2D context = new(scene);
        context.Update();
        Assert.Empty(map.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => scene.CollisionWorld.MoveAndCollide(actor.Collider!, new(50, 0)));
        using SceneCollisionRegion2D region = await scene.CollisionWorld.PrepareRegionAsync(Fixture.Region);
        Assert.True(region.IsReady);
        Collider2D collider = Assert.IsAssignableFrom<Collider2D>(Assert.Single(map.LogicalChildren));
        Assert.Same(context, collider.SimulationContext);
        Assert.Null(collider.Root);
        Assert.Same(map, scene.CollisionWorld.MoveAndCollide(actor.Collider!, new(50, 0)).Collision!.Entity);
        region.Dispose();
        Assert.Empty(map.LogicalChildren);
        Assert.Null(map.Root);
        Assert.Null(map.Surface);
    }

    private sealed class ThrowingLifetime : IDisposable
    {
        public void Dispose() => throw new IOException("binding cleanup");
    }

    private static void Worker(Action action)
    {
        Task work = Task.Run(action);
        Assert.True(SpinWait.SpinUntil(() => work.IsCompleted, TimeSpan.FromSeconds(5)), "Worker did not finish.");
        work.GetAwaiter().GetResult();
    }

    private sealed class Fixture : IDisposable
    {
        internal static readonly DrawRect Region = new(2000, 0, 100, 10);
        internal readonly Scene2D Scene = new();
        internal readonly SceneItems2D Items = new();
        internal readonly TileMap2D Map = new();
        internal readonly Sprite2D Actor = new() { X = 2000, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        internal readonly TileMapSource2D Source;
        internal readonly SceneSimulationContext2D Context;
        internal int Loads, Releases;

        internal Fixture(Func<TileMapChunkInfo2D, CancellationToken, ValueTask<SceneSpatialLease2D<TileMapChunkData2D>>>? load = null)
        {
            Source = new(Catalog(Info("wall", 2020)), (_, info, token) =>
            {
                Loads++;
                return load is null ? ValueTask.FromResult(NewLease(info, _ => Releases++)) : load(info, token);
            });
            Map.Source = Source;
            Items.ItemsSource = Array.Empty<string>();
            Items.Templates.Add(new ContentTemplate<string>("wall", null, 0, _ =>
                new Sprite2D { X = 2020, Width = 10, Height = 10 }));
            Scene.Children.Add(Map);
            Scene.Children.Add(Items);
            Scene.Children.Add(Actor);
            Context = new(Scene);
        }

        internal static TileMapChunkInfo2D Info(string id, int x, int version = 1)
        {
            SceneSpatialEntry2D spatial = new(id, new(x, 0, 10, 10), new DrawRect(x, 0, 10, 10), version: version);
            return new(spatial, 1, [new(new ResourceId<ImageResource>("NeverDecodedAtlas"))], 1);
        }

        internal static TileMapCatalog2D Catalog(params TileMapChunkInfo2D[] chunks) => new("terrain", chunks);

        internal static SceneSpatialLease2D<TileMapChunkData2D> NewLease(TileMapChunkInfo2D? info = null,
            Action<TileMapChunkData2D>? release = null)
        {
            info ??= Info("wall", 2020);
            TileColliderDescriptor2D shape = new(TileColliderShape2D.Box, width: 10, height: 10);
            TileMapChunkData2D data = new([new Tile(new(new ResourceId<ImageResource>("NeverDecodedAtlas")),
                shape, info.Spatial.Bounds.X, info.Spatial.Bounds.Y, 10, 10)]);
            return new(data, release);
        }

        internal SceneNode2D AddCollectionWall()
        {
            Items.ItemsSource = new[] { "wall" };
            return Assert.IsAssignableFrom<SceneNode2D>(Assert.Single(Items.LogicalChildren));
        }

        internal MoveCollisionResult2D Cast() => Scene.CollisionWorld.MoveAndCollide(Actor.Collider!, new(50, 0));
        internal Task<SceneCollisionRegion2D> BeginPrepare() => Scene.CollisionWorld.PrepareRegionAsync(Region).AsTask();
        internal SceneCollisionRegion2D Prepare()
        {
            Task<SceneCollisionRegion2D> preparation = BeginPrepare();
            PumpUntil(() => preparation.IsCompleted);
            return preparation.GetAwaiter().GetResult();
        }
        internal void PumpUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Context.Update();
            return done();
        }, TimeSpan.FromSeconds(5)), "Headless preparation did not finish.");
        public void Dispose() => Context.Dispose();
    }

    private sealed class Model : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? changed;
        private float x;
        private bool blocked;
        internal int Subscribers { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { changed += value; Subscribers++; }
            remove { changed -= value; Subscribers--; }
        }
        public float X { get => x; set { x = value; changed?.Invoke(this, new(nameof(X))); } }
        public bool Blocked { get => blocked; set { blocked = value; changed?.Invoke(this, new(nameof(Blocked))); } }
    }
}
