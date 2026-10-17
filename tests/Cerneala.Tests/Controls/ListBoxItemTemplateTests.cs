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

    // WPF runs ItemTemplate for a null item too (ContentPresenter.ChooseTemplate has no null check on Content).
    [Fact]
    public void FreshListBoxItemRunsTheItemTemplateForANullItem()
    {
        int calls = 0;
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0, context =>
            {
                calls++;
                return new Fixed(new LayoutSize(80, 10), context.Data);
            })
        };
        listBox.ItemsSource = new string?[] { null };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        Assert.Equal(1, calls);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Null(element.Data);
        Assert.Equal(new LayoutSize(80, 10), row.DesiredSize);
    }

    [Fact]
    public void RecycledListBoxItemRunsTheItemTemplateForANullItem()
    {
        int calls = 0;
        ListBox listBox = new()
        {
            ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0, context =>
            {
                calls++;
                return new Fixed(new LayoutSize(80, 10), context.Data);
            })
        };
        listBox.ItemsSource = new string?[] { "A" };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        calls = 0;

        listBox.ItemsSource = new string?[] { null };
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(first, row);
        Assert.Equal(1, calls);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Null(element.Data);
        Assert.Equal(new LayoutSize(80, 10), row.DesiredSize);
    }

    [Fact]
    public void FreshAndRecycledListBoxItemsPresentANullItemIdentically()
    {
        ContentTemplate<string> template = new("row", key: null, priority: 0,
            context => new Fixed(new LayoutSize(80, 10), context.Data));

        ListBox fresh = new() { ItemTemplate = template };
        fresh.ItemsSource = new string?[] { null };
        UIRoot freshRoot = new(100, 100);
        freshRoot.VisualChildren.Add(fresh);
        freshRoot.ProcessFrame();

        ListBox recycled = new() { ItemTemplate = template };
        recycled.ItemsSource = new string?[] { "A" };
        UIRoot recycledRoot = new(100, 100);
        recycledRoot.VisualChildren.Add(recycled);
        recycledRoot.ProcessFrame();
        recycled.ItemsSource = new string?[] { null };
        recycledRoot.ProcessFrame();

        ListBoxItem freshRow = Assert.IsType<ListBoxItem>(fresh.ItemContainerGenerator.RealizedContainers[0]);
        ListBoxItem recycledRow = Assert.IsType<ListBoxItem>(recycled.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal(recycledRow.DesiredSize, freshRow.DesiredSize);
        Assert.Equal(
            Descendants(recycledRow).Select(element => element.GetType()),
            Descendants(freshRow).Select(element => element.GetType()));
        Assert.Equal(recycled.DesiredSize, fresh.DesiredSize);
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
