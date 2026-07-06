using Cerneala.Playground.Samples;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Playground;

public sealed class PlaygroundGameLoopSmokeTests
{
    [Fact]
    public void StatsOverlayUpdatedAfterHostUpdateInvalidatesDrawLikeRealCrash()
    {
        UiHost host = HostWithSelector(out SampleSelector selector);
        FakeDrawingBackend backend = new();

        UiFrame frame = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        selector.UpdateFrame(frame);

        Assert.Throws<InvalidOperationException>(() => host.Draw(backend));
    }

    [Fact]
    public void StatsOverlayUpdatedBeforeNextHostUpdateKeepsDrawCommitted()
    {
        UiHost host = HostWithSelector(out SampleSelector selector);
        FakeDrawingBackend backend = new();

        UiFrame first = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        selector.UpdateFrame(first);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        host.Draw(backend);

        Assert.Equal(1, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
    }

    [Fact]
    public void InitialPlaygroundTreeRequiresCommittedFrameBeforeFirstDraw()
    {
        UiHost host = HostWithSelector(out _);
        FakeDrawingBackend backend = new();

        Assert.Throws<InvalidOperationException>(() => host.Draw(backend));

        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        host.Draw(backend);

        Assert.Equal(1, backend.RenderCalls);
    }

    [Fact]
    public void RootReplacementRequiresUpdateBeforeDrawAndGameLoopPatternCommitsIt()
    {
        UiHost host = HostWithSelector(out _);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        UIRoot replacement = RootWithSelector(out _);

        host.SetRoot(replacement);

        Assert.Throws<InvalidOperationException>(() => host.Draw(backend));

        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        host.Draw(backend);

        Assert.Equal(1, backend.RenderCalls);
    }

    [Fact]
    public void SampleSelectorCommandSelectionDuringHostUpdateCommitsBeforeDraw()
    {
        UiHost host = HostWithSelector(out SampleSelector selector);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        Button layoutButton = ButtonWithText(selector.Root, "Layout");
        float x = layoutButton.ArrangedBounds.X + (layoutButton.ArrangedBounds.Width / 2);
        float y = layoutButton.ArrangedBounds.Y + (layoutButton.ArrangedBounds.Height / 2);

        host.Update(PointerFrame(x, y, currentDown: true), new UiViewport(800, 600), TimeSpan.Zero);
        host.Update(PointerFrame(x, y, previousDown: true), new UiViewport(800, 600), TimeSpan.Zero);
        host.Draw(backend);

        Assert.Equal("Layout", selector.ActiveSample.Name);
        Assert.Equal(1, backend.RenderCalls);
    }

    [Fact]
    public void ExternalSampleSelectionAfterHostUpdateStillThrowsByDesign()
    {
        UiHost host = HostWithSelector(out SampleSelector selector);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        selector.SelectSample(1);

        Assert.Throws<InvalidOperationException>(() => host.Draw(backend));
    }

    private static UiHost HostWithSelector(out SampleSelector selector)
    {
        UIRoot root = RootWithSelector(out selector);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static UIRoot RootWithSelector(out SampleSelector selector)
    {
        UIRoot root = new(420, 320);
        selector = SampleSelector.CreateDefault();
        root.VisualChildren.Add(selector.Root);
        return root;
    }

    private static Button ButtonWithText(UIElement root, string text)
    {
        foreach (Button button in DescendantsAndSelf<Button>(root))
        {
            if (button.Content is TextBlock textBlock && textBlock.Text == text)
            {
                return button;
            }
        }

        throw new InvalidOperationException($"Could not find button with text '{text}'.");
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

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
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
}
