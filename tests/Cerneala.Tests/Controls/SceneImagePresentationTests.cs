using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneImagePresentationTests
{
    [Fact]
    public void ColdSpriteStartsAsyncPreparationAndWithholdsTheWholeScene()
    {
        using Fixture fixture = new();
        fixture.Scene.Children.Add(new Sprite2D { Image = new(new TestImage()), X = 30, Width = 10, Height = 10 });
        fixture.UseColdImage();
        fixture.Surface.Draw += (_, frame) => frame.FillRectangle(new(0, 0, 2, 2), Color.HotPink);

        DrawCommandList commands = fixture.Record();

        Assert.Equal(0, fixture.Loader.SynchronousLoads);
        Assert.Single(fixture.Loader.Requests);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.Empty(commands.Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.FillRectangle));

        TestImage loaded = fixture.Loader.Requests[0].Complete();
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Assert.Equal(2, fixture.Record().Count(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.False(loaded.Disposed);
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void PendingSpriteRejectsCapturedInputButKeepsSimulationAndCollisions()
    {
        using Fixture fixture = new();
        fixture.Record();
        ElementInputBridge bridge = new();
        ElementInputRouteMap routes = fixture.Root.InputCache.EnsureCurrent(fixture.Root);
        bridge.PointerCaptureManager.Capture(fixture.Actor, routes);
        Assert.True(bridge.FocusManager.Focus(fixture.Actor, routes));
        int pointerEvents = 0;
        fixture.Actor.MouseDown += (_, _) => pointerEvents++;
        AnimationNode simulation = new();
        fixture.Scene.Children.Add(simulation);
        fixture.Scene.Children.Add(new Sprite2D { X = 20, Collider = new BoxCollider2D { Width = 10, Height = 10 } });
        fixture.UseColdImage();

        bridge.Dispatch(fixture.Root, Press());

        Assert.Equal(0, pointerEvents);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.Null(bridge.FocusManager.FocusedElement);
        for (int i = 0; i < 64; i++)
        {
            ((ITimeSensitiveRenderElement)fixture.Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(i * 16));
            Assert.NotNull(fixture.Scene.CollisionWorld.MoveAndCollide(fixture.Actor.Collider!, new(20, 0)).Collision);
            Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        }
        Assert.Equal(64, simulation.Advances);
        Assert.True(fixture.Actor.IsAttached);
        Assert.True(fixture.Actor.Collider!.Enabled);
        Assert.Single(fixture.Loader.Requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InvisibleSpriteDoesNotStartImagePreparation(int reason)
    {
        using Fixture fixture = new();
        if (reason == 0) { fixture.Actor.X = 1000; }
        if (reason == 1) { fixture.Actor.IsVisible = false; }
        if (reason == 2) { fixture.Scene.Opacity = 0; }
        fixture.UseColdImage();
        fixture.Root.ProcessFrame();

        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
        Assert.Empty(fixture.Loader.Requests);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
    }

    [Fact]
    public void FailedRequiredImageIsObservableWithoutAutomaticRetryAndReplacementRecovers()
    {
        using Fixture fixture = new();
        fixture.UseColdImage();
        fixture.Record();
        PendingImage request = Assert.Single(fixture.Loader.Requests);
        IOException error = new("atlas unavailable");
        request.Completion.SetException(error);
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Error);

        Assert.Same(error, fixture.Surface.PresentationError);
        for (int i = 0; i < 32; i++) { Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage)); }
        Assert.Single(fixture.Loader.Requests);

        fixture.Actor.Image = new(new TestImage());
        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
    }

    [Fact]
    public void CameraRetiresPendingImageAndDisposesEveryLateResultAcross32Cycles()
    {
        using Fixture fixture = new();
        for (int i = 0; i < 32; i++)
        {
            fixture.Surface.ViewBox = new(0, 0, 100, 100);
            fixture.UseColdImage($"atlas-{i}.png");
            fixture.Record();
            Assert.Equal(i + 1, fixture.Loader.Requests.Count);
            Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);

            fixture.Surface.ViewBox = new(1000, 0, 100, 100);
            Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            TestImage late = fixture.Loader.Requests[i].Complete();
            fixture.DrainUntil(() => late.Disposed);
            Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
            Assert.True(fixture.Actor.IsAttached);
        }
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void MissingResourceStillOmitsTheSpriteWithoutInventingALoadingFailure()
    {
        using Fixture fixture = new();
        fixture.Actor.Image = new(new ResourceId<ImageResource>("missing"));
        Assert.Empty(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.Empty(fixture.Loader.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColdPrismMaskIsPreparedBeforeAnySceneCommand(bool group)
    {
        using Fixture fixture = new();
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("mask"), new ImageResource("mask.png"));
        using IDisposable effect = AttachMask(group ? fixture.Scene : fixture.Actor, "mask");

        DrawCommandList commands = fixture.Record();

        Assert.Equal(0, fixture.Loader.SynchronousLoads);
        PendingImage request = Assert.Single(fixture.Loader.Requests);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);
        Assert.Empty(commands.Where(c => c.Kind is DrawCommandKind.DrawImage or DrawCommandKind.BeginPrism));
        request.Complete();
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void SpriteAndPrismShareOnePendingAtlasButOwnIndependentAcquisitions()
    {
        using Fixture fixture = new();
        fixture.UseColdImage();
        using IDisposable effect = AttachMask(fixture.Scene, "atlas");
        fixture.Record();
        TestImage loaded = Assert.Single(fixture.Loader.Requests).Complete();
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        fixture.Actor.Image = new(new TestImage());
        fixture.Record();
        Assert.False(loaded.Disposed);
        effect.Dispose();
        fixture.Record();
        Assert.True(loaded.Disposed);
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    [Fact]
    public void FailedPrismMaskWithholdsTheSceneUntilTheEffectNoLongerRequiresIt()
    {
        using Fixture fixture = new();
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("mask"), new ImageResource("mask.png"));
        using IDisposable effect = AttachMask(fixture.Scene, "mask");
        fixture.Record();
        IOException failure = new("mask unavailable");
        Assert.Single(fixture.Loader.Requests).Completion.SetException(failure);
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Error);
        Assert.Same(failure, fixture.Surface.PresentationError);
        for (int i = 0; i < 32; i++) { fixture.Record(); }
        Assert.Single(fixture.Loader.Requests);

        GeneratedMarkup.GetPrismInstance(fixture.Scene).GetLayerState(new PrismNodeId(1)).Visible = false;

        Assert.Single(fixture.Record().Where(c => c.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HidingAnAncestorRetiresAlreadyPendingImagesWithoutDetachingSprites(bool transparent)
    {
        using Fixture fixture = new();
        fixture.UseColdImage();
        fixture.Record();
        PendingImage request = Assert.Single(fixture.Loader.Requests);
        if (transparent) { fixture.Scene.Opacity = 0; }
        else { fixture.Scene.IsVisible = false; }
        fixture.Record();
        TestImage late = request.Complete();
        fixture.DrainUntil(() => late.Disposed);
        Assert.True(fixture.Actor.IsAttached);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovingSpriteContentRetiresItsPendingPrismImage(bool missingResource)
    {
        using Fixture fixture = new();
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("mask"), new ImageResource("mask.png"));
        using IDisposable effect = AttachMask(fixture.Actor, "mask");
        fixture.Record();
        PendingImage request = Assert.Single(fixture.Loader.Requests);
        Assert.Equal(RenderSurface2DPresentationState.Loading, fixture.Surface.PresentationState);

        fixture.Actor.Image = missingResource ? new(new ResourceId<ImageResource>("missing")) : null;

        Assert.Empty(fixture.Record().Where(c => c.Kind is DrawCommandKind.DrawImage or DrawCommandKind.BeginPrism));
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        TestImage late = request.Complete();
        fixture.DrainUntil(() => late.Disposed);
        Assert.Equal(0, fixture.Root.ImageResourceCache!.ResidentCount);
        Assert.True(fixture.Actor.IsAttached);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImageCompletionDuringCursorResolutionLeavesTheHostFrameCommitted(bool fails)
    {
        using Fixture fixture = new();
        CursorPlatform platform = new();
        UiHost host = new(new UiHostOptions
        {
            Root = fixture.Root, Viewport = new(100, 100), PlatformServices = platform
        });
        fixture.UseColdImage();
        IOException failure = new("cursor-boundary image failure");
        Task<ImageResourceLease>? completion = null;
        platform.BeforePublish = () => _ = fixture.Root.RetainedRenderer.Render(fixture.Root);
        platform.BeforeCursor = () =>
        {
            platform.BeforeCursor = null;
            PendingImage request = Assert.Single(fixture.Loader.Requests);
            completion = fixture.Root.ImageResourceCache!.AcquireAsync(new("atlas.png")).AsTask();
            if (fails) { request.Completion.SetException(failure); }
            else { request.Complete(); }
            // Hold only the test's external completion boundary, not the UI
            // relay. Cursor hit testing then observes a newly ready/failed image.
            Assert.True(SpinWait.SpinUntil(() => completion.IsCompleted, TimeSpan.FromSeconds(5)));
            if (fails) { Assert.Same(failure, completion.Exception!.InnerException); }
        };

        try
        {
            host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty.WithPosition(5, 5),
                KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);

            Assert.Null(platform.BeforeCursor);
            Assert.Equal(1, platform.Publications);
            Assert.Equal(fails ? RenderSurface2DPresentationState.Error : RenderSurface2DPresentationState.Ready,
                fixture.Surface.PresentationState);
            Assert.Same(fails ? failure : null, fixture.Surface.PresentationError);
            _ = fixture.Root.RetainedRenderer.Render(fixture.Root);
        }
        finally
        {
            if (completion?.IsCompletedSuccessfully == true) { (await completion).Dispose(); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddingASharedAtlasSpriteThroughButtonInputCommitsItsFirstFrame(bool platformCursor)
    {
        using Fixture fixture = new();
        fixture.UseColdImage();
        fixture.Record();
        Assert.Single(fixture.Loader.Requests).Complete();
        fixture.DrainUntil(() => fixture.Surface.PresentationState == RenderSurface2DPresentationState.Ready);
        Button add = new() { Width = 20, Height = 20 };
        Cerneala.UI.Servo.Servo.SetId(add, "add-sprite");
        fixture.Root.VisualChildren.Add(add);
        CursorPlatform platform = new();
        UiHost host = new(new UiHostOptions
        {
            Root = fixture.Root, Viewport = new(100, 100),
            PlatformServices = platformCursor ? platform : null
        });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        int additions = 0;
        add.Click += (_, _) =>
        {
            additions++;
            fixture.Scene.Children.Add(new Sprite2D
            {
                Image = new(new ResourceId<ImageResource>("atlas")), X = 30, Width = 10, Height = 10
            });
        };
        int publicationsBefore = platform.Publications;
        platform.BeforePublish = () => _ = fixture.Root.RetainedRenderer.Render(fixture.Root);

        await new Cerneala.UI.Servo.Servo(host).ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("add-sprite"));

        Assert.Equal(1, additions);
        Assert.Equal(platformCursor, platform.Publications > publicationsBefore);
        _ = fixture.Root.RetainedRenderer.Render(fixture.Root);
        Assert.Equal(2, fixture.Record().Count(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Single(fixture.Loader.Requests);
        Assert.Equal(0, fixture.Loader.SynchronousLoads);
    }

    private static IDisposable AttachMask(SceneNode2D owner, string resource) => GeneratedMarkup.AttachPrism(owner,
        () => new PrismInstance(new PrismCompositionDefinition("image-preparation",
            [new PrismLayerDefinition(new PrismNodeId(1), "content",
                filters: [new PrismFilterDefinition(Cerneala.Drawing.Prism.Catalog.PrismFilterId.Blur)],
                mask: new(new PrismResourceId(resource)))])));

    private static InputFrame Press() => new(PointerSnapshot.Empty,
        PointerSnapshot.Empty.WithPosition(5, 5).WithButton(InputMouseButton.Left, true),
        KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.Space]), []);

    private sealed class Fixture : IDisposable
    {
        private static readonly ResourceId<ImageResource> Atlas = new("atlas");
        internal Fixture()
        {
            Root.SetImageLoader(Loader);
            Scene.Children.Add(Actor);
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Root.ProcessFrame();
        }
        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal Loader Loader { get; } = new();
        internal Sprite2D Actor { get; } = new()
        {
            Image = new(new TestImage()), Width = 10, Height = 10, Focusable = true,
            Collider = new BoxCollider2D { Width = 10, Height = 10 }
        };
        internal void UseColdImage(string path = "atlas.png")
        {
            Surface.Resources.SetResource(Atlas, new ImageResource(path));
            Actor.Image = new(Atlas);
        }
        internal DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, new(0, 0, 100, 100));
            return commands;
        }
        internal void DrainUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            Record();
            return done();
        }, TimeSpan.FromSeconds(5)));
        public void Dispose()
        {
            Root.VisualChildren.Remove(Surface);
            foreach (PendingImage request in Loader.Requests)
            {
                if (!request.Completion.Task.IsCompleted) { request.Complete(); }
            }
            Task disposed = Root.ImageResourceCache!.DisposeAsync().AsTask();
            Assert.True(SpinWait.SpinUntil(() => disposed.IsCompleted, TimeSpan.FromSeconds(5)));
            Assert.True(disposed.IsCompletedSuccessfully, disposed.Exception?.ToString());
        }
    }

    private sealed class CursorPlatform : IPlatformServices, ICursorService
    {
        internal Action? BeforeCursor;
        internal Action? BeforePublish;
        internal int Publications;
        ICursorService? IPlatformServices.Cursor
        {
            get { BeforeCursor?.Invoke(); return this; }
        }
        public IClipboard? Clipboard => null;
        public IFileDialogService? FileDialogs => null;
        public ITextInputPlatform? TextInput => null;
        public IDpiProvider? Dpi => null;
        public IAccessibilityPlatform? Accessibility => null;
        public IReducedMotionSource? ReducedMotion => null;
        public CursorShape Current { get; private set; }
        public void SetCursor(CursorShape shape)
        {
            BeforePublish?.Invoke();
            Publications++;
            Current = shape;
        }
    }

    private sealed class Loader : IAsyncImageLoader
    {
        internal int SynchronousLoads;
        internal List<PendingImage> Requests { get; } = [];
        public IDrawImage Load(string path) { SynchronousLoads++; return new TestImage(); }
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            PendingImage request = new();
            Requests.Add(request);
            // Deliberately finish after cancellation to exercise real retirement.
            return new(request.Completion.Task);
        }
    }

    private sealed class PendingImage
    {
        internal TaskCompletionSource<IDrawImage> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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

    private sealed class AnimationNode : SceneNode2D
    {
        internal int Advances;
        internal override bool HasActiveAnimation => true;
        internal override bool AdvanceAnimation(TimeSpan frameTime) { Advances++; return true; }
        internal override void Record(Scene2DRecordContext context) { }
        internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Empty;
    }
}
