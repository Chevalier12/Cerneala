using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
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
        npc.Collider = new BoxCollider2D { Width = 10, Height = 10, IsSimulated = true };
        SceneItems2D actors = new() { ItemsSource = new[] { "npc" } };
        actors.Templates.Add(new ContentTemplate<string>("npc", null, 0, _ => npc));
        fixture.Scene.Children.Add(actors);
        fixture.Record();
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);

        fixture.Surface.ViewBox = new(1000, 0, 100, 100);
        fixture.RequirePendingData(1000);
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
        fixture.Complete();
        fixture.DrainUntil(() => fixture.Map.Preparation.IsCompleted);

        DrawCommandList commands = fixture.Record();
        Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.DrawImage));
        DrawSpriteBatch batch = Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.DrawSpriteBatch)).SpriteBatch!;
        Assert.Equal(new DrawRect(50, 0, 10, 10), Assert.Single(batch.Sprites).Destination);
        Assert.Equal(1, fixture.Map.GetDiagnosticsSnapshot().DrawnTiles);
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
        fixture.DrainUntil(() => fixture.Map.Preparation.IsCompleted);
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Same(failure, fixture.Surface.PresentationError);
        for (int i = 0; i < 16; i++) { fixture.Record(); }
        Assert.Equal(1, fixture.Loads);

        fixture.Map.Source = null;

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
        fixture.RequirePendingData(1000);
        fixture.Scene.Children.Add(new Sprite2D
        {
            X = 1000,
            Collider = new BoxCollider2D { Width = 10, Height = 10, IsSimulated = true }
        });
        Assert.Contains(fixture.Scene.CollisionWorld.GetSpatialCollisionInterest(), interest =>
            interest.Kind == SceneBoundsKind.Known && interest.Bounds.X <= 1000 && interest.Bounds.Right >= 1010);
        ((ITimeSensitiveRenderElement)fixture.Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
        fixture.Tick();
        fixture.DrainUntil(() => fixture.Loads == 1);
        Assert.Equal(1, fixture.Loads);
        Assert.False(fixture.Map.Preparation.IsCompleted);
        if (fail)
        {
            fixture.Fail(new IOException("offscreen simulation load"));
            fixture.DrainUntil(() => fixture.Map.Preparation.IsCompleted);
            Assert.NotNull(fixture.Map.PreparationError);
        }
        else { Assert.False(fixture.Map.Preparation.IsCompleted); }

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
        fixture.Scene.Children.Remove(fixture.Map);
        group.Children.Add(fixture.Map);
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
        private readonly TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ImageReference picture = new(new TestImage());
        internal Fixture()
        {
            Source = new(new TileMapCatalog2D("presentation", []), (_, _, _) =>
            {
                Loads++;
                return new(completion.Task);
            });
            Map = TileMap2D.FromSource(Source);
            Scene.Children.Add(Actor);
            Scene.Children.Add(Map);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Root.ProcessFrame();
        }
        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal TileMap2D Map { get; }
        internal TileMapSource2D Source { get; }
        internal int Loads { get; private set; }
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal Sprite2D Actor { get; } = new()
        {
            Image = new(new TestImage()), Width = 10, Height = 10, Focusable = true,
            Collider = new BoxCollider2D { Width = 10, Height = 10 }
        };
        internal void RequirePendingData() => RequirePendingData(50);
        internal void RequirePendingData(int x)
        {
            SceneSpatialEntry2D spatial = new("pending", new(x, 0, 10, 10), new DrawRect(x, 0, 10, 10));
            Source.SetCatalog(new("presentation", [new TileMapChunkInfo2D(spatial, 1, [picture], 1)]));
        }
        internal void Complete()
        {
            TileColliderDescriptor2D shape = new(TileColliderShape2D.Box, width: 10, height: 10);
            TileMapChunkData2D data = new([new Tile(picture, shape, 50, 0, 10, 10)]);
            completion.SetResult(new(data));
        }
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
            completion.TrySetCanceled();
            Root.VisualChildren.Remove(Surface);
            Surface.Scene = null;
            if (Map.LogicalParent is Scene2D parent) { parent.Children.Remove(Map); }
            Map.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
