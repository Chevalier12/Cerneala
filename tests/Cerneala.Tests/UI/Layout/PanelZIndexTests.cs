using Cerneala.Drawing;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;
using Cerneala.UI.Servo;
using LayoutPanel = Cerneala.UI.Layout.Panels.Panel;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Layout;

public sealed class PanelZIndexTests
{
    // Scene used by most tests: two overlapping 120x120 borders in a 300x300 Canvas.
    // back  = red,   at (10, 10)  -> covers x,y in [10, 130]
    // front = green, at (60, 60)  -> covers x,y in [60, 180]
    // The overlap is [60, 130]. The point (100, 100) is inside the overlap.
    // The center of back (70, 70) and the center of front (120, 120) are both inside the overlap.
    private const float OverlapX = 100;
    private const float OverlapY = 100;

    // ---------------------------------------------------------------- 10. API

    [Fact]
    public void ZIndexDefaultsToZeroAndRoundTrips()
    {
        Border border = new();

        Assert.Equal(0, LayoutPanel.GetZIndex(border));

        LayoutPanel.SetZIndex(border, 7);
        Assert.Equal(7, LayoutPanel.GetZIndex(border));

        LayoutPanel.SetZIndex(border, -3);
        Assert.Equal(-3, LayoutPanel.GetZIndex(border));
    }

    [Fact]
    public void ZIndexResolvesThroughTheControlsPanelAndStaysOnTheLayoutPanelProperty()
    {
        Border border = new();

        Cerneala.UI.Controls.Panel.SetZIndex(border, 4);

        Assert.Equal(4, Cerneala.UI.Controls.Panel.GetZIndex(border));
        Assert.Equal(4, LayoutPanel.GetZIndex(border));
        Assert.Equal(4, border.GetValue(LayoutPanel.ZIndexProperty));
    }

    [Fact]
    public void ZIndexAccessorsRejectNullElement()
    {
        Assert.Throws<ArgumentNullException>(() => LayoutPanel.GetZIndex(null!));
        Assert.Throws<ArgumentNullException>(() => LayoutPanel.SetZIndex(null!, 1));
    }

    // ------------------------------------------------- 1. draw order follows ZIndex

    [Fact]
    public void HigherZIndexChildDrawsAfterLowerEvenWhenItComesFirstInChildren()
    {
        Scene scene = Scene.Create(frontFirst: true);
        LayoutPanel.SetZIndex(scene.Front, 1);
        scene.Update();

        Assert.Equal(["back", "front"], scene.DrawOrder());
    }

    // --------------------------------------------- 2. hit testing follows ZIndex

    [Fact]
    public void HigherZIndexChildReceivesPointerClickInOverlapEvenWhenItComesFirstInChildren()
    {
        Scene scene = Scene.Create(frontFirst: true);
        LayoutPanel.SetZIndex(scene.Front, 1);
        scene.Update();

        scene.ClickAt(OverlapX, OverlapY);

        Assert.Equal(1, scene.FrontMouseDowns);
        Assert.Equal(0, scene.BackMouseDowns);
    }

    [Fact]
    public async Task ServoClickOnTheHigherZIndexChildReachesItEvenWhenItComesFirstInChildren()
    {
        Scene scene = Scene.Create(frontFirst: true);
        LayoutPanel.SetZIndex(scene.Front, 1);
        scene.Update();
        ServoApi servo = new(scene.Host);

        // Servo clicks the center of the target (120, 120), which is inside the overlap.
        await servo.ClickAsync(ServoTarget.ById("front"));

        Assert.Equal(1, scene.FrontMouseDowns);
        Assert.Equal(0, scene.BackMouseDowns);
    }

    // ------------------------------------- 3. equal ZIndex keeps Children order

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void EqualZIndexKeepsChildrenOrderForDrawingAndHitTesting(int zIndex)
    {
        Scene scene = Scene.Create(frontFirst: false);
        LayoutPanel.SetZIndex(scene.Back, zIndex);
        LayoutPanel.SetZIndex(scene.Front, zIndex);
        scene.Update();

        Assert.Equal(["back", "front"], scene.DrawOrder());

        scene.ClickAt(OverlapX, OverlapY);

        Assert.Equal(1, scene.FrontMouseDowns);
        Assert.Equal(0, scene.BackMouseDowns);
    }

