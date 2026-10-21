using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

// The editing members of issue #682, driven through ElementInputBridge with real input frames.
public sealed class TextBoxEditingApiTests
{
    [Fact]
    public void TypedTextUndoesAsOneUnitAndRedoesWithCtrlYAndCtrlShiftZ()
    {
        TextBox textBox = NewTextBox();
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("a"));
        bridge.Dispatch(root, TextFrame("b"));
        bridge.Dispatch(root, TextFrame("c"));
        Assert.Equal("abc", textBox.Text);

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));
        Assert.Equal(string.Empty, textBox.Text);

        bridge.Dispatch(root, KeyFrame(InputKey.Y, InputKey.LeftCtrl));
        Assert.Equal("abc", textBox.Text);

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));
        Assert.Equal(string.Empty, textBox.Text);

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl, InputKey.LeftShift));
        Assert.Equal("abc", textBox.Text);
    }

    [Fact]
    public void PublicUndoAndRedoReportWhetherTheyChangedTheText()
    {
        TextBox textBox = NewTextBox();
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        Assert.False(textBox.Undo());
        bridge.Dispatch(root, TextFrame("abc"));

        Assert.True(textBox.Undo());
        Assert.Equal(string.Empty, textBox.Text);
        Assert.True(textBox.Redo());
        Assert.Equal("abc", textBox.Text);
        Assert.False(textBox.Redo());
    }

    [Fact]
    public void PasswordBoxCtrlZDoesNothing()
    {
        PasswordBox passwordBox = new() { Width = 200, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(passwordBox, clipboard: null, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("abc"));
        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));

        Assert.Equal("abc", passwordBox.Password);
    }

    [Fact]
    public void MaxLengthTruncatesTypedText()
    {
        TextBox textBox = NewTextBox();
        textBox.MaxLength = 3;
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("abcdef"));
        bridge.Dispatch(root, TextFrame("g"));

        Assert.Equal("abc", textBox.Text);
    }

    [Fact]
    public void MaxLengthTruncatesPastedText()
    {
        TextBox textBox = NewTextBox();
        textBox.MaxLength = 3;
        UIRoot root = Host(textBox, new FakeClipboard("abcdef"), out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.V, InputKey.LeftCtrl));

        Assert.Equal("abc", textBox.Text);
    }

    [Fact]
    public void MaxLengthCountsTheReplacedSelection()
    {
        TextBox textBox = NewTextBox("hello");
        textBox.MaxLength = 5;
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.A, InputKey.LeftCtrl));
        bridge.Dispatch(root, TextFrame("abcdefg"));

        Assert.Equal("abcde", textBox.Text);
    }

    [Fact]
    public void MaxLengthDoesNotLimitProgrammaticTextAndRejectsNegativeValues()
    {
        TextBox textBox = new();

        Assert.Equal(0, textBox.MaxLength);
        textBox.MaxLength = 3;
        textBox.Text = "abcdef";

        Assert.Equal("abcdef", textBox.Text);
        Assert.Throws<ArgumentException>(() => textBox.MaxLength = -1);
    }

    [Fact]
    public void CharacterCasingChangesTypedAndPastedTextOnly()
    {
        TextBox textBox = NewTextBox();
        Assert.Equal(CharacterCasing.Normal, textBox.CharacterCasing);
        textBox.CharacterCasing = CharacterCasing.Upper;
        UIRoot root = Host(textBox, new FakeClipboard("xY"), out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("abC"));
        bridge.Dispatch(root, KeyFrame(InputKey.V, InputKey.LeftCtrl));
        Assert.Equal("ABCXY", textBox.Text);

        textBox.CharacterCasing = CharacterCasing.Lower;
        bridge.Dispatch(root, TextFrame("QrS"));
        Assert.Equal("ABCXYqrs", textBox.Text);

        textBox.Text = "MiXeD";
        Assert.Equal("MiXeD", textBox.Text);
    }

    [Fact]
    public void AcceptsTabInsertsTabAndKeepsFocusWhileCtrlTabStillNavigates()
    {
        TextBox textBox = NewTextBox();
        TextBox next = NewTextBox();
        Assert.False(textBox.AcceptsTab);
        textBox.AcceptsTab = true;
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge, next);

        bridge.Dispatch(root, TextFrame("a"));
        bridge.Dispatch(root, KeyFrame(InputKey.Tab));
        bridge.Dispatch(root, KeyFrame(InputKey.Tab, InputKey.LeftShift));
        bridge.Dispatch(root, TextFrame("b"));

        Assert.Equal("a\t\tb", textBox.Text);
        Assert.Same(textBox, bridge.FocusManager.FocusedElement);

        bridge.Dispatch(root, KeyFrame(InputKey.Tab, InputKey.LeftCtrl));

        Assert.Same(next, bridge.FocusManager.FocusedElement);
        Assert.Equal("a\t\tb", textBox.Text);
    }

    [Fact]
    public void TabWithoutAcceptsTabMovesFocus()
    {
        TextBox textBox = NewTextBox();
        TextBox next = NewTextBox();
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge, next);

        bridge.Dispatch(root, KeyFrame(InputKey.Tab));

        Assert.Same(next, bridge.FocusManager.FocusedElement);
        Assert.Equal(string.Empty, textBox.Text);
    }

    [Fact]
    public void IsUndoEnabledFalseClearsHistoryAndStopsRecording()
    {
        TextBox textBox = NewTextBox();
        Assert.True(textBox.IsUndoEnabled);
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("abc"));
        Assert.True(textBox.CanUndo);

        textBox.IsUndoEnabled = false;
        Assert.False(textBox.CanUndo);
        bridge.Dispatch(root, TextFrame("d"));
        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));

        Assert.Equal("abcd", textBox.Text);
        Assert.False(textBox.CanUndo);
    }

    [Fact]
    public void UndoLimitKeepsOnlyTheNewestUnits()
    {
        TextBox textBox = NewTextBox();
        Assert.Equal(-1, textBox.UndoLimit);
        textBox.UndoLimit = 1;
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        // A caret move between the letters ends each typed run, so every letter is its own undo unit.
        foreach (string letter in new[] { "a", "b", "c" })
        {
            bridge.Dispatch(root, TextFrame(letter));
            bridge.Dispatch(root, KeyFrame(InputKey.Left));
            bridge.Dispatch(root, KeyFrame(InputKey.Right));
        }

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));
        Assert.Equal("ab", textBox.Text);
        Assert.False(textBox.CanUndo);
        Assert.True(textBox.CanRedo);

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));
        Assert.Equal("ab", textBox.Text);
        Assert.Throws<ArgumentException>(() => textBox.UndoLimit = -2);
    }

    [Fact]
    public void CtrlRightAndCtrlLeftMoveByWordAndShiftExtends()
    {
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);
        bridge.Dispatch(root, KeyFrame(InputKey.Home, InputKey.LeftCtrl));

        bridge.Dispatch(root, KeyFrame(InputKey.Right, InputKey.LeftCtrl));
        Assert.Equal(6, textBox.Caret.Position);
        Assert.True(textBox.Selection.IsEmpty);

        bridge.Dispatch(root, KeyFrame(InputKey.Left, InputKey.LeftCtrl));
        Assert.Equal(0, textBox.Caret.Position);

        bridge.Dispatch(root, KeyFrame(InputKey.Right, InputKey.LeftCtrl, InputKey.LeftShift));
        Assert.Equal(0, textBox.Selection.Anchor);
        Assert.Equal(6, textBox.Selection.Active);
    }

    [Fact]
    public void DoubleClickSelectsTheWordUnderThePointer()
    {
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float worldX, float y) = CharacterPoint(textBox, 8);
        Click(root, bridge, worldX, y);
        Click(root, bridge, worldX, y);

        Assert.Equal(6, textBox.Selection.Start);
        Assert.Equal(11, textBox.Selection.End);

        // A pause longer than the double-click time starts a new click sequence.
        bridge.Dispatch(root, IdleFrame(worldX, y), TimeSpan.FromSeconds(1));
        (float helloX, _) = CharacterPoint(textBox, 2);
        Click(root, bridge, helloX, y);
        Click(root, bridge, helloX, y);

        Assert.Equal(0, textBox.Selection.Start);
        Assert.Equal(6, textBox.Selection.End);
    }

    [Fact]
    public void DoubleClickUsesTheCharacterUnderThePointer()
    {
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        // The right half of the 'o' of "hello" is nearest the caret position after it, before the space.
        (float oX, float y) = CharacterPoint(textBox, 4, fraction: 0.8f);
        Click(root, bridge, oX, y);
        Click(root, bridge, oX, y);

        Assert.Equal(0, textBox.Selection.Start);
        Assert.Equal(6, textBox.Selection.End);

        bridge.Dispatch(root, IdleFrame(oX, y), TimeSpan.FromSeconds(1));
        (float dX, _) = CharacterPoint(textBox, 10, fraction: 0.8f);
        Click(root, bridge, dX, y);
        Click(root, bridge, dX, y);

        Assert.Equal(6, textBox.Selection.Start);
        Assert.Equal(11, textBox.Selection.End);
    }

    [Fact]
    public void DoubleClickPastTheEndOfALineSelectsTheWordBeforeIt()
    {
        TextBox textBox = NewTextBox("one two\nthree");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float x, float y) = CharacterPoint(textBox, 6, fraction: 1f);
        x += 30;
        Click(root, bridge, x, y);
        Click(root, bridge, x, y);

        Assert.Equal(4, textBox.Selection.Start);
        Assert.Equal(7, textBox.Selection.End);
    }

    [Fact]
    public void TripleClickSelectsTheTextLineWithoutItsBreak()
    {
        TextBox textBox = NewTextBox("one two\nthree");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float x, float y) = CharacterPoint(textBox, 1);
        Click(root, bridge, x, y);
        Click(root, bridge, x, y);
        Click(root, bridge, x, y);

        Assert.Equal(0, textBox.Selection.Start);
        Assert.Equal(7, textBox.Selection.End);
    }

    [Fact]
    public void DragAfterDoubleClickExtendsByWholeWords()
    {
        TextBox textBox = NewTextBox("hello brave world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float helloX, float y) = CharacterPoint(textBox, 2);
        (float braveX, _) = CharacterPoint(textBox, 8);
        Click(root, bridge, helloX, y);
        Press(root, bridge, helloX, y);
        Move(root, bridge, helloX, braveX, y);
        Release(root, bridge, braveX, y);

        Assert.Equal(0, textBox.Selection.Anchor);
        Assert.Equal(12, textBox.Selection.Active);
    }

    [Fact]
    public void BackwardDragAfterDoubleClickExtendsToTheWordUnderThePointer()
    {
        TextBox textBox = NewTextBox("hello brave world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float worldX, float y) = CharacterPoint(textBox, 14);
        (float oX, _) = CharacterPoint(textBox, 4, fraction: 0.8f);
        Click(root, bridge, worldX, y);
        Press(root, bridge, worldX, y);
        Move(root, bridge, worldX, oX, y);
        Release(root, bridge, oX, y);

        Assert.Equal(17, textBox.Selection.Anchor);
        Assert.Equal(0, textBox.Selection.Active);
    }

    [Fact]
    public void DragAfterTripleClickExtendsByWholeLines()
    {
        TextBox textBox = NewTextBox("aa\nbb\ncc");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);

        (float x, float firstY) = CharacterPoint(textBox, 1);
        (_, float secondY) = CharacterPoint(textBox, 4, line: 1);
        Click(root, bridge, x, firstY);
        Click(root, bridge, x, firstY);
        Press(root, bridge, x, firstY);
        MoveVertically(root, bridge, x, firstY, secondY);
        Release(root, bridge, x, secondY);

        Assert.Equal(0, textBox.Selection.Anchor);
        Assert.Equal(5, textBox.Selection.Active);
    }

    [Fact]
    public void SelectedTextGetsTheSelectionAndSetReplacesItAsOneUndoUnit()
    {
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard: null, out _);
        textBox.Select(6, 11);

        Assert.Equal("world", textBox.SelectedText);

        textBox.SelectedText = "there";

        Assert.Equal("hello there", textBox.Text);
        Assert.True(textBox.Undo());
        Assert.Equal("hello world", textBox.Text);
        GC.KeepAlive(root);
    }

    [Fact]
    public void SelectedTextSetSelectsTheNewTextAndCtrlZRestoresTheOldTextAndSelection()
    {
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard: null, out ElementInputBridge bridge);
        textBox.Select(6, 11);

        textBox.SelectedText = "xyz";

        Assert.Equal("hello xyz", textBox.Text);
        Assert.Equal(6, textBox.Selection.Start);
        Assert.Equal(9, textBox.Selection.End);
        Assert.Equal("xyz", textBox.SelectedText);

        bridge.Dispatch(root, KeyFrame(InputKey.Z, InputKey.LeftCtrl));

        Assert.Equal("hello world", textBox.Text);
        Assert.Equal(6, textBox.Selection.Start);
        Assert.Equal(11, textBox.Selection.End);
        Assert.False(textBox.CanUndo);
    }

    [Fact]
    public void CopyCutAndPasteMethodsUseThePlatformClipboard()
    {
        FakeClipboard clipboard = new();
        TextBox textBox = NewTextBox("hello world");
        UIRoot root = Host(textBox, clipboard, out _);

        textBox.Select(0, 5);
        textBox.Copy();
        Assert.Equal("hello", clipboard.Text);
        Assert.Equal("hello world", textBox.Text);

        textBox.Select(5, 11);
        textBox.Cut();
        Assert.Equal(" world", clipboard.Text);
        Assert.Equal("hello", textBox.Text);

        textBox.MoveCaret(0);
        textBox.Paste();
        Assert.Equal(" worldhello", textBox.Text);

        textBox.IsReadOnly = true;
        textBox.Select(0, 6);
        textBox.Cut();
        textBox.Paste();
        Assert.Equal(" worldhello", textBox.Text);
        GC.KeepAlive(root);
    }

    [Fact]
    public void PasswordBoxCtrlCAndCtrlXLeaveTheClipboardAndPasswordUnchanged()
    {
        FakeClipboard clipboard = new("keep");
        PasswordBox passwordBox = new() { Password = "secret", Width = 200, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(passwordBox, clipboard, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.A, InputKey.LeftCtrl));
        bridge.Dispatch(root, KeyFrame(InputKey.C, InputKey.LeftCtrl));
        Assert.Equal("keep", clipboard.Text);

        bridge.Dispatch(root, KeyFrame(InputKey.X, InputKey.LeftCtrl));
        Assert.Equal("keep", clipboard.Text);
        Assert.Equal("secret", passwordBox.Password);
    }

    private static TextBox NewTextBox(string text = "")
    {
        return new TextBox { Text = text, Width = 200, VerticalAlignment = VerticalAlignment.Top };
    }

    private static UIRoot Host(Control control, IClipboard? clipboard, out ElementInputBridge bridge, params Control[] others)
    {
        UIRoot root = new(300, 200);
        root.SetPlatformServices(new FakePlatformServices(clipboard));
        root.VisualChildren.Add(control);
        foreach (Control other in others)
        {
            root.VisualChildren.Add(other);
        }

        root.ProcessFrame();
        bridge = new ElementInputBridge();
        Assert.True(bridge.FocusManager.Focus(control, root.InputCache.EnsureCurrent(root)));
        root.ProcessFrame();
        return root;
    }

    // The point at fraction of the width of the character at textIndex (0.5 is its middle), on the given drawn line of an
    // unscrolled text box.
    private static (float X, float Y) CharacterPoint(TextBox textBox, int textIndex, int line = 0, float fraction = 0.5f)
    {
        int lineStart = 0;
        for (int i = 0; i < line; i++)
        {
            lineStart += textBox.GetLineText(i).Length;
        }

        TextAspect aspect = new(textBox.FontFamily, textBox.FontSize);
        string prefix = textBox.Text[lineStart..textIndex];
        float before = TextMeasurer.Default.Measure(prefix, aspect, float.PositiveInfinity).Size.Width;
        float character = TextMeasurer.Default.Measure(textBox.Text[textIndex].ToString(), aspect, float.PositiveInfinity).Size.Width;
        float lineHeight = TextMeasurer.Default.Measure(string.Empty, aspect, float.PositiveInfinity).Size.Height;
        LayoutRect view = textBox.TextView.ArrangedBounds;
        return (view.X + before + (character * fraction), view.Y + ((line + 0.5f) * lineHeight));
    }

    private static void Click(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        Press(root, bridge, x, y);
        Release(root, bridge, x, y);
    }

    private static void Press(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        PointerSnapshot up = PointerSnapshot.Empty.WithPosition(x, y);
        bridge.Dispatch(root, new InputFrame(up, up.WithButton(InputMouseButton.Left, true), KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    private static void Release(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        PointerSnapshot up = PointerSnapshot.Empty.WithPosition(x, y);
        bridge.Dispatch(root, new InputFrame(up.WithButton(InputMouseButton.Left, true), up, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    private static void Move(UIRoot root, ElementInputBridge bridge, float fromX, float toX, float y)
    {
        PointerSnapshot from = PointerSnapshot.Empty.WithPosition(fromX, y).WithButton(InputMouseButton.Left, true);
        PointerSnapshot to = PointerSnapshot.Empty.WithPosition(toX, y).WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(from, to, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    private static void MoveVertically(UIRoot root, ElementInputBridge bridge, float x, float fromY, float toY)
    {
        PointerSnapshot from = PointerSnapshot.Empty.WithPosition(x, fromY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot to = PointerSnapshot.Empty.WithPosition(x, toY).WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(from, to, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    private static InputFrame IdleFrame(float x, float y)
    {
        PointerSnapshot pointer = PointerSnapshot.Empty.WithPosition(x, y);
        return new InputFrame(pointer, pointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame TextFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
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
