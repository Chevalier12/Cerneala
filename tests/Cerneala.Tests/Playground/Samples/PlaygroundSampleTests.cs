using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Playground.Samples;

public sealed class PlaygroundSampleTests
{
    [Fact]
    public void ButtonSampleBuildsRetainedControls()
    {
        UIElement root = new RetainedButtonSample().Build();

        Assert.Contains(DescendantsAndSelf<StackPanel>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<TextBlock>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<Button>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<Border>(root), _ => true);
    }

    [Fact]
    public void LayoutAndTextSamplesUseRetainedControls()
    {
        UIElement layout = new LayoutSample().Build();
        UIElement text = new TextSample().Build();

        Assert.Contains(DescendantsAndSelf<StackPanel>(layout), _ => true);
        Assert.Contains(DescendantsAndSelf<Border>(layout), _ => true);
        Assert.Contains(DescendantsAndSelf<TextBlock>(text), _ => true);
        Assert.Contains(DescendantsAndSelf<Border>(text), _ => true);
    }

    [Fact]
    public void SelectorExposesDefaultSamplesAndSwitchesActiveRetainedElement()
    {
        SampleSelector selector = SampleSelector.CreateDefault();
        UIElement? initial = selector.ActiveElement;

        Assert.Equal(new[] { "Retained App", "Button", "Layout", "Text", "Diagnostics", "Runtime Preview", "Authoring App", "Getting Started" }, selector.Samples.Select(sample => sample.Name));

        selector.SelectSample(1);

        Assert.Equal(1, selector.ActiveIndex);
        Assert.Equal("Button", selector.ActiveSample.Name);
        Assert.NotNull(selector.ActiveElement);
        Assert.NotSame(initial, selector.ActiveElement);
    }

    [Fact]
    public void SelectorPassesImageResourceToPreviewSamplesWhenProvided()
    {
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Playground/PreviewImage");
        resources.SetResource(imageId, new ImageResource(new TestImage(2, 2)));
        SampleSelector selector = SampleSelector.CreateDefault(resources, null, imageId);

        Image retainedImage = DescendantsAndSelf<Image>(selector.ActiveElement!).Single();
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Runtime Preview"));
        Image runtimeImage = DescendantsAndSelf<Image>(selector.ActiveElement!).Single();

        Assert.Equal(imageId, retainedImage.SourceResourceId);
        Assert.Same(resources, retainedImage.ResourceProvider);
        Assert.Equal(imageId, runtimeImage.SourceResourceId);
        Assert.Same(resources, runtimeImage.ResourceProvider);
    }

    [Fact]
    public void DiagnosticsSampleBuildsRetainedDebugUi()
    {
        UIElement root = new DiagnosticsSample().Build();

        Assert.Contains(DescendantsAndSelf<TextBlock>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<DebugAdorner>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<Button>(root), _ => true);
    }

    [Fact]
    public void SelectorSamplesCannotMutateSelectorState()
    {
        SampleSelector selector = SampleSelector.CreateDefault();
        int originalCount = selector.Samples.Count;

        IList<IPlaygroundSample> mutableSamples = Assert.IsAssignableFrom<IList<IPlaygroundSample>>(selector.Samples);

        Assert.True(mutableSamples.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableSamples.Clear());

        Assert.Equal(originalCount, selector.Samples.Count);
        Assert.Equal("Retained App", selector.ActiveSample.Name);
    }

    [Fact]
    public void SelectorButtonCommandsSwitchActiveSample()
    {
        SampleSelector selector = SampleSelector.CreateDefault();
        Button layoutButton = ButtonWithText(selector.Root, "Layout");

        layoutButton.Command?.Execute(null);

        Assert.Equal("Layout", selector.ActiveSample.Name);
    }

