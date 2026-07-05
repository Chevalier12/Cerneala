using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Styling;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Hosting;

public sealed class CorePreviewContractTests
{
    [Fact]
    public void CorePreviewFirstFrameDoesLayoutRenderHitTestStyleWork()
    {
        UiHost host = CorePreviewHost(out _, out _);

        UiFrame frame = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(frame.Stats.StyledElements > 0);
        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
    }

    [Fact]
    public void CorePreviewSecondUnchangedFrameDoesNoRetainedWork()
    {
        UiHost host = CorePreviewHost(out _, out _);

        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        UiFrame second = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void CorePreviewDrawNeverGeneratesRetainedWork()
    {
        UiHost host = CorePreviewHost(out UIRoot root, out _);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.NotEmpty(backend.LastCommands);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void CorePreviewButtonWorksWithMouseAndKeyboard()
    {
        UiHost host = CorePreviewHost(out UIRoot root, out RetainedAppSample sample);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        LayoutRect buttonBounds = sample.PrimaryButton!.ArrangedBounds;
        float x = buttonBounds.X + (buttonBounds.Width / 2);
        float y = buttonBounds.Y + (buttonBounds.Height / 2);

        host.Update(PointerFrame(x, y, currentDown: true), new UiViewport(800, 600), TimeSpan.Zero);
        host.Update(PointerFrame(x, y, previousDown: true), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.Equal("Command executed 1 time(s).", sample.StatusText!.Text);

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        host.InputBridge.FocusManager.Focus(sample.PrimaryButton, routeMap);
        Assert.Same(sample.PrimaryButton, host.InputBridge.FocusManager.FocusedElement);
        host.Update(KeyPressFrame(InputKey.Enter), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.Equal("Command executed 2 time(s).", sample.StatusText.Text);
    }

    [Fact]
    public void CorePreviewThemeChangeInvalidatesStyledRenderOnlyWhenPossible()
    {
        UiHost host = CorePreviewHost(out _, out _, out ThemeProvider provider);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        provider.Theme = new Theme("Changed")
            .Set(DefaultTheme.PaletteKey, DefaultTheme.Create().Get(DefaultTheme.PaletteKey))
            .Set(DefaultTheme.BackgroundKey, new DrawColor(11, 12, 13))
            .Set(DefaultTheme.ForegroundKey, new DrawColor(21, 22, 23))
            .Set(DefaultTheme.SurfaceKey, new DrawColor(31, 32, 33))
            .Set(DefaultTheme.BorderKey, new DrawColor(41, 42, 43))
            .Set(DefaultTheme.AccentKey, new DrawColor(51, 52, 53));

        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(changed.Stats.StyledElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.Equal(0, changed.Stats.MeasureCalls);
        Assert.Equal(0, changed.Stats.ArrangeCalls);
    }

    [Fact]
    public void CorePreviewTextWrapsAndRerendersAfterViewportWidthChange()
    {
        UiHost host = CorePreviewHost(out UIRoot root, out _);
        TextBlock wrappingText = new()
        {
            Text = "A retained text block should reflow when viewport width changes in the core preview gate.",
            TextWrapping = TextWrapping.Wrap
        };
        root.VisualChildren.Add(wrappingText);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        LayoutSize wide = wrappingText.DesiredSize;

        UiFrame resized = host.Update(EmptyFrame(), new UiViewport(240, 600), TimeSpan.Zero);

        Assert.True(resized.Stats.MeasuredElements > 0);
        Assert.True(resized.Stats.RenderedElements > 0);
        Assert.NotEqual(wide, wrappingText.DesiredSize);
    }

    [Fact]
    public void CorePreviewListMutationInvalidatesRetainedWork()
    {
        UiHost host = CorePreviewHost(out UIRoot root, out _);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        ListBox listBox = TreeSearch.FindDescendant<ListBox>(root);

        listBox.Items.Add(new TextBlock { Text = "Added row" });
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(changed.Stats.MeasuredElements > 0);
        Assert.True(changed.Stats.ArrangedElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.True(changed.Stats.HitTestElements > 0);
    }

    [Fact]
    public void CorePreviewScrollInvalidatesArrangeRenderHitTestWithoutFullMeasure()
    {
        UiHost host = CorePreviewHost(out UIRoot root, out _);
        ScrollViewer scrollViewer = new()
        {
            Content = new FixedElement(new LayoutSize(120, 500)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(scrollViewer);
        host.Update(EmptyFrame(), new UiViewport(220, 140), TimeSpan.Zero);
        LayoutRect bounds = scrollViewer.ArrangedBounds;

        UiFrame scrolled = host.Update(
            PointerWheelFrame(bounds.X + 4, bounds.Y + 4, -120),
            new UiViewport(220, 140),
            TimeSpan.Zero);

        Assert.True(scrollViewer.Presenter.VerticalOffset > 0);
        Assert.Equal(0, scrolled.Stats.MeasureCalls);
        Assert.Equal(0, scrolled.Stats.MeasuredElements);
        Assert.True(scrolled.Stats.ArrangedElements > 0);
        Assert.True(scrolled.Stats.RenderedElements > 0);
        Assert.True(scrolled.Stats.HitTestElements > 0);
    }

    private static UiHost CorePreviewHost(out UIRoot root, out RetainedAppSample sample)
    {
        return CorePreviewHost(out root, out sample, out _);
    }

    private static UiHost CorePreviewHost(out UIRoot root, out RetainedAppSample sample, out ThemeProvider themeProvider)
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("CorePreview/Body");
        resources.SetResource(fontId, new FontResource(new TestFont("CorePreview", 16)));
        themeProvider = new ThemeProvider(DefaultTheme.Create());
        root = new UIRoot();
        root.SetResourceProvider(resources);
        root.SetThemeProvider(themeProvider);
        root.SetStyleSheet(DefaultTheme.CreateStyleSheet());
        sample = new RetainedAppSample(resources, fontId);
        root.VisualChildren.Add(sample.Build());
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame KeyPressFrame(InputKey key)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([key]),
            []);
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

    private static InputFrame PointerWheelFrame(float x, float y, int delta)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y).WithWheelValue(delta);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private static class TreeSearch
    {
        public static T FindDescendant<T>(UIElement root)
            where T : UIElement
        {
            foreach (UIElement element in Walk(root))
            {
                if (element is T match)
                {
                    return match;
                }
            }

            throw new InvalidOperationException($"Could not find descendant of type '{typeof(T).Name}'.");
        }

        private static IEnumerable<UIElement> Walk(UIElement element)
        {
            yield return element;
            foreach (UIElement child in element.VisualChildren)
            {
                foreach (UIElement descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
