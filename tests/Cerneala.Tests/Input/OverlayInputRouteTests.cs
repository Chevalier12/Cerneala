using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;

namespace Cerneala.Tests.Input;

// Projected overlay content routes like WPF Popup content: from the projected presenter to the Overlay,
// then along the Overlay's own ancestors (issue #404).
public sealed class OverlayInputRouteTests
{
    [Fact]
    public void KeyEventsFromASubmenuItemRouteThroughItsOwnerMenuItemAndTheWindow()
    {
        MenuItem open = Leaf("Open");
        MenuItem file = Branch("File", open, Leaf("Save"));
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        Window window = new() { Content = menuBar, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        List<string> log = [];
        LogKeys(log, "Window", window, includeBubble: true);
        LogKeys(log, "File", file, includeBubble: false);
        LogKeys(log, "Open", open, includeBubble: false);
        ElementInputBridge bridge = new();

        Click(bridge, root, file);
        root.ProcessFrame();
        Assert.True(file.IsSubmenuOpen);
        Assert.Same(file, bridge.FocusManager.FocusedElement);
        log.Clear();
        Tap(bridge, root, InputKey.F5);
        Assert.Equal("Window.PreviewKeyDown F5 | File.PreviewKeyDown F5 | Window.KeyDown F5", string.Join(" | ", log));

        Tap(bridge, root, InputKey.Down);
        Assert.Same(open, bridge.FocusManager.FocusedElement);
        log.Clear();
        Tap(bridge, root, InputKey.F5);

        Assert.Equal(
            "Window.PreviewKeyDown F5 | File.PreviewKeyDown F5 | Open.PreviewKeyDown F5 | Window.KeyDown F5",
            string.Join(" | ", log));
    }

    [Fact]
    public void WindowPreviewKeyDownThatHandlesTheKeyStopsTheFocusedSubmenuItemKeyDown()
    {
        MenuItem open = Leaf("Open");
        MenuItem file = Branch("File", open);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        Window window = new() { Content = menuBar, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        int windowPreviewF5 = 0;
        int openKeyDownF5 = 0;
        window.AddHandler(UIElement.PreviewKeyDownEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.F5)
            {
                windowPreviewF5++;
                args.Handled = true;
            }
        });
        open.AddHandler(UIElement.KeyDownEvent, (_, args) =>
        {
            if (((KeyEventArgs)args).Key == InputKey.F5)
            {
                openKeyDownF5++;
            }
        });
        ElementInputBridge bridge = Focused(root, file);
        Tap(bridge, root, InputKey.Down);
        Assert.Same(open, bridge.FocusManager.FocusedElement);

        Tap(bridge, root, InputKey.F5);

        Assert.Equal(1, windowPreviewF5);
        Assert.Equal(0, openKeyDownF5);
    }