    [Fact]
    public async Task ServoClickWithEqualZIndexReachesTheLaterChild()
    {
        Scene scene = Scene.Create(frontFirst: false);
        LayoutPanel.SetZIndex(scene.Back, 5);
        LayoutPanel.SetZIndex(scene.Front, 5);
        scene.Update();
        ServoApi servo = new(scene.Host);

        await servo.ClickAsync(ServoTarget.ById("front"));

        Assert.Equal(1, scene.FrontMouseDowns);
        Assert.Equal(0, scene.BackMouseDowns);
    }

    // ----------------------------------------------------- 4. negative ZIndex

    [Fact]
    public void NegativeZIndexDrawsTheChildFirstAndLetsTheEarlierZeroChildWinTheClick()
    {
        // Children = [a (back), b (front)], b gets -1: b is drawn first, a is on top.
        Scene scene = Scene.Create(frontFirst: false);
        LayoutPanel.SetZIndex(scene.Front, -1);
        scene.Update();

        Assert.Equal(["front", "back"], scene.DrawOrder());

        scene.ClickAt(OverlapX, OverlapY);

        Assert.Equal(1, scene.BackMouseDowns);
        Assert.Equal(0, scene.FrontMouseDowns);
    }

    // ---------------------------------- 5. ZIndex change after the first frame

