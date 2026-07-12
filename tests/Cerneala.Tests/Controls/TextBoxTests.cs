using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxTests
{
    [Fact]
    public void TextInputBridgeUpdatesFocusedTextBoxText()
    {
        UIRoot root = new(200, 80);
        TextBox textBox = new();
        root.VisualChildren.Add(textBox);
        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        focusManager.Focus(textBox, routeMap);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], focusManager, routeMap);

        Assert.Equal("a", textBox.Text);
        Assert.True(textBox.DirtyState.Has(Cerneala.UI.Invalidation.InvalidationFlags.Render));
    }

    [Fact]
    public void TextBoxUndoRestoresPreviousText()
    {
        TextBox textBox = new() { Text = "ab" };
        textBox.ReceiveTextInput("c");

        Assert.True(textBox.Undo());

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void TextBoxUndoDoesNotClearProgrammaticInitialText()
    {
        TextBox textBox = new() { Text = "ab" };
        textBox.ReceiveTextInput("c");

        Assert.True(textBox.Undo());
        Assert.False(textBox.Undo());

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void BackspaceKeyDeletesTextWithoutInsertingTextInputControlCharacter()
    {
        UIRoot root = RootWithFocusedTextBox("ab", out TextBox textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyboardAndTextInputFrame(InputKey.Back, "\b"));

        Assert.Equal("a", textBox.Text);
        Assert.Equal(1, textBox.Caret.Position);
    }

    [Fact]
    public void DeleteKeyDeletesTextWithoutInsertingTextInputControlCharacter()
    {
        UIRoot root = RootWithFocusedTextBox("ab", out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(0);

        bridge.Dispatch(root, KeyboardAndTextInputFrame(InputKey.Delete, "\u007f"));

        Assert.Equal("b", textBox.Text);
        Assert.Equal(0, textBox.Caret.Position);
    }

    [Fact]
    public void ArrowKeysMoveCaretAcrossTextElements()
    {
        string face = "\U0001F600";
        UIRoot root = RootWithFocusedTextBox("a" + face + "b", out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(1 + face.Length);

        bridge.Dispatch(root, KeyboardFrame(InputKey.Left));

        Assert.Equal(1, textBox.Caret.Position);

        bridge.Dispatch(root, KeyboardFrame(InputKey.Right));

        Assert.Equal(1 + face.Length, textBox.Caret.Position);
    }

    [Fact]
    public void MouseDownInsideTextBoxMovesCaretToNearestCharacter()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 220, out TextBox textBox);
        bool rootSawMouseDown = false;
        root.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => rootSawMouseDown = true);
        float stop2 = TextCaretLayout.Default.GetCaretX(textBox.Text, 2, CreateTextAspect(textBox), new FontResolver(textBox.ResourceProvider!));
        float stop3 = TextCaretLayout.Default.GetCaretX(textBox.Text, 3, CreateTextAspect(textBox), new FontResolver(textBox.ResourceProvider!));
        float clickX = ContentX(textBox) + stop2 + ((stop3 - stop2) * 0.75f);

        Click(root, clickX, 10);

        Assert.True(textBox.IsKeyboardFocused);
        Assert.Equal(3, textBox.Caret.Position);
        Assert.True(textBox.Selection.IsEmpty);
        Assert.False(rootSawMouseDown);
    }

    [Fact]
    public void MouseDownUsesHorizontalTextOffset()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 56, out TextBox textBox);
        textBox.MoveCaret(textBox.Text.Length);
        float fullWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Text.Length, CreateTextAspect(textBox), new FontResolver(textBox.ResourceProvider!));
        float horizontalOffset = MathF.Max(0, fullWidth - ContentWidth(textBox));
        int expectedIndex = TextCaretLayout.Default.GetCaretIndexAtX(
            textBox.Text,
            2 + horizontalOffset,
            CreateTextAspect(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Click(root, ContentX(textBox) + 2, 10);

        Assert.True(expectedIndex > 0);
        Assert.Equal(expectedIndex, textBox.Caret.Position);
    }

    [Fact]
    public void MouseDownOutsideTextContentClampsCaret()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 220, out TextBox textBox);

        Click(root, ContentX(textBox) - 2, 10);

        Assert.Equal(0, textBox.Caret.Position);

        Click(root, ContentX(textBox) + ContentWidth(textBox) + 2, 10);

        Assert.Equal(textBox.Text.Length, textBox.Caret.Position);
    }

    [Fact]
    public void MouseDragInsideTextBoxSelectsForwardRange()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 220, out TextBox textBox);
        float startX = ContentX(textBox) + CaretX(textBox, 1);
        float endX = ContentX(textBox) + CaretX(textBox, 4);

        Drag(root, startX, endX, 10);

        Assert.Equal(1, textBox.Selection.Anchor);
        Assert.Equal(4, textBox.Selection.Active);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(4, textBox.Selection.End);
        Assert.Equal(4, textBox.Caret.Position);
    }

    [Fact]
    public void MouseDragInsideTextBoxSelectsBackwardRange()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 220, out TextBox textBox);
        float startX = ContentX(textBox) + CaretX(textBox, 4);
        float endX = ContentX(textBox) + CaretX(textBox, 1);

        Drag(root, startX, endX, 10);

        Assert.Equal(4, textBox.Selection.Anchor);
        Assert.Equal(1, textBox.Selection.Active);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(4, textBox.Selection.End);
        Assert.Equal(1, textBox.Caret.Position);
    }

    [Fact]
    public void MouseReleaseOutsideTextBoxEndsCapturedSelectionDrag()
    {
        UIRoot root = RootWithTextBox("iiiiWWWW", 220, out TextBox textBox);
        ElementInputBridge bridge = new();
        float startX = ContentX(textBox) + CaretX(textBox, 1);
        float outsideX = textBox.ArrangedBounds.X + textBox.ArrangedBounds.Width + 20;

        bridge.Dispatch(root, PointerFrame(startX, 10, pressed: true));
        Assert.Same(textBox, bridge.PointerCaptureManager.CapturedElement);

        bridge.Dispatch(root, PointerFrame(startX, 10, outsideX, 10, previousDown: true, currentDown: true));
        Assert.Same(textBox, bridge.PointerCaptureManager.CapturedElement);

        bridge.Dispatch(root, PointerFrame(outsideX, 10, outsideX, 10, previousDown: true));

        TextSelection selectionAfterRelease = textBox.Selection;
        Assert.False(bridge.PointerCaptureManager.HasCapture);

        bridge.Dispatch(root, PointerFrame(outsideX, 10, startX, 10));

        Assert.Equal(selectionAfterRelease, textBox.Selection);
    }

    [Fact]
    public void ShiftRightExtendsSelectionFromCaret()
    {
        UIRoot root = RootWithFocusedTextBox("abcd", out TextBox textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(1);

        bridge.Dispatch(root, KeyboardFrame(InputKey.Right, InputKey.LeftShift));

        Assert.Equal(1, textBox.Selection.Anchor);
        Assert.Equal(2, textBox.Selection.Active);
        Assert.Equal(1, textBox.Selection.Start);
        Assert.Equal(2, textBox.Selection.End);
        Assert.Equal(2, textBox.Caret.Position);
    }

    private static UIRoot RootWithFocusedTextBox(string text, out TextBox textBox, out ElementInputBridge bridge)
    {
        UIRoot root = new(200, 80);
        textBox = new() { Text = text };
        root.VisualChildren.Add(textBox);
        bridge = new ElementInputBridge();
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        bridge.FocusManager.Focus(textBox, routeMap);
        return root;
    }

    private static UIRoot RootWithTextBox(string text, float width, out TextBox textBox)
    {
        ResourceStore store = new();
        ResourceId<FontResource> fontId = new("Input");
        store.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 20)));
        UIRoot root = new((int)MathF.Ceiling(width), 80);
        textBox = new()
        {
            Text = text,
            FontSize = 20,
            FontResourceId = fontId,
            ResourceProvider = store
        };
        textBox.Measure(new MeasureContext(new LayoutSize(width, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 32)));
        root.VisualChildren.Add(textBox);
        return root;
    }

    private static void Click(UIRoot root, float x, float y)
    {
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, pressed: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
    }

    private static void Drag(UIRoot root, float startX, float endX, float y)
    {
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(startX, y, pressed: true));
        bridge.Dispatch(root, PointerFrame(startX, y, endX, y, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(endX, y, endX, y, previousDown: true));
    }

    private static InputFrame PointerFrame(float x, float y, bool pressed = false, bool previousDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (pressed)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
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

    private static InputFrame KeyboardAndTextInputFrame(InputKey key, string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            [new TextInputSnapshotEvent(text)]);
    }

    private static InputFrame KeyboardFrame(InputKey key, params InputKey[] modifiers)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(modifiers),
            KeyboardSnapshot.FromDownKeys([.. modifiers, key]),
            []);
    }

    private static TextAspect CreateTextAspect(TextBox textBox)
    {
        return new TextAspect(textBox.FontFamily, textBox.FontSize, foreground: textBox.Foreground, fontResourceId: textBox.FontResourceId);
    }

    private static float ContentX(TextBox textBox)
    {
        return textBox.ArrangedBounds.X + textBox.BorderThickness.Left + textBox.Padding.Left;
    }

    private static float ContentWidth(TextBox textBox)
    {
        return textBox.ArrangedBounds.Width - textBox.BorderThickness.Left - textBox.Padding.Left - textBox.BorderThickness.Right - textBox.Padding.Right;
    }

    private static float CaretX(TextBox textBox, int position)
    {
        return TextCaretLayout.Default.GetCaretX(
            textBox.Text,
            position,
            CreateTextAspect(textBox),
            new FontResolver(textBox.ResourceProvider!));
    }
}
