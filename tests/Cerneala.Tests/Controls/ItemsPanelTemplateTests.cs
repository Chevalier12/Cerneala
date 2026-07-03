using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ItemsPanelTemplateTests
{
    [Fact]
    public void ItemsPanelTemplateCreatesPanelRoot()
    {
        ItemsPanelTemplate template = new(() => new Panel());

        Panel panel = template.CreatePanel();

        Assert.NotNull(panel);
    }

    [Fact]
    public void ItemsPresenterMaterializesItemsThroughDataTemplate()
    {
        ItemsPresenter presenter = new()
        {
            Items = new[] { "one", "two" },
            ItemTemplate = new DataTemplate<string>(value => new ItemElement(value))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Panel panel = presenter.PanelRoot!;
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
            ItemTemplate = new DataTemplate<string>(value => new ItemElement(value))
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement oldChild = presenter.PanelRoot!.VisualChildren[0];

        presenter.Items = new[] { "new-1", "new-2" };

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Equal(["new-1", "new-2"], presenter.PanelRoot!.VisualChildren.Cast<ItemElement>().Select(item => item.Value));
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
        Panel oldPanel = presenter.PanelRoot!;

        presenter.Items = new UIElement[] { item };

        Panel newPanel = presenter.PanelRoot!;
        Assert.NotSame(oldPanel, newPanel);
        Assert.Empty(oldPanel.VisualChildren);
        Assert.Empty(oldPanel.LogicalChildren);
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
            ItemTemplate = new DataTemplate<string>(value =>
            {
                created++;
                return new ItemElement(value);
            })
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement child = presenter.PanelRoot!.VisualChildren[0];
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, created);
        Assert.Same(child, presenter.PanelRoot!.VisualChildren[0]);
    }

    private sealed class ItemElement(string value) : UIElement
    {
        public string Value { get; } = value;
    }
}