    [Fact]
    public void ZIndexChangeAfterTheFirstFrameReordersDrawingAndHitTestingWithoutLayoutWork()
    {
        Scene scene = Scene.Create(frontFirst: false);
        scene.Update();
        Assert.Equal(["back", "front"], scene.DrawOrder());
        scene.ClickAt(OverlapX, OverlapY);
        Assert.Equal(1, scene.FrontMouseDowns);
        Assert.Equal(0, scene.BackMouseDowns);
        LayoutRect backBounds = scene.Back.ArrangedBounds;
        LayoutRect frontBounds = scene.Front.ArrangedBounds;
        UIRoot root = scene.Root;

        LayoutPanel.SetZIndex(scene.Back, 1);

        Assert.True(root.RenderQueue.Count > 0, "Render must be requested by a ZIndex change.");
        Assert.True(root.HitTestQueue.Count > 0, "HitTest must be requested by a ZIndex change.");
        Assert.Equal(0, root.LayoutQueue.MeasureCount);
        Assert.Equal(0, root.LayoutQueue.ArrangeCount);
        Assert.False(scene.Back.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(scene.Back.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.False(scene.Canvas.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(scene.Canvas.DirtyState.Has(InvalidationFlags.Arrange));

        UiFrame frame = scene.Update();

        Assert.Equal(0, frame.Stats.MeasuredElements);
        Assert.Equal(0, frame.Stats.ArrangedElements);
        Assert.Equal(backBounds, scene.Back.ArrangedBounds);
        Assert.Equal(frontBounds, scene.Front.ArrangedBounds);
        Assert.Equal(["front", "back"], scene.DrawOrder());

        scene.ResetCounters();
        scene.ClickAt(OverlapX, OverlapY);

        Assert.Equal(1, scene.BackMouseDowns);
        Assert.Equal(0, scene.FrontMouseDowns);
    }

    // ------------------------------ 6. child collection changes after a sorted frame

    [Fact]
    public void ChildCollectionChangesAfterASortedFrameKeepTheStableZIndexOrder()
    {
        Scene scene = Scene.Create(frontFirst: false);
        // Rename for clarity: Back = "a", Front = "b".
        Border c = scene.AddBorder("c", Color.Blue, left: 20, top: 150, zIndex: 1);
        // Children = [back, front, c]; z = back 1, front 0, c 1.
        LayoutPanel.SetZIndex(scene.Back, 1);
        LayoutPanel.SetZIndex(scene.Front, 0);
        scene.Update();
        Assert.Equal(["front", "back", "c"], scene.DrawOrder());

        // Insert d (z 0) at the end: Children = [back, front, c, d].
        Border d = scene.AddBorder("d", Color.Yellow, left: 150, top: 150, zIndex: 0);
        scene.Update();
        Assert.Equal(["front", "d", "back", "c"], scene.DrawOrder());

        // Remove front: Children = [back, c, d].
        Assert.True(scene.Canvas.Children.Remove(scene.Front));
        scene.Update();
        Assert.Equal(["d", "back", "c"], scene.DrawOrder());

        // Move c (index 1) to index 0: Children = [c, back, d]. Ties at z 1 flip: c is now before back.
        scene.Canvas.VisualChildren.Move(1, 0);
        scene.Update();
        Assert.Equal(["d", "c", "back"], scene.DrawOrder());

        GC.KeepAlive(c);
        GC.KeepAlive(d);
    }

    // ------------------------------------------------ 7. Tab order follows ZIndex

    [Fact]
    public async Task TabOrderFollowsChildrenOrderWithoutZIndex()
    {
        (UiHost host, Button a, Button b, Button c) = CreateTabScene();
        ServoApi servo = new(host);

        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(a, host.InputBridge.FocusManager.FocusedElement);
        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(b, host.InputBridge.FocusManager.FocusedElement);
        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(c, host.InputBridge.FocusManager.FocusedElement);
    }

    [Fact]
    public async Task TabOrderFollowsZIndex()
    {
        (UiHost host, Button a, Button b, Button c) = CreateTabScene();
        LayoutPanel.SetZIndex(a, 1);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        ServoApi servo = new(host);

        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(b, host.InputBridge.FocusManager.FocusedElement);
        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(c, host.InputBridge.FocusManager.FocusedElement);
        await servo.PressKeyAsync(InputKey.Tab);
        Assert.Same(a, host.InputBridge.FocusManager.FocusedElement);
    }

    private static (UiHost Host, Button A, Button B, Button C) CreateTabScene()
    {
        UIRoot root = new(300, 300);
        Canvas canvas = new();
        Button a = new() { Content = "A", Width = 40, Height = 30 };
        Button b = new() { Content = "B", Width = 40, Height = 30 };
        Button c = new() { Content = "C", Width = 40, Height = 30 };
        Canvas.SetLeft(a, 0);
        Canvas.SetLeft(b, 60);
        Canvas.SetLeft(c, 120);
        canvas.Children.Add(a);
        canvas.Children.Add(b);
        canvas.Children.Add(c);
        root.VisualChildren.Add(canvas);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(300, 300) });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        return (host, a, b, c);
    }

    // --------------------------------------- 8. semantics tree order follows ZIndex

    [Fact]
    public void SemanticsTreeChildOrderFollowsZIndex()
    {
        UIRoot root = new(300, 300);
        Canvas canvas = new();
        Button a = new() { Content = "A", Width = 40, Height = 30 };
        Button b = new() { Content = "B", Width = 40, Height = 30 };
        Button c = new() { Content = "C", Width = 40, Height = 30 };
        canvas.Children.Add(a);
        canvas.Children.Add(b);
        canvas.Children.Add(c);
        root.VisualChildren.Add(canvas);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(300, 300) });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);

        Assert.Equal(["A", "B", "C"], ButtonNames(root.GetSemanticsTree().Root));

        LayoutPanel.SetZIndex(a, 1);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);

        Assert.Equal(["B", "C", "A"], ButtonNames(root.GetSemanticsTree().Root));
    }

    // ------------------------------------------------------ 9. layout unchanged

    [Fact]
    public void ZIndexDoesNotChangeStackPanelArrangement()
    {
        UIRoot root = new(300, 300);
        StackPanel stack = new();
        Border a = new() { Height = 20, Background = new SolidColorBrush(Color.Red) };
        Border b = new() { Height = 20, Background = new SolidColorBrush(Color.Green) };
        stack.Children.Add(a);
        stack.Children.Add(b);
        root.VisualChildren.Add(stack);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(300, 300) });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        LayoutRect aBefore = a.ArrangedBounds;
        LayoutRect bBefore = b.ArrangedBounds;

        LayoutPanel.SetZIndex(a, 1);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);

        Assert.Equal(0, a.ArrangedBounds.Y);
        Assert.Equal(20, b.ArrangedBounds.Y);
        Assert.Equal(aBefore, a.ArrangedBounds);
        Assert.Equal(bBefore, b.ArrangedBounds);
    }

    [Fact]
    public void ZIndexDoesNotChangeCanvasArrangement()
    {
        Scene scene = Scene.Create(frontFirst: false);
        scene.Update();
        LayoutRect backBefore = scene.Back.ArrangedBounds;
        LayoutRect frontBefore = scene.Front.ArrangedBounds;

        LayoutPanel.SetZIndex(scene.Back, 3);
        scene.Update();

        Assert.Equal(new LayoutRect(10, 10, 120, 120), scene.Back.ArrangedBounds);
        Assert.Equal(new LayoutRect(60, 60, 120, 120), scene.Front.ArrangedBounds);
        Assert.Equal(backBefore, scene.Back.ArrangedBounds);
        Assert.Equal(frontBefore, scene.Front.ArrangedBounds);
    }

    // ---------------------------------------------------------------- helpers

    private static List<string> ButtonNames(SemanticsNode node)
    {
        List<string> names = [];
        Collect(node);
        return names;

        void Collect(SemanticsNode current)
        {
            if (current.Role == SemanticsRole.Button)
            {
                names.Add(current.Name ?? string.Empty);
            }

            foreach (SemanticsNode child in current.Children)
            {
                Collect(child);
            }
        }
    }

    private static InputFrame EmptyInput() =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);

    private sealed class Scene
    {
        private readonly Dictionary<Color, string> namesByColor = [];

        private Scene()
        {
        }

        public UIRoot Root { get; private set; } = null!;

        public Canvas Canvas { get; private set; } = null!;

        public UiHost Host { get; private set; } = null!;

        public Border Back { get; private set; } = null!;

        public Border Front { get; private set; } = null!;

        public int BackMouseDowns { get; private set; }

        public int FrontMouseDowns { get; private set; }

        public static Scene Create(bool frontFirst)
        {
            Scene scene = new();
            scene.Root = new UIRoot(300, 300);
            scene.Canvas = new Canvas();
            scene.Back = scene.AddBorder("back", Color.Red, left: 10, top: 10, zIndex: 0, add: false);
            scene.Front = scene.AddBorder("front", Color.Green, left: 60, top: 60, zIndex: 0, add: false);
            scene.Back.MouseDown += (_, _) => scene.BackMouseDowns++;
            scene.Front.MouseDown += (_, _) => scene.FrontMouseDowns++;
            if (frontFirst)
            {
                scene.Canvas.Children.Add(scene.Front);
                scene.Canvas.Children.Add(scene.Back);
            }
            else
            {
                scene.Canvas.Children.Add(scene.Back);
                scene.Canvas.Children.Add(scene.Front);
            }

            scene.Root.VisualChildren.Add(scene.Canvas);
            scene.Host = new UiHost(new UiHostOptions { Root = scene.Root, Viewport = new UiViewport(300, 300) });
            scene.Update();
            return scene;
        }

        public Border AddBorder(string name, Color color, float left, float top, int zIndex, bool add = true)
        {
            Border border = new()
            {
                Width = 120,
                Height = 120,
                Background = new SolidColorBrush(color)
            };
            ServoApi.SetId(border, name);
            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);
            LayoutPanel.SetZIndex(border, zIndex);
            namesByColor[color] = name;
            if (add)
            {
                Canvas.Children.Add(border);
            }

            return border;
        }

        public UiFrame Update() => Host.Update(EmptyInput(), Host.Viewport, TimeSpan.Zero);

        public void ResetCounters()
        {
            BackMouseDowns = 0;
            FrontMouseDowns = 0;
        }

        // Names of the filled rectangles in the order the root draw command list draws them.
        public List<string> DrawOrder()
        {
            DrawCommandList commands = Root.RetainedRenderer.Commit(Root);
            List<string> order = [];
            for (int index = 0; index < commands.Count; index++)
            {
                // A Border fills its Background brush, so the color is on the brush, not on the command.
                if (commands[index].Kind == DrawCommandKind.FillRectangle &&
                    commands[index].Brush?.SolidColor is { } color &&
                    namesByColor.TryGetValue(color, out string? name))
                {
                    order.Add(name);
                }
            }

            return order;
        }

        // A real left click through the host input path: move, press, release.
        public void ClickAt(float x, float y)
        {
            PointerSnapshot moved = PointerSnapshot.Empty.WithPosition(x, y);
            PointerSnapshot pressed = moved.WithButton(InputMouseButton.Left, true);
            Host.Update(Frame(PointerSnapshot.Empty, moved), Host.Viewport, TimeSpan.Zero);
            Host.Update(Frame(moved, pressed), Host.Viewport, TimeSpan.Zero);
            Host.Update(Frame(pressed, moved), Host.Viewport, TimeSpan.Zero);
        }

        private static InputFrame Frame(PointerSnapshot previous, PointerSnapshot current) =>
            new(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