    [Fact]
    public void SelectorButtonClickSwitchesActiveSample()
    {
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        selector.Root.Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest | InvalidationFlags.Subtree,
            "Initial selector test frame");
        root.ProcessFrame();
        Button layoutButton = ButtonWithText(selector.Root, "Layout");
        float x = layoutButton.ArrangedBounds.X + (layoutButton.ArrangedBounds.Width / 2);
        float y = layoutButton.ArrangedBounds.Y + (layoutButton.ArrangedBounds.Height / 2);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));

        Assert.Equal("Layout", selector.ActiveSample.Name);
    }

    [Fact]
    public void SelectorPlacesFrameStatsBeforeActiveSampleContent()
    {
        UIRoot root = new(1000, 640);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        selector.Root.Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree,
            "Initial selector layout test frame");
        root.ProcessFrame();
        Border stats = StatsOverlayBorder(selector.Root);

        float statsBottom = stats.ArrangedBounds.Y + stats.ArrangedBounds.Height;

        Assert.NotNull(selector.ActiveElement);
        Assert.True(
            statsBottom <= selector.ActiveElement!.ArrangedBounds.Y,
            $"Expected frame stats bottom {statsBottom} to be before active sample Y {selector.ActiveElement.ArrangedBounds.Y}.");
    }

    [Fact]
    public void RetainedAppSelectionKeepsRowsScrollViewerInsideCompactViewport()
    {
        UIRoot root = new(800, 360);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);

        root.ProcessFrame();

        ScrollViewer rows = DescendantsAndSelf<ScrollViewer>(selector.ActiveElement!).Single();
        float rowsBottom = rows.ArrangedBounds.Y + rows.ArrangedBounds.Height;

        Assert.True(
            rowsBottom <= 360,
            $"Expected retained rows ScrollViewer bottom {rowsBottom} to fit inside the 360px viewport.");
        Assert.True(
            rows.ScrollInfo.ExtentHeight > rows.ScrollInfo.ViewportHeight,
            "Expected retained rows to remain scrollable inside the compact viewport.");
    }

    [Fact]
    public void PlaygroundSamplesUseSkiaFontsWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.Root.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Initial playground sample test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void GettingStartedSelectionUsesSkiaFontsWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Getting Started"));
        selector.Root.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Initial getting started sample test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void RuntimePreviewSelectionUsesSkiaFontsWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Runtime Preview"));
        selector.Root.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Initial runtime preview sample test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void RuntimePreviewTextBoxCaretUsesRasterInkVerticalMetrics()
    {
        ResourceStore resources = CreateFontResources(out ResourceId<FontResource> fontId);
        UIRoot root = new(1000, 600);
        RuntimePreviewSample sample = new(resources, fontId);
        root.VisualChildren.Add(sample.Build());
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);
        TextBox textBox = sample.InputTextBox!;
        textBox.CaretColor = new DrawColor(11, 22, 33);
        textBox.IsKeyboardFocused = true;
        textBox.MoveCaret(textBox.Text.Length);

        host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);
        DrawCommand caret = CaretCommand(root, textBox.CaretColor);
        TextCaretVerticalMetrics metrics = TextCaretLayout.Default.GetCaretVerticalMetrics(
            CreateTextStyle(textBox),
            new FontResolver(textBox.ResourceProvider!));
        float contentY = textBox.ArrangedBounds.Y + textBox.BorderThickness.Top + textBox.Padding.Top;

        Assert.Equal(contentY + metrics.OffsetY, caret.Rect.Y, precision: 2);
        Assert.Equal(contentY, caret.Rect.Y, precision: 2);
        Assert.Equal(metrics.Height, caret.Rect.Height, precision: 2);
    }

    [Fact]
    public void AuthoringAppSelectionUsesSkiaFontsAfterTextInputWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Authoring App"));
        root.ProcessFrame();
        AuthoringAppSample sample = Assert.IsType<AuthoringAppSample>(selector.ActiveSample);

        sample.NameTextBox!.ReceiveTextInput("Ada");
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void AuthoringAppTextInputRendersVisibleAlignedCaretWithSkiaFont()
    {
        ResourceStore resources = CreateFontResources(out ResourceId<FontResource> fontId);
        UIRoot root = new(800, 600);
        AuthoringAppSample sample = new(resources, fontId);
        root.VisualChildren.Add(sample.Build());
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        TextBox textBox = sample.NameTextBox!;
        textBox.CaretColor = new DrawColor(9, 210, 160);
        textBox.IsKeyboardFocused = true;

        textBox.ReceiveTextInput("hahahehe");
        Exception? exception = Record.Exception(() => host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero));
        DrawCommand caret = CaretCommand(root, textBox.CaretColor);
        float expectedX = ContentX(textBox) + TextCaretLayout.Default.GetCaretX(
            textBox.Text,
            textBox.Caret.Position,
            CreateTextStyle(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Null(exception);
        Assert.InRange(caret.Rect.X, ContentX(textBox), ContentX(textBox) + ContentWidth(textBox));
        Assert.Equal(expectedX, caret.Rect.X, precision: 2);
    }

    [Fact]
    public void GettingStartedSelectionUsesSkiaFontsAfterTextInputWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Getting Started"));
        root.ProcessFrame();
        GettingStartedSample sample = Assert.IsType<GettingStartedSample>(selector.ActiveSample);

        sample.EntryTextBox!.ReceiveTextInput("Ada");
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void GettingStartedTextInputRendersVisibleCaretWithSkiaFont()
    {
        ResourceStore resources = CreateFontResources(out ResourceId<FontResource> fontId);
        UIRoot root = new(800, 600);
        GettingStartedSample sample = new(resources, fontId);
        root.VisualChildren.Add(sample.Build());
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        TextBox textBox = sample.EntryTextBox!;
        textBox.CaretColor = new DrawColor(120, 20, 240);
        textBox.IsKeyboardFocused = true;

        textBox.ReceiveTextInput("Ada");
        Exception? exception = Record.Exception(() => host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero));

        Assert.Null(exception);
        Assert.Equal(1, root.RetainedRenderer.Render(root).Count(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == textBox.CaretColor));
    }

    [Fact]
    public void DiagnosticsSampleUsesSkiaFontsWhenFontResourceIsProvided()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        root.VisualChildren.Add(new DiagnosticsSample(resources, fontId).Build());
        root.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree, "Initial diagnostics sample test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void StatsOverlayMapsNoWorkFrameToRetainedText()
    {
        FrameStats stats = new();
        stats.CountNoWorkFrame();
        UiFrame frame = new(TimeSpan.FromMilliseconds(16), new UiViewport(800, 600), EmptyInputFrame(), stats);
        InvalidationStatsOverlay overlay = new();

        overlay.Update(frame);

        Assert.Contains("queuedMeasure=0", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("queuedArrange=0", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("measureCalls=0", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("arrangeCalls=0", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("renderCache=0", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("reusedCaches=1", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("noWork=1", overlay.Text, StringComparison.Ordinal);
        Assert.Contains(DescendantsAndSelf<TextBlock>(overlay.Root), _ => true);
    }

    [Fact]
    public void StatsOverlayRetainsLastTextWhenFrameIsUnavailable()
    {
        FrameStats stats = new();
        stats.CountNoWorkFrame();
        UiFrame frame = new(TimeSpan.FromMilliseconds(16), new UiViewport(800, 600), EmptyInputFrame(), stats);
        InvalidationStatsOverlay overlay = new();

        overlay.Update(frame);
        string lastFrameText = overlay.Text;
        overlay.Update(null);

        Assert.Equal(lastFrameText, overlay.Text);
    }

    [Fact]
    public void StatsOverlayUpdateDoesNotCreateRetainedWorkForNextFrame()
    {
        UIRoot root = new(1000, 600);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(1000, 600) });
        UiFrame first = host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);

        selector.UpdateFrame(first);
        UiFrame second = host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.MeasureCalls);
        Assert.Equal(0, second.Stats.ArrangeCalls);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
        Assert.Contains("queuedMeasure=", selector.StatsText, StringComparison.Ordinal);
    }

    [Fact]
    public void StatsOverlayWrapsRenderedTextInsideAvailableWidth()
    {
        UIRoot root = new(420, 320);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(420, 320) });
        UiFrame first = host.Update(EmptyInputFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        Border stats = StatsOverlayBorder(selector.Root);

        selector.UpdateFrame(first);
        host.Update(EmptyInputFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        DrawCommandList commands = root.RetainedRenderer.Render(root);
        DrawCommand[] statLines = commands
            .Where(command => command.Kind == DrawCommandKind.DrawText && IsFrameStatsText(command.Text!))
            .ToArray();
        float contentWidth = stats.ArrangedBounds.Width - stats.Padding.Left - stats.Padding.Right;

        Assert.True(statLines.Length > 1, "Frame stats should wrap onto multiple rendered text lines.");
        Assert.All(statLines, command => Assert.True(
            command.TextRun!.Size * 0.5f * command.Text!.Length <= contentWidth,
            $"Expected '{command.Text}' to fit inside stats overlay content width {contentWidth}."));
    }

    [Fact]
    public void RetainedAppListScrollBarStaysInsideViewport()
    {
        UIRoot root = new(1000, 600);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(1000, 600) });
        UiFrame first = host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);

        selector.UpdateFrame(first);
        host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);
        ScrollViewer scrollViewer = DescendantsAndSelf<ScrollViewer>(selector.ActiveElement!).Single();
        float scrollBarBottom = scrollViewer.VerticalScrollBar.ArrangedBounds.Y + scrollViewer.VerticalScrollBar.ArrangedBounds.Height;

        Assert.True(
            scrollBarBottom <= root.ViewportHeight,
            $"Expected retained app scrollbar bottom {scrollBarBottom} to stay inside viewport height {root.ViewportHeight}.");
    }

    [Fact]
    public void UnchangedRootFramesReportNoRetainedRegeneration()
    {
        UIRoot root = new(800, 600);
        root.VisualChildren.Add(new RetainedButtonSample().Build());
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    private static InputFrame EmptyInputFrame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            Array.Empty<TextInputSnapshotEvent>());
    }

    private static ResourceStore CreateFontResources(out ResourceId<FontResource> fontId)
    {
        ResourceStore resources = new();
        fontId = new ResourceId<FontResource>("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        return resources;
    }

    private static DrawCommand CaretCommand(UIRoot root, DrawColor caretColor)
    {
        return root.RetainedRenderer.Render(root).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == caretColor);
    }

    private static TextRunStyle CreateTextStyle(TextBox textBox)
    {
        return new TextRunStyle(textBox.FontFamily, textBox.FontSize, color: textBox.Foreground, fontResourceId: textBox.FontResourceId);
    }

    private static float ContentX(TextBox textBox)
    {
        return textBox.ArrangedBounds.X + textBox.BorderThickness.Left + textBox.Padding.Left;
    }

    private static float ContentWidth(TextBox textBox)
    {
        return textBox.ArrangedBounds.Width - textBox.BorderThickness.Left - textBox.Padding.Left - textBox.BorderThickness.Right - textBox.Padding.Right;
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
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

    private static IEnumerable<T> DescendantsAndSelf<T>(UIElement element)
        where T : UIElement
    {
        if (element is T match)
        {
            yield return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            foreach (T descendant in DescendantsAndSelf<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static Button ButtonWithText(UIElement root, string text)
    {
        return DescendantsAndSelf<Button>(root)
            .Single(button => button.Content is TextBlock textBlock && textBlock.Text == text);
    }

    private static Border StatsOverlayBorder(UIElement root)
    {
        return DescendantsAndSelf<Border>(root)
            .Single(border =>
                border.Child is TextBlock &&
                border.Background == new DrawColor(24, 28, 36, 230) &&
                border.BorderColor == new DrawColor(74, 86, 104));
    }

    private static bool IsFrameStatsText(string text)
    {
        return text.StartsWith("Frame stats:", StringComparison.Ordinal) ||
            text.Contains("queuedMeasure=", StringComparison.Ordinal) ||
            text.Contains("measureCalls=", StringComparison.Ordinal) ||
            text.Contains("renderCache=", StringComparison.Ordinal) ||
            text.Contains("noWork=", StringComparison.Ordinal);
    }

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }
}
