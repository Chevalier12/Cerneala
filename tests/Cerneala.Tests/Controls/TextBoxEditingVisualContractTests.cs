using Cerneala.Drawing;
using Cerneala.UI.Hosting;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxEditingVisualContractTests
{
    private static readonly Color CaretColor = new(250, 10, 20);
    private static readonly Color SelectionColor = new(20, 120, 250);

    [Fact]
    public void FocusedTextBoxRendersCaretCommand()
    {
        TextBox textBox = ArrangedTextBox("abc");
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(1);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));

        Assert.Equal(CaretColor, Assert.IsType<SolidColorBrush>(caret.Brush).Color);
        Assert.True(caret.Rect.Height > 0);
        Assert.True(caret.Rect.Width > 0);
    }

    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void CaretRemainsOnePhysicalPixelWideAtEveryTextPosition(float scale)
    {
        UIRoot root = new(240, 80, scale);
        TextBox textBox = new()
        {
            Text = "Editable semantic TextBox",
            IsKeyboardFocused = true,
            CaretBrush = new SolidColorBrush(CaretColor)
        };
        root.VisualChildren.Add(textBox);
        textBox.Measure(new MeasureContext(new LayoutSize(220, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 220, 32)));

        for (int position = 0; position <= textBox.Text.Length; position++)
        {
            textBox.MoveCaret(position);
            DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));

            int left = UiCoordinateMapper.LogicalToPhysicalPixel(caret.Rect.X, scale);
            int right = UiCoordinateMapper.LogicalToPhysicalPixel(caret.Rect.X + caret.Rect.Width, scale);
            Assert.Equal(1, right - left);
        }
    }

    [Fact]
    public void CaretUsesTextLineVerticalBounds()
    {
        TextBox textBox = ArrangedSystemFontTextBox("a", width: 160, fontSize: 16);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));
        TextCaretVerticalMetrics expectedMetrics = TextCaretLayout.Default.GetCaretVerticalMetrics(
            CreateTextAspect(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Equal(ContentY(textBox) + expectedMetrics.OffsetY, caret.Rect.Y, precision: 2);
        Assert.Equal(ContentY(textBox), caret.Rect.Y, precision: 2);
        Assert.Equal(expectedMetrics.Height, caret.Rect.Height, precision: 2);
    }

    [Fact]
    public void UnfocusedTextBoxDoesNotRenderCaretCommand()
    {
        TextBox textBox = ArrangedTextBox("abc");
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(1);

        Assert.DoesNotContain(Render(textBox), command => HasSolidFillBrush(command, CaretColor));
    }

    [Fact]
    public void CaretMoveInvalidatesRenderWithoutMeasure()
    {
        TextBox textBox = ArrangedTextBox("abc");

        textBox.MoveCaret(1);

        Assert.True(textBox.TextView.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(textBox.TextView.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void CaretBrushChangeInvalidatesRenderWithoutMeasure()
    {
        TextBox textBox = ArrangedTextBox("abc");

        textBox.CaretBrush = new SolidColorBrush(CaretColor);

        Assert.True(textBox.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(textBox.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void SelectionRangeRendersHighlightBeforeText()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.SelectionBackground = SelectionColor;
        textBox.Select(1, 3);

        DrawCommand[] commands = Render(textBox).ToArray();
        int selectionIndex = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.FillRectangle && command.Color == SelectionColor);
        int normalTextIndex = Array.FindIndex(commands, command => HasSolidTextBrush(command, Color.Black));
        int selectedTextIndex = Array.FindIndex(commands, command => HasSolidTextBrush(command, Color.White));

        Assert.True(normalTextIndex < selectionIndex);
        Assert.True(selectionIndex < selectedTextIndex);
    }

    [Fact]
    public void SelectionHighlightUsesTextLineVerticalBounds()
    {
        TextBox textBox = ArrangedSystemFontTextBox("abcd", width: 160, fontSize: 16);
        textBox.SelectionBackground = SelectionColor;
        textBox.Select(1, 3);

        DrawCommand highlight = Render(textBox).Single(
            command => command.Kind == DrawCommandKind.FillRectangle && command.Color == SelectionColor);
        TextCaretVerticalMetrics expectedMetrics = TextCaretLayout.Default.GetCaretVerticalMetrics(
            CreateTextAspect(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Equal(ContentY(textBox) + expectedMetrics.OffsetY, highlight.Rect.Y, precision: 2);
        Assert.Equal(expectedMetrics.Height, highlight.Rect.Height, precision: 2);
        Assert.True(highlight.Rect.Height < ContentHeight(textBox));
    }

    [Fact]
    public void SelectionRangeRendersSelectedTextInWhiteAboveNormalText()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.Select(1, 3);

        DrawCommand[] textCommands = Render(textBox)
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .ToArray();

        Assert.Equal(2, textCommands.Length);
        Assert.True(HasSolidTextBrush(textCommands[0], Color.Black));
        Assert.True(HasSolidTextBrush(textCommands[1], Color.White));
    }

    [Fact]
    public void SelectionRangeMasksNormalTextBeforeSelectedTextPass()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.Select(1, 3);

        DrawCommand[] commands = Render(textBox).ToArray();
        int normalTextIndex = Array.FindIndex(commands, command => HasSolidTextBrush(command, Color.Black));
        int selectionIndex = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.FillRectangle && command.Color == textBox.SelectionBackground);
        int selectedTextIndex = Array.FindIndex(commands, command => HasSolidTextBrush(command, Color.White));

        Assert.True(normalTextIndex < selectionIndex);
        Assert.True(selectionIndex < selectedTextIndex);
    }

    [Fact]
    public void SelectionRangeRendersFullTextInWhiteInsideSelectionClip()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.Select(1, 3);

        DrawCommand[] commands = Render(textBox).ToArray();
        DrawCommand selectedText = Render(textBox)
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Single(command => HasSolidTextBrush(command, Color.White));
        int clipIndex = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.PushClip);
        int selectedTextIndex = Array.FindIndex(commands, command => HasSolidTextBrush(command, Color.White));

        Assert.Equal("abcd", selectedText.Text);
        Assert.True(clipIndex < selectedTextIndex);
    }

    [Fact]
    public void DefaultSelectionBackgroundIsOpaqueBlue()
    {
        TextBox textBox = new();

        Assert.Equal(new Color(0, 120, 215), textBox.SelectionBackground);
    }

    [Fact]
    public void SelectionChangeInvalidatesRenderWithoutMeasure()
    {
        TextBox textBox = ArrangedTextBox("abcd");

        textBox.Select(1, 3);

        Assert.True(textBox.TextView.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(textBox.TextView.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void ReplacingSelectedTextUpdatesTextAndClearsSelectionPredictably()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.Select(1, 3);

        textBox.ReceiveTextInput("X");

        Assert.Equal("aXd", textBox.Text);
        Assert.True(textBox.Selection.IsEmpty);
        Assert.Equal(2, textBox.Caret.Position);
    }

    [Fact]
    public void CaretScrollsIntoViewWhenTextExceedsContentWidth()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));

        Assert.InRange(caret.Rect.X, textBox.ArrangedBounds.X, textBox.ArrangedBounds.X + textBox.ArrangedBounds.Width);
    }

    // As before the template: the first frame after an edit draws the text scrolled so the caret is inside the box.
    [Fact]
    public void TypedCharacterScrollsCaretIntoViewInTheSameFrame()
    {
        TextBox textBox = new()
        {
            Text = "abcdefghijklmnop",
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CaretBrush = new SolidColorBrush(CaretColor)
        };
        UIRoot root = HostFocused(textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(textBox.Text.Length);
        root.ProcessFrame();
        root.ProcessFrame();

        bridge.Dispatch(root, TextFrame("q"));
        root.ProcessFrame();
        DrawCommandList commands = RenderOnce(textBox);

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        float fullWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Text.Length, CreateTextAspect(textBox), new FontResolver());
        // The extent ends one caret width (one device pixel) after the text, so the caret at the end is drawn inside.
        float expectedOffset = fullWidth + DeviceCaretWidth(textBox) - presenter.ViewportWidth;
        DrawCommand caret = commands.Single(command => HasSolidFillBrush(command, CaretColor));
        float textX = commands.Where(command => command.Kind == DrawCommandKind.DrawText).Min(command => command.Position.X);

        Assert.Equal("abcdefghijklmnopq", textBox.Text);
        Assert.True(expectedOffset > 0);
        Assert.InRange(caret.Rect.X, ContentX(textBox), ContentX(textBox) + ContentWidth(textBox));
        Assert.Equal(presenter.ArrangedBounds.X - expectedOffset, textX, precision: 2);
    }

    // As WPF's text view, whose extent includes the caret width: the caret at the end of text wider than the box is drawn
    // inside the clip of the content presenter, not on its right edge.
    [Fact]
    public void CaretAtEndOfOverflowingTextIsInsideThePresenterClip()
    {
        TextBox textBox = new()
        {
            Text = "abcdefghijklmnop",
            Width = 48,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CaretBrush = new SolidColorBrush(CaretColor)
        };
        UIRoot root = HostFocused(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, KeyFrame(InputKey.End));
        root.ProcessFrame();
        DrawCommandList commands = RenderOnce(textBox);

        LayoutRect clip = ContentHost(textBox).Presenter.ArrangedBounds;
        DrawCommand caret = commands.Single(command => HasSolidFillBrush(command, CaretColor));
        Assert.True(caret.Rect.X >= clip.X, $"Caret x {caret.Rect.X} is left of the clip {clip}.");
        Assert.True(
            caret.Rect.X + caret.Rect.Width <= clip.X + clip.Width && caret.Rect.X < clip.X + clip.Width,
            $"Caret {caret.Rect.X}..{caret.Rect.X + caret.Rect.Width} is outside the clip right edge {clip.X + clip.Width}.");
    }

    // A caret move right after the template changes reveals the caret in the new template's content host, after one frame.
    [Fact]
    public void CaretMoveAfterTemplateChangeScrollsTheNewContentHost()
    {
        TextBox textBox = new()
        {
            Text = "abcdefghijklmnop",
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CaretBrush = new SolidColorBrush(CaretColor)
        };
        UIRoot root = HostFocused(textBox, out ElementInputBridge bridge);
        bridge.Dispatch(root, KeyFrame(InputKey.Home));
        root.ProcessFrame();

        textBox.ComponentTemplate = new Cerneala.UI.Controls.Templates.ComponentTemplate<TextBox>(
            "TextBox.Copy",
            context =>
            {
                ScrollViewer contentHost = new();
                Border border = new() { Child = contentHost };
                context.RequirePart("PART_ContentHost", contentHost);
                context.Bind(Control.BorderThicknessProperty, border, Control.BorderThicknessProperty);
                context.Bind(Control.PaddingProperty, contentHost, UIElement.MarginProperty);
                return border;
            });
        bridge.Dispatch(root, KeyFrame(InputKey.End));
        root.ProcessFrame();
        DrawCommandList commands = RenderOnce(textBox);

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        LayoutRect clip = presenter.ArrangedBounds;
        DrawCommand caret = commands.Single(command => HasSolidFillBrush(command, CaretColor));
        Assert.True(presenter.HorizontalOffset > 0, $"Offset {presenter.HorizontalOffset}.");
        Assert.InRange(caret.Rect.X, clip.X, clip.X + clip.Width);
    }

    [Fact]
    public void TypedTextGrowsAutoWidthTextBoxInTheSameFrame()
    {
        TextBox textBox = new() { Text = "ab", HorizontalAlignment = HorizontalAlignment.Left };
        StackPanel panel = new();
        panel.Children.Add(textBox);
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(textBox, root.InputCache.EnsureCurrent(root)));
        root.ProcessFrame();
        textBox.MoveCaret(textBox.Text.Length);
        float before = textBox.ArrangedBounds.Width;

        bridge.Dispatch(root, TextFrame("cdefgh"));
        root.ProcessFrame();

        Assert.Equal("abcdefgh", textBox.Text);
        Assert.True(textBox.ArrangedBounds.Width > before, $"Width {textBox.ArrangedBounds.Width} did not grow from {before}.");
    }

    [Fact]
    public void EnterOnLastVisibleLineScrollsCaretIntoViewInTheSameFrame()
    {
        TextBox textBox = new()
        {
            AcceptsReturn = true,
            MaxLines = 3,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top,
            CaretBrush = new SolidColorBrush(CaretColor)
        };
        UIRoot root = HostFocused(textBox, out ElementInputBridge bridge);
        bridge.Dispatch(root, KeyFrame(InputKey.Enter));
        bridge.Dispatch(root, KeyFrame(InputKey.Enter));
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(3, textBox.LineCount);

        bridge.Dispatch(root, KeyFrame(InputKey.Enter));
        root.ProcessFrame();
        DrawCommandList commands = RenderOnce(textBox);

        LayoutRect viewport = ContentHost(textBox).Presenter.ArrangedBounds;
        DrawCommand caret = commands.Single(command => HasSolidFillBrush(command, CaretColor));

        Assert.Equal(4, textBox.LineCount);
        Assert.InRange(caret.Rect.Y, viewport.Y - 0.5f, viewport.Y + viewport.Height);
        Assert.InRange(caret.Rect.Y + caret.Rect.Height, viewport.Y, viewport.Y + viewport.Height + 0.5f);
    }

    [Fact]
    public void CaretAtEndUsesShapedTextMetrics()
    {
        TextBox textBox = ArrangedSystemFontTextBox("iiiiWWWW", width: 220);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));
        float expectedX = ContentX(textBox) + TextCaretLayout.Default.GetCaretX(
            textBox.Text,
            textBox.Text.Length,
            CreateTextAspect(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Equal(expectedX, caret.Rect.X, precision: 2);
    }

    [Fact]
    public void HorizontalViewportKeepsCaretAlignedToShapedMetrics()
    {
        TextBox textBox = ArrangedSystemFontTextBox("iiiiWWWW", width: 56);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);
        textBox.MoveCaret(textBox.Text.Length - 1);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));
        float contentX = ContentX(textBox);
        float contentWidth = ContentWidth(textBox);
        float fullWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Text.Length, CreateTextAspect(textBox), new FontResolver(textBox.ResourceProvider!));
        float caretWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Caret.Position, CreateTextAspect(textBox), new FontResolver(textBox.ResourceProvider!));
        float expectedX = contentX + caretWidth - MathF.Max(0, fullWidth + DeviceCaretWidth(textBox) - contentWidth);

        Assert.InRange(caret.Rect.X, contentX, contentX + contentWidth);
        Assert.Equal(expectedX, caret.Rect.X, precision: 2);
    }

    [Fact]
    public void BackspaceNearStartScrollsCaretBackIntoView()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);
        textBox.MoveCaret(1);

        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));

        Assert.InRange(caret.Rect.X, textBox.ArrangedBounds.X, textBox.ArrangedBounds.X + textBox.ArrangedBounds.Width);
    }

    [Fact]
    public void ProgrammaticTextResetResetsHorizontalViewportWhenCaretAtStart()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretBrush = new SolidColorBrush(CaretColor);
        textBox.MoveCaret(textBox.Text.Length);

        textBox.Text = "a";
        textBox.MoveCaret(0);
        DrawCommand caret = Render(textBox).Single(command => HasSolidFillBrush(command, CaretColor));

        Assert.True(caret.Rect.X <= textBox.ArrangedBounds.X + 6);
    }

    private static TextBox ArrangedTextBox(string text, float width = 160)
    {
        TextBox textBox = new() { Text = text };
        textBox.Measure(new MeasureContext(new LayoutSize(width, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 32)));
        textBox.DirtyState.ClearAll();
        textBox.TextView.DirtyState.ClearAll();
        return textBox;
    }

    private static TextBox ArrangedSystemFontTextBox(string text, float width, float fontSize = 20)
    {
        ResourceStore store = new();
        ResourceId<FontResource> fontId = new("Input");
        store.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", fontSize)));
        TextBox textBox = new()
        {
            Text = text,
            FontSize = fontSize,
            FontResourceId = fontId,
            ResourceProvider = store
        };
        textBox.Measure(new MeasureContext(new LayoutSize(width, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 32)));
        textBox.DirtyState.ClearAll();
        textBox.TextView.DirtyState.ClearAll();
        return textBox;
    }

    private static TextAspect CreateTextAspect(TextBox textBox)
    {
        return new TextAspect(textBox.FontFamily, textBox.FontSize, foreground: textBox.Foreground, fontResourceId: textBox.FontResourceId);
    }

    private static float ContentX(TextBox textBox)
    {
        return textBox.ArrangedBounds.X + Math.Max(0, -textBox.BorderThickness.Left) + textBox.Padding.Left;
    }

    private static float ContentWidth(TextBox textBox)
    {
        return textBox.ArrangedBounds.Width - Math.Max(0, -textBox.BorderThickness.Left) - textBox.Padding.Left - Math.Max(0, -textBox.BorderThickness.Right) - textBox.Padding.Right;
    }

    private static float ContentY(TextBox textBox)
    {
        return textBox.ArrangedBounds.Y + Math.Max(0, -textBox.BorderThickness.Top) + textBox.Padding.Top;
    }

    private static float ContentHeight(TextBox textBox)
    {
        return textBox.ArrangedBounds.Height - Math.Max(0, -textBox.BorderThickness.Top) - textBox.Padding.Top - Math.Max(0, -textBox.BorderThickness.Bottom) - textBox.Padding.Bottom;
    }

    // The text box draws through its template: the Border draws the chrome and the text view inside the content host
    // draws the text, the selection and the caret, so the commands are those of the whole subtree, in tree order. The
    // content host scrolls the text in layout, so the text box is laid out in a root, at the place it was arranged at,
    // as a frame lays it out before it draws.
    private static DrawCommandList Render(TextBox textBox)
    {
        UIRoot root = textBox.Root ?? HostAtArrangedBounds(textBox);
        root.ProcessFrame();
        DrawCommandList commands = new();
        RenderSubtree(textBox, new DrawingContext(commands));
        return commands;
    }

    // The commands of the last frame's layout, without another frame.
    private static DrawCommandList RenderOnce(TextBox textBox)
    {
        DrawCommandList commands = new();
        RenderSubtree(textBox, new DrawingContext(commands));
        return commands;
    }

    private static UIRoot HostFocused(TextBox textBox, out ElementInputBridge bridge)
    {
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(textBox);
        root.ProcessFrame();
        bridge = new ElementInputBridge();
        Assert.True(bridge.FocusManager.Focus(textBox, root.InputCache.EnsureCurrent(root)));
        root.ProcessFrame();
        return root;
    }

    // The caret is one device pixel wide, and the text view keeps that width free right of the text.
    private static float DeviceCaretWidth(TextBox textBox) => 1f / (textBox.Root?.Scale ?? 1f);

    private static ScrollViewer ContentHost(TextBox textBox)
    {
        return Assert.IsType<ScrollViewer>(textBox.ComponentTemplateInstance!.Parts["PART_ContentHost"]);
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

    private static InputFrame KeyFrame(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            []);
    }

    private static UIRoot HostAtArrangedBounds(TextBox textBox)
    {
        LayoutRect bounds = textBox.ArrangedBounds;
        UIRoot root = new(bounds.X + bounds.Width, bounds.Y + bounds.Height);
        root.VisualChildren.Add(textBox);
        return root;
    }

    private static void RenderSubtree(UIElement element, DrawingContext drawingContext)
    {
        if (!UIElementVisibility.ParticipatesInRendering(element))
        {
            return;
        }

        element.Render(new RenderContext(
            element,
            drawingContext,
            element.ArrangedBounds,
            RenderLayer.Default,
            new RenderCounters()));
        foreach (UIElement child in element.VisualChildren)
        {
            RenderSubtree(child, drawingContext);
        }
    }

    private static bool HasSolidTextBrush(DrawCommand command, Color color)
    {
        return command.Kind == DrawCommandKind.DrawText &&
            command.Brush is SolidColorBrush brush &&
            brush.Color == color;
    }

    private static bool HasSolidFillBrush(DrawCommand command, Color color)
    {
        return command.Kind == DrawCommandKind.FillRectangle &&
            command.Brush is SolidColorBrush brush &&
            brush.Color == color;
    }
}
