using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxClipboardShortcutTests
{
    [Fact]
    public void CtrlASelectsAllText()
    {
        UIRoot root = FocusedTextBox("hello", clipboard: new FakeClipboard(), out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(2);

        DispatchCtrlShortcut(root, bridge, InputKey.A);

        Assert.Equal(0, textBox.Selection.Start);
        Assert.Equal(textBox.Text.Length, textBox.Selection.End);
        Assert.Equal(textBox.Text.Length, textBox.Caret.Position);
    }

    [Fact]
    public void CtrlCCopiesSelectionToPlatformClipboard()
    {
        FakeClipboard clipboard = new();
        UIRoot root = FocusedTextBox("hello", clipboard, out TextBox textBox, out ElementInputBridge bridge);
        textBox.Select(1, 4);

        DispatchCtrlShortcut(root, bridge, InputKey.C);

        Assert.Equal("ell", clipboard.Text);
        Assert.Equal("hello", textBox.Text);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(4, textBox.Selection.End);
    }

    [Fact]
    public void CtrlXCopiesSelectionAndDeletesIt()
    {
        FakeClipboard clipboard = new();
        UIRoot root = FocusedTextBox("hello", clipboard, out TextBox textBox, out ElementInputBridge bridge);
        textBox.Select(1, 4);

        DispatchCtrlShortcut(root, bridge, InputKey.X);

        Assert.Equal("ell", clipboard.Text);
        Assert.Equal("ho", textBox.Text);
        Assert.True(textBox.Selection.IsEmpty);
        Assert.Equal(1, textBox.Caret.Position);
    }

    [Fact]
    public void CtrlVPastesClipboardTextAtCaret()
    {
        FakeClipboard clipboard = new(" brave");
        UIRoot root = FocusedTextBox("hello", clipboard, out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(textBox.Text.Length);

        DispatchCtrlShortcut(root, bridge, InputKey.V);

        Assert.Equal("hello brave", textBox.Text);
        Assert.True(textBox.Selection.IsEmpty);
        Assert.Equal("hello brave".Length, textBox.Caret.Position);
    }

    [Fact]
    public void CtrlVPastesClipboardTextThroughTextInputNormalization()
    {
        FakeClipboard clipboard = new("a\r\nb\tc");
        UIRoot root = FocusedTextBox("hello", clipboard, out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(textBox.Text.Length);

        DispatchCtrlShortcut(root, bridge, InputKey.V);

        Assert.Equal("helloabc", textBox.Text);
        Assert.Equal("helloabc".Length, textBox.Caret.Position);
    }

    [Fact]
    public void ClipboardShortcutsDoNothingWhenNoClipboardIsAvailable()
    {
        UIRoot root = FocusedTextBox("hello", clipboard: null, out TextBox textBox, out ElementInputBridge bridge);
        textBox.Select(1, 4);

        DispatchCtrlShortcut(root, bridge, InputKey.C);
        DispatchCtrlShortcut(root, bridge, InputKey.X);
        DispatchCtrlShortcut(root, bridge, InputKey.V);

        Assert.Equal("hello", textBox.Text);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(4, textBox.Selection.End);
    }

    [Fact]
    public void HandledPreviewKeyDownSuppressesClipboardShortcut()
    {
        FakeClipboard clipboard = new();
        UIRoot root = FocusedTextBox("hello", clipboard, out TextBox textBox, out ElementInputBridge bridge);
        textBox.Select(1, 4);
        textBox.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, (_, args) => args.Handled = true);

        DispatchCtrlShortcut(root, bridge, InputKey.C);

        Assert.Null(clipboard.Text);
        Assert.Equal("hello", textBox.Text);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(4, textBox.Selection.End);
    }

    private static UIRoot FocusedTextBox(
        string text,
        IClipboard? clipboard,
        out TextBox textBox,
        out ElementInputBridge bridge)
    {
        UIRoot root = new(200, 80);
        root.SetPlatformServices(new FakePlatformServices(clipboard));
        textBox = new TextBox { Text = text };
        root.VisualChildren.Add(textBox);

        bridge = new ElementInputBridge();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        Assert.True(bridge.FocusManager.Focus(textBox, routeMap));

        return root;
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

    private sealed class FakeClipboard : IClipboard
    {
        public FakeClipboard(string? text = null)
        {
            Text = text;
        }

        public string? Text { get; private set; }

        public bool HasText => Text is not null;

        public string? GetText()
        {
            return Text;
        }

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
