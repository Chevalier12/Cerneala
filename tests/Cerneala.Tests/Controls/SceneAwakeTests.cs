using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Stage 0 of docs/plans/2026-10-25-scene2d-awake.md: IsAwake and Awake on Sprite2D and Scene2D.
// Awake is raised once per time advance of a live RenderSurface2D (a UiHost update), while IsAwake is true and the node
// is in a scene shown by that surface. Elapsed is zero on the first Awake after the node becomes awake.
public sealed class SceneAwakeTests
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);
    private static readonly KeyboardSnapshot NoKeys = KeyboardSnapshot.Empty;
    private static readonly KeyboardSnapshot HeldD = KeyboardSnapshot.FromDownKeys([InputKey.D]);

    public enum NodeKind
    {
        Sprite,
        Scene
    }

    // IsAwake defaults to false on both classes, and the property field is public.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void IsAwakeDefaultsToFalse(NodeKind kind)
    {
        SceneNode2D node = Create(kind);

        Assert.False(IsAwake(node));
        Assert.NotNull(kind == NodeKind.Sprite ? (object)Sprite2D.IsAwakeProperty : Scene2D.IsAwakeProperty);
    }

    // A node that was never made awake gets no Awake, whatever the host does.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void NoAwakeIsRaisedWhileIsAwakeIsFalse(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        scene.Children.Add(node);
        List<TimeSpan> elapsed = Record(node);

        Tick(host);
        Tick(host);
        Tick(host);

        Assert.Empty(elapsed);
    }

    // One Awake per update, sender is the node, Elapsed is 0 on the first one and then the time of the update.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void AwakeIsRaisedOncePerUpdateWithZeroThenTheFrameTime(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        scene.Children.Add(node);
        List<object?> senders = [];
        List<TimeSpan> elapsed = [];
        Subscribe(node, (sender, args) =>
        {
            senders.Add(sender);
            elapsed.Add(args.Elapsed);
        });

        SetAwake(node, true);
        Tick(host);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame, Frame], elapsed);
        Assert.All(senders, sender => Assert.Same(node, sender));
    }

    // Setting IsAwake to false stops the events from the next update; setting it true again restarts at Elapsed 0.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void SettingIsAwakeFalseStopsTheEventsAndWakingAgainRestartsAtZero(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        scene.Children.Add(node);
        List<TimeSpan> elapsed = Record(node);
        SetAwake(node, true);
        Tick(host);
        Tick(host);

        SetAwake(node, false);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame], elapsed);

        SetAwake(node, true);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame, TimeSpan.Zero, Frame], elapsed);
    }

    // A handler that sets IsAwake to false on its own node receives no event from the next update on.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void ANodeThatSleepsInItsOwnHandlerGetsNoEventFromTheNextUpdate(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        scene.Children.Add(node);
        List<TimeSpan> elapsed = [];
        Subscribe(node, (_, args) =>
        {
            elapsed.Add(args.Elapsed);
            SetAwake(node, false);
        });
        SetAwake(node, true);

        Tick(host);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero], elapsed);
        Assert.False(IsAwake(node));
    }

    // A node removed from the scene stops receiving Awake; added back while awake, its first Elapsed is 0 again.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void ANodeRemovedFromTheSceneStopsAndWhenAddedBackStartsAtZeroAgain(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        scene.Children.Add(node);
        List<TimeSpan> elapsed = Record(node);
        SetAwake(node, true);
        Tick(host);
        Tick(host);

        scene.Children.Remove(node);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame], elapsed);
        Assert.True(IsAwake(node));

        scene.Children.Add(node);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame, TimeSpan.Zero, Frame], elapsed);
    }

    // A node woken inside a handler during delivery gets its first Awake on the next update, with Elapsed 0.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void ANodeWokenInAHandlerGetsItsFirstAwakeOnTheNextUpdate(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D waker = new();
        SceneNode2D sleeper = Create(kind);
        scene.Children.Add(waker);
        scene.Children.Add(sleeper);
        List<string> log = [];
        waker.Awake += (_, args) =>
        {
            log.Add($"waker {args.Elapsed.TotalMilliseconds}");
            SetAwake(sleeper, true);
        };
        Subscribe(sleeper, (_, args) => log.Add($"sleeper {args.Elapsed.TotalMilliseconds}"));
        waker.IsAwake = true;

        Tick(host);

        Assert.Equal(["waker 0"], log);

        log.Clear();
        Tick(host);

        Assert.Equal(["waker 16", "sleeper 0"], log);

        log.Clear();
        Tick(host);

        Assert.Equal(["waker 16", "sleeper 16"], log);
    }

    // A node that is already awake when it enters the scene starts with Elapsed 0 on the first update after that.
    [Theory]
    [InlineData(NodeKind.Sprite)]
    [InlineData(NodeKind.Scene)]
    public void ANodeAlreadyAwakeWhenItEntersTheSceneStartsAtZero(NodeKind kind)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        SceneNode2D node = Create(kind);
        SetAwake(node, true);
        List<TimeSpan> elapsed = Record(node);
        Tick(host);
        Assert.Empty(elapsed);

        scene.Children.Add(node);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame], elapsed);
    }

    // An off-screen (culled) sprite keeps receiving Awake: animation parking does not apply.
    [Fact]
    public void AnOffscreenSpriteKeepsReceivingAwake()
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D sprite = new() { X = 100_000, Y = 100_000, Width = 16, Height = 16 };
        scene.Children.Add(sprite);
        List<TimeSpan> elapsed = Record(sprite);
        sprite.IsAwake = true;

        Tick(host);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame, Frame], elapsed);
    }

    // A RenderSurface2D removed from the UI root stops delivering Awake; re-added while the node is awake, Elapsed restarts at 0.
    [Fact]
    public void ASurfaceRemovedFromTheRootStopsAwakeAndWhenReaddedStartsAtZeroAgain()
    {
        (UiHost host, Scene2D scene, RenderSurface2D surface, UIRoot root) = CreateRig();
        Sprite2D sprite = new();
        scene.Children.Add(sprite);
        List<TimeSpan> elapsed = Record(sprite);
        sprite.IsAwake = true;
        Tick(host);
        Tick(host);

        root.VisualChildren.Remove(surface);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame], elapsed);

        root.VisualChildren.Add(surface);
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero, Frame, TimeSpan.Zero, Frame], elapsed);
    }

    // Order between nodes is the order in which they became awake; a node that sleeps and wakes again goes to the end.
    [Fact]
    public void NodesReceiveAwakeInTheOrderTheyBecameAwakeAndAReawokenNodeGoesToTheEnd()
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D a = new();
        Sprite2D b = new();
        scene.Children.Add(a);
        scene.Children.Add(b);
        List<string> order = [];
        a.Awake += (_, _) => order.Add("a");
        b.Awake += (_, _) => order.Add("b");
        b.IsAwake = true;
        a.IsAwake = true;

        Tick(host);
        Tick(host);

        Assert.Equal(["b", "a", "b", "a"], order);

        order.Clear();
        b.IsAwake = false;
        b.IsAwake = true;
        Tick(host);
        Tick(host);

        Assert.Equal(["a", "b", "a", "b"], order);
    }

    // Nodes already awake in a scene that gets attached to a surface register in scene order, parent before children,
    // whatever order they were woken in.
    [Fact]
    public void AwakeNodesOfASceneAttachedToASurfaceRegisterInSceneOrderParentFirst()
    {
        (UiHost host, _, RenderSurface2D surface, _) = CreateRig();
        Scene2D top = new();
        Sprite2D first = new();
        Scene2D inner = new();
        Sprite2D nested = new();
        Sprite2D last = new();
        top.Children.Add(first);
        top.Children.Add(inner);
        inner.Children.Add(nested);
        top.Children.Add(last);
        List<string> order = [];
        top.Awake += (_, _) => order.Add("top");
        first.Awake += (_, _) => order.Add("first");
        inner.Awake += (_, _) => order.Add("inner");
        nested.Awake += (_, _) => order.Add("nested");
        last.Awake += (_, _) => order.Add("last");
        // Woken in the reverse of scene order.
        last.IsAwake = true;
        nested.IsAwake = true;
        inner.IsAwake = true;
        first.IsAwake = true;
        top.IsAwake = true;

        surface.Scene = top;
        Tick(host);
        Tick(host);

        Assert.Equal(
            ["top", "first", "inner", "nested", "last", "top", "first", "inner", "nested", "last"],
            order);
    }

    // The same order holds when the scene is set on a surface that is not under a root yet and the surface is added to the root later.
    [Fact]
    public void AwakeNodesOfASceneRegisterInSceneOrderParentFirstWhenTheSurfaceIsAddedToTheRoot()
    {
        UIRoot root = new(200, 200);
        UiHost host = VisionHostRig.CreateHost(root);
        RenderSurface2D surface = new() { Width = 200, Height = 200 };
        surface.Measure(new Cerneala.UI.Layout.MeasureContext(new Cerneala.UI.Layout.LayoutSize(200, 200)));
        surface.Arrange(new Cerneala.UI.Layout.ArrangeContext(new Cerneala.UI.Layout.LayoutRect(0, 0, 200, 200)));
        Scene2D top = new();
        Sprite2D first = new();
        Scene2D inner = new();
        Sprite2D nested = new();
        Sprite2D last = new();
        top.Children.Add(first);
        top.Children.Add(inner);
        inner.Children.Add(nested);
        top.Children.Add(last);
        List<string> order = [];
        top.Awake += (_, _) => order.Add("top");
        first.Awake += (_, _) => order.Add("first");
        inner.Awake += (_, _) => order.Add("inner");
        nested.Awake += (_, _) => order.Add("nested");
        last.Awake += (_, _) => order.Add("last");
        // Woken in the reverse of scene order.
        last.IsAwake = true;
        nested.IsAwake = true;
        inner.IsAwake = true;
        first.IsAwake = true;
        top.IsAwake = true;
        surface.Scene = top;
        Tick(host);
        Assert.Empty(order);

        root.VisualChildren.Add(surface);
        Tick(host);
        Tick(host);

        Assert.Equal(
            ["top", "first", "inner", "nested", "last", "top", "first", "inner", "nested", "last"],
            order);
    }

    // The arguments reject a negative Elapsed and keep the value they were given.
    [Fact]
    public void FrameTimeEventArgsRejectsANegativeElapsedAndKeepsTheValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTimeEventArgs(TimeSpan.FromTicks(-1)));
        Assert.Equal(TimeSpan.FromMilliseconds(16), new FrameTimeEventArgs(TimeSpan.FromMilliseconds(16)).Elapsed);
        Assert.Equal(TimeSpan.Zero, new FrameTimeEventArgs(TimeSpan.Zero).Elapsed);
    }

    // A node put to sleep or removed from the scene by another node's handler earlier in the same update gets no Awake in that update.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ANodePutToSleepOrRemovedByAnEarlierHandlerGetsNoAwakeInThatUpdate(bool remove)
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D first = new();
        Sprite2D second = new();
        scene.Children.Add(first);
        scene.Children.Add(second);
        bool act = false;
        first.Awake += (_, _) =>
        {
            if (!act) { return; }

            if (remove) { scene.Children.Remove(second); }
            else { second.IsAwake = false; }
        };
        List<TimeSpan> elapsed = Record(second);
        first.IsAwake = true;
        second.IsAwake = true;
        Tick(host);
        Assert.Equal([TimeSpan.Zero], elapsed);

        act = true;
        Tick(host);
        Tick(host);

        Assert.Equal([TimeSpan.Zero], elapsed);
    }

    // Awake is raised in the same update before InputHeld, because the time advance happens before input dispatch.
    [Fact]
    public void AwakeIsRaisedBeforeInputHeldInTheSameUpdate()
    {
        (UiHost host, Scene2D scene, _, UIRoot root) = CreateRig();
        Sprite2D sprite = new();
        scene.Children.Add(sprite);
        List<string> order = [];
        sprite.Awake += (_, _) => order.Add("awake");
        root.InputHeld += (_, _) => order.Add("input-held");
        sprite.IsAwake = true;

        host.Update(KeyFrame(NoKeys, HeldD), host.Viewport, Frame);
        host.Update(KeyFrame(HeldD, HeldD), host.Viewport, Frame);

        Assert.Equal(["awake", "input-held", "awake", "input-held"], order);
    }

    // A throwing handler does not stop the other awake nodes in that update; the update then throws an AggregateException with it.
    [Fact]
    public void AThrowingAwakeHandlerDoesNotStopTheOthersAndTheUpdateThrowsAnAggregateException()
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D thrower = new();
        Sprite2D other = new();
        scene.Children.Add(thrower);
        scene.Children.Add(other);
        InvalidOperationException failure = new("Awake handler failure");
        thrower.Awake += (_, _) => throw failure;
        List<TimeSpan> elapsed = Record(other);
        thrower.IsAwake = true;
        other.IsAwake = true;

        AggregateException aggregate = Assert.Throws<AggregateException>(() => Tick(host));

        Assert.Same(failure, Assert.Single(aggregate.InnerExceptions));
        Assert.Equal([TimeSpan.Zero], elapsed);
    }

    // Headless (D9): a scene driven only by SceneSimulationContext2D.Update() has no RenderSurface2D and never gets Awake.
    [Fact]
    public void AHeadlessSceneNeverGetsAwake()
    {
        Scene2D scene = new();
        Sprite2D sprite = new();
        scene.Children.Add(sprite);
        using SceneSimulationContext2D context = new(scene);
        List<TimeSpan> elapsed = Record(sprite);
        sprite.IsAwake = true;

        context.Update();
        context.Update();
        context.Update();

        Assert.Empty(elapsed);
    }

    // The example of the plan summary compiles and works: the bee stops receiving Awake once it fell asleep.
    [Fact]
    public void TheSummaryExampleCompilesAndWorks()
    {
        (UiHost host, Scene2D scene, _, _) = CreateRig();
        Sprite2D bee = new();
        scene.Children.Add(bee);
        float hunger = 30;
        int events = 0;
        bee.Awake += (_, _) => events++;

        bee.IsAwake = true;
        bee.Awake += (_, e) =>
        {
            hunger -= (float)e.Elapsed.TotalSeconds;
            if (hunger < 25)
            {
                bee.IsAwake = false;
            }
        };

        for (int update = 0; update < 20 && bee.IsAwake; update++)
        {
            Tick(host, TimeSpan.FromSeconds(1));
        }

        // Elapsed is 0, then 1 s per update: hunger is 25 after 6 events and 24 after the 7th, which puts the bee to sleep.
        Assert.False(bee.IsAwake);
        Assert.Equal(24f, hunger);
        Assert.Equal(7, events);

        Tick(host, TimeSpan.FromSeconds(1));
        Tick(host, TimeSpan.FromSeconds(1));

        Assert.Equal(7, events);
        Assert.Equal(24f, hunger);
    }

    private static (UiHost Host, Scene2D Scene, RenderSurface2D Surface, UIRoot Root) CreateRig()
    {
        UIRoot root = VisionHostRig.CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        UiHost host = VisionHostRig.CreateHost(root);
        return (host, scene, surface, root);
    }

    private static void Tick(UiHost host) => Tick(host, Frame);

    private static void Tick(UiHost host, TimeSpan elapsed) => VisionHostRig.Update(host, elapsed);

    private static SceneNode2D Create(NodeKind kind) => kind switch
    {
        NodeKind.Sprite => new Sprite2D(),
        NodeKind.Scene => new Scene2D(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static bool IsAwake(SceneNode2D node) => node switch
    {
        Sprite2D sprite => sprite.IsAwake,
        Scene2D scene => scene.IsAwake,
        _ => throw new ArgumentException("Only Sprite2D and Scene2D have IsAwake.", nameof(node))
    };

    private static void SetAwake(SceneNode2D node, bool value)
    {
        switch (node)
        {
            case Sprite2D sprite:
                sprite.IsAwake = value;
                break;
            case Scene2D scene:
                scene.IsAwake = value;
                break;
            default:
                throw new ArgumentException("Only Sprite2D and Scene2D have IsAwake.", nameof(node));
        }
    }

    private static void Subscribe(SceneNode2D node, EventHandler<FrameTimeEventArgs> handler)
    {
        switch (node)
        {
            case Sprite2D sprite:
                sprite.Awake += handler;
                break;
            case Scene2D scene:
                scene.Awake += handler;
                break;
            default:
                throw new ArgumentException("Only Sprite2D and Scene2D have Awake.", nameof(node));
        }
    }

    private static List<TimeSpan> Record(SceneNode2D node)
    {
        List<TimeSpan> elapsed = [];
        Subscribe(node, (_, args) => elapsed.Add(args.Elapsed));
        return elapsed;
    }

    private static InputFrame KeyFrame(KeyboardSnapshot previous, KeyboardSnapshot current) =>
        new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);
}
