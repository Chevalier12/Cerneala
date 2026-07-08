using Cerneala.Playground.Samples;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Theming;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.Playground.Samples;

public sealed class GettingStartedSampleContractTests
{
    [Fact]
    public void GettingStartedSampleBuildsRootTextBoxButtonListAndStatusText()
    {
        GettingStartedSample sample = new();

        UIElement root = sample.Build();

        Assert.NotNull(root);
        Assert.NotNull(sample.RootElement);
        Assert.NotNull(sample.EntryTextBox);
        Assert.NotNull(sample.AddButton);
        Assert.NotNull(sample.StatusBlock);
        Assert.NotNull(sample.ListBox);
    }

    [Fact]
    public void GettingStartedSampleUsesObservableListItemsSource()
    {
        GettingStartedSample sample = new();

        sample.Build();

        Assert.IsType<ObservableList<string>>(sample.Items);
        Assert.Same(sample.Items, sample.ListBox!.ItemsSource);
    }

    [Fact]
    public void GettingStartedSampleUsesTypedTwoWayTextBinding()
    {
        GettingStartedSample sample = new();
        sample.Build();

        sample.EntryTextBox!.ReceiveTextInput("Ada");

        Assert.Equal("Ada", sample.EntryText.Value);
        Assert.Equal("Ready to add Ada.", sample.StatusText.Value);
        Assert.Equal("Ready to add Ada.", sample.StatusBlock!.Text);
    }

    [Fact]
    public void GettingStartedSampleUsesDefaultAspectForButtonChrome()
    {
        UIRoot root = ThemedRoot();
        GettingStartedSample sample = new();
        root.VisualChildren.Add(sample.Build());

        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, sample.AddButton!.GetValueSource(Control.BackgroundProperty));
        Assert.DoesNotContain("Background =", SourceText(), StringComparison.Ordinal);
        Assert.DoesNotContain("BorderColor =", SourceText(), StringComparison.Ordinal);
        Assert.DoesNotContain("BorderThickness =", SourceText(), StringComparison.Ordinal);
    }

    [Fact]
    public void GettingStartedSampleUsesGridOrStackPanelWithoutUnsupportedLayoutApis()
    {
        GettingStartedSample sample = new();

        UIElement root = sample.Build();

        Assert.True(root is Grid or StackPanel, $"Expected Grid or StackPanel root, got {root.GetType().Name}.");
        Assert.DoesNotContain("Canvas.Set", SourceText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Absolute", SourceText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GettingStartedSampleFirstFrameDoesRetainedWork()
    {
        UiHost host = HostWithSample(out _);

        UiFrame frame = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.True(frame.Stats.HasWork);
        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.RenderedElements > 0);
        Assert.True(frame.Stats.HitTestElements > 0);
    }

    [Fact]
    public void GettingStartedSampleSecondUnchangedFrameDoesNoRetainedWork()
    {
        UiHost host = HostWithSample(out _);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        UiFrame second = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void GettingStartedSampleDrawDoesNotGenerateRetainedWork()
    {
        UiHost host = HostWithSample(out UIRoot root);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        int cacheVersion = root.RetainedRenderCache.Version;

        host.Draw(backend);
        host.Draw(backend);

        Assert.Equal(2, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.Equal(cacheVersion, root.RetainedRenderCache.Version);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void GettingStartedSampleTabMovesFocusFromTextBoxToButton()
    {
        UiHost host = HostWithSample(out UIRoot root, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        sample.EntryTextBox!.ReceiveTextInput("Ada");
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        host.InputBridge.FocusManager.Focus(sample.EntryTextBox, routeMap);

        host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.Same(sample.AddButton, host.InputBridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void GettingStartedSampleTextInputEnablesCommandAndButtonAddsListItem()
    {
        UiHost host = HostWithSample(out _, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        int initialCount = sample.Items.Count;

        sample.EntryTextBox!.ReceiveTextInput("Linus");
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        sample.AddCommand.Execute(null);
        UiFrame changed = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.True(changed.Stats.HasWork);
        Assert.Equal(initialCount + 1, sample.Items.Count);
        Assert.Contains("Linus", sample.Items);
        Assert.Equal("Added Linus.", sample.StatusText.Value);
        Assert.Equal(string.Empty, sample.EntryText.Value);
    }

    [Fact]
    public void GettingStartedSampleGridDefinitionMutationStillProducesNoWorkNextFrame()
    {
        UiHost host = HostWithSample(out _, out GettingStartedSample sample);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        sample.LayoutGrid!.RowDefinitions[0].Height = GridLength.Pixels(52);
        host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);
        UiFrame unchanged = host.Update(EmptyFrame(), new UiViewport(420, 320), TimeSpan.Zero);

        Assert.Equal(0, unchanged.Stats.MeasuredElements);
        Assert.Equal(0, unchanged.Stats.ArrangedElements);
        Assert.Equal(0, unchanged.Stats.RenderedElements);
        Assert.Equal(0, unchanged.Stats.HitTestElements);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);
    }

    private static UiHost HostWithSample(out UIRoot root)
    {
        return HostWithSample(out root, out _);
    }

    private static UiHost HostWithSample(out UIRoot root, out GettingStartedSample sample)
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

    private static string SourceText()
    {
        return File.ReadAllText(Path.Combine(RepoRoot(), "Playground", "Cerneala.Playground", "Samples", "GettingStartedSample.cs"));
    }

    private static string RepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Cerneala.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
