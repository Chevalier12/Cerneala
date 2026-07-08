using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Hosting;

public sealed class DeveloperPreviewContractTests
{
    [Fact]
    public void DeveloperPreviewGettingStartedSampleFirstFrameDoesWorkAndSecondFrameDoesNoWork()
    {
        UiHost host = HostWithGettingStartedSample(out _);

        UiFrame first = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        UiFrame second = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.True(first.Stats.HasWork);
        Assert.True(first.Stats.MeasuredElements > 0);
        Assert.True(first.Stats.ArrangedElements > 0);
        Assert.True(first.Stats.RenderedElements > 0);
        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void DeveloperPreviewDrawLoopDoesNotGenerateRetainedWork()
    {
        UiHost host = HostWithGettingStartedSample(out UIRoot root);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        int cacheVersion = root.RetainedRenderCache.Version;

        for (int i = 0; i < 10; i++)
        {
            host.Draw(backend);
        }

        Assert.Equal(10, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.Equal(cacheVersion, root.RetainedRenderCache.Version);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void DeveloperPreviewTabNavigationWorksInGettingStartedSample()
    {
        UiHost host = HostWithGettingStartedSample(out UIRoot root, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        sample.EntryTextBox!.ReceiveTextInput("Ada");
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        host.InputBridge.FocusManager.Focus(sample.EntryTextBox, routeMap);

        UiFrame tab = host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.Same(sample.AddButton, host.InputBridge.FocusManager.FocusedElement);
        Assert.Equal(0, tab.Stats.MeasuredElements);
        Assert.Equal(0, tab.Stats.ArrangedElements);
    }

    [Fact]
    public void DeveloperPreviewTextInputCommandAndObservableListWorkTogether()
    {
        UiHost host = HostWithGettingStartedSample(out _, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        int initialCount = sample.Items.Count;

        sample.EntryTextBox!.ReceiveTextInput("Grace");
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        sample.AddCommand.Execute(null);
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        UiFrame settled = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.True(changed.Stats.HasWork);
        Assert.Equal(initialCount + 1, sample.Items.Count);
        Assert.Contains("Grace", sample.Items);
        Assert.Equal("Added Grace.", sample.StatusText.Value);
        Assert.Equal(1, settled.Stats.NoWorkFrames);
    }

    [Fact]
    public void DeveloperPreviewGridDefinitionMutationInvalidatesThenSettles()
    {
        UiHost host = HostWithGettingStartedSample(out _, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        sample.LayoutGrid!.RowDefinitions[0].Height = GridLength.Pixels(54);
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        UiFrame settled = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.True(changed.Stats.MeasuredElements > 0);
        Assert.True(changed.Stats.ArrangedElements > 0);
        Assert.True(changed.Stats.RenderedElements > 0);
        Assert.Equal(0, settled.Stats.MeasuredElements);
        Assert.Equal(0, settled.Stats.ArrangedElements);
        Assert.Equal(0, settled.Stats.RenderedElements);
        Assert.Equal(1, settled.Stats.NoWorkFrames);
    }

    [Fact]
    public void DeveloperPreviewDetachingSampleStopsExternalNotifications()
    {
        UIRoot root = ThemedRoot();
        GettingStartedSample sample = new();
        UIElement sampleRoot = sample.Build();
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();

        root.VisualChildren.Remove(sampleRoot);
        root.ProcessFrame();
        sample.EntryText.Value = "Detached";
        sample.Items.Add("Detached item");
        sample.AddCommand.RaiseCanExecuteChanged();
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(0, stats.RenderedElements);
        Assert.Equal(0, stats.HitTestElements);
        Assert.Equal(1, stats.NoWorkFrames);
    }

    private static UiHost HostWithGettingStartedSample(out UIRoot root)
    {
        return HostWithGettingStartedSample(out root, out _);
    }

    private static UiHost HostWithGettingStartedSample(out UIRoot root, out GettingStartedSample sample)
    {
        root = ThemedRoot();
        sample = new GettingStartedSample();
        root.VisualChildren.Add(sample.Build());
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static UIRoot ThemedRoot()
    {
        UIRoot root = new(420, 320);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        return root;
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
}
