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

        Assert.Equal(new[] { "Retained App", "Button", "Layout", "Text", "Diagnostics" }, selector.Samples.Select(sample => sample.Name));

        selector.SelectSample(1);

        Assert.Equal(1, selector.ActiveIndex);
        Assert.Equal("Button", selector.ActiveSample.Name);
        Assert.NotNull(selector.ActiveElement);
        Assert.NotSame(initial, selector.ActiveElement);
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
}
