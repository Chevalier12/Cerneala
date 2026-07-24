using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
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
