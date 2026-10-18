using System.Collections.ObjectModel;
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

    // The row presents a UIElement item in ContentPresenter's order: ItemTemplate, then the registry with
    // ItemTemplateKey, then the element itself, as WPF's DefaultSelector finds a DataTemplate before UIElementContentTemplate.
    [Fact]
    public void ItemTemplateKeySelectsTheKeyedTemplateOfTemplatesForAUIElementItem()
    {
        int calls = 0;
        Fixed item = new(new LayoutSize(30, 7), data: null);
        ListBox listBox = new() { ItemTemplateKey = "wide" };
        listBox.Templates.Add(new ContentTemplate<UIElement>("wide", key: "wide", priority: 0, context =>
        {
            calls++;
            return new Fixed(new LayoutSize(90, 12), context.Data);
        }));
        listBox.SetItems(new UIElement[] { item });
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        Assert.Equal(1, calls);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(item, row.Content);
        Assert.Null(item.VisualParent);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Same(item, element.Data);
        Assert.Equal(
            new LayoutSize(
                90 + row.Padding.Horizontal + row.BorderThickness.Horizontal,
                12 + row.Padding.Vertical + row.BorderThickness.Vertical),
            row.DesiredSize);
    }

    [Fact]
    public void UnkeyedTemplateOfTemplatesMatchingTheItemTypePresentsAUIElementItem()
    {
        int calls = 0;
        Fixed item = new(new LayoutSize(30, 7), data: null);
        ListBox listBox = new();
        listBox.Templates.Add(new ContentTemplate<Fixed>("byType", key: null, priority: 0, context =>
        {
            calls++;
            return new Fixed(new LayoutSize(80, 10), context.Data);
        }));
        listBox.SetItems(new UIElement[] { item });
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);

        root.ProcessFrame();

        Assert.Equal(1, calls);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Null(item.VisualParent);
        Fixed element = Assert.Single(Descendants(row).OfType<Fixed>());
        Assert.Same(item, element.Data);
    }

    [Fact]
    public void UIElementItemWithoutAMatchingTemplateIsHostedDirectlyByItsListBoxItem()
    {
        int calls = 0;
        Fixed keyedItem = new(new LayoutSize(30, 7), data: null);
        ListBox keyed = new() { ItemTemplateKey = "wide" };
        keyed.Templates.Add(new ContentTemplate<UIElement>("unkeyed", key: null, priority: 0, context =>
        {
            calls++;
            return new Fixed(new LayoutSize(90, 12), context.Data);
        }));
        keyed.SetItems(new UIElement[] { keyedItem });
        Fixed typedItem = new(new LayoutSize(30, 7), data: null);
        ListBox typed = new();
        typed.Templates.Add(new ContentTemplate<string>("text", key: null, priority: 0, context =>
        {
            calls++;
            return new Fixed(new LayoutSize(90, 12), context.Data);
        }));
        typed.SetItems(new UIElement[] { typedItem });
        UIRoot keyedRoot = new(100, 100);
        keyedRoot.VisualChildren.Add(keyed);
        UIRoot typedRoot = new(100, 100);
        typedRoot.VisualChildren.Add(typed);

        keyedRoot.ProcessFrame();
        typedRoot.ProcessFrame();

        Assert.Equal(0, calls);
        Assert.Same(keyed.ItemContainerGenerator.RealizedContainers[0], keyedItem.VisualParent);
        Assert.Same(typed.ItemContainerGenerator.RealizedContainers[0], typedItem.VisualParent);
    }

    [Fact]
    public void RecycledListBoxItemMovesBetweenATemplatedAndADirectlyHostedUIElementItem()
    {
        Fixed templated = new(new LayoutSize(30, 7), data: null);
        Plain direct = new();
        ListBox listBox = new();
        listBox.Templates.Add(new ContentTemplate<Fixed>("wrap", key: null, priority: 0,
            context => new Wrapper(context.Data)));
        listBox.ItemsSource = new UIElement[] { templated };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Wrapper wrapper = Assert.Single(Descendants(row).OfType<Wrapper>());
        Assert.Same(wrapper, templated.VisualParent);

        listBox.ItemsSource = new UIElement[] { direct };
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, direct.VisualParent);
        Assert.Null(templated.VisualParent);
        Assert.Empty(Descendants(row).OfType<Wrapper>());

        listBox.ItemsSource = new UIElement[] { templated };
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Null(direct.VisualParent);
        Assert.IsType<Wrapper>(templated.VisualParent);
        Assert.Same(row, Ancestors(templated).OfType<ListBoxItem>().First());
    }

    // A replaced item is prepared into its realized row without clearing the row first.
    [Fact]
    public void ReplacingATemplatedUIElementItemWithADirectlyHostedOneReleasesTheTemplatedItem()
    {
        Fixed templated = new(new LayoutSize(30, 7), data: null);
        Plain direct = new();
        ObservableCollection<UIElement> items = [templated];
        ListBox listBox = new();
        listBox.Templates.Add(new ContentTemplate<Fixed>("wrap", key: null, priority: 0,
            context => new Wrapper(context.Data)));
        listBox.ItemsSource = items;
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.IsType<Wrapper>(templated.VisualParent);

        items[0] = direct;
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, direct.VisualParent);
        Assert.Null(templated.VisualParent);

        items[0] = templated;
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Null(direct.VisualParent);
        Assert.IsType<Wrapper>(templated.VisualParent);
        Assert.Same(row, Ancestors(templated).OfType<ListBoxItem>().First());
    }

    // The row releases the item it really hosted, whatever a template predicate answers when the item leaves.
    [Fact]
    public void RecycledListBoxItemReleasesADirectlyHostedItemThatATemplatePredicateStartedToMatch()
    {
        HashSet<UIElement> wrapped = [];
        Plain item = new();
        Plain other = new();
        ListBox listBox = new();
        listBox.Templates.Add(new ContentTemplate<Plain>("wrap", key: null, priority: 0,
            context => new Wrapper(context.Data),
            predicate: context => context.Data is Plain plain && wrapped.Contains(plain)));
        listBox.ItemsSource = new UIElement[] { item };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, item.VisualParent);

        wrapped.Add(item);
        listBox.ItemsSource = new UIElement[] { other };
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, other.VisualParent);
        Assert.Null(item.VisualParent);
        Assert.Null(item.LogicalParent);
        Assert.DoesNotContain(item, row.VisualChildren);
        new ContentControl().Content = item;
    }

    [Fact]
    public void RecycledListBoxItemReleasesATemplatedItemThatATemplatePredicateStoppedMatching()
    {
        HashSet<UIElement> wrapped = [];
        Plain item = new();
        Plain other = new();
        wrapped.Add(item);
        ListBox listBox = new();
        listBox.Templates.Add(new ContentTemplate<Plain>("wrap", key: null, priority: 0,
            context => new Wrapper(context.Data),
            predicate: context => context.Data is Plain plain && wrapped.Contains(plain)));
        listBox.ItemsSource = new UIElement[] { item };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.IsType<Wrapper>(item.VisualParent);

        wrapped.Remove(item);
        listBox.ItemsSource = new UIElement[] { other };
        root.ProcessFrame();

        Assert.Same(row, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, other.VisualParent);
        Assert.Null(item.VisualParent);
        Assert.Null(item.LogicalParent);
        Assert.DoesNotContain(item, Descendants(row));
        new ContentControl().Content = item;
    }

    private static IEnumerable<UIElement> Ancestors(UIElement element)
    {
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            yield return current;
        }
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

    private sealed class Plain : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context) => new(20, 6);
    }

    // A template element that places its content inside itself.
    private sealed class Wrapper : UIElement
    {
        public Wrapper(object? content)
        {
            if (content is UIElement element)
            {
                VisualChildren.Add(element);
            }
        }

        protected override LayoutSize MeasureCore(MeasureContext context) => new(80, 10);
    }
}
