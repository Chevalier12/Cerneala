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

    // WPF applies ItemTemplate only to items that are not visuals; a UIElement item is the row's own content.
    [Fact]
    public void UIElementItemIsHostedDirectlyByItsListBoxItemWhenItemTemplateIsSet()
    {
        Fixed item = new(new LayoutSize(30, 7), data: null);
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0,
                context => new Fixed(new LayoutSize(80, 10), context.Data))
        };
        listBox.SetItems(new UIElement[] { item });
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(item, row.Content);
        Assert.Same(row, item.VisualParent);
        Assert.Same(item, Assert.Single(Descendants(row).OfType<Fixed>()));
        Assert.Equal(new LayoutSize(30, 7), row.DesiredSize);
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
