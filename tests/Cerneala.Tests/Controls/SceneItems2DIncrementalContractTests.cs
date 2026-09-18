using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneItems2DIncrementalContractTests
{
    [Fact]
    public void TenThousandStaticEntriesOnlyMaterializeTheViewportAndReleaseOnPan()
    {
        List<string> loaded = [], released = [];
        SceneSpatialSource2D<object> source = new(
            Enumerable.Range(0, 10_000).Select(i => Entry(i.ToString(), i * 200)),
            (entry, _) =>
            {
                loaded.Add(entry.Id);
                return ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id, value => released.Add((string)value)));
            });
        using Fixture fixture = new(source);
        Assert.Equal(["0"], loaded);
        Assert.Equal(["0"], fixture.Nodes.Select(n => n.Id));
        TrackingNode before = fixture.Nodes[0];

        fixture.Pan(200);
        Assert.Equal(["0", "1"], loaded);
        Assert.Equal(["0"], released);
        Assert.False(before.IsAttached);
        Assert.Null(before.Surface);
        Assert.Equal(1, fixture.Items.RealizedItemCount);
        Assert.Equal(["1"], fixture.Nodes.Select(n => n.Id));
    }

    [Fact]
    public void CatalogReorderAndInsertionPreserveAllExistingInstancesAndNoTemplateIndex()
    {
        SceneSpatialSource2D<object> source = Source(Entry("a"), Entry("b"), Entry("c"));
        using Fixture fixture = new(source);
        TrackingNode[] before = fixture.Nodes;
        SceneItems2DUpdateSnapshot counts = fixture.Items.UpdateCounters.Snapshot();
        source.SetEntries([Entry("c"), Entry("x"), Entry("a"), Entry("b")]);
        Assert.Equal(["c", "x", "a", "b"], fixture.Nodes.Select(n => n.Id));
        Assert.Same(before[2], fixture.Nodes[0]);
        Assert.Same(before[0], fixture.Nodes[2]);
        Assert.Same(before[1], fixture.Nodes[3]);
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.Index));
        Assert.All(before, node => { Assert.Equal(1, node.AttachCount); Assert.Equal(0, node.DetachCount); });
        Assert.Equal(1, fixture.Items.UpdateCounters.CreatedNodes - counts.CreatedNodes);
        Assert.Equal(0, fixture.Items.UpdateCounters.RemovedNodes - counts.RemovedNodes);
        Assert.Same(before[0], fixture.Get("a"));
    }

    [Fact]
    public void MetadataPublicationKeepsPayloadVersionAndSimulationIdentity()
    {
        int loads = 0;
        SceneSpatialSource2D<object> source = new([Entry("npc", simulated: true)],
            (entry, _) => { loads++; return ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)); });
        using Fixture fixture = new(source);
        TrackingNode npc = fixture.Get("npc");
        for (int iteration = 0; iteration < 256; iteration++)
        {
            source.SetEntries([Entry("npc", 1000 + iteration, simulated: true)]);
            fixture.Tick();
        }
        Assert.Same(npc, fixture.Get("npc"));
        Assert.Equal(1, loads);
        Assert.Equal(1, npc.AttachCount);
        Assert.Equal(0, npc.DetachCount);
        Assert.True(npc.AdvanceCount >= 256);
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.FillRectangle));
        Assert.True(npc.IsVisible);
    }

    [Fact]
    public void VersionChangeReplacesOnlyTheChangedPayload()
    {
        SceneSpatialSource2D<object> source = Source(Entry("a"), Entry("b"));
        using Fixture fixture = new(source);
        TrackingNode a = fixture.Get("a"), b = fixture.Get("b");
        source.SetEntries([Entry("a", version: 2), Entry("b")]);
        Assert.NotSame(a, fixture.Get("a"));
        Assert.False(a.IsAttached);
        Assert.Same(b, fixture.Get("b"));
        source.SetEntries([Entry("b")]);
        Assert.Same(b, Assert.Single(fixture.Nodes));
        Assert.Equal(0, b.DetachCount);
    }

    [Fact]
    public void TemplateChangeReplacesNodesButNotAcquiredPayloads()
    {
        int loads = 0;
        SceneSpatialSource2D<object> source = new([Entry("a")],
            (entry, _) => { loads++; return ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)); });
        using Fixture fixture = new(source);
        TrackingNode before = fixture.Get("a");
        fixture.Items.Templates[0] = Template();
        Assert.NotSame(before, fixture.Get("a"));
        Assert.False(before.IsAttached);
        Assert.Equal(1, loads);
    }

    [Fact]
    public void DetachUnsubscribesReleasesAndReattachUsesOneSubscription()
    {
        CountingSource source = new();
        using Fixture fixture = new(source);
        Assert.Equal(1, source.Handlers);
        TrackingNode before = fixture.Get("a");
        fixture.Root.VisualChildren.Remove(fixture.Surface);
        Assert.Equal(0, source.Handlers);
        Assert.Equal(1, source.Released);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.False(before.IsAttached);
        fixture.Root.VisualChildren.Add(fixture.Surface);
        fixture.Tick();
        Assert.Equal(1, source.Handlers);
        Assert.NotSame(before, fixture.Get("a"));
        Assert.Equal(2, source.Loaded);
    }

    [Fact]
    public void SourceReplacementCannotReuseEqualIdsFromThePreviousSource()
    {
        CountingSource first = new(), second = new();
        using Fixture fixture = new(first);
        TrackingNode before = fixture.Get("a");
        fixture.Items.ItemsSource = second;
        Assert.Equal(0, first.Handlers);
        Assert.Equal(1, first.Released);
        Assert.Equal(1, second.Handlers);
        Assert.NotSame(before, fixture.Get("a"));
    }

    [Fact]
    public void FailedLoadIsObservableDoesNotRetryEveryFrameAndCanBeRetriedExplicitly()
    {
        int attempts = 0;
        SceneSpatialSource2D<object> source = new([Entry("a")], (entry, _) =>
            ++attempts == 1
                ? ValueTask.FromException<SceneSpatialLease2D<object>>(new IOException("expected load failure"))
                : ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)));
        using Fixture fixture = new(source);
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.IsType<IOException>(fixture.Items.PreparationError);
        Assert.True(fixture.Items.Preparation.IsFaulted);
        for (int i = 0; i < 16; i++) { fixture.Tick(); }
        Assert.Equal(1, attempts);
        fixture.Items.Refresh();
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.Null(fixture.Items.PreparationError);
        Assert.Single(fixture.Nodes);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void WorkerCompletionCreatesTheTemplateOnlyOnTheUiThread()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialSource2D<object> source = new([Entry("a")], (_, _) => new(completion.Task));
        using Fixture fixture = new(source);
        int uiThread = Environment.CurrentManagedThreadId;
        Assert.Empty(fixture.Nodes);
        ThreadPool.QueueUserWorkItem(_ => completion.SetResult(new("a")));
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.True(fixture.Items.Preparation.IsCompletedSuccessfully);
        Assert.Equal(uiThread, fixture.Get("a").CreatedThread);
    }

    [Fact]
    public void LateCompletionAfterReplacementReleasesInsteadOfAttachingAnObsoleteNode()
    {
        int released = 0;
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialSource2D<object> source = new([Entry("old")], (_, _) => new(completion.Task));
        using Fixture fixture = new(source);
        fixture.Items.ItemsSource = Source(Entry("new"));
        completion.SetResult(new("old", _ => Interlocked.Increment(ref released)));
        fixture.DrainUntil(() => Volatile.Read(ref released) == 1);
        Assert.Equal(["new"], fixture.Nodes.Select(node => node.Id));
        Assert.Null(fixture.Items.PreparationError);
    }

    [Fact]
    public void CameraChurnSharesAnUncooperativePendingSimulatedPayload()
    {
        int loaded = 0, released = 0;
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SceneSpatialSource2D<object> source = new([Entry("npc", simulated: true)], (_, _) =>
        {
            loaded++;
            return new(completion.Task);
        });
        using Fixture fixture = new(source);
        for (int i = 0; i < 64; i++) { fixture.Pan(i * 200); }
        Assert.Equal(1, loaded);
        completion.SetResult(new("npc", _ => released++));
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.Single(fixture.Nodes);
        Assert.Equal(0, released);
    }

    [Fact]
    public void WorkerCatalogPublicationIsMarshaledAndPreservesTheExistingNode()
    {
        SceneSpatialSource2D<object> source = Source(Entry("a"));
        using Fixture fixture = new(source);
        TrackingNode before = fixture.Get("a");
        ThreadPool.QueueUserWorkItem(_ => source.SetEntries([Entry("b"), Entry("a")]));
        fixture.DrainUntil(() => fixture.Items.RealizedItemCount == 2);
        Assert.Same(before, fixture.Get("a"));
        Assert.All(fixture.Nodes, node => Assert.Equal(Environment.CurrentManagedThreadId, node.CreatedThread));
    }

    [Theory]
    [InlineData(false, 0f)]
    [InlineData(false, 1000f)]
    [InlineData(true, 0f)]
    [InlineData(true, 1000f)]
    public void OffCameraSpriteKeepsMotionAndItsActualCollisionOwner(bool spatial, float startX)
    {
        ManualMotionClock clock = new();
        SceneSpatialSource2D<object> source = Source(Entry("npc", startX, simulated: true));
        using Fixture fixture = new(source, clock);
        Sprite2D npc = new() { Width = 10, Height = 10, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        // Match an authored/bound starting position. A Local setter intentionally
        // outranks Animation in Cerneala and would mask the Motion sample.
        npc.SetValue(Sprite2D.XProperty, startX, UiPropertyValueSource.MarkupBase);
        if (spatial)
        {
            fixture.Items.Templates[0] = new ContentTemplate<string>("npc", null, 0, _ => npc);
            Assert.Same(npc, fixture.GetNode("npc"));
        }
        else
        {
            fixture.Items.ItemsSource = null;
            fixture.Scene.Children.Add(npc);
        }
        Sprite2D wall = new() { X = startX + 20, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        fixture.Scene.Children.Add(wall);
        var collision = fixture.Scene.CollisionWorld.MoveAndCollide(npc.Collider!, new Vector2(20, 0));
        // Continuous casts include the documented 1e-5 scene-unit contact epsilon.
        Assert.InRange(collision.Travel.X, 9.99998f, 10f);
        Assert.NotNull(collision.Collision);
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        using var motion = npc.Motion().Animate(Sprite2D.XProperty).To(startX + 4)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(motion.IsActive);
        fixture.Root.ProcessFrame();
        Assert.True(motion.IsActive);
        clock.Advance(TimeSpan.FromMilliseconds(50));
        fixture.Root.ProcessFrame();
        Assert.True(npc.X > startX + 0.01f && npc.X < startX + 3.99f,
            $"X={npc.X}; attached={npc.IsAttached}; handleActive={motion.IsActive}; rootBindings={fixture.Root.Motion.Properties.BindingCount}; clock={clock.Now}; frame={fixture.Root.Motion.LastFrameResult}; source={npc.GetValueSource(Sprite2D.XProperty)}");
        if (spatial) { Assert.Same(npc, fixture.GetNode("npc")); }
        Assert.True(npc.IsVisible);
        Assert.True(npc.Collider!.Enabled);
        Assert.True(npc.IsAttached);
        Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(npc.Collider!, new Vector2(20, 0)).Collision);
    }

    [Fact]
    public void TemplateSourceReplacementCannotPublishAnObsoleteCatalog()
    {
        using Fixture fixture = new(Source(Entry("a")));
        fixture.Items.Templates[0] = new ContentTemplate<string>("reentrant", null, 0, context =>
        {
            if (context.Data == "a") { fixture.Items.ItemsSource = Source(Entry("b")); }
            return new TrackingNode(context.Data!, context.Index);
        });
        fixture.Tick();
        Assert.Equal(["b"], fixture.Nodes.Select(node => node.Id));
        Assert.Null(fixture.Items.PreparationError);
    }

    [Fact]
    public void ReentrantAsyncReplacementKeepsTheNewPreparationTask()
    {
        TaskCompletionSource<SceneSpatialLease2D<object>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Fixture fixture = new(Source(Entry("a")));
        fixture.Items.Templates[0] = new ContentTemplate<string>("reentrant-async", null, 0, context =>
        {
            if (context.Data == "a")
            {
                fixture.Items.ItemsSource = new SceneSpatialSource2D<object>([Entry("b")], (_, _) => new(completion.Task));
            }
            return new TrackingNode(context.Data!, context.Index);
        });
        Assert.False(fixture.Items.Preparation.IsCompleted);
        completion.SetResult(new("b"));
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.True(fixture.Items.Preparation.IsCompletedSuccessfully);
        Assert.Equal(["b"], fixture.Nodes.Select(node => node.Id));
    }

    [Fact]
    public void OffscreenSimulationReleasesItsImageAndReloadsOnReturn()
    {
        using Fixture fixture = new(Source(Entry("npc", simulated: true)));
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("npc-image", null, 0,
            _ => new Sprite2D { Image = new(id), Width = 10, Height = 10 });
        SceneNode2D npc = fixture.GetNode("npc");
        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);
        fixture.Pan(1000);
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(1, loader.Disposed);
        Assert.Same(npc, fixture.GetNode("npc"));
        fixture.Pan(0);
        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(2, loader.Loaded);
    }

    [Fact]
    public void HiddenAncestorReleasesPresentationWithoutDetachingSimulatedNodes()
    {
        using Fixture fixture = new(Source(Entry("npc", simulated: true)));
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("npc-image", null, 0,
            _ => new Sprite2D { Image = new(id), Width = 10, Height = 10 });
        SceneNode2D npc = fixture.GetNode("npc");
        fixture.Record();
        Assert.Equal(1, loader.Loaded);
        fixture.Scene.Opacity = 0;
        fixture.Tick();
        fixture.Record();
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
        Assert.True(npc.IsAttached);
        Assert.True(npc.IsVisible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SpatialPointerMissDoesNotReloadAnOffscreenSimulatedImage(bool transformed)
    {
        using Fixture fixture = new(Source(new SceneSpatialEntry2D("npc", new(0, 0, 16, 16), isSimulated: true)));
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("natural-size", null, 0,
            _ => new Sprite2D { Image = new(id) });
        if (transformed) { fixture.Scene.TranslateX = 40; fixture.Scene.Rotation = 0.3f; }
        fixture.Record();
        Assert.Equal(1, loader.Loaded);
        fixture.Pan(1000);
        fixture.Record();
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
        Vector2 point = Vector2.Transform(new(1005, 5), SceneGeometry2D.GetLocalToSceneTransform(fixture.Items));
        for (int iteration = 0; iteration < 16; iteration++)
        {
            Assert.Null(SceneHitTest2D.HitTest(fixture.Scene, point, HitTestFilter.IncludeAll, []));
        }
        Assert.Equal(1, loader.Loaded);
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.True(fixture.GetNode("npc").IsAttached);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColliderOutsideVisualMetadataRemainsPickableWithoutReloadingImage(bool transformed)
    {
        using Fixture fixture = new(Source(Entry("npc", simulated: true)));
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("large-collider", null, 0,
            _ => new Sprite2D
            {
                Image = new(id), Width = 10, Height = 10,
                Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = 20 }
            });
        if (transformed) { fixture.Scene.TranslateX = 40; fixture.Scene.Rotation = 0.3f; }
        fixture.Record();
        fixture.Pan(1000);
        fixture.Record();
        Vector2 point = Vector2.Transform(new(25, 5), SceneGeometry2D.GetLocalToSceneTransform(fixture.Items));
        Assert.Same(fixture.GetNode("npc"), SceneHitTest2D.HitTest(fixture.Scene, point, HitTestFilter.IncludeAll, []));
        Assert.Equal(1, loader.Loaded);
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
    }

    [Fact]
    public void FailedTemplatePreparationReleasesUnadoptedPayloadsWithoutRetiringTheUnchangedNode()
    {
        Dictionary<string, int> releases = new();
        SceneSpatialSource2D<object> source = new([Entry("a")], (entry, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id,
                _ => releases[entry.Id] = releases.GetValueOrDefault(entry.Id) + 1)));
        using Fixture fixture = new(source);
        fixture.Items.Templates[0] = new ContentTemplate<string>("throwing", null, 0, context =>
            context.Data == "c" ? throw new InvalidOperationException("template failure") : new TrackingNode(context.Data!, context.Index));
        SceneNode2D unchanged = fixture.GetNode("a");
        source.SetEntries([Entry("a"), Entry("b"), Entry("c")]);
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.IsType<InvalidOperationException>(fixture.Items.PreparationError);
        Assert.Same(unchanged, fixture.GetNode("a"));
        Assert.Equal(1, fixture.Items.RealizedItemCount);
        Assert.Equal(0, releases.GetValueOrDefault("a"));
        Assert.Equal(1, releases.GetValueOrDefault("b"));
        Assert.Equal(1, releases.GetValueOrDefault("c"));
        fixture.Dispose();
        Assert.Equal(1, releases.GetValueOrDefault("a"));
    }

    [Fact]
    public void ThrowingPayloadReleaseDoesNotLeaveOtherDeletedNodesAttachedOrRetained()
    {
        List<string> released = [];
        SceneSpatialSource2D<object> source = new([Entry("a"), Entry("b")], (entry, _) =>
            ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id, _ =>
            {
                released.Add(entry.Id);
                if (entry.Id == "b") { throw new IOException("release failure"); }
            })));
        using Fixture fixture = new(source);
        TrackingNode[] before = fixture.Nodes;
        Assert.Throws<AggregateException>(() => source.SetEntries([]));
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.All(before, node => Assert.False(node.IsAttached));
        Assert.Equal(["a", "b"], released.Order());
        fixture.Dispose();
        Assert.Equal(2, released.Count);
    }

    private sealed class ImageLoader : IAsyncImageLoader
    {
        internal int Loaded, Disposed;
        public IDrawImage Load(string path) { Loaded++; return new Image(this); }
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
        private sealed class Image(ImageLoader owner) : IDrawImage, IDisposable
        {
            public int Width => 16;
            public int Height => 16;
            public void Dispose() => owner.Disposed++;
        }
    }

    private static SceneSpatialEntry2D Entry(string id, float x = 0, bool simulated = false, long version = 1) =>
        new(id, new(x, 0, 10, 10), simulated, version);

    private static SceneSpatialSource2D<object> Source(params SceneSpatialEntry2D[] entries) =>
        new(entries, (entry, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)));

    private static ContentTemplate<string> Template() => new("tracking", null, 0,
        context => new TrackingNode(context.Data!, context.Index) { DataContext = context.Data });

    private sealed class Fixture : IDisposable
    {
        internal Fixture(ISceneSpatialSource2D<object> source, ManualMotionClock? clock = null)
        {
            Root = clock is null ? new(100, 100) : new(100, 100, motionClock: clock);
            Items.Templates.Add(Template());
            Items.ItemsSource = source;
            Scene.Children.Add(Items);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Tick();
        }
        internal UIRoot Root { get; }
        internal Scene2D Scene { get; } = new();
        internal SceneItems2D Items { get; } = new();
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal TrackingNode[] Nodes => Items.LogicalChildren.Cast<TrackingNode>().ToArray();
        internal SceneNode2D GetNode(string id)
        {
            Assert.True(Items.TryGetRealizedNode(id, out SceneNode2D? node));
            return node!;
        }
        internal TrackingNode Get(string id) => Assert.IsType<TrackingNode>(GetNode(id));
        internal void Pan(float x) { Surface.ViewBox = new(x, 0, 100, 100); Tick(); }
        internal void Tick()
        {
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
            Root.ProcessFrame();
        }
        internal void DrainUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            return done();
        }, TimeSpan.FromSeconds(5)), "Spatial preparation did not finish within five seconds.");
        internal DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, new(0, 0, 100, 100));
            return commands;
        }
        public void Dispose() => Root.VisualChildren.Remove(Surface);
    }

    private sealed class TrackingNode(string id, int index) : SceneNode2D
    {
        internal string Id { get; } = id;
        internal int Index { get; } = index;
        internal int CreatedThread { get; } = Environment.CurrentManagedThreadId;
        internal int AttachCount { get; private set; }
        internal int DetachCount { get; private set; }
        internal int AdvanceCount { get; private set; }
        internal override bool HasActiveAnimation => true;
        internal override bool AdvanceAnimation(TimeSpan elapsed) { AdvanceCount++; return false; }
        protected override void OnAttached() { AttachCount++; base.OnAttached(); }
        protected override void OnDetached() { DetachCount++; base.OnDetached(); }
        internal override void Record(Scene2DRecordContext context) => context.Frame.FillRectangle(new(0, 0, 10, 10), Color.Black);
        internal override SceneBounds2D GetVisibleLocalBounds() =>
            throw new InvalidOperationException("Spatial metadata must not be inferred by measuring a realized node.");
    }

    private sealed class CountingSource : ISceneSpatialSource2D<object>
    {
        private EventHandler? changed;
        internal int Handlers { get; private set; }
        internal int Loaded { get; private set; }
        internal int Released { get; private set; }
        public IReadOnlyList<SceneSpatialEntry2D> Entries { get; } = Array.AsReadOnly(new[] { Entry("a") });
        public event EventHandler? Changed
        {
            add { changed += value; Handlers++; }
            remove { changed -= value; Handlers--; }
        }
        public ValueTask<SceneSpatialLease2D<object>> LoadAsync(SceneSpatialEntry2D entry, CancellationToken cancellationToken = default)
        {
            Loaded++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id, _ => Released++));
        }
    }
}
