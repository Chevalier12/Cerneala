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
