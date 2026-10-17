using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ListBoxTests
{
    [Fact]
    public void ListBoxCreatesListBoxItemContainers()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "one" });

        IReadOnlyList<UIElement> containers = listBox.ItemContainerGenerator.Realize();

        Assert.IsType<ListBoxItem>(containers[0]);
    }

    [Fact]
    public void ListBoxClickSelectsItem()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new UIElement[] { new FixedElement(), new FixedElement() });
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 15, previousDown: true));

        Assert.Equal(1, listBox.SelectedIndex);
        ListBoxItem item = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public void ListBoxSelectionSurvivesContainerRecycling()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 2;

        listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        listBox.ItemContainerGenerator.Realize(new RealizationWindow(2, 3));

        ListBoxItem selected = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[2]);
        Assert.True(selected.IsSelected);
    }

    [Fact]
    public void ListBoxStringItemShowsItsText()
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new();
        listBox.SetItems(new[] { "Apple", "Banana" });
        TextBlock reference = new() { Text = "Apple" };
        root.VisualChildren.Add(listBox);
        root.VisualChildren.Add(reference);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);

        Assert.Equal("Apple", row.Content);
        Assert.Equal("Apple", Assert.Single(FindTextBlocks(row)).Text);
        Assert.True(reference.DesiredSize.Height > 0);
        Assert.Equal(reference.DesiredSize.Height, row.DesiredSize.Height);
    }

    [Fact]
    public void ListBoxRowShowsTextOfReplacedStringItem()
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new();
        listBox.SetItems(new[] { "Apple", "Banana" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        listBox.Items[0] = "Cherry";
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal("Cherry", Assert.Single(FindTextBlocks(row)).Text);
    }

    [Fact]
    public void ListBoxRowTextUsesRowFontAndForeground()
    {
        UIRoot root = new(300, 200);
        SolidColorBrush foreground = new(Color.White);
        ListBox listBox = new()
        {
            ItemContainerAspect = new ElementAspect(
            [
                new ElementAspectValue(Control.FontSizeProperty, 30f),
                new ElementAspectValue(Control.ForegroundProperty, foreground)
            ])
        };
        listBox.SetItems(new[] { "Apple" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        TextBlock text = Assert.Single(FindTextBlocks(listBox.ItemContainerGenerator.RealizedContainers[0]));

        Assert.Equal(30, text.FontSize);
        Assert.Same(foreground, text.Foreground);
    }

    [Fact]
    public void ListBoxItemTemplateCreatesRowContent()
    {
        UIRoot root = new(300, 200);
        List<string?> templated = [];
        ListBox listBox = new()
        {
            ItemsSource = new List<string> { "Alpha", "Beta" },
            ItemTemplate = new ContentTemplate<string>("Text", key: null, priority: 0, context =>
            {
                templated.Add(context.Data);
                return new TextBlock { Text = "[" + context.Data + "]" };
            })
        };
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Equal(["Alpha", "Beta"], templated);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        Assert.Equal("Beta", row.Content);
        Assert.Equal("[Beta]", Assert.Single(FindTextBlocks(row)).Text);
        Assert.True(row.DesiredSize.Height > 0);
    }

    [Fact]
    public void ListBoxElementItemStaysTheOnlyChildOfItsRow()
    {
        UIRoot root = new(200, 200);
        FixedElement element = new();
        ListBox listBox = new();
        listBox.SetItems(new UIElement[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);

        Assert.Same(element, row.Content);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Equal(new LayoutSize(20, 10), element.DesiredSize);
        Assert.Equal(10, row.DesiredSize.Height);
    }

    // WPF copies ItemTemplate to the row's ContentTemplate, and its ContentPresenter tries that template before UIElement content.
    [Fact]
    public void ListBoxItemTemplatePresentsElementItem()
    {
        UIRoot root = new(300, 200);
        Marker item = new("raw");
        List<(object? Data, int Index)> templated = [];
        ListBox listBox = new() { ItemTemplate = MarkerTemplate(templated) };
        listBox.SetItems(new object[] { "first", item });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);

        Assert.Same(item, row.Content);
        Assert.Equal(["T1:Marker"], MarkerLabels(row));
        Assert.Contains(templated, entry => ReferenceEquals(entry.Data, item) && entry.Index == 1);
        Assert.Null(item.VisualParent);
        Assert.Equal(new LayoutSize(40, 10), row.DesiredSize);
    }

    [Fact]
    public void ListBoxItemIsItsOwnRowWithoutItemTemplate()
    {
        UIRoot root = new(300, 200);
        ListBoxItem item = new() { Content = new Marker("own") };
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { item });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Same(item, listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal(["own"], MarkerLabels(item));
    }

    [Fact]
    public void ListBoxRowPresentsItemTemplateWhenItsItemChangesBetweenElementAndData()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { "data" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Assert.Equal(["T0:String"], MarkerLabels(listBox));

        listBox.Items[0] = element;
        root.ProcessFrame();
        Assert.Equal(["T0:Marker"], MarkerLabels(listBox));
        Assert.Null(element.VisualParent);

        listBox.Items[0] = "data";
        root.ProcessFrame();
        Assert.Equal(["T0:String"], MarkerLabels(listBox));
    }

    [Fact]
    public void ListBoxRowHostsElementItemAgainWhenItemTemplateIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = MarkerTemplate([]) };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Assert.Equal(["T0:Marker"], MarkerLabels(listBox));

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Equal(["raw"], MarkerLabels(listBox));
    }

    [Fact]
    public void ListBoxRowHostsElementItemWhenItemTemplateThatWrappedItIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = WrapElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Border wrapper = Assert.IsType<Border>(element.VisualParent);

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, element.VisualParent);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.Empty(wrapper.VisualChildren);
    }

    [Fact]
    public void ListBoxRowWrapsElementItemInReplacedItemTemplate()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = WrapElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        Border first = Assert.IsType<Border>(element.VisualParent);

        listBox.ItemTemplate = WrapElementTemplate();
        root.ProcessFrame();

        Border second = Assert.IsType<Border>(element.VisualParent);
        Assert.NotSame(first, second);
        Assert.Same(element, second.Child);
        Assert.Empty(first.VisualChildren);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, second.VisualParent!.VisualParent);
    }

    [Fact]
    public void ListBoxItemWithStringContentShowsItsTextInsidePadding()
    {
        UIRoot root = new(200, 200);
        ListBoxItem item = new() { Content = "Ink", Padding = new Thickness(4) };
        TextBlock reference = new() { Text = "Ink" };
        root.VisualChildren.Add(item);
        root.VisualChildren.Add(reference);
        root.ProcessFrame();

        Assert.Equal("Ink", Assert.Single(FindTextBlocks(item)).Text);
        Assert.Equal(reference.DesiredSize.Height + 8, item.DesiredSize.Height);
    }

    [Fact]
    public void TabControlSelectedTabItemReturnsUnrealizedTabItem()
    {
        TabControl tabControl = new();
        TabItem first = new()
        {
            Header = "First",
            Content = "First content"
        };
        TabItem second = new()
        {
            Header = "Second",
            Content = "Second content"
        };
        tabControl.SetItems(new[] { first, second });

        tabControl.SelectedIndex = 1;

        Assert.Same(second, tabControl.SelectedItem);
        Assert.Same(second, tabControl.SelectedTabItem);
        TabItem selected = tabControl.SelectedTabItem!;
        Assert.Equal("Second", selected.Header);
        Assert.Equal("Second content", selected.Content);
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static List<TextBlock> FindTextBlocks(UIElement element)
    {
        List<TextBlock> found = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (child is TextBlock textBlock)
            {
                found.Add(textBlock);
            }

            found.AddRange(FindTextBlocks(child));
        }

        return found;
    }

    private static ContentTemplate MarkerTemplate(List<(object? Data, int Index)> templated)
    {
        return new ContentTemplate("Row", typeof(object), key: null, priority: 0, context =>
        {
            templated.Add((context.Data, context.Index));
            return new Marker("T" + context.Index + ":" + context.Data!.GetType().Name);
        });
    }

    private static ContentTemplate WrapElementTemplate()
    {
        return new ContentTemplate("wrap", typeof(object), key: null, priority: 0,
            context => new Border { Child = (UIElement?)context.Data });
    }

    private static List<string> MarkerLabels(UIElement element)
    {
        List<string> labels = [];
        foreach (UIElement child in element.VisualChildren)
        {
            if (child is Marker marker)
            {
                labels.Add(marker.Label);
            }

            labels.AddRange(MarkerLabels(child));
        }

        return labels;
    }

    private sealed class Marker(string label) : UIElement
    {
        public string Label { get; } = label;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(40, 10);
        }
    }

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }
    }
}
