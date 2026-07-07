using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Playground.Samples;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
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

        Assert.Equal(
            new[]
            {
                "Retained App",
                "Button",
                "Layout",
                "Text",
                "Diagnostics",
                "Runtime Preview",
                "Authoring App",
                "Getting Started",
                "Motion",
                "Layout Motion",
                "Presence",
                "Scroll Motion"
            },
            selector.Samples.Select(sample => sample.Name));

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
    public void RetainedAppPreviewUsesBoundedUntintedImageSlot()
    {
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Playground/PreviewImage");
        resources.SetResource(imageId, new ImageResource(new TestImage(512, 512)));
        UIRoot root = new(1000, 600);
        RetainedAppSample sample = new(resources, null, imageId);
        root.SetResourceProvider(resources);
        root.VisualChildren.Add(sample.Build());
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(1000, 600) });

        host.Update(EmptyInputFrame(), new UiViewport(1000, 600), TimeSpan.Zero);
        DrawCommand preview = root.RetainedRenderer.Render(root).Single(command => command.Kind == DrawCommandKind.DrawImage);

        Assert.Equal(DrawColor.White, preview.Color);
        Assert.True(preview.Rect.Height <= 160, $"Expected preview height {preview.Rect.Height} to stay thumbnail-sized.");
        Assert.Equal(preview.Rect.Width, preview.Rect.Height, precision: 2);
    }

    [Fact]
    public void RetainedAppInteractionContentScrollsInsideSelectorViewport()
    {
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Playground/PreviewImage");
        resources.SetResource(imageId, new ImageResource(new TestImage(512, 512)));
        UIRoot root = new(1000, 480);
        SampleSelector selector = SampleSelector.CreateDefault(resources, null, imageId);
        root.VisualChildren.Add(selector.Root);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(1000, 480) });
        UiFrame first = host.Update(EmptyInputFrame(), new UiViewport(1000, 480), TimeSpan.Zero);

        selector.UpdateFrame(first);
        host.Update(EmptyInputFrame(), new UiViewport(1000, 480), TimeSpan.Zero);
        RetainedAppSample retainedSample = Assert.IsType<RetainedAppSample>(selector.ActiveSample);
        ScrollViewer interactionScrollViewer = DescendantsAndSelf<ScrollViewer>(selector.ActiveElement!)
            .Single(scrollViewer => scrollViewer.Content is StackPanel stackPanel &&
                stackPanel.VisualChildren.Contains(retainedSample.StatusText!));
        float scrollViewerBottom = interactionScrollViewer.ArrangedBounds.Y + interactionScrollViewer.ArrangedBounds.Height;

        Assert.True(
            interactionScrollViewer.ScrollInfo.ExtentHeight > interactionScrollViewer.ScrollInfo.ViewportHeight,
            "Expected the retained app interaction card to scroll overflowing preview, status, and button content.");
        Assert.Equal(ScrollBarVisibility.Auto, interactionScrollViewer.VerticalScrollBarVisibility);
        Assert.True(interactionScrollViewer.IsVerticalScrollBarVisible);
        Assert.True(
            scrollViewerBottom <= root.ViewportHeight,
            $"Expected interaction ScrollViewer bottom {scrollViewerBottom} to stay inside viewport height {root.ViewportHeight}.");
    }

    [Fact]
    public void RetainedAppAutoScrollBarStabilizesAfterCompactLayout()
    {
        ResourceStore resources = new();
        ResourceId<ImageResource> imageId = new("Playground/PreviewImage");
        resources.SetResource(imageId, new ImageResource(new TestImage(512, 512)));
        UIRoot root = new(1000, 480);
        SampleSelector selector = SampleSelector.CreateDefault(resources, null, imageId);
        root.VisualChildren.Add(selector.Root);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(1000, 480) });
        UiFrame first = host.Update(EmptyInputFrame(), new UiViewport(1000, 480), TimeSpan.Zero);

        selector.UpdateFrame(first);
        host.Update(EmptyInputFrame(), new UiViewport(1000, 480), TimeSpan.Zero);
        UiFrame settled = host.Update(EmptyInputFrame(), new UiViewport(1000, 480), TimeSpan.Zero);

        Assert.Equal(0, settled.Stats.MeasuredElements);
        Assert.Equal(0, settled.Stats.ArrangedElements);
        Assert.Equal(0, settled.Stats.MeasureCalls);
        Assert.Equal(0, settled.Stats.ArrangeCalls);
        Assert.Equal(1, settled.Stats.NoWorkFrames);
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
    public void MotionSampleBuildsExpectedMotionControls()
    {
        UIElement root = new MotionSample().Build();

        Button hover = ButtonWithText(root, "Hover color");
        Button press = ButtonWithText(root, "Press scale");
        Button animate = ButtonWithText(root, "Animate");
        Button cancel = ButtonWithText(root, "Cancel");
        Button restart = ButtonWithText(root, "Restart");

        Assert.NotNull(hover.Command);
        Assert.NotNull(press.Command);
        Assert.NotNull(animate.Command);
        Assert.NotNull(cancel.Command);
        Assert.NotNull(restart.Command);
        Assert.Contains(DescendantsAndSelf<Border>(root), border => border.Opacity < 1 || border.Scale != 1);
    }

    [Fact]
    public void MotionSampleAnimateCommandStartsOpacityMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement sampleRoot = new MotionSample().Build();
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();
        Button animate = ButtonWithText(sampleRoot, "Animate");
        Border target = DescendantsAndSelf<Border>(sampleRoot)
            .Single(border => border.Child is TextBlock textBlock && textBlock.Text == "Motion target");

        Exception? exception = Record.Exception(() => animate.Command!.Execute(null));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(90));
        root.ProcessFrame();

        Assert.Null(exception);
        Assert.InRange(target.Opacity, 0.35f, 0.88f);
        Assert.NotEqual(0.88f, target.Opacity);
    }

    [Fact]
    public void MotionSamplePressScaleCommandAnimatesScale()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement sampleRoot = new MotionSample().Build();
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();
        Button pressScale = ButtonWithText(sampleRoot, "Press scale");
        Border target = DescendantsAndSelf<Border>(sampleRoot)
            .Single(border => border.Child is TextBlock textBlock && textBlock.Text == "Motion target");

        Exception? exception = Record.Exception(() => pressScale.Command!.Execute(null));
        root.ProcessFrame();
        float startFrameScale = target.Scale;
        clock.Advance(TimeSpan.FromMilliseconds(90));
        root.ProcessFrame();

        Assert.Null(exception);
        Assert.Equal(0.98f, startFrameScale);
        Assert.InRange(target.Scale, 1f, 1.04f);
        Assert.NotEqual(1.04f, target.Scale);
    }

    [Fact]
    public void LayoutMotionSampleBuildsReorderExpandAndStatsTargets()
    {
        UIElement root = new LayoutMotionSample().Build();

        Assert.NotNull(ButtonWithText(root, "Reorder").Command);
        Assert.NotNull(ButtonWithText(root, "Expand").Command);
        Assert.Contains(DescendantsAndSelf<Border>(root), border => border.LayoutMotionId is not null);
        Assert.Contains(DescendantsAndSelf<TextBlock>(root), text => text.Text.Contains("measure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LayoutMotionSampleExpandTwiceKeepsRenderCachesValid()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(800, 600, motionClock: clock);
        UIElement sampleRoot = new LayoutMotionSample().Build();
        root.VisualChildren.Add(sampleRoot);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(800, 600) });
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        Button expand = ButtonWithText(sampleRoot, "Expand");

        Exception? firstExpand = Record.Exception(() =>
        {
            expand.Command!.Execute(null);
            host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        });
        clock.Advance(TimeSpan.FromMilliseconds(16));
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        Exception? secondExpand = Record.Exception(() =>
        {
            expand.Command!.Execute(null);
            host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        });

        Assert.Null(firstExpand);
        Assert.Null(secondExpand);
    }

    [Fact]
    public void LayoutMotionSampleExpandStartsFromCurrentVisualBounds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(800, 600, motionClock: clock);
        UIElement sampleRoot = new LayoutMotionSample().Build();
        root.VisualChildren.Add(sampleRoot);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(800, 600) });
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        Button expand = ButtonWithText(sampleRoot, "Expand");
        DrawRect initial = FillRectWithColor(root, new DrawColor(219, 234, 254));

        expand.Command!.Execute(null);
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        DrawRect firstExpandStart = FillRectWithColor(root, new DrawColor(219, 234, 254));
        clock.Advance(TimeSpan.FromMilliseconds(16));
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        clock.Advance(TimeSpan.FromMilliseconds(16));
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        DrawRect beforeInterruptedExpand = FillRectWithColor(root, new DrawColor(219, 234, 254));

        expand.Command!.Execute(null);
        host.Update(EmptyInputFrame(), new UiViewport(800, 600), TimeSpan.FromMilliseconds(16));
        DrawRect interruptedExpandStart = FillRectWithColor(root, new DrawColor(219, 234, 254));

        Assert.True(
            MathF.Abs(firstExpandStart.Y - initial.Y) <= 1,
            $"Expected first expand to start near Y={initial.Y}, but rendered at Y={firstExpandStart.Y}.");
        Assert.True(
            MathF.Abs(interruptedExpandStart.Y - beforeInterruptedExpand.Y) <= 1,
            $"Expected interrupted expand to start near Y={beforeInterruptedExpand.Y}, but rendered at Y={interruptedExpandStart.Y}.");
        Assert.True(
            MathF.Abs(interruptedExpandStart.Height - beforeInterruptedExpand.Height) <= 1,
            $"Expected interrupted expand to preserve height {beforeInterruptedExpand.Height}, but rendered height {interruptedExpandStart.Height}.");
    }

    [Fact]
    public void PresenceSampleBuildsAddRemoveAndReducedMotionToggle()
    {
        UIElement root = new PresenceMotionSample().Build();

        Assert.NotNull(ButtonWithText(root, "Add").Command);
        Assert.NotNull(ButtonWithText(root, "Remove").Command);
        Assert.NotNull(ButtonWithText(root, "Reduced motion").Command);
        Assert.Contains(DescendantsAndSelf<Border>(root), border => border.Presence is not null);
    }

    [Fact]
    public void ScrollMotionSampleBuildsHeaderParallaxAndProgressIndicator()
    {
        UIElement root = new ScrollMotionSample().Build();

        Assert.Contains(DescendantsAndSelf<ScrollViewer>(root), _ => true);
        Assert.NotNull(BorderWithText(root, "Header fade"));
        Assert.NotNull(BorderWithText(root, "Parallax"));
        Assert.NotNull(BorderWithBackground(root, new DrawColor(34, 197, 94)));
    }

    [Fact]
    public void ScrollMotionSampleLinksVisualsToScrollOffset()
    {
        UIElement sampleRoot = new ScrollMotionSample().Build();
        UIRoot root = new(800, 360);
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();

        ScrollViewer scrollViewer = DescendantsAndSelf<ScrollViewer>(sampleRoot).Single();
        Border header = BorderWithText(sampleRoot, "Header fade");
        Border parallax = BorderWithText(sampleRoot, "Parallax");
        Border progress = BorderWithBackground(sampleRoot, new DrawColor(34, 197, 94));

        Assert.True(
            scrollViewer.ScrollInfo.ExtentHeight > scrollViewer.ScrollInfo.ViewportHeight,
            "Expected Scroll Motion sample content to exceed the viewport so scroll-linked motion can be demonstrated.");

        float initialOpacity = header.Opacity;
        float initialTranslateY = parallax.TranslateY;
        float initialScaleX = progress.ScaleX;
        float maxOffset = scrollViewer.ScrollInfo.ExtentHeight - scrollViewer.ScrollInfo.ViewportHeight;

        scrollViewer.ScrollInfo.SetVerticalOffset(maxOffset * 0.5f);
        root.ProcessFrame();

        Assert.True(header.Opacity < initialOpacity, $"Expected header opacity to fade below {initialOpacity}, but was {header.Opacity}.");
        Assert.True(parallax.TranslateY < initialTranslateY, $"Expected parallax Y to move above {initialTranslateY}, but was {parallax.TranslateY}.");
        Assert.True(progress.ScaleX > initialScaleX, $"Expected progress scale to grow above {initialScaleX}, but was {progress.ScaleX}.");
    }

    [Fact]
    public void ScrollMotionSampleKeepsMotionChromeOutsideScrollableContent()
    {
        UIElement sampleRoot = new ScrollMotionSample().Build();
        ScrollViewer scrollViewer = DescendantsAndSelf<ScrollViewer>(sampleRoot).Single();
        UIElement scrollContent = Assert.IsAssignableFrom<UIElement>(scrollViewer.Content);

        Border header = BorderWithText(sampleRoot, "Header fade");
        Border parallax = BorderWithText(sampleRoot, "Parallax");
        Border progress = BorderWithBackground(sampleRoot, new DrawColor(34, 197, 94));
        IEnumerable<UIElement> scrollDescendants = DescendantsAndSelf<UIElement>(scrollContent);

        Assert.DoesNotContain(scrollDescendants, element => ReferenceEquals(element, header));
        Assert.DoesNotContain(scrollDescendants, element => ReferenceEquals(element, parallax));
        Assert.DoesNotContain(scrollDescendants, element => ReferenceEquals(element, progress));
    }

    [Fact]
    public void ScrollMotionSampleClipsParallaxInsideDedicatedViewport()
    {
        UIElement sampleRoot = new ScrollMotionSample().Build();
        Border parallax = BorderWithText(sampleRoot, "Parallax");

        Border viewport = Assert.IsType<Border>(parallax.VisualParent);

        Assert.True(viewport.ClipToBounds, "Expected Parallax to be clipped by its own viewport instead of drawing over surrounding playground chrome.");
    }

    [Fact]
    public void ScrollMotionSampleUsesCompactPolishedChrome()
    {
        UIElement sampleRoot = new ScrollMotionSample().Build();
        UIRoot root = new(800, 360);
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();

        ScrollViewer scrollViewer = DescendantsAndSelf<ScrollViewer>(sampleRoot).Single();
        Border parallax = BorderWithText(sampleRoot, "Parallax");
        Border parallaxViewport = Assert.IsType<Border>(parallax.VisualParent);
        Border progress = BorderWithBackground(sampleRoot, new DrawColor(34, 197, 94));

        Assert.Null(progress.Child);
        Assert.InRange(progress.ArrangedBounds.Height, 2, 6);
        Assert.InRange(parallaxViewport.ArrangedBounds.Height, 56, 96);
        Assert.True(
            scrollViewer.ArrangedBounds.Y >= progress.ArrangedBounds.Y + progress.ArrangedBounds.Height,
            "Expected scroll rows to start below the fixed motion chrome.");
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

        ScrollViewer rows = DescendantsAndSelf<ScrollViewer>(selector.ActiveElement!)
            .Single(scrollViewer => scrollViewer.Content is ListBox);
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

    [Theory]
    [InlineData("Motion")]
    [InlineData("Layout Motion")]
    [InlineData("Presence")]
    [InlineData("Scroll Motion")]
    public void MotionSamplesUseSkiaFontsWhenFontResourceIsProvided(string sampleName)
    {
        ResourceStore resources = CreateFontResources(out ResourceId<FontResource> fontId);
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == sampleName));
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
    public void StatsOverlayMapsMotionFrameCountersToRetainedText()
    {
        FrameStats stats = new();
        stats.CountMotion(new MotionFrameResult(
            new MotionFrame(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender),
            NeedsAnotherFrame: true,
            MotionFrames: 1,
            MotionNodesSampled: 2,
            MotionValuesChanged: 3,
            MotionPropertyWrites: 4,
            MotionCompleted: 5,
            MotionRenderInvalidations: 6,
            MotionLayoutInvalidations: 7,
            MotionSkippedByReducedMotion: 8));
        UiFrame frame = new(TimeSpan.FromMilliseconds(16), new UiViewport(800, 600), EmptyInputFrame(), stats);
        InvalidationStatsOverlay overlay = new();

        overlay.Update(frame);

        Assert.Contains("motion=1", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("sampled=2", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("motionValues=3", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("motionWrites=4", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("completed=5", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("motionRender=6", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("motionLayout=7", overlay.Text, StringComparison.Ordinal);
        Assert.Contains("reduced=8", overlay.Text, StringComparison.Ordinal);
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
        ScrollViewer scrollViewer = DescendantsAndSelf<ScrollViewer>(selector.ActiveElement!)
            .Single(viewer => viewer.Content is ListBox);
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

    private static DrawRect FillRectWithColor(UIRoot root, DrawColor color)
    {
        return root.RetainedRenderer.Render(root)
            .Where(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == color)
            .Select(command => command.Rect)
            .First();
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

    private static Button ButtonWithString(UIElement root, string text)
    {
        return DescendantsAndSelf<Button>(root)
            .Single(button => string.Equals(button.Content as string, text, StringComparison.Ordinal));
    }

    private static Border BorderWithText(UIElement root, string text)
    {
        return DescendantsAndSelf<Border>(root)
            .Single(border => border.Child is TextBlock textBlock && textBlock.Text == text);
    }

    private static Border BorderWithBackground(UIElement root, DrawColor background)
    {
        return DescendantsAndSelf<Border>(root)
            .Single(border => border.Background == background);
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
