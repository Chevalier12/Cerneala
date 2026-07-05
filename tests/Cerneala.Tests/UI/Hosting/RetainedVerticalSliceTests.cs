using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

public sealed class RetainedVerticalSliceTests
{
    [Fact]
    public void RetainedAppSampleSecondUnchangedFrameDoesNoRetainedWork()
    {
        UiHost host = HostWithRetainedApp(out _, out _);

        UiFrame first = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        UiFrame second = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(first.Stats.MeasuredElements > 0);
        Assert.True(first.Stats.ArrangedElements > 0);
        Assert.True(first.Stats.RenderedElements > 0);
        Assert.True(first.Stats.HitTestElements > 0);
        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void RetainedAppSampleDrawSubmitsCachedCommandsWithoutRetainedWork()
    {
        UiHost host = HostWithRetainedApp(out _, out _);
        FakeDrawingBackend backend = new();
        UiFrame frame = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        RenderCounterSnapshot counters = RenderCounterSnapshot.Capture(host.Root!.RenderCounters);

        host.Draw(backend);
        host.Draw(backend);

        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.Equal(2, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.NotEmpty(backend.LastCommands);
        Assert.Equal(counters, RenderCounterSnapshot.Capture(host.Root!.RenderCounters));
    }

    [Fact]
    public void RetainedAppSampleCommandMutationInvalidatesRetainedWork()
    {
        UiHost host = HostWithRetainedApp(out _, out RetainedAppSample sample);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        UiFrame unchanged = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);

        sample.PrimaryButton!.Command!.Execute(null);
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.Equal("Command executed 1 time(s).", sample.StatusText!.Text);
        Assert.True(changed.Stats.HasWork);
        Assert.True(changed.Stats.MeasuredElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.Equal(0, changed.Stats.NoWorkFrames);
    }

    [Fact]
    public void RetainedAppSampleFontResourceMutationInvalidatesDependentText()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new TestFont("Default", 16)));
        RetainedAppSample sample = new(resources, fontId);
        UIRoot root = new();
        root.SetResourceProvider(resources);
        root.VisualChildren.Add(sample.Build());
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        resources.SetResource(fontId, new FontResource(new TestFont("Updated", 16)));
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(changed.Stats.HasWork);
        Assert.True(changed.Stats.MeasuredElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.Equal(0, changed.Stats.NoWorkFrames);
    }

    private static UiHost HostWithRetainedApp(out UIRoot root, out RetainedAppSample sample)
    {
        root = new UIRoot();
        sample = new RetainedAppSample();
        root.VisualChildren.Add(sample.Build());
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private readonly record struct RenderCounterSnapshot(
        int CacheHits,
        int CacheMisses,
        int LocalRebuilds,
        int ComposedElements,
        int EmittedCommands)
    {
        public static RenderCounterSnapshot Capture(RenderCounters counters)
        {
            return new RenderCounterSnapshot(
                counters.CacheHits,
                counters.CacheMisses,
                counters.LocalRebuilds,
                counters.ComposedElements,
                counters.EmittedCommands);
        }
    }
}
