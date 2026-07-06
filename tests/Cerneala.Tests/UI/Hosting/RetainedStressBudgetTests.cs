using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout.Panels;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.UI.Hosting;

public sealed class RetainedStressBudgetTests
{
    private const int LargeTreeSections = 12;
    private const int RowsPerSection = 12;

    [Fact]
    public void LargeStaticTreeFirstFrameDoesWorkAndSecondFrameDoesNoRetainedWork()
    {
        UiHost host = HostWithLargeStaticTree(out UIRoot root);

        UiFrame first = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        UiFrame second = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.True(first.Stats.HasWork);
        Assert.True(first.Stats.MeasuredElements > 0);
        Assert.True(first.Stats.ArrangedElements > 0);
        Assert.True(first.Stats.RenderedElements > 0);
        Assert.True(first.Stats.HitTestElements > 0);
        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void LargeStaticTreeHundredDrawsDoNotAdvanceSchedulerOrRenderCacheVersion()
    {
        UiHost host = HostWithLargeStaticTree(out UIRoot root);
        FakeDrawingBackend backend = new();
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
        int cacheVersion = root.RetainedRenderCache.Version;
        int treeVersion = root.TreeVersion;
        int cacheHits = root.RenderCounters.CacheHits;
        int cacheMisses = root.RenderCounters.CacheMisses;
        int localRebuilds = root.RenderCounters.LocalRebuilds;

        for (int i = 0; i < 100; i++)
        {
            host.Draw(backend);
        }

        Assert.Equal(100, backend.RenderCalls);
        Assert.NotNull(backend.LastCommands);
        Assert.Equal(cacheVersion, root.RetainedRenderCache.Version);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.Equal(cacheHits, root.RenderCounters.CacheHits);
        Assert.Equal(cacheMisses, root.RenderCounters.CacheMisses);
        Assert.Equal(localRebuilds, root.RenderCounters.LocalRebuilds);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void LargeStaticTreeFocusTabNavigationTouchesOnlyFocusVisualStateBudget()
    {
        UiHost host = HostWithLargeStaticTree(out _);
        host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

        UiFrame tab = host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(800, 600), TimeSpan.Zero);

        Assert.NotNull(host.InputBridge.FocusManager.FocusedElement);
        Assert.True(tab.Stats.StyledElements > 0);
        Assert.True(tab.Stats.RenderedElements > 0);
        Assert.Equal(0, tab.Stats.MeasuredElements);
        Assert.Equal(0, tab.Stats.ArrangedElements);
        Assert.Equal(0, tab.Stats.MeasureCalls);
        Assert.Equal(0, tab.Stats.ArrangeCalls);
        Assert.True(tab.Stats.StyledElements <= 6, $"Styled {tab.Stats.StyledElements} elements for one focus move.");
        Assert.True(tab.Stats.RenderedElements <= 6, $"Rendered {tab.Stats.RenderedElements} elements for one focus move.");
        Assert.True(tab.Stats.HitTestElements <= 1, $"Rebuilt hit-test for {tab.Stats.HitTestElements} elements.");
    }

    private static UiHost HostWithLargeStaticTree(out UIRoot root)
    {
        root = new UIRoot(800, 600);
        root.VisualChildren.Add(BuildLargeStaticTree());
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static UIElement BuildLargeStaticTree()
    {
        StackPanel root = new()
        {
            Orientation = Orientation.Vertical
        };

        for (int sectionIndex = 0; sectionIndex < LargeTreeSections; sectionIndex++)
        {
            StackPanel section = new()
            {
                Orientation = Orientation.Vertical
            };
            section.VisualChildren.Add(new TextBlock { Text = $"Section {sectionIndex}" });

            for (int rowIndex = 0; rowIndex < RowsPerSection; rowIndex++)
            {
                StackPanel row = new()
                {
                    Orientation = Orientation.Horizontal
                };
                row.VisualChildren.Add(new TextBlock { Text = $"Row {sectionIndex}.{rowIndex}" });
                row.VisualChildren.Add(new Button { Content = $"Run {sectionIndex}.{rowIndex}" });
                row.VisualChildren.Add(new TextBox { Text = $"Value {sectionIndex}.{rowIndex}" });
                section.VisualChildren.Add(row);
            }

            root.VisualChildren.Add(section);
        }

        return root;
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame KeyPressFrame(params InputKey[] currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }
}
