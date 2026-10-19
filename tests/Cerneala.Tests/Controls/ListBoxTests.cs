using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

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

    // As in WPF, where ButtonBase marks the mouse button events handled, a button in a row owns its click and does
    // not select the row.
    [Fact]
    public void ListBoxClickOnTemplateCheckBoxChecksItWithoutSelectingRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data });
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(listBox.ItemContainerGenerator.RealizedContainers[1]));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect bounds = check.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + 6, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, clicks);
        Assert.True(check.IsChecked);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).IsSelected);

        Click(root, bridge, bounds.X + 6, bounds.Y + bounds.Height / 2);

        Assert.Equal(2, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(-1, listBox.SelectedIndex);
    }

    // The click stays the CheckBox's when its Click handler removes or collapses it, or detaches the ListBox, before the
    // MouseUp bubbles to the row: the row decides with the route the input bridge resolved the click with (#550).
    [Theory]
    [InlineData("remove")]
    [InlineData("collapse")]
    [InlineData("detach")]
    public void ListBoxClickOnCheckBoxThatItsClickHandlerRemovesDoesNotSelectRow(string change)
    {
        CheckBox check = new() { Content = "check", Width = 80, Height = 24 };
        ListBoxItem one = new() { Content = "One" };
        ListBoxItem two = new() { Content = check };
        ListBox listBox = new() { Width = 160, Height = 100 };
        listBox.Items.Add(one);
        listBox.Items.Add(two);
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        int clicks = 0;
        check.Click += (_, _) =>
        {
            clicks++;
            switch (change)
            {
                case "remove":
                    two.Content = "removed";
                    break;
                case "collapse":
                    check.Visibility = Visibility.Collapsed;
                    break;
                default:
                    canvas.VisualChildren.Remove(listBox);
                    break;
            }
        };
        int selectionChanges = 0;
        listBox.SelectionChanged += (_, _) => selectionChanges++;
        UIRoot root = new(300, 200);
        canvas.VisualChildren.Add(listBox);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        LayoutRect bounds = check.ArrangedBounds;

        Click(root, new ElementInputBridge(), bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, clicks);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(two.IsSelected);
        Assert.Equal(0, selectionChanges);
    }

    [Fact]
    public void ListBoxClickOnTemplateTextSelectsRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new TextBlock { Text = context.Data });
        TextBlock text = Assert.Single(FindTextBlocks(listBox.ItemContainerGenerator.RealizedContainers[1]));
        LayoutRect bounds = text.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + 2, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]).IsSelected);
    }

    [Fact]
    public void ListBoxClickOnEmptyPartOfTemplateRowSelectsRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(row));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        float x = row.ArrangedBounds.X + row.ArrangedBounds.Width - 10;
        Assert.True(x > check.ArrangedBounds.X + check.ArrangedBounds.Width);
        ElementInputBridge bridge = new();

        Click(root, bridge, x, row.ArrangedBounds.Y + row.ArrangedBounds.Height / 2);

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(row.IsSelected);
    }

    // A click into a TextBox of the ItemTemplate is the text box's, as WPF's text editor handles the left button
    // down and up before ListBoxItem sees them (#586): the text box gets the focus and the row is not selected.
    [Fact]
    public void ListBoxClickInsideTemplateTextBoxFocusesItWithoutSelectingRow()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new TextBox { Text = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        TextBox text = Assert.IsType<TextBox>(FindDescendant<TextBox>(row));
        LayoutRect bounds = text.ArrangedBounds;
        ElementInputBridge bridge = new();

        Click(root, bridge, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        Assert.True(text.IsKeyboardFocused);
        Assert.Equal(-1, listBox.SelectedIndex);
        Assert.False(row.IsSelected);
    }

    // As in WPF, where the press on the row selects it, a release on a button in the row that the button does not turn
    // into a click, because the press was on the row, is not the button's click and still selects the row.
    [Fact]
    public void ListBoxPressOnEmptyPartOfRowReleasedOnTemplateCheckBoxSelectsRowWithoutCheckingIt()
    {
        (UIRoot root, ListBox listBox) = CreateListBox(context => new CheckBox { Content = context.Data, Width = 60 });
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        CheckBox check = Assert.IsType<CheckBox>(FindDescendant<CheckBox>(row));
        int clicks = 0;
        check.Click += (_, _) => clicks++;
        LayoutRect rowBounds = row.ArrangedBounds;
        LayoutRect checkBounds = check.ArrangedBounds;
        float pressX = rowBounds.X + rowBounds.Width - 10;
        Assert.True(pressX > checkBounds.X + checkBounds.Width);
        float releaseX = checkBounds.X + 6;
        float releaseY = checkBounds.Y + checkBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(pressX, rowBounds.Y + rowBounds.Height / 2, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.False(check.IsChecked);
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(row.IsSelected);
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
    public void ListBoxRowHostsElementItemWhenItemTemplateThatReturnedItIsRemoved()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = IdentityElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(element.VisualParent);

        listBox.ItemTemplate = null;
        root.ProcessFrame();

        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, element.VisualParent);
        Assert.Same(row, element.LogicalParent);
        Assert.Same(element, Assert.Single(row.VisualChildren));
        Assert.DoesNotContain(element, presenter.VisualChildren);
        Assert.DoesNotContain(element, presenter.LogicalChildren);
    }

    [Fact]
    public void ListBoxRowWrapsElementItemWhenItemTemplateThatReturnedItIsReplaced()
    {
        UIRoot root = new(300, 200);
        Marker element = new("raw");
        ListBox listBox = new() { ItemTemplate = IdentityElementTemplate() };
        listBox.SetItems(new object[] { element });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(element.VisualParent);

        listBox.ItemTemplate = WrapElementTemplate();
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(element.VisualParent);
        Assert.Same(element, wrapper.Child);
        ListBoxItem row = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(row, wrapper.VisualParent!.VisualParent);
        Assert.NotSame(presenter.PresentedChild, element);
        Assert.DoesNotContain(element, presenter.VisualChildren);
        Assert.DoesNotContain(element, presenter.LogicalChildren);
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

    // As the Border of the WPF ListBox template, Background fills the arranged bounds behind the items (#590).
    [Fact]
    public void ListBoxFillsItsArrangedBoundsWithBackgroundBehindItsItems()
    {
        SolidColorBrush red = new(Color.Red);
        ListBox listBox = new() { Background = red };
        listBox.Items.Add(new ListBoxItem { Content = "One" });
        UIRoot root = new();
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        listBox.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        List<DrawCommand> commands = [.. root.RetainedRenderer.Commit(root)];

        DrawCommand fill = Assert.Single(commands, command => ReferenceEquals(command.Brush, red));
        Assert.Equal(DrawCommandKind.FillRectangle, fill.Kind);
        Assert.Equal(new DrawRect(3, 4, 40, 30), fill.Rect);
        Assert.Equal(0, commands.IndexOf(fill));
        Assert.True(commands.Count > 1);
    }

    [Fact]
    public void ListBoxWithoutBackgroundDrawsNoFill()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new[] { "one" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Empty(Render(listBox));
    }

    [Fact]
    public void ListBoxWithComponentTemplateLeavesItsBackgroundToTheTemplate()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new()
        {
            Background = new SolidColorBrush(Color.Red),
            ComponentTemplate = new ComponentTemplate<ListBox>("list", _ => new Border())
        };
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();

        Assert.Empty(Render(listBox));
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

    private static DrawCommandList Render(UIElement element)
    {
        DrawCommandList commands = new();
        element.Render(new RenderContext(element, new DrawingContext(commands), element.ArrangedBounds, RenderLayer.Default, new RenderCounters()));
        return commands;
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

    private static (UIRoot Root, ListBox ListBox) CreateListBox(Func<ContentTemplateContext<string>, UIElement> build)
    {
        UIRoot root = new(200, 200);
        ListBox listBox = new() { ItemTemplate = new ContentTemplate<string>("row", key: null, priority: 0, build) };
        listBox.SetItems(new[] { "one", "two" });
        root.VisualChildren.Add(listBox);
        root.ProcessFrame();
        return (root, listBox);
    }

    private static void Click(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static T? FindDescendant<T>(UIElement element) where T : UIElement
    {
        if (element is T match)
        {
            return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            if (FindDescendant<T>(child) is T found)
            {
                return found;
            }
        }

        return null;
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

    private static ContentTemplate IdentityElementTemplate()
    {
        return new ContentTemplate("identity", typeof(object), key: null, priority: 0,
            context => (UIElement?)context.Data);
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
