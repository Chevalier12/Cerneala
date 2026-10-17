using System.Collections.ObjectModel;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemsPresenterTests
{
    [Fact]
    public void ItemsPresenterMaterializesItemsThroughContentTemplate()
    {
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one", "two" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context => new ItemElement(context.Data!))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Cerneala.UI.Layout.Panels.Panel panel = presenter.LayoutPanelRoot!;
        Assert.Same(presenter, panel.LogicalParent);
        Assert.Equal(2, panel.VisualChildren.Count);
        Assert.Equal("one", Assert.IsType<ItemElement>(panel.VisualChildren[0]).Value);
        Assert.Equal("two", Assert.IsType<ItemElement>(panel.VisualChildren[1]).Value);
    }

    [Fact]
    public void ReplacingItemsDetachesStaleChildrenAndKeepsOrder()
    {
        ItemsPresenter presenter = new()
        {
            Items = new[] { "old" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context => new ItemElement(context.Data!))
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement oldChild = presenter.LayoutPanelRoot!.VisualChildren[0];

        presenter.Items = new[] { "new-1", "new-2" };

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Equal(["new-1", "new-2"], presenter.LayoutPanelRoot!.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
    }

    [Fact]
    public void ReplacingItemsCanRetainExistingElementItems()
    {
        ItemElement item = new("same");
        ItemsPresenter presenter = new()
        {
            Items = new UIElement[] { item }
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Cerneala.UI.Layout.Panels.Panel oldPanel = presenter.LayoutPanelRoot!;

        presenter.Items = new UIElement[] { item };

        Cerneala.UI.Layout.Panels.Panel newPanel = presenter.LayoutPanelRoot!;
        Assert.Same(oldPanel, newPanel);
        Assert.Same(newPanel, item.VisualParent);
        Assert.Same(newPanel, item.LogicalParent);
        Assert.Same(item, newPanel.VisualChildren[0]);
    }

    [Fact]
    public void ItemsPresenterRetainsGeneratedItemsAcrossMeasurePasses()
    {
        int created = 0;
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context =>
            {
                created++;
                return new ItemElement(context.Data!);
            })
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement child = presenter.LayoutPanelRoot!.VisualChildren[0];
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, created);
        Assert.Same(child, presenter.LayoutPanelRoot!.VisualChildren[0]);
    }

    [Fact]
    public void ItemsPresenterVirtualizationMaterializesOnlyWindowItems()
    {
        ItemsPresenter presenter = new()
        {
            Items = new[] { "zero", "one", "two", "three", "four" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context => new ItemElement(context.Data!)),
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            VirtualizationContext = new VirtualizationContext(5, 10, 20, 20)
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 20)));

        Assert.Equal(new RealizationWindow(2, 4), presenter.CurrentRealizationWindow);
        Assert.Equal(["two", "three"], presenter.LayoutPanelRoot!.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
    }

    [Fact]
    public void ClearingVirtualizationContextReleasesItFromVirtualizingStackPanel()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemsPresenter presenter = new()
        {
            Items = Enumerable.Range(0, 100).Select(index => new ItemElement(index.ToString())).ToArray(),
            ItemsPanel = panel,
            VirtualizationContext = new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)
        };
        MeasureContext context = new(new LayoutSize(100, 30));
        presenter.Measure(context);
        Assert.Equal(4, panel.VisualChildren.Count);

        presenter.VirtualizationContext = null;
        Assert.Null(panel.VirtualizationContext);
        presenter.MarkItemsDirty();
        presenter.Measure(context);

        Assert.Equal(100, panel.VisualChildren.Count);
    }

    [Fact]
    public void ReplacingVirtualizingStackPanelWithStackPanelReleasesOldPanelVirtualizationContext()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel oldPanel = new();
        ItemsPresenter presenter = new()
        {
            Items = Enumerable.Range(0, 100).Select(index => new ItemElement(index.ToString())).ToArray(),
            ItemsPanel = oldPanel,
            VirtualizationContext = new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)
        };
        MeasureContext context = new(new LayoutSize(100, 30));
        presenter.Measure(context);
        Assert.Equal(4, oldPanel.VisualChildren.Count);

        presenter.ItemsPanel = new Cerneala.UI.Layout.Panels.StackPanel();
        presenter.Measure(context);
        Assert.Null(oldPanel.VirtualizationContext);

        ItemsPresenter reuse = new()
        {
            Items = Enumerable.Range(0, 100).Select(index => new ItemElement(index.ToString())).ToArray(),
            ItemsPanel = oldPanel
        };
        reuse.Measure(new MeasureContext(new LayoutSize(100, 2000)));

        Assert.Same(oldPanel, reuse.LayoutPanelRoot);
        Assert.Equal(100, oldPanel.VisualChildren.Count);
    }

    [Fact]
    public void ReleasedVirtualizingStackPanelUsedAloneMeasuresEveryChild()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemsPresenter presenter = new()
        {
            Items = Enumerable.Range(0, 100).Select(_ => new Border { Height = 10 }).ToArray(),
            ItemsPanel = panel,
            VirtualizationContext = new VirtualizationContext(100, 10, 30, 290)
        };
        MeasureContext context = new(new LayoutSize(100, 30));
        presenter.Measure(context);
        Assert.Equal(29, panel.FirstRealizedIndex);

        presenter.ItemsPanel = new Cerneala.UI.Layout.Panels.StackPanel();
        presenter.Measure(context);
        Assert.Null(panel.VisualParent);
        Assert.Null(panel.VirtualizationContext);
        Assert.Empty(panel.VisualChildren);

        for (int index = 0; index < 10; index++)
        {
            panel.VisualChildren.Add(new Border { Height = 10 });
        }

        panel.Measure(new MeasureContext(new LayoutSize(100, 1000)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(100f, panel.DesiredSize.Height);
        for (int index = 0; index < 10; index++)
        {
            Assert.Equal(10f, panel.VisualChildren[index].DesiredSize.Height);
            Assert.Equal(new LayoutRect(0, index * 10, 100, 10), panel.VisualChildren[index].ArrangedBounds);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedPanelSwapBeforeChildrenAreAddedKeepsVirtualizationContextOnOldPanel(bool replacementVirtualizes)
    {
        bool fail = false;
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.Panel replacement = replacementVirtualizes
            ? new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()
            : new Cerneala.UI.Layout.Panels.StackPanel();
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        ItemsPresenter presenter = new()
        {
            Items = Enumerable.Range(0, 100).Select(index => index.ToString()).ToArray(),
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context =>
                fail ? throw new InvalidOperationException("template failed") : new ItemElement(context.Data!)),
            ItemsPanel = oldPanel,
            VirtualizationContext = supplied
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal(1000, presenter.DesiredSize.Height);

        fail = true;
        Assert.Throws<InvalidOperationException>(() => presenter.ItemsPanel = replacement);

        Assert.Same(oldPanel, presenter.LayoutPanelRoot);
        Assert.Equal(4, oldPanel.VisualChildren.Count);
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        Assert.Null((replacement as Cerneala.UI.Layout.Panels.VirtualizingStackPanel)?.VirtualizationContext);

        // The failed refresh stays dirty, so the next measure retries it and the same cause throws again.
        Assert.Throws<InvalidOperationException>(() => presenter.Measure(new MeasureContext(new LayoutSize(100, 31))));
        Assert.Same(oldPanel, presenter.LayoutPanelRoot);
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        oldPanel.Measure(new MeasureContext(new LayoutSize(100, 31)));
        Assert.Equal(1000, oldPanel.DesiredSize.Height);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedPanelSwapWhileAddingChildrenKeepsVirtualizationContextOnOldPanel(bool replacementVirtualizes)
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.Panel replacement = replacementVirtualizes
            ? new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()
            : new Cerneala.UI.Layout.Panels.StackPanel();
        ItemElement[] items = Enumerable.Range(0, 100).Select(index => new ItemElement(index.ToString())).ToArray();
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        ItemsPresenter presenter = new()
        {
            Items = items,
            ItemsPanel = oldPanel,
            VirtualizationContext = supplied
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 30)));
        new Cerneala.UI.Layout.Panels.StackPanel().VisualChildren.Add(items[10]);
        presenter.VirtualizationContext = new VirtualizationContext(100, 10, 30, 70, CacheItems: 1);

        Assert.Throws<InvalidOperationException>(() => presenter.ItemsPanel = replacement);

        Assert.Same(oldPanel, presenter.LayoutPanelRoot);
        Assert.Equal(["0", "1", "2", "3"], oldPanel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        Assert.Equal(0, oldPanel.FirstRealizedIndex);
        if (replacement is Cerneala.UI.Layout.Panels.VirtualizingStackPanel virtualizingReplacement)
        {
            Assert.Null(virtualizingReplacement.VirtualizationContext);
            Assert.Equal(0, virtualizingReplacement.FirstRealizedIndex);
        }
    }

    [Fact]
    public void FailedInsertOnReusedPanelKeepsPreviousChildren()
    {
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        ItemElement a = new("a"), b = new("b"), c = new("c"), x = new("x");
        ItemsPresenter presenter = new() { Items = new[] { a, b }, ItemsPanel = panel };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);

        Assert.Throws<InvalidOperationException>(() => presenter.Items = new[] { c, x });

        Assert.Same(panel, presenter.LayoutPanelRoot);
        Assert.Equal(["a", "b"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Equal(["a", "b"], panel.LogicalChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Same(panel, a.VisualParent);
        Assert.Same(panel, b.VisualParent);
        Assert.Null(c.VisualParent);
        Assert.Null(c.LogicalParent);

        // While the cause stays, each retry throws again and keeps the previous children.
        Assert.Throws<InvalidOperationException>(() => presenter.Measure(new MeasureContext(new LayoutSize(100, 101))));
        Assert.Equal(["a", "b"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));

        otherParent.VisualChildren.Remove(x);
        presenter.Measure(new MeasureContext(new LayoutSize(100, 102)));

        Assert.Equal(["c", "x"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Null(a.VisualParent);
        Assert.Null(b.VisualParent);
    }

    [Fact]
    public void FailedInsertWhileWindowMovesKeepsPreviousChildrenAndIndex()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        FixedElement[] items = Enumerable.Range(0, 100).Select(index => new FixedElement(index.ToString())).ToArray();
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        ItemsPresenter presenter = new()
        {
            Items = items,
            ItemsPanel = panel,
            VirtualizationContext = supplied
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(items[5]);
        presenter.VirtualizationContext = new VirtualizationContext(100, 10, 30, 20, CacheItems: 1);
        presenter.MarkItemsDirty();

        Assert.Throws<InvalidOperationException>(() => presenter.Measure(new MeasureContext(new LayoutSize(100, 31))));

        Assert.Same(panel, presenter.LayoutPanelRoot);
        Assert.Equal(["0", "1", "2", "3"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Equal(supplied, panel.VirtualizationContext);
        Assert.Equal(0, panel.FirstRealizedIndex);
        Assert.Null(items[4].VisualParent);
        panel.Measure(new MeasureContext(new LayoutSize(100, 30)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 30)));
        Assert.Equal(new LayoutRect(0, 10, 100, 10), items[1].ArrangedBounds);

        Assert.Throws<InvalidOperationException>(() => presenter.Measure(new MeasureContext(new LayoutSize(100, 32))));
        Assert.Equal(["0", "1", "2", "3"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Equal(0, panel.FirstRealizedIndex);

        otherParent.VisualChildren.Remove(items[5]);
        presenter.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal(["1", "2", "3", "4", "5"], panel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Equal(1, panel.FirstRealizedIndex);
    }

    [Fact]
    public void ClearingItemsOwnerWithdrawsOwnerPanelAndReleasesItsVirtualizationContext()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemsControl owner = new() { ItemsPanel = panel };
        owner.SetItems(Enumerable.Range(0, 100).Select(index => $"item {index}"));
        ItemsPresenter presenter = new()
        {
            ItemsOwner = owner,
            VirtualizationContext = new VirtualizationContext(100, 10, 30, 0, CacheItems: 1)
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Same(presenter, panel.VisualParent);
        Assert.Equal(4, panel.VisualChildren.Count);
        Assert.NotNull(panel.VirtualizationContext);

        presenter.ItemsOwner = null;

        Assert.Null(presenter.LayoutPanelRoot);
        Assert.Null(panel.VisualParent);
        Assert.Empty(panel.VisualChildren);
        Assert.Null(panel.VirtualizationContext);
    }

    [Fact]
    public void ChangingVirtualizationWindowKeepsOverlappingChildrenAttached()
    {
        LifecycleElement[] items = Enumerable.Range(0, 5)
            .Select(index => new LifecycleElement(index.ToString()))
            .ToArray();
        ItemsPresenter presenter = new()
        {
            Items = items,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
            VirtualizationContext = new VirtualizationContext(5, 10, 40, 0)
        };
        UIRoot root = new();
        root.VisualChildren.Add(presenter);
        presenter.Measure(new MeasureContext(new LayoutSize(100, 40)));

        presenter.VirtualizationContext = new VirtualizationContext(5, 10, 30, 10);
        presenter.MarkItemsDirty();
        presenter.Measure(new MeasureContext(new LayoutSize(100, 30)));

        Assert.Equal(["1", "2", "3"], presenter.LayoutPanelRoot!.VisualChildren.Cast<LifecycleElement>().Select(item => item.Value));
        Assert.Equal(1, items[0].DetachedCount);
        Assert.All(items[1..4], item =>
        {
            Assert.Equal(1, item.AttachedCount);
            Assert.Equal(0, item.DetachedCount);
        });
    }

    [Fact]
    public void PresenterItemsPanelOverridingOwnerPanelReceivesTheScrollViewport()
    {
        string[] items = [.. Enumerable.Range(0, 100).Select(index => "Item " + index)];
        (Menu singleMenu, ScrollViewer singleViewer, UIRoot singleRoot) = CreateScrolledMenu(items);
        singleRoot.ProcessFrame();
        singleViewer.ScrollInfo.SetVerticalOffset(500);
        singleRoot.ProcessFrame();

        (Menu menu, ScrollViewer viewer, UIRoot root) = CreateScrolledMenu(items);
        root.ProcessFrame();
        ItemsPresenter presenter = (ItemsPresenter)menu.ComponentTemplateInstance!.Parts["PART_ItemsPresenter"];
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel presenterPanel = new();
        presenter.ItemsPanel = presenterPanel;
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(500);
        root.ProcessFrame();

        Assert.Same(presenterPanel, presenter.LayoutPanelRoot);
        Assert.Equal(singleViewer.ScrollInfo.ExtentHeight, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(500, viewer.ScrollInfo.VerticalOffset);
        Assert.Equal(singleViewer.ScrollInfo.VerticalOffset, viewer.ScrollInfo.VerticalOffset);
        Assert.Equal(RealizedIndices(singleMenu), RealizedIndices(menu));
    }

    [Fact]
    public void FailedItemsChangeIsRetriedOnNextFrame()
    {
        bool fail = false;
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one", "two" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context =>
                fail ? throw new InvalidOperationException("template failed") : new ItemElement(context.Data!))
        };
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        fail = true;
        Assert.Throws<InvalidOperationException>(() => presenter.Items = new[] { "x", "y", "z" });
        fail = false;
        root.ProcessFrame();

        Assert.Equal(["x", "y", "z"], presenter.LayoutPanelRoot!.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
    }

    [Fact]
    public void FailedItemsPanelChangeIsRetriedOnNextFrame()
    {
        bool fail = false;
        Cerneala.UI.Layout.Panels.StackPanel panelA = new();
        Cerneala.UI.Layout.Panels.StackPanel panelB = new();
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one", "two" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context =>
                fail ? throw new InvalidOperationException("template failed") : new ItemElement(context.Data!)),
            ItemsPanel = panelA
        };
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        fail = true;
        Assert.Throws<InvalidOperationException>(() => presenter.ItemsPanel = panelB);
        fail = false;
        root.ProcessFrame();

        Assert.Same(panelB, presenter.LayoutPanelRoot);
        Assert.Equal(["one", "two"], panelB.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
        Assert.Empty(panelA.VisualChildren);
        Assert.Null(panelA.VisualParent);
    }

    [Fact]
    public void RefreshThatKeepsFailingRethrowsEachFrameAndKeepsOldItems()
    {
        bool fail = false;
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one", "two" },
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context =>
                fail ? throw new InvalidOperationException("template failed") : new ItemElement(context.Data!))
        };
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Cerneala.UI.Layout.Panels.Panel oldPanel = presenter.LayoutPanelRoot!;

        fail = true;
        Assert.Throws<InvalidOperationException>(() => presenter.Items = new[] { "x", "y", "z" });
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.Same(oldPanel, presenter.LayoutPanelRoot);
        Assert.Equal(["one", "two"], oldPanel.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
    }

    [Fact]
    public void FailedOwnerItemsRefreshIsRetriedOnNextFrame()
    {
        FailingPrepareItemsControl owner = new();
        owner.SetItems(new[] { "one", "two" });
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(owner);
        root.ProcessFrame();
        Cerneala.UI.Layout.Panels.Panel oldPanel = owner.ItemsPresenter.LayoutPanelRoot!;

        owner.Fail = true;
        owner.SetItems(new[] { "x", "y", "z" });
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        Assert.Same(oldPanel, owner.ItemsPresenter.LayoutPanelRoot);
        owner.Fail = false;
        root.ProcessFrame();

        Assert.Equal(new object?[] { "x", "y", "z" }, ContainerContents(owner.ItemsPresenter.LayoutPanelRoot!));
    }

    [Fact]
    public void FailedOwnerItemsPanelChangeIsRetriedOnNextFrame()
    {
        Cerneala.UI.Layout.Panels.StackPanel panelA = new();
        Cerneala.UI.Layout.Panels.StackPanel panelB = new();
        FailingPrepareItemsControl owner = new() { ItemsPanel = panelA };
        owner.SetItems(new[] { "one", "two" });
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(owner);
        root.ProcessFrame();

        owner.Fail = true;
        Assert.Throws<InvalidOperationException>(() => owner.ItemsPresenter.ItemsPanel = panelB);
        Assert.Same(panelA, owner.ItemsPresenter.LayoutPanelRoot);
        owner.Fail = false;
        root.ProcessFrame();

        Assert.Same(panelB, owner.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(new object?[] { "one", "two" }, ContainerContents(panelB));
        Assert.Empty(panelA.VisualChildren);
    }

    [Fact]
    public void FailedOwnerWindowMoveWhilePreparingContainersKeepsPreviousChildren()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        FailingWindowItemsControl control = new() { ItemsPanel = panel, FailFromIndex = 4 };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal("children=[0,1,2,3] first=0 contextOffset=0", DescribeWindow(control, panel));

        control.FailInPrepareItemContainer = true;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 32))));

        Assert.Equal("children=[0,1,2,3] first=0 contextOffset=0", DescribeWindow(control, panel));

        control.FailInPrepareItemContainer = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal("children=[1,2,3,4,5] first=1 contextOffset=20", DescribeWindow(control, panel));
    }

    [Fact]
    public void FailedOwnerWindowMoveAfterContainersArePreparedKeepsPreviousChildren()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        FailingWindowItemsControl control = new() { ItemsPanel = panel, FailFromIndex = 4 };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        control.FailInContainerPrepared = true;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));

        Assert.Equal("children=[0,1,2,3] first=0 contextOffset=0", DescribeWindow(control, panel));

        control.FailInContainerPrepared = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 32)));

        Assert.Equal("children=[1,2,3,4,5] first=1 contextOffset=20", DescribeWindow(control, panel));
    }

    // The failed window realizes item 4, and with OnItemContainerPrepared item 5 too, before the preparation throws.
    // The panel never takes them, so the generator must not keep them realized either.
    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 5)]
    public void FailedOwnerWindowMoveWhileCreatingContainersRealizesOnlyThePresentedChildren(
        bool failInPrepareItemContainer,
        int failFromIndex)
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        FailingWindowItemsControl control = new() { ItemsPanel = panel, FailFromIndex = failFromIndex };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        control.FailInPrepareItemContainer = failInPrepareItemContainer;
        control.FailInContainerPrepared = !failInPrepareItemContainer;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31 + attempt))));

            Assert.Equal("children=[0,1,2,3] first=0 contextOffset=0", DescribeWindow(control, panel));
            Assert.Equal([0, 1, 2, 3], RealizedIndices(control));
            Assert.Equal(4, control.RealizedItemCount);
            Assert.All(control.ItemContainerGenerator.RealizedContainers.Values, container => Assert.Same(panel, container.VisualParent));
        }

        control.FailInPrepareItemContainer = false;
        control.FailInContainerPrepared = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal("children=[1,2,3,4,5] first=1 contextOffset=20", DescribeWindow(control, panel));
        Assert.Equal([1, 2, 3, 4, 5], RealizedIndices(control));
    }

    [Fact]
    public void FailedRecycleAfterOwnerWindowMoveRestoresTheCommittedWindowOnALaterFailedInsert()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        Cerneala.UI.Layout.Panels.StackPanel element6 = new();
        FailingWindowItemsControl control = new() { ItemsPanel = panel };
        control.SetItems(Enumerable.Range(0, 100).Select(index => index == 6 ? (object)element6 : index));
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        control.FailInClearItemContainer = true;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));
        Assert.Equal("children=[1,2,3,4,5] first=1 contextOffset=20", DescribeWindow(control, panel));

        control.FailInClearItemContainer = false;
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(element6);
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 30, CacheItems: 1));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 32))));

        Assert.Equal("children=[1,2,3,4,5] first=1 contextOffset=20", DescribeWindow(control, panel));
        Assert.Equal([1, 2, 3, 4, 5], control.ItemContainerGenerator.RealizedContainers.Keys.Order());
    }

    // The panel rejects item 5, and the restore of the presented window 0..3 throws for item 0 after the generator
    // realized that window again. The panel gets back items 0..3, so the generator must not keep the container that the
    // failed move created for item 4 realized.
    [Fact]
    public void FailedRestoreAfterFailedOwnerInsertRealizesOnlyThePresentedChildren()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemElement x = new("x");
        FailingPreparedIndexItemsControl control = new() { ItemsPanel = panel };
        control.SetItems(Enumerable.Range(0, 100).Select(index => index == 5 ? (object)x : index));
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal("realized=[0,1,2,3] count=4 children=[0,1,2,3] realizedOutsidePanel=[]", DescribeRealized(control, panel));

        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);
        control.FailPreparedIndex = 0;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31 + attempt))));

            Assert.Equal("realized=[0,1,2,3] count=4 children=[0,1,2,3] realizedOutsidePanel=[]", DescribeRealized(control, panel));
            Assert.Equal(1, control.ItemContainerGenerator.RecyclePool.Count);
            ContentPresenter released = Assert.IsType<ContentPresenter>(control.ItemContainerGenerator.RecyclePool.Pop(typeof(ContentPresenter)));
            Assert.Null(released.Content);
            Assert.Null(released.VisualParent);
            control.ItemContainerGenerator.RecyclePool.Push(released);
        }

        otherParent.VisualChildren.Remove(x);
        control.FailPreparedIndex = -1;
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal("realized=[1,2,3,4,5] count=5 children=[1,2,3,4,x] realizedOutsidePanel=[]", DescribeRealized(control, panel));
    }

    // The new panel rejects item 5, and the restore of the presented window 0..3 in the old panel throws for item 0
    // after the generator realized e2, which is its own container, again under index 2. The old panel keeps the
    // previous container of item 2, so the generator must not keep e2 realized.
    [Fact]
    public void FailedRestoreAfterFailedOwnerPanelSwapRealizesOnlyThePresentedChildren()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel newPanel = new();
        ItemElement x = new("x");
        ObservableCollection<object> source = [.. Enumerable.Range(0, 100).Select(index => index == 5 ? (object)x : index)];
        FailingPreparedIndexItemsControl control = new() { ItemsPanel = oldPanel, ItemsSource = source };
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal("realized=[0,1,2,3] count=4 children=[0,1,2,3] realizedOutsidePanel=[]", DescribeRealized(control, oldPanel));

        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);
        source[2] = new ItemElement("e2");
        control.FailPreparedIndex = 0;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));

        Assert.Throws<InvalidOperationException>(() => control.ItemsPresenter.ItemsPanel = newPanel);

        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Empty(newPanel.VisualChildren);
        Assert.All(control.ItemContainerGenerator.RealizedContainers.Values, container => Assert.Same(oldPanel, container.VisualParent));
        Assert.Equal(control.ItemContainerGenerator.RealizedContainers.Count, control.RealizedItemCount);

        otherParent.VisualChildren.Remove(x);
        control.FailPreparedIndex = -1;
        control.Measure(new MeasureContext(new LayoutSize(100, 31)));

        Assert.Same(newPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal("realized=[1,2,3,4,5] count=5 children=[1,e2,3,4,x] realizedOutsidePanel=[]", DescribeRealized(control, newPanel));
    }

    // The failed move took item 0 out of the panel and inserted item 4 before the panel rejected item 5, and the restore
    // of the presented window 0..3 throws for item 0. The panel must get back the children of that window, which its
    // FirstRealizedIndex and context still describe, so each child is arranged at the row of its item.
    [Fact]
    public void FailedRestoreAfterFailedOwnerInsertKeepsThePreviousChildrenAtTheirRows()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemElement x = new("x");
        FailingPreparedIndexItemsControl control = new() { ItemsPanel = panel };
        control.SetItems(Enumerable.Range(0, 100).Select(index => index == 5 ? (object)x : index));
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 30)));

        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);
        control.FailPreparedIndex = 0;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31 + attempt))));

            Assert.Equal("children=[0,1,2,3] first=0 contextOffset=0", DescribeWindow(control, panel));
            panel.Measure(new MeasureContext(new LayoutSize(100, 30 + attempt)));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 30)));
            Assert.Equal(
                "0@0,1@10,2@20,3@30",
                string.Join(",", panel.VisualChildren.Select(child =>
                    $"{Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItem(child)}@{child.ArrangedBounds.Y}")));
        }

        otherParent.VisualChildren.Remove(x);
        control.FailPreparedIndex = -1;
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal("realized=[1,2,3,4,5] count=5 children=[1,2,3,4,x] realizedOutsidePanel=[]", DescribeRealized(control, panel));
        Assert.Equal(1, panel.FirstRealizedIndex);
    }

    // The same double failure, after item 2 was replaced by an element that is its own container. The container that
    // showed "2" is displaced and the failed move took it out of the panel, so the panel must hold it again, still
    // prepared and outside RecyclePool, until a refresh succeeds.
    [Fact]
    public void FailedRestoreAfterFailedOwnerInsertKeepsADisplacedContainerInThePanel()
    {
        Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel = new();
        ItemElement x = new("x");
        ObservableCollection<object> source = [.. Enumerable.Range(0, 100).Select(index => index == 5 ? (object)x : $"{index}")];
        FailingPreparedIndexItemsControl control = new() { ItemsPanel = panel, ItemsSource = source };
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal("children=[0,1,2,3] pool=[]", DescribePresented(control, panel));

        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);
        source[2] = new ItemElement("e2");
        control.FailPreparedIndex = 0;
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31 + attempt))));

            // The pooled container is the one the failed move created for item 4.
            Assert.Equal("children=[0,1,2,3] pool=[unparented]", DescribePresented(control, panel));
            Assert.Equal(0, panel.FirstRealizedIndex);
        }

        otherParent.VisualChildren.Remove(x);
        control.FailPreparedIndex = -1;
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal("children=[1,e2,3,4,x] pool=[unparented,unparented]", DescribePresented(control, panel));
        Assert.Equal(1, panel.FirstRealizedIndex);
    }

    // The presenter creates its items panel during the first measure, after the inherited phase of that frame.
    // The panel and the containers must still inherit the owner's values in that frame.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InheritedValuesSetBeforeTheFirstLayoutReachThePanelCreatedDuringMeasure(bool listBox)
    {
        SolidColorBrush foreground = new(Color.White);
        ItemsControl control = listBox ? new ListBox() : new ItemsControl();
        control.FontSize = 30;
        control.FontFamily = "Body";
        control.Foreground = foreground;
        control.SetItems(new[] { "a" });
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(control);

        root.ProcessFrame();

        Cerneala.UI.Layout.Panels.Panel panel = control.ItemsPresenter.LayoutPanelRoot!;
        UIElement container = control.ItemContainerGenerator.RealizedContainers[0];
        Assert.Same(panel, container.VisualParent);
        Assert.Equal(
            "panel=30/Body/True/Inherited container=30/Body/True/Inherited",
            $"panel={DescribeInherited(panel, foreground)} container={DescribeInherited(container, foreground)}");
        // A ListBoxItem holds a string item without a visual child, so only the ContentPresenter row has text.
        TextBlock? text = listBox ? null : Assert.IsType<TextBlock>(((ContentPresenter)container).PresentedChild);
        if (text is not null)
        {
            Assert.Equal("a", text.Text);
            Assert.Equal(30, text.FontSize);
            Assert.Equal("Body", text.FontFamily);
            Assert.Same(foreground, text.Foreground);
        }

        control.FontSize = 31;
        root.ProcessFrame();

        Assert.Equal(31, container.GetValue(Control.FontSizeProperty));
        Assert.Equal(31, text?.FontSize ?? 31);
    }

    // The ContentPresenter re-creates its template child during its measure, after the inherited phase of that
    // frame, which queues the ContentPresenter for that phase. An ItemsPresenter measured in the same measure must
    // leave that work queued, so the new subtree inherits the ContentPresenter's values in the next frame.
    [Fact]
    public void InheritedWorkThatAnAncestorQueuesDuringTheSameMeasureReachesItsNewSubtree()
    {
        TextBlock? label = null;
        ItemsControl? inner = null;
        ContentPresenter presenter = new()
        {
            FontSize = 30,
            Content = "data",
            ContentTemplate = new ContentTemplate("t", typeof(string), null, 0, _ =>
            {
                label = new TextBlock { Text = "label" };
                inner = new ItemsControl();
                inner.SetItems(new[] { "x" });
                Cerneala.UI.Layout.Panels.StackPanel stack = new();
                stack.VisualChildren.Add(label);
                stack.VisualChildren.Add(inner);
                return stack;
            })
        };
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        presenter.ContentIndex = 1;
        presenter.Invalidate(InvalidationFlags.Measure, "test");
        root.ProcessFrame();
        root.ProcessFrame();

        Cerneala.UI.Layout.Panels.Panel panel = inner!.ItemsPresenter.LayoutPanelRoot!;
        UIElement container = inner.ItemContainerGenerator.RealizedContainers[0];
        Assert.Equal(
            "label=30/Inherited inner=30/Inherited presenter=30/Inherited panel=30/Inherited container=30/Inherited",
            $"label={DescribeFontSize(label!)} inner={DescribeFontSize(inner)} " +
            $"presenter={DescribeFontSize(inner.ItemsPresenter)} panel={DescribeFontSize(panel)} " +
            $"container={DescribeFontSize(container)}");
    }

    // Adding the template child during the ContentPresenter's measure also queues the new Border for the Aspect
    // phase. The ItemsPresenter below it performs only its panel's Aspect work, so it must leave the Border's queued.
    [Fact]
    public void AspectWorkThatAnAncestorQueuesDuringTheSameMeasureIsApplied()
    {
        Border? border = null;
        ContentPresenter presenter = new()
        {
            Content = "data",
            ContentTemplate = new ContentTemplate("t", typeof(string), null, 0, _ =>
            {
                ItemsControl inner = new();
                inner.SetItems(new[] { "x" });
                border = new Border { Child = inner };
                return border;
            })
        };
        UIRoot root = new(300, 200);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components => components.AddRule(
            new AspectRuleSet(
                "border",
                AspectLayer.App,
                new AspectTarget(typeof(Border)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))],
                0))));
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        presenter.ContentIndex = 1;
        presenter.Invalidate(InvalidationFlags.Measure, "test");
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, border!.GetValueSource(Control.BackgroundProperty));
    }

    private static string DescribeFontSize(UIElement element)
    {
        return $"{element.GetValue(Control.FontSizeProperty)}/{element.GetValueSource(Control.FontSizeProperty)}";
    }

    private static string DescribeInherited(UIElement element, SolidColorBrush foreground)
    {
        return $"{element.GetValue(Control.FontSizeProperty)}/{element.GetValue(Control.FontFamilyProperty)}/" +
            $"{ReferenceEquals(foreground, element.GetValue(Control.ForegroundProperty))}/" +
            $"{element.GetValueSource(Control.FontSizeProperty)}";
    }

    // Each child is described by the item it shows, and each realized container that the panel does not present is listed.
    private static string DescribeRealized(ItemsControl control, Cerneala.UI.Layout.Panels.Panel panel)
    {
        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        string children = string.Join(",", panel.VisualChildren.Select(child =>
            Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItem(child) is ItemElement element
                ? element.Value
                : $"{Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItem(child)}"));
        string outside = string.Join(",", realized
            .Where(pair => !ReferenceEquals(pair.Value.VisualParent, panel))
            .Select(pair => pair.Key)
            .Order());
        return $"realized=[{string.Join(",", realized.Keys.Order())}] count={control.RealizedItemCount} children=[{children}] realizedOutsidePanel=[{outside}]";
    }

    [Fact]
    public void FailedOwnerPanelSwapAfterTheGeneratorRecycledAContainerRestoresThePresentedItems()
    {
        Cerneala.UI.Layout.Panels.StackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.StackPanel newPanel = new();
        System.Collections.ObjectModel.ObservableCollection<object> source = ["0", "1", "2", "3"];
        ItemsControl control = new() { ItemsPanel = oldPanel, ItemsSource = source };
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal("children=[0,1,2,3] pool=0 pooledWithParent=0", DescribeOwnerPanel(control, oldPanel));

        // The item that is its own container makes the next realization recycle the container that showed "2",
        // and the appended element still has another parent, so placing it in the new panel throws.
        ItemElement e2 = new("e2");
        ItemElement x = new("x");
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(x);
        source[2] = e2;
        source.Add(x);

        Assert.Throws<InvalidOperationException>(() => control.ItemsPresenter.ItemsPanel = newPanel);

        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal("children=[0,1,e2,3] pool=1 pooledWithParent=0", DescribeOwnerPanel(control, oldPanel));

        // While the cause stays, each retry throws again and keeps the same presentation.
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));
        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal("children=[0,1,e2,3] pool=1 pooledWithParent=0", DescribeOwnerPanel(control, oldPanel));

        otherParent.VisualChildren.Remove(x);
        control.Measure(new MeasureContext(new LayoutSize(100, 102)));

        Assert.Same(newPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal("children=[0,1,e2,3,x] pool=1 pooledWithParent=0", DescribeOwnerPanel(control, newPanel));
        Assert.Empty(oldPanel.VisualChildren);
    }

    // Each child must be the container realized under its item's index and show that item. The pool is emptied and
    // refilled in the same order to count pooled containers that still have a visual or logical parent.
    private static string DescribeOwnerPanel(ItemsControl control, Cerneala.UI.Layout.Panels.Panel panel)
    {
        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        string children = string.Join(",", panel.VisualChildren.Select(child =>
        {
            int index = Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItemIndex(child);
            object? content = child is ContentPresenter presenter ? presenter.Content : (child as ItemElement)?.Value;
            bool showsItem = index >= 0 &&
                realized.TryGetValue(index, out UIElement? container) &&
                ReferenceEquals(container, child) &&
                Equals(child is ContentPresenter ? content : child, control.GetItemAt(index));
            return showsItem ? $"{content}" : $"unrealized:{content ?? "null"}";
        }));

        Cerneala.UI.Controls.Items.ItemContainerRecyclePool pool = control.ItemContainerGenerator.RecyclePool;
        Stack<UIElement> pooled = [];
        while (pool.Pop(typeof(ContentPresenter)) is UIElement container)
        {
            pooled.Push(container);
        }

        int pooledWithParent = pooled.Count(container => container.VisualParent is not null || container.LogicalParent is not null);
        while (pooled.TryPop(out UIElement? container))
        {
            pool.Push(container);
        }

        return $"children=[{children}] pool={pool.Count} pooledWithParent={pooledWithParent}";
    }

    [Fact]
    public void FailedOwnerRefreshKeepsAContainerThatAReplacedItemOfAnotherContainerTypeDisplaced()
    {
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        ObservableCollection<object> source = ["0", "1", "2", "3"];
        FailingItemItemsControl control = new() { ItemsPanel = panel, ItemsSource = source };
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal("children=[0,1,2,3] pool=[]", DescribePresented(control, panel));

        source[2] = new ItemElement("e2");
        source[3] = "3b";
        control.FailItem = "3b";
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));
        Assert.Equal("children=[0,1,2,3] pool=[]", DescribePresented(control, panel));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 102))));
        Assert.Equal("children=[0,1,2,3] pool=[]", DescribePresented(control, panel));

        control.FailItem = null;
        control.Measure(new MeasureContext(new LayoutSize(100, 103)));

        Assert.Equal("children=[0,1,e2,3b] pool=[unparented]", DescribePresented(control, panel));
    }

    [Fact]
    public void FailedOwnerRefreshKeepsAContainerThatAMovedOwnContainerDisplaced()
    {
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        ItemElement e = new("E");
        ObservableCollection<object> source = ["0", "1", e, "3"];
        FailingItemItemsControl control = new() { ItemsPanel = panel, ItemsSource = source };
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal("children=[0,1,E,3] pool=[]", DescribePresented(control, panel));

        source[2] = "2b";
        source[0] = e;
        control.FailItem = "2b";
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));
        // The container created for "2b", whose preparation threw, goes to the pool cleared and unparented.
        Assert.Equal("children=[0,1,E,3] pool=[unparented]", DescribePresented(control, panel));
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 102))));
        Assert.Equal("children=[0,1,E,3] pool=[unparented]", DescribePresented(control, panel));

        control.FailItem = null;
        control.Measure(new MeasureContext(new LayoutSize(100, 103)));

        Assert.Equal("children=[E,1,2b,3] pool=[unparented]", DescribePresented(control, panel));
    }

    [Fact]
    public void ItemsOwnerChangeAfterFailedRefreshDoesNotLetALaterCommitDetachADisplacedOwnContainerFromAnotherParent()
    {
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        ItemElement x = new("x");
        ObservableCollection<object> source = [x, "1"];
        FailingPrepareItemsControl control = new() { ItemsPanel = panel, ItemsSource = source };
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal("children=[x,1] pool=[]", DescribePresented(control, panel));

        source[0] = "0b";
        control.Fail = true;
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));
        // The container created for "0b", whose prepared callback threw, goes to the pool; x stays displaced.
        Assert.Equal("children=[x,1] pool=[unparented]", DescribePresented(control, panel));

        control.ItemsPresenter!.ItemsOwner = null;
        Assert.Null(x.VisualParent);
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.LogicalChildren.Add(x);
        otherParent.VisualChildren.Add(x);

        control.ItemsPresenter.ItemsOwner = control;
        control.Fail = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 102)));

        Assert.Equal(
            "xVisualParentIsOther=True xLogicalParentIsOther=True otherChildren=1",
            $"xVisualParentIsOther={ReferenceEquals(x.VisualParent, otherParent)} " +
            $"xLogicalParentIsOther={ReferenceEquals(x.LogicalParent, otherParent)} " +
            $"otherChildren={otherParent.VisualChildren.Count}");
        Assert.Equal("children=[0b,1] pool=[]", DescribePresented(control, panel));
        Assert.Equal([0, 1], control.ItemContainerGenerator.RealizedContainers.Keys.Order());
    }

    [Fact]
    public void ItemsOwnerChangeAfterFailedRefreshGivesBackADisplacedGeneratedContainer()
    {
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        ItemElement y = new("y");
        ObservableCollection<object> source = ["0", "1"];
        FailingPrepareItemsControl control = new() { ItemsPanel = panel, ItemsSource = source };
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal("children=[0,1] pool=[]", DescribePresented(control, panel));

        source[0] = y;
        control.Fail = true;
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));
        Assert.Equal("children=[0,1] pool=[]", DescribePresented(control, panel));

        control.ItemsPresenter!.ItemsOwner = null;

        // The container that showed "0" no longer represents an index, so the owner change clears it and pools it.
        Assert.Equal("children=[] pool=[unparented]", DescribePresented(control, panel));

        control.ItemsPresenter.ItemsOwner = control;
        control.Fail = false;
        control.Measure(new MeasureContext(new LayoutSize(100, 102)));

        Assert.Equal("children=[y,1] pool=[unparented]", DescribePresented(control, panel));
    }

    // Each child is described by the item it shows and must have the panel as its visual and logical parent,
    // and each pooled container is described by its parents, so a pooled container that a panel still holds shows.
    private static string DescribePresented(ItemsControl control, Cerneala.UI.Layout.Panels.Panel panel)
    {
        string children = string.Join(",", panel.VisualChildren.Select(child =>
        {
            object? shown = child is ContentPresenter presenter ? presenter.Content : child;
            string description = shown is ItemElement element ? element.Value : $"{shown}";
            return ReferenceEquals(child.LogicalParent, panel) ? description : $"{description}(no logical parent)";
        }));
        int pooledCount = control.ItemContainerGenerator.RecyclePool.Count;
        List<UIElement> pooled = [];
        while (control.ItemContainerGenerator.RecyclePool.Pop(typeof(ContentPresenter)) is UIElement container)
        {
            pooled.Add(container);
        }

        foreach (UIElement container in Enumerable.Reverse(pooled))
        {
            control.ItemContainerGenerator.RecyclePool.Push(container);
        }

        Assert.Equal(pooledCount, pooled.Count);
        string pool = string.Join(",", pooled.Select(container =>
            container.VisualParent is null && container.LogicalParent is null ? "unparented" : "parented"));
        return $"children=[{children}] pool=[{pool}]";
    }

    // Each child must be the container realized under its item's index, so a panel that kept a recycled
    // container, or a container realized under another index, does not describe as its item.
    private static string DescribeWindow(ItemsControl control, Cerneala.UI.Layout.Panels.VirtualizingStackPanel panel)
    {
        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        string children = string.Join(",", panel.VisualChildren.Select(child =>
        {
            object? item = Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItem(child);
            return item is int index && realized.TryGetValue(index, out UIElement? container) && ReferenceEquals(container, child)
                ? index.ToString()
                : $"unrealized:{item ?? "null"}";
        }));
        return $"children=[{children}] first={panel.FirstRealizedIndex} contextOffset={panel.VirtualizationContext?.ScrollOffset}";
    }

    private static object?[] ContainerContents(Cerneala.UI.Layout.Panels.Panel panel)
    {
        return [.. panel.VisualChildren.Cast<ContentPresenter>().Select(container => container.Content)];
    }

    private sealed class FailingWindowItemsControl : ItemsControl
    {
        public bool FailInPrepareItemContainer { get; set; }

        public bool FailInContainerPrepared { get; set; }

        public bool FailInClearItemContainer { get; set; }

        public int FailFromIndex { get; set; }

        protected internal override void ClearItemContainer(UIElement container)
        {
            base.ClearItemContainer(container);
            if (FailInClearItemContainer)
            {
                throw new InvalidOperationException("container clearing failed");
            }
        }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            base.PrepareItemContainer(container, index, item);
            if (FailInPrepareItemContainer && index >= FailFromIndex)
            {
                throw new InvalidOperationException("container preparation failed");
            }
        }

        protected internal override void OnItemContainerPrepared(UIElement container, int index)
        {
            if (FailInContainerPrepared && index >= FailFromIndex)
            {
                throw new InvalidOperationException("container preparation failed");
            }

            base.OnItemContainerPrepared(container, index);
        }
    }

    private sealed class FailingItemItemsControl : ItemsControl
    {
        public object? FailItem { get; set; }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            if (FailItem is not null && Equals(item, FailItem))
            {
                throw new InvalidOperationException("container preparation failed");
            }

            base.PrepareItemContainer(container, index, item);
        }
    }

    private sealed class FailingPrepareItemsControl : ItemsControl
    {
        public bool Fail { get; set; }

        protected internal override void OnItemContainerPrepared(UIElement container, int index)
        {
            if (Fail)
            {
                throw new InvalidOperationException("container preparation failed");
            }

            base.OnItemContainerPrepared(container, index);
        }
    }

    private sealed class FailingPreparedIndexItemsControl : ItemsControl
    {
        public int FailPreparedIndex { get; set; } = -1;

        protected internal override void OnItemContainerPrepared(UIElement container, int index)
        {
            if (index == FailPreparedIndex)
            {
                throw new InvalidOperationException("container prepared callback failed");
            }

            base.OnItemContainerPrepared(container, index);
        }
    }

    private static (Menu Menu, ScrollViewer Viewer, UIRoot Root) CreateScrolledMenu(string[] items)
    {
        Menu menu = new()
        {
            ItemsSource = items,
            ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()
        };
        ScrollViewer viewer = new()
        {
            Content = menu,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(200, 100);
        root.VisualChildren.Add(viewer);
        return (menu, viewer, root);
    }

    private static int[] RealizedIndices(ItemsControl control)
    {
        return [.. control.ItemContainerGenerator.RealizedContainers.Keys.Order()];
    }

    private class ItemElement(string value) : UIElement
    {
        public string Value { get; } = value;
    }

    private sealed class FixedElement(string value) : ItemElement(value)
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(100, 10);
        }
    }

    private sealed class LifecycleElement(string value) : ItemElement(value)
    {
        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }

        protected override void OnDetached()
        {
            DetachedCount++;
        }
    }
}
