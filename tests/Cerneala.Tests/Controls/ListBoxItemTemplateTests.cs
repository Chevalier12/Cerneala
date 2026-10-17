using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ListBoxItemTemplateTests
{
    [Fact]
    public void ItemTemplateCreatesOneTemplateElementPerListBoxItem()
    {
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0,
                context => new Fixed(new LayoutSize(80, 10), context.Data))
        };
        listBox.ItemsSource = new[] { "a", "b" };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        Assert.Equal(2, listBox.RealizedItemCount);
        for (int index = 0; index < 2; index++)
        {
            ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[index]);
            Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
            Assert.Equal(index == 0 ? "a" : "b", element.Data);
            Assert.Equal(new LayoutSize(80, 10), row.DesiredSize);
        }

        Assert.Equal(new LayoutSize(80, 20), listBox.DesiredSize);
    }

    [Fact]
    public void ItemTemplateKeySelectsTheKeyedTemplateOfTemplatesForListBoxItems()
    {
        ListBox listBox = new() { ItemTemplateKey = "wide" };
        listBox.Templates.Add(new ContentTemplate<string>("narrow", key: null, priority: 0,
            context => new Fixed(new LayoutSize(40, 5), context.Data)));
        listBox.Templates.Add(new ContentTemplate<string>("wide", key: "wide", priority: 0,
            context => new Fixed(new LayoutSize(90, 12), context.Data)));
        listBox.ItemsSource = new[] { "a" };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Equal("a", element.Data);
        Assert.Equal(new LayoutSize(90, 12), row.DesiredSize);
    }

    [Fact]
    public void RecycledListBoxItemPresentsTheTemplateElementOfItsNewItem()
    {
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0,
                context => new Fixed(new LayoutSize(80, 10), context.Data))
        };
        listBox.ItemsSource = new[] { "a" };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);

        listBox.ItemsSource = new[] { "b" };
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(first, row);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Equal("b", element.Data);
        Assert.Equal(new LayoutSize(80, 10), row.DesiredSize);
    }

    // As in WPF, the row's presenter tries ItemTemplate before UIElement content, so a UIElement item is only the template's data.
    [Fact]
    public void UIElementItemIsPresentedThroughItemTemplateByItsListBoxItem()
    {
        Fixed item = new(new LayoutSize(30, 7), data: null);
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<UIElement>("row", key: null, priority: 0,
                context => new Fixed(new LayoutSize(80, 10), context.Data))
        };
        listBox.SetItems(new UIElement[] { item });
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(item, row.Content);
        Assert.Null(item.VisualParent);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.NotSame(item, element);
        Assert.Same(item, element.Data);
        Assert.Equal(
            new LayoutSize(
                80 + row.Padding.Horizontal + row.BorderThickness.Horizontal,
                10 + row.Padding.Vertical + row.BorderThickness.Vertical),
            row.DesiredSize);
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class Fixed(LayoutSize size, object? data) : UIElement
    {
        public object? Data { get; } = data;

        protected override LayoutSize MeasureCore(MeasureContext context) => size;
    }
}
