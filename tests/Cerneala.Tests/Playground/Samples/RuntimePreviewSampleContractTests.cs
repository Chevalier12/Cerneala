using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Playground.Samples;

public sealed class RuntimePreviewSampleContractTests
{
    [Fact]
    public void RuntimePreviewSampleBuildsWithoutPlatformServices()
    {
        RuntimePreviewSample sample = new();
        UIRoot root = new(640, 360, scale: 2);

        UIElement element = sample.Build();
        root.VisualChildren.Add(element);

        FrameStats stats = root.ProcessFrame();

        Assert.NotNull(element);
        Assert.NotNull(sample.RootElement);
        Assert.True(stats.MeasuredElements > 0);
    }

    [Fact]
    public void RuntimePreviewSampleUsesPathBackedOrResourceBackedImage()
    {
        ResourceId<ImageResource> imageId = new("RuntimePreview/Hero");
        ResourceStore resources = new();
        resources.SetResource(imageId, new ImageResource("Content/runtime-preview.png"));
        RuntimePreviewSample sample = new(resources, imageResourceId: imageId);

        sample.Build();

        Assert.NotNull(sample.PreviewImage);
        Assert.Equal(imageId, sample.PreviewImage!.SourceResourceId);
    }

    [Fact]
    public void RuntimePreviewSampleUsesUntintedPreviewImage()
    {
        RuntimePreviewSample sample = new();

        sample.Build();

        Assert.NotNull(sample.PreviewImage);
        Assert.Equal(DrawColor.White, sample.PreviewImage!.Foreground);
    }

    [Fact]
    public void RuntimePreviewSampleWrapsContentInVerticalScrollViewer()
    {
        RuntimePreviewSample sample = new();

        UIElement root = sample.Build();

        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(root);
        Assert.Equal(ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
        Assert.NotNull(scrollViewer.Content);
    }

    [Fact]
    public void RuntimePreviewSampleContainsTextBoxButtonAndObservableList()
    {
        RuntimePreviewSample sample = new();

        UIElement root = sample.Build();

        Assert.NotNull(sample.InputTextBox);
        Assert.NotNull(sample.ActionButton);
        Assert.NotNull(sample.ItemsList);
        Assert.IsType<ObservableList<string>>(sample.Items);
        Assert.Same(sample.Items, sample.ItemsList!.ItemsSource);
        Assert.Contains(DescendantsAndSelf<TextBox>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<Button>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<ListBox>(root), _ => true);
    }

    [Fact]
    public void RuntimePreviewSampleExposesDiagnosticsOverlayText()
    {
        RuntimePreviewSample sample = new();
        sample.Build();
        FrameStats stats = new();
        stats.CountNoWorkFrame();
        UiFrame frame = new(TimeSpan.FromMilliseconds(16), new UiViewport(640, 360, scale: 2), EmptyInputFrame(), stats);

        sample.UpdateFrame(frame);

        Assert.NotNull(sample.DiagnosticsText);
        Assert.Contains("runtime", sample.DiagnosticsText!.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scale=2", sample.DiagnosticsText.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noWork=1", sample.DiagnosticsText.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimePreviewSampleSecondUnchangedFrameDoesNoRetainedWork()
    {
        UIRoot root = new(640, 360, scale: 2);
        RuntimePreviewSample sample = new();
        root.VisualChildren.Add(sample.Build());
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Fact]
    public void RuntimePreviewSampleDrawDoesNotGenerateRetainedWork()
    {
        UIRoot root = new(640, 360, scale: 2);
        RuntimePreviewSample sample = new();
        root.VisualChildren.Add(sample.Build());
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        FrameStats afterDraw = root.ProcessFrame();

        Assert.NotEmpty(commands);
        Assert.Equal(0, afterDraw.MeasuredElements);
        Assert.Equal(0, afterDraw.ArrangedElements);
        Assert.Equal(0, afterDraw.RenderedElements);
        Assert.Equal(1, afterDraw.NoWorkFrames);
    }

    [Fact]
    public void RuntimePreviewTextInputPreservesScrollOffset()
    {
        UIRoot root = new(800, 220);
        RuntimePreviewSample sample = new();
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(sample.Build());
        root.VisualChildren.Add(scrollViewer);
        root.ProcessFrame();
        scrollViewer.Presenter.SetVerticalOffset(120);
        root.ProcessFrame();
        float offsetBeforeInput = scrollViewer.Presenter.VerticalOffset;
        Assert.True(offsetBeforeInput > 0);

        sample.InputTextBox!.ReceiveTextInput("x");
        root.ProcessFrame();

        Assert.Equal(offsetBeforeInput, scrollViewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void RuntimePreviewHostTextInputPreservesScrollOffsetAfterFrameDiagnosticsUpdate()
    {
        UIRoot root = new(800, 360);
        SampleSelector selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Runtime Preview"));
        UiHost host = new(new UiHostOptions { Root = root });
        UiViewport viewport = new(800, 360);
        host.Update(EmptyInputFrame(), viewport, TimeSpan.Zero);
        RuntimePreviewSample sample = Assert.IsType<RuntimePreviewSample>(selector.ActiveSample);
        ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(sample.RootElement);
        scrollViewer.Presenter.SetVerticalOffset(120);
        host.Update(EmptyInputFrame(), viewport, TimeSpan.Zero);
        float offsetBeforeInput = scrollViewer.Presenter.VerticalOffset;
        Assert.True(offsetBeforeInput > 0);
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        host.InputBridge.FocusManager.Focus(sample.InputTextBox!, routeMap);

        selector.UpdateFrame(host.LastFrame);
        host.Update(TextInputFrame("x"), viewport, TimeSpan.FromMilliseconds(16));

        Assert.Equal(offsetBeforeInput, scrollViewer.Presenter.VerticalOffset);
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

    private static InputFrame TextInputFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
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
}
