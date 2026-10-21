using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.Controls;

public sealed class PasswordBoxTests
{
    [Fact]
    public void PasswordBoxStoresPasswordButRendersMaskText()
    {
        UIRoot root = new(200, 80);
        PasswordBox passwordBox = new() { Password = "secret" };
        root.VisualChildren.Add(passwordBox);
        passwordBox.Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render,
            "Initial password test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal("secret", passwordBox.Password);
        Assert.Contains(
            commands,
            command => command.Kind == DrawCommandKind.DrawText && command.Text == "******");
        Assert.DoesNotContain(commands, command => command.Text == "secret");
    }

    [Fact]
    public void PasswordBoxDoesNotRecordUndoHistory()
    {
        UIRoot root = FocusedPasswordBox(
            "secret",
            clipboard: null,
            out PasswordBox passwordBox,
            out ElementInputBridge bridge);

        bridge.Dispatch(root, TextInputFrame("!"));

        Assert.Equal("secret!", passwordBox.Password);
        Assert.Equal(0, passwordBox.UndoHistoryCount);
    }

    [Theory]
    [InlineData(InputKey.C)]
    [InlineData(InputKey.X)]
    public void PasswordBoxBlocksCopyAndCutShortcuts(InputKey key)
    {
        FakeClipboard clipboard = new();
        UIRoot root = FocusedPasswordBox(
            "secret",
            clipboard,
            out PasswordBox passwordBox,
            out ElementInputBridge bridge);
        SelectAll(root, bridge);

        DispatchCtrlShortcut(root, bridge, key);

        Assert.Null(clipboard.Text);
        Assert.Equal("secret", passwordBox.Password);
    }

    [Fact]
    public void PasswordBoxAllowsNormalizedPaste()
    {
        FakeClipboard clipboard = new("!\r\n");
        UIRoot root = FocusedPasswordBox(
            "secret",
            clipboard,
            out PasswordBox passwordBox,
            out ElementInputBridge bridge);

        DispatchCtrlShortcut(root, bridge, InputKey.V);

        Assert.Equal("secret!", passwordBox.Password);
        Assert.Equal(0, passwordBox.UndoHistoryCount);
    }

