using Cerneala.Drawing;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ServoSemanticsCacheTests
{
    [Fact]
    public async Task WaitForOverIdleFramesBuildsTheServoSemanticsTreeOnce()
    {
        UIRoot root = new(240, 120);
        root.VisualChildren.Add(new Button { Content = "Existing", Width = 120, Height = 40 });
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        await servo.WaitForIdleAsync();
        int buildsBefore = root.ServoSemanticsBuildCount;

        Task wait = servo.WaitForAsync(ServoTarget.ById("later"), ServoCondition.Exists);
        for (int frame = 0; frame < 10; frame++)
        {
            PumpFrameAndAwaitNextEvaluation(host, root, wait);
        }

        Assert.False(wait.IsCompleted);
        Assert.Equal(1, root.ServoSemanticsBuildCount - buildsBefore);

        Button later = new() { Content = "Later" };
        ServoApi.SetId(later, "later");
        root.VisualChildren.Add(later);
        PumpFrameAndAwaitNextEvaluation(host, root, wait);
        await wait;

        Assert.Equal(2, root.ServoSemanticsBuildCount - buildsBefore);
    }

    [Fact]
    public async Task RepeatedQueriesWithoutChangesShareOneServoSemanticsTree()
    {
        UIRoot root = new(240, 120);
        Button save = new() { Content = "Save", Width = 120, Height = 40 };
        ServoApi.SetId(save, "save");
        root.VisualChildren.Add(save);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        await servo.WaitForIdleAsync();
        int buildsBefore = root.ServoSemanticsBuildCount;

        for (int query = 0; query < 4; query++)
        {
            Assert.Equal("Save", (await servo.FindAsync(ServoTarget.ById("save"))).Name);
            Assert.Single(await servo.FindAllAsync(ServoTarget.ByRole(SemanticsRole.Button)));
            Assert.True(await servo.ExistsAsync(ServoTarget.ByName("Save")));
        }

        Assert.Equal(1, root.ServoSemanticsBuildCount - buildsBefore);
    }

    [Fact]
    public async Task SemanticPropertyChangeIsVisibleToTheNextQueryWithoutAFrame()
    {
        UIRoot root = new(240, 120);
        Button button = new() { Content = "Save", Width = 120, Height = 40 };
        root.VisualChildren.Add(button);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        Assert.True(await servo.ExistsAsync(ServoTarget.ByName("Save")));

        button.Content = "Store";
        Assert.False(await servo.ExistsAsync(ServoTarget.ByName("Save")));
        Assert.Equal("Store", (await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button))).Name);

        AccessibleName.SetName(button, "Explicit");
        Assert.Equal("Explicit", (await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button))).Name);

        ServoApi.SetId(button, "renamed");
        Assert.Equal("Explicit", (await servo.FindAsync(ServoTarget.ById("renamed"))).Name);
    }

    [Fact]
    public async Task TreeChangeIsVisibleToTheNextQueryWithoutAFrame()
    {
        UIRoot root = new(240, 120);
        UIElement container = new();
        ServoApi.SetId(container, "container");
        root.VisualChildren.Add(container);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        ServoTarget scoped = ServoTarget.ById("later").Within(ServoTarget.ById("container"));
        Assert.False(await servo.ExistsAsync(scoped));

        Button later = new() { Content = "Later" };
        ServoApi.SetId(later, "later");
        container.VisualChildren.Add(later);
        Assert.True(await servo.ExistsAsync(scoped));

        container.VisualChildren.Remove(later);
        root.VisualChildren.Add(later);
        Assert.False(await servo.ExistsAsync(scoped));
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("later")));
    }

    [Fact]
    public async Task FrameWorkInvalidatesTheServoSemanticsTree()
    {
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>> chunkLoad =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TileMapSource2D source = new(
            new TileMapCatalog2D("presentation", []),
            (_, _, _) => new(chunkLoad.Task));
        TileMap2D map = TileMap2D.FromSource(source);
        Sprite2D actor = new() { Width = 10, Height = 10 };
        ServoApi.SetId(actor, "actor");
        Scene2D scene = new();
        scene.Children.Add(actor);
        scene.Children.Add(map);
        RenderSurface2D surface = new() { ViewBox = new(0, 0, 100, 100), Scene = scene };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(surface);
        // The pointer stays outside the window, so no frame changes IsPointerOver, which
        // would invalidate semantics on its own.
        InputFrame pointerOutside = PointerAt(-50, -50);
        UiHost host = CreateHost(root, pointerOutside);
        ServoApi servo = new(host);
        try
        {
            Assert.True(await servo.ExistsAsync(ServoTarget.ById("actor")));

            // A visible chunk that is still loading makes the scene presentation Loading.
            // That readiness change raises no semantics invalidation; it reaches the
            // Servo projection only through the next frame's work.
            ImageReference picture = new(new TestImage());
            SceneSpatialEntry2D spatial = new("pending", new(50, 0, 10, 10), new DrawRect(50, 0, 10, 10));
            source.SetCatalog(new("presentation", [new TileMapChunkInfo2D(spatial, 1, [picture], 1)]));
            host.Update(pointerOutside, host.Viewport, TimeSpan.FromMilliseconds(16));

            Assert.Equal(RenderSurface2DPresentationState.Loading, surface.PresentationState);
            Assert.False(await servo.ExistsAsync(ServoTarget.ById("actor")));
        }
        finally
        {
            chunkLoad.TrySetCanceled();
            root.VisualChildren.Remove(surface);
            surface.Scene = null;
            scene.Children.Remove(map);
            await map.DisposeAsync();
        }
    }

    [Fact]
    public void ScopedMatchingAllocatesNoPerAncestorArrays()
    {
        UIRoot root = new(240, 120);
        UIElement parent = root;
        for (int level = 0; level < 64; level++)
        {
            UIElement child = new();
            parent.VisualChildren.Add(child);
            parent = child;
        }

        ServoQueryEngine engine = new();
        ServoTarget unscoped = ServoTarget.ById("missing");
        ServoTarget scoped = ServoTarget.ByRole(SemanticsRole.Group).Within(ServoTarget.ById("missing"));
        Assert.Empty(engine.ResolveAll(root, unscoped));
        Assert.Empty(engine.ResolveAll(root, scoped));

        long unscopedBytes = MeasureAllocatedBytes(() => engine.ResolveAll(root, unscoped));
        long scopedBytes = MeasureAllocatedBytes(() => engine.ResolveAll(root, scoped));

        // Every one of the 64 Group nodes matches its own criteria and checks all of its
        // ancestors against the scope; that check must not copy the ancestor list.
        Assert.True(
            scopedBytes - unscopedBytes <= 256,
            $"Scoped matching allocated {scopedBytes} bytes; the same walk without a scope allocated {unscopedBytes} bytes.");
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    // Each frame releases the waiting predicate, which queues its next evaluation on the
    // Relay; the following frame drains it. Waiting for that queued work keeps one
    // predicate evaluation per frame.
    private static void PumpFrameAndAwaitNextEvaluation(UiHost host, UIRoot root, Task wait)
    {
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
        Assert.True(
            SpinWait.SpinUntil(() => wait.IsCompleted || root.Relay.HasPendingWork, TimeSpan.FromSeconds(5)),
            "The Servo wait did not queue its next evaluation.");
    }

    private static UiHost CreateHost(UIRoot root, InputFrame? firstInput = null)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        host.Update(firstInput ?? EmptyInput(), host.Viewport, TimeSpan.Zero);
        return host;
    }

    private static InputFrame PointerAt(float x, float y) =>
        new(
            PointerSnapshot.Empty.WithPosition(x, y),
            PointerSnapshot.Empty.WithPosition(x, y),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);

    private static InputFrame EmptyInput() =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);

    private sealed class TestImage : IDrawImage
    {
        public int Width => 10;
        public int Height => 10;
    }
}
