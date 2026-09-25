using System.Collections;
using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Xunit.Abstractions;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneItems2DIncrementalContractTests(ITestOutputHelper output)
{
    [Fact]
    public void CollectionMaterializesAllOccurrencesAndPanDoesNotChangeMembership()
    {
        CountingEnumerable<string> source = new(["0", "200", "400"]);
        using Fixture fixture = new(source);
        TrackingNode[] original = fixture.Nodes;

        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(3, original.Length);
        Assert.Equal(3, fixture.Items.RealizedItemCount);
        Assert.Equal(["0", "200", "400"], original.Select(node => node.Id));

        fixture.Pan(200);

        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(original, fixture.Nodes);
        Assert.All(original, node => Assert.True(node.IsAttached));
    }

    [Fact]
    public void ObservableDeltasOnlyCreateAndRetireAffectedOccurrences()
    {
        ObservableCollection<string> source = ["0", "200", "400"];
        using Fixture fixture = new(source);
        TrackingNode[] original = fixture.Nodes;
        SceneItems2DUpdateSnapshot counts = fixture.Items.UpdateCounters.Snapshot();

        source.Move(2, 0);
        Assert.Equal(new[] { original[2], original[0], original[1] }, fixture.Nodes);
        Assert.Equal(counts.CreatedNodes, fixture.Items.UpdateCounters.CreatedNodes);
        Assert.Equal(counts.RemovedNodes, fixture.Items.UpdateCounters.RemovedNodes);

        source.Insert(1, "100");

        Assert.Equal(["400", "100", "0", "200"], fixture.Nodes.Select(node => node.Id));
        TrackingNode inserted = fixture.Nodes[1];
        Assert.Same(original[2], fixture.Nodes[0]);
        Assert.Same(original[0], fixture.Nodes[2]);
        Assert.Same(original[1], fixture.Nodes[3]);
        Assert.All(original, node => Assert.Equal(0, node.DetachCount));
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.Index));
        Assert.Equal(1, fixture.Items.UpdateCounters.CreatedNodes - counts.CreatedNodes);
        Assert.Equal(0, fixture.Items.UpdateCounters.RemovedNodes - counts.RemovedNodes);

        source.RemoveAt(1);
        Assert.Equal(new[] { original[2], original[0], original[1] }, fixture.Nodes);
        Assert.Equal(1, inserted.DetachCount);
        Assert.Equal(1, fixture.Items.UpdateCounters.CreatedNodes - counts.CreatedNodes);
        Assert.Equal(1, fixture.Items.UpdateCounters.RemovedNodes - counts.RemovedNodes);

        source[1] = "150";
        TrackingNode replacement = fixture.Nodes[1];
        Assert.Equal(new[] { original[2], replacement, original[1] }, fixture.Nodes);
        Assert.Equal("150", replacement.Id);
        Assert.Equal(1, original[0].DetachCount);
        Assert.Equal(2, fixture.Items.UpdateCounters.CreatedNodes - counts.CreatedNodes);
        Assert.Equal(2, fixture.Items.UpdateCounters.RemovedNodes - counts.RemovedNodes);
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.Index));

        source.Clear();
        Assert.Empty(fixture.Nodes);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(1, original[1].DetachCount);
        Assert.Equal(1, original[2].DetachCount);
        Assert.Equal(1, replacement.DetachCount);
        Assert.Equal(2, fixture.Items.UpdateCounters.CreatedNodes - counts.CreatedNodes);
        Assert.Equal(5, fixture.Items.UpdateCounters.RemovedNodes - counts.RemovedNodes);
    }

    [Fact]
    public void IdleFramesDoNotReenumerateOrPublishCollisionMutations()
    {
        CountingEnumerable<string> source = new(["0"]);
        using Fixture fixture = new(source, traceInvalidations: true);
        fixture.Surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        FrameStats modeWarmup = fixture.Tick();
        fixture.Record();
        FrameStats recordWarmup = fixture.Tick();
        Assert.False(fixture.Root.Scheduler.HasWork);

        int enumerations = source.EnumerationCount;
        long collisionVersion = fixture.Scene.CollisionMutationVersion;
        TrackingNode node = Assert.Single(fixture.Nodes);
        SceneItems2DUpdateSnapshot updates = fixture.Items.UpdateCounters.Snapshot();
        InvalidationTrace trace = fixture.Root.Detective.Invalidation;
        int requestsBefore = trace.Entries.Count(entry => entry.Kind == InvalidationTraceEventKind.Request);
        FrameStats[] idle = new FrameStats[16];

        for (int frame = 0; frame < idle.Length; frame++)
        {
            idle[frame] = fixture.Tick();
            fixture.Record();
        }

        int idleRequests = trace.Entries.Count(entry => entry.Kind == InvalidationTraceEventKind.Request) - requestsBefore;
        output.WriteLine($"warmup mode: {DescribeFrame(modeWarmup)}; warmup record: {DescribeFrame(recordWarmup)}");
        output.WriteLine($"idle frames={idle.Length}; measured={idle.Sum(frame => frame.MeasuredElements)}; arranged={idle.Sum(frame => frame.ArrangedElements)}; measureCalls={idle.Sum(frame => frame.MeasureCalls)}; arrangeCalls={idle.Sum(frame => frame.ArrangeCalls)}; rendered={idle.Sum(frame => frame.RenderedElements)}; invalidationRequests={idleRequests}; noWork={idle.Sum(frame => frame.NoWorkFrames)}");

        Assert.All(idle, frame =>
        {
            Assert.Equal(1, frame.NoWorkFrames);
            Assert.False(frame.HasWork);
            Assert.Equal(0, frame.MeasuredElements);
            Assert.Equal(0, frame.ArrangedElements);
            Assert.Equal(0, frame.MeasureCalls);
            Assert.Equal(0, frame.ArrangeCalls);
            Assert.Equal(0, frame.RenderedElements);
        });
        Assert.Equal(0, idleRequests);
        Assert.False(fixture.Root.Scheduler.HasWork);

        Assert.Equal(enumerations, source.EnumerationCount);
        Assert.Equal(collisionVersion, fixture.Scene.CollisionMutationVersion);
        Assert.Same(node, Assert.Single(fixture.Nodes));
        Assert.Equal(updates, fixture.Items.UpdateCounters.Snapshot());
    }

    private static string DescribeFrame(FrameStats frame) =>
        $"measured={frame.MeasuredElements}, arranged={frame.ArrangedElements}, measureCalls={frame.MeasureCalls}, arrangeCalls={frame.ArrangeCalls}, rendered={frame.RenderedElements}, noWork={frame.NoWorkFrames}";

    [Theory]
    [InlineData(false, 0f)]
    [InlineData(false, 1000f)]
    [InlineData(true, 0f)]
    [InlineData(true, 1000f)]
    public void OffCameraSpriteKeepsMotionAndItsActualCollisionOwner(bool collection, float startX)
    {
        ManualMotionClock clock = new();
        using Fixture fixture = new(new[] { "npc" }, clock);
        Sprite2D npc = new() { Width = 10, Height = 10, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        npc.SetValue(Sprite2D.XProperty, startX, UiPropertyValueSource.MarkupBase);
        if (collection)
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
        Assert.InRange(collision.Travel.X, 9.99998f, 10f);
        Assert.NotNull(collision.Collision);
        Assert.Empty(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        using var motion = npc.Motion().Animate(Sprite2D.XProperty).To(startX + 4)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(motion.IsActive);
        fixture.Root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        fixture.Root.ProcessFrame();
        Assert.True(npc.X > startX + 0.01f && npc.X < startX + 3.99f);
        if (collection) { Assert.Same(npc, fixture.GetNode("npc")); }
        Assert.True(npc.IsAttached);
        Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(npc.Collider!, new Vector2(20, 0)).Collision);
    }

    [Fact]
    public void OffscreenRealizedSpriteReleasesItsImageAndReloadsOnReturn()
    {
        using Fixture fixture = new(new[] { "npc" });
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("npc-image", null, 0,
            _ => new Sprite2D { Image = new(id), Width = 10, Height = 10 });
        SceneNode2D npc = fixture.GetNode("npc");
        Assert.Single(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);

        fixture.Pan(1000);
        // The last recorded frame still owns the image until the next frame is recorded.
        Assert.Equal(1, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(1, loader.Loaded);
        Assert.Empty(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(1, loader.Disposed);
        Assert.Same(npc, fixture.GetNode("npc"));

        for (int frame = 0; frame < 16; frame++)
        {
            fixture.Tick();
            Assert.Empty(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
            Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
            Assert.Equal(1, loader.Loaded);
        }

        fixture.Pan(0);
        Assert.Single(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(2, loader.Loaded);
    }

    [Fact]
    public void HiddenAncestorReleasesPresentationWithoutDetachingRealizedNodes()
    {
        using Fixture fixture = new(new[] { "npc" });
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
    public void PointerMissOutsideAnOffscreenSpriteDoesNotReloadItsImage(bool transformed)
    {
        using Fixture fixture = new(new[] { "npc" });
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("NpcAtlas");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        fixture.Items.Templates[0] = new ContentTemplate<string>("explicit-size", null, 0,
            _ => new Sprite2D { Image = new(id), Width = 16, Height = 16 });
        if (transformed) { fixture.Scene.TranslateX = 40; fixture.Scene.Rotation = 0.3f; }
        fixture.Record();
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
    public void ColliderOutsideSpriteVisualRemainsPickableWithoutReloadingImage(bool transformed)
    {
        using Fixture fixture = new(new[] { "npc" });
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

    private sealed class CountingEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        private readonly T[] snapshot = values.ToArray();
        internal int EnumerationCount { get; private set; }
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            return ((IEnumerable<T>)snapshot).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class Fixture : IDisposable
    {
        internal Fixture(IEnumerable source, ManualMotionClock? clock = null, bool traceInvalidations = false)
        {
            Root = traceInvalidations
                ? new UIRoot(new InvalidationTrace(), 100, 100, motionClock: clock)
                : clock is null ? new(100, 100) : new(100, 100, motionClock: clock);
            Items.Templates.Add(new ContentTemplate<string>("tracking", null, 0,
                context => new TrackingNode(context.Data!, context.Index)));
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
        internal SceneNode2D GetNode(string id) =>
            Assert.Single(Items.LogicalChildren.OfType<SceneNode2D>(),
                node => string.Equals(node.DataContext as string, id, StringComparison.Ordinal));
        internal void Pan(float x) { Surface.ViewBox = new(x, 0, 100, 100); Tick(); }
        internal FrameStats Tick()
        {
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
            return Root.ProcessFrame();
        }
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
        internal int DetachCount { get; private set; }
        protected override void OnDetached() { DetachCount++; base.OnDetached(); }
        internal override void Record(Scene2DRecordContext context) =>
            context.Frame.FillRectangle(Bounds, Color.Black);
        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Known(Bounds);
        private DrawRect Bounds => float.TryParse(Id, out float x) ? new(x, 0, 10, 10) : new(0, 0, 10, 10);
    }
}
