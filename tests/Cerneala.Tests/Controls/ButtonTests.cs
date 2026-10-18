using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ButtonTests
{
    [Fact]
    public void ButtonContentElementParticipatesInTreeAndLayout()
    {
        Button button = new()
        {
            Padding = new Thickness(1),
            Content = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(100, 100)));
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        UIElement child = Assert.IsType<FixedElement>(button.Content);
        Assert.Same(button, child.LogicalParent);
        Assert.Same(button, child.VisualParent);
        Assert.Equal(new LayoutSize(12, 7), desired);
        Assert.Equal(new LayoutRect(1, 1, 28, 18), child.ArrangedBounds);
    }

    [Fact]
    public void ButtonReplacingEqualElementContentUpdatesRetainedOwnership()
    {
        Button button = new();
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);
        button.Content = oldChild;

        button.Content = newChild;

        Assert.Same(newChild, button.Content);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(button, newChild.LogicalParent);
        Assert.Same(button, newChild.VisualParent);
    }

    [Fact]
    public void TemplatedButtonPresentsElementContentAndUpdatesEqualElementReplacement()
    {
        Button button = new();
        ContentPresenter? presenter = null;
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context =>
        {
            presenter = new ContentPresenter();
            context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
            return presenter;
        });
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);

        button.Content = oldChild;
        button.Content = newChild;

        Assert.Same(newChild, button.Content);
        Assert.Same(newChild, presenter!.PresentedChild);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(presenter, newChild.LogicalParent);
        Assert.Same(presenter, newChild.VisualParent);
        Assert.DoesNotContain(newChild, button.LogicalChildren);
        Assert.DoesNotContain(newChild, button.VisualChildren);
    }

    [Fact]
    public void ButtonRendersTextContentAndVisualState()
    {
        UIRoot root = new();
        Button button = new()
        {
            Content = "Go",
            Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
            Foreground = new Cerneala.UI.Media.SolidColorBrush(Color.Black)
        };
        root.VisualChildren.Add(button);
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(3, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
        Assert.Equal(DrawCommandKind.DrawText, commands[2].Kind);
    }

    [Fact]
    public void ButtonCanBeHitHoveredPressedFocusedClickedAndCommandBound()
    {
        Executed = false;
        UIRoot root = new(100, 100);
        Button button = new()
        {
            Command = new ActionCommand(_ => Executed = true)
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.True(button.IsPointerOver);
        Assert.False(button.IsPressed);
        Assert.True(button.IsKeyboardFocused);
        Assert.True(Executed);
    }

    [Fact]
    public void ButtonClickOnElementContentExecutesButtonCommand()
    {
        Executed = false;
        UIRoot root = new(100, 100);
        Button button = new()
        {
            Content = new FixedElement(new LayoutSize(10, 10)),
            Command = new ActionCommand(_ => Executed = true)
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.True(Executed);
    }

    // One click does one thing (#421): the click on the inner button is the inner button's, and its MouseUp bubbles to
    // the outer button without making a Click of the outer button. The inner Click still bubbles to outer.Click.
    [Fact]
    public void ClickOnButtonInsideButtonRaisesOnlyTheInnerButtonClick()
    {
        int innerCommands = 0;
        int outerCommands = 0;
        Button inner = new() { Content = "inner", Width = 80, Height = 30, Command = new ActionCommand(_ => innerCommands++) };
        Button outer = new() { Content = inner, Command = new ActionCommand(_ => outerCommands++) };
        UIRoot root = CreateCanvasRoot(outer);
        int innerClicks = 0;
        int outerOwnClicks = 0;
        int outerBubbledClicks = 0;
        inner.Click += (_, _) => innerClicks++;
        outer.Click += (_, args) =>
        {
            if (IsSource(args.OriginalSource, outer))
            {
                outerOwnClicks++;
            }
            else if (IsSource(args.OriginalSource, inner))
            {
                outerBubbledClicks++;
            }
        };
        LayoutRect bounds = inner.ArrangedBounds;

        Click(root, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        Assert.Equal(1, innerClicks);
        Assert.Equal(1, outerBubbledClicks);
        Assert.Equal(0, outerOwnClicks);
        Assert.Equal(1, innerCommands);
        Assert.Equal(0, outerCommands);
    }

    [Fact]
    public void ClickOnOuterButtonOutsideInnerButtonRaisesOuterClickAndCommandOnce()
    {
        int innerCommands = 0;
        int outerCommands = 0;
        Button inner = new() { Content = "inner", Width = 80, Height = 30, Command = new ActionCommand(_ => innerCommands++) };
        Button outer = new() { Content = inner, Padding = new Thickness(12), Command = new ActionCommand(_ => outerCommands++) };
        UIRoot root = CreateCanvasRoot(outer);
        int innerClicks = 0;
        int outerOwnClicks = 0;
        inner.Click += (_, _) => innerClicks++;
        outer.Click += (_, args) =>
        {
            if (IsSource(args.OriginalSource, outer))
            {
                outerOwnClicks++;
            }
        };
        LayoutRect bounds = outer.ArrangedBounds;
        float x = bounds.X + 5;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(outer, new HitTestService().HitTest(root, x, y)?.Element);

        Click(root, x, y);

        Assert.Equal(1, outerOwnClicks);
        Assert.Equal(1, outerCommands);
        Assert.Equal(0, innerClicks);
        Assert.Equal(0, innerCommands);
    }

    [Fact]
    public void ClickOnDropDownToggleOfComboBoxInsideButtonOpensDropDownWithoutButtonClick()
    {
        ComboBox comboBox = new() { IsEditable = false, Width = 180, Height = 32 };
        comboBox.SetItems(new[] { "one", "two", "three" });
        comboBox.SelectedIndex = 0;
        Button outer = new() { Content = comboBox };
        UIRoot root = CreateCanvasRoot(outer);
        Cerneala.UI.Controls.Primitives.ToggleButton toggle = Assert.IsType<Cerneala.UI.Controls.Primitives.ToggleButton>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        int outerOwnClicks = 0;
        outer.Click += (_, args) =>
        {
            if (IsSource(args.OriginalSource, outer))
            {
                outerOwnClicks++;
            }
        };
        LayoutRect bounds = toggle.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(toggle, new HitTestService().HitTest(root, x, y)?.Element);

        Click(root, x, y);

        Assert.True(comboBox.IsDropDownOpen);
        Assert.Equal(0, outerOwnClicks);
    }

    // A selector item owns the click that selects it, as a WPF ListBoxItem handles the press (#490): the enclosing
    // button is neither clicked nor runs its command.
    [Fact]
    public void ClickOnListBoxItemInsideButtonSelectsTheItemWithoutButtonClick()
    {
        int commands = 0;
        ListBoxItem two = new() { Content = "Two" };
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        list.Items.Add(two);
        Button button = new() { Content = list, Width = 200, Height = 100, Command = new ActionCommand(_ => commands++) };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);

        ClickCenter(root, two);

        Assert.Equal(1, list.SelectedIndex);
        Assert.Equal(0, clicks);
        Assert.Equal(0, commands);
    }

    [Fact]
    public void ClickOnButtonOutsideNestedListBoxRaisesButtonClickOnce()
    {
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        Button button = new() { Content = list, Width = 200, Height = 100 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + 5;
        float y = bounds.Y + 5;
        Assert.Same(button, new HitTestService().HitTest(root, x, y)?.Element);

        Click(root, x, y);

        Assert.Equal(-1, list.SelectedIndex);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void PickingComboBoxDropDownItemInsideButtonSelectsTheItemWithoutButtonClick()
    {
        ComboBoxItem two = new() { Content = "Two" };
        ComboBox comboBox = new() { Width = 120 };
        comboBox.Items.Add(new ComboBoxItem { Content = "One" });
        comboBox.Items.Add(two);
        Button button = new() { Content = comboBox, Width = 200, Height = 100 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.True(two.IsAttached);

        ClickCenter(root, two);

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(comboBox.IsDropDownOpen);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void PickingComboBoxDropDownItemInsideRadioButtonLeavesTheRadioButtonUnchecked()
    {
        ComboBoxItem two = new() { Content = "Two" };
        ComboBox comboBox = new() { Width = 120 };
        comboBox.Items.Add(new ComboBoxItem { Content = "One" });
        comboBox.Items.Add(two);
        RadioButton radio = new() { Content = comboBox, Width = 200, Height = 100 };
        UIRoot root = CreateWindowRoot(radio);
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        Assert.True(two.IsAttached);

        ClickCenter(root, two);

        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.False(radio.IsChecked);
    }

    private static UIRoot CreateWindowRoot(UIElement child)
    {
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        canvas.VisualChildren.Add(child);
        UIRoot root = new(360, 240);
        root.VisualChildren.Add(new Window { Content = canvas, Width = 360, Height = 240 });
        root.ProcessFrame();
        return root;
    }

    private static void ClickCenter(UIRoot root, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        Click(root, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    private static UIRoot CreateCanvasRoot(UIElement child)
    {
        UIRoot root = new(300, 200);
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        canvas.VisualChildren.Add(child);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        return root;
    }

    private static void Click(UIRoot root, float x, float y)
    {
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static bool IsSource(object? source, UIElement element)
    {
        return ReferenceEquals(source, element) || Equals(source, element.ElementId);
    }

    private static bool Executed { get; set; }

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

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class EqualElement(int id) : UIElement
    {
        private readonly int id = id;

        public override bool Equals(object? obj)
        {
            return obj is EqualElement other && other.id == id;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
