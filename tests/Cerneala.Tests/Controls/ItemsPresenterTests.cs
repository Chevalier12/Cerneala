using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
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

    private static object?[] ContainerContents(Cerneala.UI.Layout.Panels.Panel panel)
    {
        return [.. panel.VisualChildren.Cast<ContentPresenter>().Select(container => container.Content)];
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