    [Fact]
    public void KeyEventsFromAThirdLevelSubmenuItemTunnelAndBubbleThroughEveryOwnerAndOverlay()
    {
        MenuItem leaf = Leaf("Leaf");
        MenuItem third = Branch("Third", leaf);
        MenuItem second = Branch("Second", third);
        MenuItem first = Branch("First", second);
        MenuBar menuBar = new();
        menuBar.Items.Add(first);
        Window window = new() { Content = menuBar, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        ElementInputBridge bridge = Focused(root, first);
        Tap(bridge, root, InputKey.Down);
        Tap(bridge, root, InputKey.Right);
        Tap(bridge, root, InputKey.Right);
        Assert.Same(leaf, bridge.FocusManager.FocusedElement);
        List<string> log = [];
        LogKeys(log, "Window", window, includeBubble: true);
        LogKeys(log, "First", first, includeBubble: true);
        LogKeys(log, "FirstOverlay", first.SubmenuOverlay!, includeBubble: true);
        LogKeys(log, "Second", second, includeBubble: true);
        LogKeys(log, "SecondOverlay", second.SubmenuOverlay!, includeBubble: true);
        LogKeys(log, "Third", third, includeBubble: true);
        LogKeys(log, "ThirdOverlay", third.SubmenuOverlay!, includeBubble: true);
        LogKeys(log, "Leaf", leaf, includeBubble: true);

        Tap(bridge, root, InputKey.F5);

        Assert.Equal(
            [
                "Window.PreviewKeyDown F5", "First.PreviewKeyDown F5", "FirstOverlay.PreviewKeyDown F5",
                "Second.PreviewKeyDown F5", "SecondOverlay.PreviewKeyDown F5", "Third.PreviewKeyDown F5",
                "ThirdOverlay.PreviewKeyDown F5", "Leaf.PreviewKeyDown F5",
                "Leaf.KeyDown F5", "ThirdOverlay.KeyDown F5", "Third.KeyDown F5", "SecondOverlay.KeyDown F5",
                "Second.KeyDown F5", "FirstOverlay.KeyDown F5", "First.KeyDown F5", "Window.KeyDown F5"
            ],
            log);
    }

    [Fact]
    public void KeyEventsFromAComboBoxDropDownItemRouteThroughTheComboBoxAndTheWindow()
    {
        ComboBoxItem first = new() { Content = "One", Focusable = true };
        ComboBox comboBox = new() { Width = 120 };
        comboBox.Items.Add(first);
        comboBox.Items.Add(new ComboBoxItem { Content = "Two", Focusable = true });
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(comboBox);
        Window window = new() { Content = canvas, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.True(first.IsAttached);
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(first, root.InputCache.EnsureCurrent(root)));
        List<string> log = [];
        LogKeys(log, "Window", window, includeBubble: true);
        LogKeys(log, "ComboBox", comboBox, includeBubble: true);
        LogKeys(log, "Item", first, includeBubble: true);

        Tap(bridge, root, InputKey.F5);

        Assert.Equal(
            [
                "Window.PreviewKeyDown F5", "ComboBox.PreviewKeyDown F5", "Item.PreviewKeyDown F5",
                "Item.KeyDown F5", "ComboBox.KeyDown F5", "Window.KeyDown F5"
            ],
            log);
    }

    [Fact]
    public void AKeyThatASubmenuItemLeavesUnhandledDoesNotDriveTheOwnerMenuItem()
    {
        MenuItem last = Leaf("Last");
        MenuItem recent = Branch("Recent", Leaf("First"), last);
        MenuItem save = Leaf("Save");
        MenuItem file = Branch("File", recent, save);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        Window window = new() { Content = menuBar, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        ElementInputBridge bridge = Focused(root, file);
        Tap(bridge, root, InputKey.Down);
        Tap(bridge, root, InputKey.Right);
        Tap(bridge, root, InputKey.Down);
        Assert.Same(last, bridge.FocusManager.FocusedElement);
        List<string> log = [];
        LogKeys(log, "Window", window, includeBubble: true);

        Tap(bridge, root, InputKey.Down);

        Assert.Same(last, bridge.FocusManager.FocusedElement);
        Assert.Equal([file, recent], menuBar.Session.OpenPath);
        Assert.Contains("Window.KeyDown Down", log);
    }

    [Fact]
    public void ClosingANestedSubmenuKeepsItsParentSubmenusOpen()
    {
        MenuItem leaf = Leaf("Leaf");
        MenuItem third = Branch("Third", leaf);
        MenuItem second = Branch("Second", third);
        MenuItem first = Branch("First", second);
        MenuBar menuBar = new();
        menuBar.Items.Add(first);
        UIRoot root = Attach(menuBar);
        ElementInputBridge bridge = Focused(root, first);
        Tap(bridge, root, InputKey.Down);
        Tap(bridge, root, InputKey.Right);
        Tap(bridge, root, InputKey.Right);
        Assert.Same(leaf, bridge.FocusManager.FocusedElement);

        Tap(bridge, root, InputKey.Left);

        Assert.False(third.IsSubmenuOpen);
        Assert.True(second.IsSubmenuOpen);
        Assert.True(first.IsSubmenuOpen);
        Assert.Equal([first, second], menuBar.Session.OpenPath);
        Assert.Same(third, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void PointerEventsFromProjectedContentRouteToTheOwnerButPointerOverStaysVisual()
    {
        MenuItem open = Leaf("Open");
        MenuItem file = Branch("File", open);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        Window window = new() { Content = menuBar, Width = 360, Height = 240 };
        UIRoot root = Attach(window);
        ElementInputBridge bridge = Focused(root, file);
        Tap(bridge, root, InputKey.Down);
        Assert.Same(open, bridge.FocusManager.FocusedElement);
        MovePointer(bridge, root, file);
        int fileMoves = 0;
        int windowMoves = 0;
        file.AddHandler(UIElement.MouseMoveEvent, (_, _) => fileMoves++, handledEventsToo: true);
        window.AddHandler(UIElement.MouseMoveEvent, (_, _) => windowMoves++, handledEventsToo: true);

        MovePointer(bridge, root, open);

        Assert.True(open.IsPointerOver);
        Assert.False(file.IsPointerOver);
        Assert.Equal(1, fileMoves);
        Assert.Equal(1, windowMoves);
    }

    // The click owner of a press is visual, as its pressed state and click command are: a click on content that an
    // Overlay in a Button's content projects bubbles its MouseUp to the button but is not the button's click, so the
    // button raises no Click and runs no Command (issue #1203).
    [Fact]
    public void AClickOnProjectedOverlayContentInsideAButtonRaisesNoClickAndRunsNoCommand()
    {
        TextBlock projected = new() { Text = "popup text", Width = 80, Height = 20 };
        Overlay overlay = new() { Content = projected, IsOpen = true };
        TextBlock label = new() { Text = "label", Width = 60, Height = 20 };
        StackPanel content = new();
        content.Children.Add(label);
        content.Children.Add(overlay);
        Button button = new() { Content = content, Width = 100, Height = 40 };
        int clicks = 0;
        int executions = 0;
        int mouseUps = 0;
        button.Click += (_, _) => clicks++;
        button.Command = new ActionCommand(_ => executions++);
        button.AddHandler(UIElement.MouseUpEvent, (_, _) => mouseUps++, handledEventsToo: true);
        UIRoot root = Attach(button);
        root.ProcessFrame();
        Assert.True(overlay.IsProjected);
        ElementInputBridge bridge = new();

        Click(bridge, root, projected);
        root.ProcessFrame();

        Assert.Equal(1, mouseUps);
        Assert.False(button.IsPressed);
        Assert.Equal((0, 0), (clicks, executions));

        Click(bridge, root, label);
        root.ProcessFrame();

        Assert.Equal((1, 1), (clicks, executions));
    }

    // Opened and Closed describe the overlay that raised them, like WPF Popup.Opened and Popup.Closed (issue #491).
    [Fact]
    public void OpeningAndClosingAComboBoxDropDownInsideAnOpenOverlayDoesNotRaiseTheOverlayOpenedOrClosed()
    {
        ComboBox comboBox = new() { Width = 120 };
        comboBox.Items.Add(new ComboBoxItem { Content = "One" });
        StackPanel content = new();
        content.VisualChildren.Add(comboBox);
        (Overlay outer, UIRoot root, List<string> log) = OpenOuterOverlay(content);

        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.True(comboBox.IsDropDownOpen);
        comboBox.IsDropDownOpen = false;
        root.ProcessFrame();

        Assert.True(outer.IsOpen);
        Assert.Equal(["Opened outer"], log);
    }

    [Fact]
    public void OpeningAndClosingANestedOverlayDoesNotRaiseTheEnclosingOverlayOpenedOrClosed()
    {
        Overlay inner = new() { Content = new Border { Width = 40, Height = 20 } };
        StackPanel content = new();
        content.VisualChildren.Add(inner);
        (Overlay outer, UIRoot root, List<string> log) = OpenOuterOverlay(content);
        int innerOpened = 0;
        int innerClosed = 0;
        inner.Opened += (_, _) => innerOpened++;
        inner.Closed += (_, _) => innerClosed++;

        inner.IsOpen = true;
        root.ProcessFrame();
        inner.IsOpen = false;
        root.ProcessFrame();

        Assert.Equal(1, innerOpened);
        Assert.Equal(1, innerClosed);
        Assert.True(outer.IsOpen);
        Assert.Equal(["Opened outer"], log);
    }

    [Fact]
    public void OpeningAndClosingASubmenuInsideAnOpenOverlayDoesNotRaiseTheOverlayOpenedOrClosed()
    {
        MenuItem file = Branch("File", Leaf("Open"));
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        (Overlay outer, UIRoot root, List<string> log) = OpenOuterOverlay(menuBar);

        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        Assert.True(file.SubmenuOverlay!.IsProjected);
        file.IsSubmenuOpen = false;
        root.ProcessFrame();

        Assert.True(outer.IsOpen);
        Assert.Equal(["Opened outer"], log);
    }

    [Fact]
    public void OpeningAndClosingAnOverlayRaisesItsOwnOpenedAndClosedOnce()
    {
        (Overlay outer, UIRoot root, List<string> log) = OpenOuterOverlay(new Border { Width = 40, Height = 20 });

        outer.IsOpen = false;
        root.ProcessFrame();

        Assert.Equal(["Opened outer", "Closed outer"], log);
    }

    // Opens an overlay that holds content inside a Window and logs its Opened and Closed with their OriginalSource.
    private static (Overlay Outer, UIRoot Root, List<string> Log) OpenOuterOverlay(UIElement content)
    {
        Overlay outer = new() { Content = content };
        List<string> log = [];
        outer.Opened += (_, args) => log.Add("Opened " + (ReferenceEquals(args.OriginalSource, outer) ? "outer" : args.OriginalSource.GetType().Name));
        outer.Closed += (_, args) => log.Add("Closed " + (ReferenceEquals(args.OriginalSource, outer) ? "outer" : args.OriginalSource.GetType().Name));
        LayoutCanvas canvas = new();
        canvas.VisualChildren.Add(outer);
        UIRoot root = Attach(new Window { Content = canvas, Width = 360, Height = 240 });
        outer.IsOpen = true;
        root.ProcessFrame();
        Assert.True(outer.IsProjected);
        return (outer, root, log);
    }

    private static MenuItem Branch(string header, params MenuItem[] children)
    {
        MenuItem item = Leaf(header);
        foreach (MenuItem child in children)
        {
            item.Items.Add(child);
        }

        return item;
    }

    private static MenuItem Leaf(string header)
    {
        return new MenuItem { Header = header, Width = 90, Height = 28 };
    }

    private static UIRoot Attach(UIElement element)
    {
        UIRoot root = new(360, 240);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        return root;
    }

    private static ElementInputBridge Focused(UIRoot root, UIElement element)
    {
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(element, root.InputCache.EnsureCurrent(root)));
        return bridge;
    }

    private static void LogKeys(List<string> log, string name, UIElement element, bool includeBubble)
    {
        element.AddHandler(UIElement.PreviewKeyDownEvent, (_, args) => log.Add($"{name}.PreviewKeyDown {((KeyEventArgs)args).Key}"), handledEventsToo: true);
        if (includeBubble)
        {
            element.AddHandler(UIElement.KeyDownEvent, (_, args) => log.Add($"{name}.KeyDown {((KeyEventArgs)args).Key}"), handledEventsToo: true);
        }
    }

    private static void Tap(ElementInputBridge bridge, UIRoot root, InputKey key)
    {
        bridge.Dispatch(root, KeyboardFrame([], [key]));
        bridge.Dispatch(root, KeyboardFrame([key], []));
        root.ProcessFrame();
    }

    private static void Click(ElementInputBridge bridge, UIRoot root, UIElement element)
    {
        (float x, float y) = Center(element);
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: false, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, currentDown: false));
    }

    private static void MovePointer(ElementInputBridge bridge, UIRoot root, UIElement element)
    {
        (float x, float y) = Center(element);
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: false, currentDown: false));
    }

    private static (float X, float Y) Center(UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        return (bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
    }

    private static InputFrame KeyboardFrame(IEnumerable<InputKey> previous, IEnumerable<InputKey> current)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previous),
            KeyboardSnapshot.FromDownKeys(current),
            []);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown, bool currentDown)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, previousDown);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y).WithButton(InputMouseButton.Left, currentDown);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
