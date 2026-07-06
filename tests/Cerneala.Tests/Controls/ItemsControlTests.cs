using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemsControlTests
{
    [Fact]
    public void ItemsControlExposesRetainedItemCollection()
    {
        ItemsControl control = new();

        control.Items.Add("one");
        control.Items.Add("two");

        Assert.Equal(2, control.Items.Count);
        Assert.Equal("one", control.Items[0]);
        Assert.Equal("two", control.Items[1]);
    }

    [Fact]
    public void ItemsControlUsesDataTemplateAndItemsPanel()
    {
        ItemsControl control = new()
        {
            ItemTemplate = new DataTemplate<string>(value => new FixedElement(value, new LayoutSize(10, 5))),
            ItemsPanel = new ItemsPanelTemplate(() => new Cerneala.UI.Controls.Panel())
        };
        control.SetItems(new[] { "a", "b" });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<Cerneala.UI.Controls.Panel>(control.ItemsPresenter.PanelRoot);
        Assert.Equal(2, control.ItemsPresenter.PanelRoot!.VisualChildren.Count);
        ContentPresenter firstContainer = Assert.IsType<ContentPresenter>(control.ItemsPresenter.PanelRoot.VisualChildren[0]);
        Assert.IsType<FixedElement>(firstContainer.PresentedChild);
    }

    [Fact]
    public void ItemTemplateCreatesContainerForElementItem()
    {
        UIElement item = new();
        ItemsControl control = new()
        {
            ItemTemplate = new DataTemplate<UIElement>(_ => new FixedElement("templated", new LayoutSize(10, 5))),
            ItemsPanel = new ItemsPanelTemplate(() => new Cerneala.UI.Controls.Panel())
        };
        control.SetItems(new[] { item });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        ContentPresenter container = Assert.IsType<ContentPresenter>(control.ItemsPresenter.PanelRoot!.VisualChildren[0]);
        FixedElement child = Assert.IsType<FixedElement>(container.PresentedChild);
        Assert.Equal("templated", child.Value);
        Assert.Null(item.LogicalParent);
        Assert.Null(item.VisualParent);
    }

    [Fact]
    public void ItemsControlVirtualizationRealizesOnlyWindow()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));

        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        Assert.Equal(new RealizationWindow(0, 4), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(4, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren.Count);
    }

    [Fact]
    public void UpdatingVirtualizationContextRefreshesPresenterWhenMeasureSizeIsUnchanged()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0));
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);

        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 30));
        control.Measure(context);

        Assert.Equal(new RealizationWindow(3, 6), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(3, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
    }

    [Fact]
    public void UpdatingVirtualizationFromScrollInfoRefreshesPresenterWhenWindowChanges()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel())
        };
        TestScrollInfo scrollInfo = new()
        {
            ViewportHeight = 30
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.UpdateVirtualizationFromScrollInfo(scrollInfo, itemExtent: 10);
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);

        scrollInfo.SetVerticalOffset(30);
        control.UpdateVirtualizationFromScrollInfo(scrollInfo, itemExtent: 10);
        control.Measure(context);

        Assert.Equal(new RealizationWindow(3, 6), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(3, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
    }

    private sealed class FixedElement(string value, LayoutSize size) : UIElement
    {
        public string Value { get; } = value;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class TestScrollInfo : IScrollInfo
    {
        public float HorizontalOffset { get; private set; }

        public float VerticalOffset { get; private set; }

        public float ExtentWidth { get; set; }

        public float ExtentHeight { get; set; }

        public float ViewportWidth { get; set; }

        public float ViewportHeight { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public void SetHorizontalOffset(float offset)
        {
            HorizontalOffset = offset;
        }

        public void SetVerticalOffset(float offset)
        {
            VerticalOffset = offset;
        }
    }
}
