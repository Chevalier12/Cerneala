using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ScenePresentationTests
{
    [Fact]
    public void PendingVisibleDataWithholdsEverySceneCommandButNotImperativeDrawing()
    {
        using Fixture fixture = new();
        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        fixture.Surface.Draw += (_, frame) => frame.FillRectangle(new(0, 0, 2, 2), Color.HotPink);
        fixture.RequirePendingData();

        DrawCommandList commands = fixture.Record();

        Assert.Empty(commands.Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.FillRectangle));
        Assert.True(fixture.Actor.IsAttached);
        Assert.True(fixture.Actor.IsVisible);
        Assert.True(fixture.Actor.Collider!.Enabled);
    }

    [Fact]
    public void PendingSceneDoesNotReceiveCapturedPointerOrFocusedKeyboardInput()
    {
        using Fixture fixture = new();
        fixture.Record();
        int pointerEvents = 0, keyEvents = 0;
        fixture.Actor.MouseDown += (_, _) => pointerEvents++;
        fixture.Actor.KeyDown += (_, _) => keyEvents++;
        ElementInputBridge bridge = new();
        ElementInputRouteMap map = fixture.Root.InputCache.EnsureCurrent(fixture.Root);
        Assert.True(bridge.FocusManager.Focus(fixture.Actor, map));
        bridge.PointerCaptureManager.Capture(fixture.Actor, map);
        fixture.RequirePendingData();

        bridge.Dispatch(fixture.Root, Press());

        Assert.Equal(0, pointerEvents);
        Assert.Equal(0, keyEvents);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.Null(bridge.FocusManager.FocusedElement);
        Assert.True(fixture.Actor.IsEnabled);
    }

    [Fact]
    public void WorkerPublicationCannotRouteIntoTheOldSceneBeforeRelayPublication()
    {
        using Fixture fixture = new();
        fixture.Record();
        ElementInputBridge bridge = new();
        ElementInputRouteMap map = fixture.Root.InputCache.EnsureCurrent(fixture.Root);
        int pointerEvents = 0;
        fixture.Actor.MouseDown += (_, _) => pointerEvents++;
        bridge.PointerCaptureManager.Capture(fixture.Actor, map);
        Task publication = Task.Run(fixture.RequirePendingData);
        Assert.True(SpinWait.SpinUntil(() => publication.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.True(publication.IsCompletedSuccessfully, publication.Exception?.ToString());

        // Deliberately do not process the UI relay between publication and input.
        bridge.Dispatch(fixture.Root, Press());

        Assert.Equal(0, pointerEvents);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    [Fact]
    public void UnreadySceneStillRetiresImagesOfAnOffscreenSimulatedNode()
    {
        using Fixture fixture = new();
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("npc-image");
        fixture.Surface.Resources.SetResource(id, new ImageResource("npc.png"));
        Sprite2D npc = new() { Image = new(id), Width = 10, Height = 10 };
        SceneItems2D actors = new()
        {
            ItemsSource = new SceneSpatialSource2D<object>([new("npc", new(0, 0, 10, 10), isSimulated: true)],
                (_, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(npc)))
        };
        fixture.Scene.Children.Add(actors);
        fixture.Record();
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);

        fixture.Surface.ViewBox = new(1000, 0, 100, 100);
        fixture.Source.SetEntries([new("pending", new(1000, 0, 10, 10))]);
        fixture.Tick();
        fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.True(npc.IsAttached);
        Assert.True(npc.IsVisible);
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(1, loader.Disposed);
    }

    [Fact]
    public void CompleteVisiblePreparationPublishesTheWholeSceneAndReadonlyReadyState()
    {
        using Fixture fixture = new();
        fixture.RequirePendingData();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.Throws<InvalidOperationException>(() =>
            fixture.Surface.SetValue(RenderSurface2D.PresentationStateProperty, RenderSurface2DPresentationState.Ready));
        fixture.Complete(new Sprite2D { Image = new(new TestImage()), X = 50, Width = 10, Height = 10 });
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);

        Assert.Equal(2, fixture.Record().Count(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.True(fixture.Root.InputCache.EnsureCurrent(fixture.Root).TryGetId(fixture.Actor, out _));
    }

    [Fact]
    public void VisibleFailureIsExposedAsErrorAndSourceReplacementRecovers()
    {
        using Fixture fixture = new();
        fixture.RequirePendingData();
        IOException failure = new("chunk unavailable");
        fixture.Fail(failure);
        fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Same(failure, fixture.Surface.PresentationError);
        for (int i = 0; i < 16; i++) { fixture.Record(); }
        Assert.Equal(1, fixture.Loads);

        fixture.Items.ItemsSource = null;

        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OffscreenSimulationPreparationDoesNotHideAnAlreadyReadyViewport(bool fail)
    {
        using Fixture fixture = new();
        fixture.Source.SetEntries([new("pending", new(1000, 0, 10, 10), isSimulated: true)]);
        if (fail)
        {
            fixture.Fail(new IOException("offscreen simulation load"));
            fixture.DrainUntil(() => fixture.Items.Preparation.IsCompleted);
            Assert.NotNull(fixture.Items.PreparationError);
        }
        else { Assert.False(fixture.Items.Preparation.IsCompleted); }

        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenOrTransparentAncestorDoesNotRequireInvisibleData(bool transparent)
    {
        using Fixture fixture = new();
        fixture.RequirePendingData();
        Scene2D group = new();
        fixture.Scene.Children.Remove(fixture.Items);
        group.Children.Add(fixture.Items);
        fixture.Scene.Children.Add(group);
        if (transparent) { group.Opacity = 0; }
        else { group.IsVisible = false; }

        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
    }

    [Fact]
    public void OrdinaryUiRemainsRoutableWhileTheSceneIsLoading()
    {
        using Fixture fixture = new();
        Button button = new() { Width = 100, Height = 100 };
        fixture.Surface.Content = button;
        fixture.RequirePendingData();
        fixture.Tick();
        fixture.Record();
        int events = 0;
        button.MouseDown += (_, _) => events++;

        new ElementInputBridge().Dispatch(fixture.Root, Press());

        Assert.Equal(1, events);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
    }

    [Fact]
    public void PublicationDuringRecordingDiscardsEvenTheAlreadyRecordedScenePrefix()
    {
        using Fixture fixture = new();
        fixture.Scene.Children.Add(new RecordingNode(fixture.RequirePendingData));
        fixture.Surface.Draw += (_, frame) => frame.FillEllipse(new(0, 0, 2, 2), Color.HotPink);

        DrawCommandList commands = fixture.Record();

        Assert.Equal(DrawCommandKind.FillEllipse, Assert.Single(commands).Kind);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
    }

    [Fact]
    public void SimulationAnimationAndCollisionRemainActiveWhilePresentationIsWithheld()
    {
        using Fixture fixture = new();
        AnimationNode simulation = new();
        fixture.Scene.Children.Add(simulation);
        Sprite2D wall = new() { X = 20, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        fixture.Scene.Children.Add(wall);
        fixture.RequirePendingData();
        for (int i = 0; i < 64; i++)
        {
            ((ITimeSensitiveRenderElement)fixture.Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(i * 16));
            Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(20, 0)).Collision);
            Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        }
        Assert.Equal(64, simulation.Advances);
        Assert.True(simulation.IsAttached);
    }

    private static InputFrame Press() => new(PointerSnapshot.Empty,
        PointerSnapshot.Empty.WithPosition(5, 5).WithButton(InputMouseButton.Left, true),
        KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.Space]), []);

    private sealed class Fixture : IDisposable
    {
        private readonly TaskCompletionSource<SceneSpatialLease2D<object>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Fixture()
        {
            Source = new([], (_, _) => { Loads++; return new(completion.Task); });
            Items.ItemsSource = Source;
            Scene.Children.Add(Actor);
            Scene.Children.Add(Items);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Root.ProcessFrame();
        }
        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal SceneItems2D Items { get; } = new();
        internal SceneSpatialSource2D<object> Source { get; }
        internal int Loads { get; private set; }
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal Sprite2D Actor { get; } = new()
        {
            Image = new(new TestImage()), Width = 10, Height = 10, Focusable = true,
            Collider = new BoxCollider2D { Width = 10, Height = 10 }
        };
        internal void RequirePendingData() => Source.SetEntries([new("pending", new(50, 0, 10, 10))]);
        internal void Complete(object value) => completion.SetResult(new(value));
        internal void Fail(Exception error) => completion.SetException(error);
        internal void Tick() => Root.ProcessFrame();
        internal void DrainUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            return done();
        }, TimeSpan.FromSeconds(5)));
        internal DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, new(0, 0, 100, 100));
            return commands;
        }
        public void Dispose()
        {
            Root.VisualChildren.Remove(Surface);
            completion.TrySetCanceled();
        }
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 10;
        public int Height => 10;
    }

    private sealed class RecordingNode(Action duringRecord) : SceneNode2D
    {
        internal override void Record(Scene2DRecordContext context)
        {
            context.Frame.FillRectangle(new(0, 0, 5, 5), Color.Black);
            duringRecord();
        }
        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Empty;
    }

    private sealed class AnimationNode : SceneNode2D
    {
        internal int Advances { get; private set; }
        internal override bool HasActiveAnimation => true;
        internal override bool AdvanceAnimation(TimeSpan time) { Advances++; return true; }
        internal override void Record(Scene2DRecordContext context) { }
        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Empty;
    }

    private sealed class ImageLoader : IAsyncImageLoader
    {
        internal int Disposed;
        public IDrawImage Load(string path) => new Image(this);
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
        private sealed class Image(ImageLoader owner) : IDrawImage, IDisposable
        {
            public int Width => 10;
            public int Height => 10;
            public void Dispose() => owner.Disposed++;
        }
    }
}