    [Theory]
    [InlineData("abc", "abcX")]
    [InlineData("a\U0001F600b", "a\U0001F600bX")]
    [InlineData("e\u0301xy", "e\u0301xyX")]
    public void PasswordBoxClickPastLastMaskCharacterThenTypingAppends(string password, string expected)
    {
        UIRoot root = FocusedPasswordBox(
            password,
            clipboard: null,
            out PasswordBox passwordBox,
            out ElementInputBridge bridge);
        root.ProcessFrame();
        bridge.Dispatch(root, KeyFrame(InputKey.Home));

        LayoutRect bounds = passwordBox.ArrangedBounds;
        float x = bounds.X + bounds.Width - 5;
        float y = bounds.Y + (bounds.Height / 2);
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: false, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, currentDown: false));
        bridge.Dispatch(root, TextInputFrame("X"));

        Assert.Equal(expected, passwordBox.Password);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void PasswordBoxClickPlacesCaretAtMaskCharacterDrawnUnderPointer(float scale)
    {
        // The box is arranged at a non-zero origin inside a parent that draws its content `scale` times
        // larger from the root origin, so a layout point (x, y) is drawn at root (x, y) * scale.
        UIRoot root = new(500, 200);
        root.SetPlatformServices(new FakePlatformServices(clipboard: null));
        UIElement parent = new()
        {
            RenderTransform = new Cerneala.UI.Media.Transform(Cerneala.UI.Media.Matrix3x2.CreateScale(scale, scale)),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        PasswordBox passwordBox = new() { Password = "abcdef" };
        parent.VisualChildren.Add(passwordBox);
        root.VisualChildren.Add(parent);
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 240, 80)));
        passwordBox.Measure(new MeasureContext(new LayoutSize(160, 40)));
        passwordBox.Arrange(new ArrangeContext(new LayoutRect(40, 10, 160, 32)));
        ElementInputBridge bridge = new();
        float x = (passwordBox.ArrangedBounds.X + passwordBox.BorderThickness.Left + passwordBox.Padding.Left + 1) * scale;
        float y = (passwordBox.ArrangedBounds.Y + 10) * scale;

        bridge.Dispatch(root, PointerFrame(x, y, previousDown: false, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, currentDown: false));
        bridge.Dispatch(root, TextInputFrame("X"));

        Assert.Equal("Xabcdef", passwordBox.Password);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("a\U0001F600b")]
    [InlineData("e\u0301xy")]
    public void PasswordBoxDrawnCaretMovesOneMaskCharacterPerArrowKey(string password)
    {
        UIRoot root = FocusedPasswordBox(
            password,
            clipboard: null,
            out _,
            out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.Home));
        float[] rightStops = new float[4];
        rightStops[0] = DrawnCaretX(root);
        for (int i = 1; i < rightStops.Length; i++)
        {
            bridge.Dispatch(root, KeyFrame(InputKey.Right));
            rightStops[i] = DrawnCaretX(root);
        }

        bridge.Dispatch(root, KeyFrame(InputKey.End));
        float[] leftStops = new float[4];
        leftStops[0] = DrawnCaretX(root);
        for (int i = 1; i < leftStops.Length; i++)
        {
            bridge.Dispatch(root, KeyFrame(InputKey.Left));
            leftStops[i] = DrawnCaretX(root);
        }

        for (int i = 1; i < rightStops.Length; i++)
        {
            Assert.True(
                rightStops[i] > rightStops[i - 1],
                $"Right stops: {string.Join(", ", rightStops)}");
        }

        Assert.Equal(Enumerable.Reverse(rightStops), leftStops);
    }

    // A password has no word structure, as in WPF's PasswordBox: Ctrl+Left and Ctrl+Right go to its start and end, so the
    // caret does not show where its words are.
    [Fact]
    public void PasswordBoxCtrlLeftAndCtrlRightMoveToTheStartAndEnd()
    {
        UIRoot root = FocusedPasswordBox("ab cd", clipboard: null, out PasswordBox passwordBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.End));
        bridge.Dispatch(root, KeyFrame(InputKey.Left, InputKey.LeftCtrl));
        bridge.Dispatch(root, TextInputFrame("X"));
        Assert.Equal("Xab cd", passwordBox.Password);

        bridge.Dispatch(root, KeyFrame(InputKey.Right, InputKey.LeftCtrl));
        bridge.Dispatch(root, TextInputFrame("Y"));
        Assert.Equal("Xab cdY", passwordBox.Password);
    }

    [Fact]
    public void PasswordBoxCtrlShiftLeftSelectsToTheStart()
    {
        UIRoot root = FocusedPasswordBox("ab cd", clipboard: null, out PasswordBox passwordBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.End));
        bridge.Dispatch(root, KeyFrame(InputKey.Left, InputKey.LeftCtrl, InputKey.LeftShift));
        bridge.Dispatch(root, TextInputFrame("X"));

        Assert.Equal("X", passwordBox.Password);
    }

    [Fact]
    public void PasswordBoxDoubleClickSelectsTheWholePassword()
    {
        UIRoot root = FocusedPasswordBox("ab cd", clipboard: null, out PasswordBox passwordBox, out ElementInputBridge bridge);
        root.ProcessFrame();
        LayoutRect bounds = passwordBox.ArrangedBounds;
        float x = bounds.X + passwordBox.BorderThickness.Left + passwordBox.Padding.Left + 2;
        float y = bounds.Y + (bounds.Height / 2);

        for (int click = 0; click < 2; click++)
        {
            bridge.Dispatch(root, PointerFrame(x, y, previousDown: false, currentDown: true));
            bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, currentDown: false));
        }

        bridge.Dispatch(root, TextInputFrame("X"));

        Assert.Equal("X", passwordBox.Password);
    }

    [Fact]
    public void PasswordBoxDoesNotExposePlainTextEditingApi()
    {
        Type type = typeof(PasswordBox);

        Assert.Null(type.GetProperty("Text"));
        Assert.Null(type.GetProperty("Editor"));
        Assert.Null(type.GetProperty("Selection"));
        Assert.Null(type.GetProperty("Caret"));
        Assert.Null(type.GetMethod("Undo"));
        Assert.Null(type.GetMethod("Redo"));
        Assert.Null(type.GetMethod("Select"));
        Assert.Null(type.GetMethod("MoveCaret"));
    }

    private static UIRoot FocusedPasswordBox(
        string password,
        IClipboard? clipboard,
        out PasswordBox passwordBox,
        out ElementInputBridge bridge)
    {
        UIRoot root = new(200, 80);
        root.SetPlatformServices(new FakePlatformServices(clipboard));
        passwordBox = new PasswordBox { Password = password };
        root.VisualChildren.Add(passwordBox);
        bridge = new ElementInputBridge();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        Assert.True(bridge.FocusManager.Focus(passwordBox, routeMap));
        return root;
    }

    private static void SelectAll(UIRoot root, ElementInputBridge bridge)
    {
        DispatchCtrlShortcut(root, bridge, InputKey.A);
    }

    private static void DispatchCtrlShortcut(UIRoot root, ElementInputBridge bridge, InputKey key)
    {
        bridge.Dispatch(
            root,
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl]),
                KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl, key]),
                []));
    }

    private static float DrawnCaretX(UIRoot root)
    {
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        return Assert.Single(
            commands,
            command => command.Kind == DrawCommandKind.FillRectangle && command.Rect.Width == 1).Rect.X;
    }

    private static InputFrame KeyFrame(InputKey key, params InputKey[] modifiers)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(modifiers),
            KeyboardSnapshot.FromDownKeys([.. modifiers, key]),
            []);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown, bool currentDown)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame TextInputFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
    }

    private sealed class FakeClipboard : IClipboard
    {
        public FakeClipboard(string? text = null)
        {
            Text = text;
        }

        public string? Text { get; private set; }

        public bool HasText => Text is not null;

        public string? GetText() => Text;

        public void SetText(string text)
        {
            Text = text;
        }
    }

    private sealed class FakePlatformServices : IPlatformServices
    {
        public FakePlatformServices(IClipboard? clipboard)
        {
            Clipboard = clipboard;
        }

        public IClipboard? Clipboard { get; }

        public ICursorService? Cursor => null;

        public IFileDialogService? FileDialogs => null;

        public ITextInputPlatform? TextInput => null;

        public IDpiProvider? Dpi => null;

        public IAccessibilityPlatform? Accessibility => null;

        public IReducedMotionSource? ReducedMotion => null;
    }
}
