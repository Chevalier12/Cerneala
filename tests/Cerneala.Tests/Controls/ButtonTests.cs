using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;
using Cerneala.UI.Theming;

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
        // As in WPF's default Button, the content is centered inside the border and padding at its desired size (#621).
        Assert.Equal(new LayoutRect(10, 7.5f, 10, 5), child.ArrangedBounds);
    }

    [Fact]
    public void ButtonDrawsStringContentCenteredInsideBorderAndPadding()
    {
        const string label = "Hold me";
        (Button button, DrawCommand text) = RenderStringButton(label, width: 300, height: 70);
        (Button autoSized, DrawCommand autoText) = RenderStringButton(label, width: float.NaN, height: float.NaN, HorizontalAlignment.Left, VerticalAlignment.Top);
        LayoutSize textSize = TextMeasurer.Default.Measure(label, new TextAspect(button.FontFamily, button.FontSize, foreground: button.Foreground), float.PositiveInfinity).Size;
        LayoutRect inner = Deflate(button.ArrangedBounds, button);
        // A top-left Button arranged at its desired size draws its text at its inner top: this gives the
        // distance between the top of the text and the DrawText position.
        float baselineOffset = autoText.Position.Y - Deflate(autoSized.ArrangedBounds, autoSized).Y;

        Assert.True(textSize.Width < inner.Width && textSize.Height < inner.Height);
        Assert.Equal(inner.X + ((inner.Width - textSize.Width) / 2), text.Position.X, 0.5f);
        Assert.Equal(inner.Y + ((inner.Height - textSize.Height) / 2) + baselineOffset, text.Position.Y, 0.5f);
    }

    [Fact]
    public void ContentAlignmentDefaultsFollowWpf()
    {
        Assert.Equal(HorizontalAlignment.Left, new ContentControl().HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, new ContentControl().VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, new Button().HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, new Button().VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, new Cerneala.UI.Controls.Primitives.ToggleButton().HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, new Cerneala.UI.Controls.Primitives.RepeatButton().VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Left, new CheckBox().HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, new CheckBox().VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Left, new RadioButton().HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, new RadioButton().VerticalContentAlignment);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 1, 1, 28, 18)]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.Top, 1, 1, 10, 5)]
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 19, 14, 10, 5)]
    [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Center, 1, 7.5f, 28, 5)]
    public void ButtonArrangesElementContentByContentAlignment(
        HorizontalAlignment horizontal,
        VerticalAlignment vertical,
        float x,
        float y,
        float width,
        float height)
    {
        FixedElement child = new(new LayoutSize(10, 5));
        Button button = new()
        {
            Padding = new Thickness(1),
            Content = child,
            HorizontalContentAlignment = horizontal,
            VerticalContentAlignment = vertical
        };

        button.Measure(new MeasureContext(new LayoutSize(100, 100)));
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutRect(x, y, width, height), child.ArrangedBounds);
    }

    [Fact]
    public void ButtonDrawsStringContentAtTopLeftForLeftTopOrStretchContentAlignment()
    {
        const string label = "Hold me";
        (Button leftTop, DrawCommand leftTopText) = RenderStringButton(label, width: 300, height: 70, HorizontalAlignment.Left, VerticalAlignment.Top);
        (Button stretch, DrawCommand stretchText) = RenderStringButton(label, width: 300, height: 70, HorizontalAlignment.Stretch, VerticalAlignment.Stretch);
        (Button autoSized, DrawCommand autoText) = RenderStringButton(label, width: float.NaN, height: float.NaN, HorizontalAlignment.Left, VerticalAlignment.Top);
        float baselineOffset = autoText.Position.Y - Deflate(autoSized.ArrangedBounds, autoSized).Y;

        Assert.Equal(Deflate(leftTop.ArrangedBounds, leftTop).X, leftTopText.Position.X, 0.01f);
        Assert.Equal(Deflate(leftTop.ArrangedBounds, leftTop).Y + baselineOffset, leftTopText.Position.Y, 0.01f);
        Assert.Equal(Deflate(stretch.ArrangedBounds, stretch).X, stretchText.Position.X, 0.01f);
        Assert.Equal(Deflate(stretch.ArrangedBounds, stretch).Y + baselineOffset, stretchText.Position.Y, 0.01f);
    }

    [Fact]
    public void ChangingContentAlignmentMovesStringContentOnTheNextFrame()
    {
        UIRoot root = new(400, 200);
        Button button = new() { Width = 300, Height = 70, Content = "Hold me" };
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        float centeredX = FirstDrawText(root.RetainedRenderer.Commit(root)).Position.X;

        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        root.ProcessFrame();
        float leftX = FirstDrawText(root.RetainedRenderer.Commit(root)).Position.X;

        Assert.Equal(Deflate(button.ArrangedBounds, button).X, leftX, 0.01f);
        Assert.True(centeredX > leftX + 50);
    }

    [Theory]
    [InlineData("DefaultTheme")]
    [InlineData("Modern")]
    public void TemplatedButtonStretchesElementContentForStretchContentAlignment(string template)
    {
        TextBlock block = new() { Text = "Hold me" };
        Button button = CreateTemplatedButton(template, block, HorizontalAlignment.Stretch, VerticalAlignment.Stretch);

        Assert.Equal(Deflate(button.ArrangedBounds, button), block.ArrangedBounds);
    }

    [Theory]
    [InlineData("DefaultTheme")]
    [InlineData("Modern")]
    public void TemplatedButtonPlacesElementContentAtTopLeftForLeftTopContentAlignment(string template)
    {
        TextBlock block = new() { Text = "Hold me" };
        Button button = CreateTemplatedButton(template, block, HorizontalAlignment.Left, VerticalAlignment.Top);
        LayoutRect inner = Deflate(button.ArrangedBounds, button);

        Assert.Equal(new LayoutRect(inner.X, inner.Y, block.DesiredSize.Width, block.DesiredSize.Height), block.ArrangedBounds);
    }

    [Theory]
    [InlineData("DefaultTheme")]
    [InlineData("Modern")]
    public void TemplatedButtonCentersElementContentInsideBorderAndPadding(string template)
    {
        TextBlock block = new() { Text = "Hold me" };
        Button button = CreateTemplatedButton(template, block);

        AssertCenteredAtDesiredSize(block, Deflate(button.ArrangedBounds, button));
    }

    [Theory]
    [InlineData("DefaultTheme")]
    [InlineData("Modern")]
    public void TemplatedButtonCentersStringContentInsideBorderAndPadding(string template)
    {
        Button button = CreateTemplatedButton(template, "Hold me");
        ContentPresenter presenter = FindPresenter(button);
        UIElement generated = Assert.IsType<TextBlock>(presenter.PresentedChild);

        AssertCenteredAtDesiredSize(generated, Deflate(button.ArrangedBounds, button));
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

    // The click stays the inner button's when its Click handler changes the tree before the MouseUp reaches the outer
    // button (#501): the outer button decides with the route the input system resolved the click with.
    [Theory]
    [InlineData("removeInner")]
    [InlineData("collapseInnerAncestor")]
    [InlineData("detachOuter")]
    public void ClickOnButtonInsideButtonRaisesNoOuterClickWhenInnerClickChangesTheTree(string treeChange)
    {
        int outerCommands = 0;
        Button inner = new() { Content = "inner", Width = 80, Height = 30 };
        StackPanel panel = new();
        panel.Children.Add(new TextBlock { Text = "label" });
        panel.Children.Add(inner);
        Button outer = new() { Content = panel, Command = new ActionCommand(_ => outerCommands++) };
        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        UIRoot root = CreateCanvasRoot(canvas, outer);
        int innerClicks = 0;
        int outerOwnClicks = 0;
        int outerBubbledClicks = 0;
        inner.Click += (_, _) =>
        {
            innerClicks++;
            switch (treeChange)
            {
                case "removeInner":
                    panel.Children.Remove(inner);
                    break;
                case "collapseInnerAncestor":
                    panel.Visibility = Visibility.Collapsed;
                    break;
                case "detachOuter":
                    canvas.VisualChildren.Remove(outer);
                    break;
            }
        };
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
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(inner, new HitTestService().HitTest(root, x, y)?.Element);

        Click(root, x, y);

        Assert.Equal(1, innerClicks);
        Assert.Equal(1, outerBubbledClicks);
        Assert.Equal(0, outerOwnClicks);
        Assert.Equal(0, outerCommands);
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

    // A press on a selector item belongs to the item, as a WPF ListBoxItem handles MouseLeftButtonDown before the
    // enclosing ButtonBase sees it (#589): the button is not pressed while the mouse is held.
    [Fact]
    public void HoldingMouseOnListBoxItemInsideButtonLeavesButtonUnpressed()
    {
        ListBoxItem two = new() { Content = "Two" };
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        list.Items.Add(two);
        Button button = new() { Content = list, Width = 200, Height = 100 };
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = two.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bool pressedWhileHeld = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.False(pressedWhileHeld);
        Assert.False(button.IsPressed);
        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void HoldingMouseOnButtonOutsideNestedListBoxPressesButtonUntilRelease()
    {
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        Button button = new() { Content = list, Width = 200, Height = 100 };
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + 5;
        float y = bounds.Y + 5;
        Assert.Same(button, new HitTestService().HitTest(root, x, y)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bool pressedWhileHeld = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.True(pressedWhileHeld);
        Assert.False(button.IsPressed);
    }

    // The enclosing button never took the press, so a release on the button's own area after a press on an item does
    // not click it, as a WPF ButtonBase clicks only for a release while IsPressed is true.
    [Fact]
    public void ReleaseOnButtonAfterPressOnListBoxItemInsideButtonDoesNotClickTheButton()
    {
        ListBoxItem two = new() { Content = "Two" };
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        list.Items.Add(two);
        Button button = new() { Content = list, Width = 200, Height = 100 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect itemBounds = two.ArrangedBounds;
        float itemX = itemBounds.X + itemBounds.Width / 2;
        float itemY = itemBounds.Y + itemBounds.Height / 2;
        LayoutRect buttonBounds = button.ArrangedBounds;
        float buttonX = buttonBounds.X + 5;
        float buttonY = buttonBounds.Y + 5;
        Assert.Same(button, new HitTestService().HitTest(root, buttonX, buttonY)?.Element);
        PointerSnapshot heldOnItem = PointerSnapshot.Empty.WithPosition(itemX, itemY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot heldOnButton = PointerSnapshot.Empty.WithPosition(buttonX, buttonY).WithButton(InputMouseButton.Left, true);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(itemX, itemY, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, new InputFrame(heldOnItem, heldOnButton, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        root.ProcessFrame();
        bool pressedWhileHeldOnButton = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(buttonX, buttonY, previousDown: true));
        root.ProcessFrame();

        Assert.False(pressedWhileHeldOnButton);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void HoldingMouseOnListBoxItemInsideRepeatButtonNeitherPressesNorRepeatsIt()
    {
        ListBoxItem two = new() { Content = "Two" };
        ListBox list = new() { Width = 120, Height = 80 };
        list.Items.Add(new ListBoxItem { Content = "One" });
        list.Items.Add(two);
        Cerneala.UI.Controls.Primitives.RepeatButton button = new() { Content = list, Width = 200, Height = 100 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = two.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true, currentDown: true), TimeSpan.FromSeconds(2));
        bool pressedWhileHeld = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.False(pressedWhileHeld);
        Assert.Equal(0, clicks);
        Assert.Equal(1, list.SelectedIndex);
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

    // A click into a text box belongs to the text box, as a WPF TextBox handles MouseLeftButtonDown and
    // MouseLeftButtonUp (#586): it gets the focus and the caret, and the enclosing button is neither pressed, clicked,
    // nor runs its command.
    [Fact]
    public void ClickInsideTextBoxInsideButtonPlacesCaretWithoutButtonClick()
    {
        int commands = 0;
        TextBox text = new() { Text = "abc", Width = 120, Height = 30 };
        Button button = new() { Content = text, Width = 200, Height = 80, Command = new ActionCommand(_ => commands++) };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        text.MoveCaret(0);
        LayoutRect bounds = text.ArrangedBounds;
        float x = bounds.X + bounds.Width - 4;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(text, new HitTestService().HitTest(root, x, y)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bool pressedDuringPress = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.Equal(0, commands);
        Assert.False(pressedDuringPress);
        Assert.True(text.IsKeyboardFocused);
        Assert.Equal(3, text.Caret.Position);
    }

    [Fact]
    public void ClickInsidePasswordBoxInsideButtonRaisesNoButtonClick()
    {
        int commands = 0;
        PasswordBox password = new() { Width = 120, Height = 30 };
        Button button = new() { Content = password, Width = 200, Height = 80, Command = new ActionCommand(_ => commands++) };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = password.ArrangedBounds;
        float x = bounds.X + bounds.Width / 2;
        float y = bounds.Y + bounds.Height / 2;
        Assert.Same(password, new HitTestService().HitTest(root, x, y)?.Element);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bool pressedDuringPress = button.IsPressed;
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
        root.ProcessFrame();

        Assert.Equal(0, clicks);
        Assert.Equal(0, commands);
        Assert.False(pressedDuringPress);
        Assert.True(password.IsKeyboardFocused);
    }

    [Fact]
    public void ClickOnButtonOutsideNestedTextBoxRaisesButtonClickAndCommandOnce()
    {
        int commands = 0;
        TextBox text = new() { Width = 120, Height = 30 };
        Button button = new() { Content = text, Width = 200, Height = 80, Command = new ActionCommand(_ => commands++) };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + 5;
        float y = bounds.Y + 5;
        Assert.Same(button, new HitTestService().HitTest(root, x, y)?.Element);

        Click(root, x, y);

        Assert.Equal(1, clicks);
        Assert.Equal(1, commands);
        Assert.False(text.IsKeyboardFocused);
    }

    // A press on the button followed by a release over the text box is the button's click, as the WPF Button that
    // took the press captures the mouse and clicks for a release over its content.
    [Fact]
    public void PressOnButtonAndReleaseOverNestedTextBoxRaisesButtonClickOnce()
    {
        TextBox text = new() { Width = 120, Height = 30 };
        Button button = new() { Content = text, Width = 200, Height = 80 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UIRoot root = CreateWindowRoot(button);
        LayoutRect buttonBounds = button.ArrangedBounds;
        LayoutRect textBounds = text.ArrangedBounds;
        float pressX = buttonBounds.X + 5;
        float pressY = buttonBounds.Y + 5;
        float releaseX = textBounds.X + textBounds.Width / 2;
        float releaseY = textBounds.Y + textBounds.Height / 2;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(pressX, pressY, currentDown: true));
        bridge.Dispatch(root, MoveFrame(pressX, pressY, releaseX, releaseY));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, previousDown: true));
        root.ProcessFrame();

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

    [Fact]
    public void ClickingButtonInsideRadioButtonClicksOnlyTheInnerButton()
    {
        Button inner = new() { Content = "Inner", Width = 100, Height = 30 };
        RadioButton radio = new() { Content = inner, Width = 200, Height = 80 };
        int innerClicks = 0;
        int radioClicks = 0;
        inner.Click += (_, _) => innerClicks++;
        radio.Click += (_, args) =>
        {
            if (IsSource(args.OriginalSource, radio))
            {
                radioClicks++;
            }
        };
        UIRoot root = CreateWindowRoot(radio);

        ClickCenter(root, inner);

        Assert.Equal(1, innerClicks);
        Assert.Equal(0, radioClicks);
        Assert.False(radio.IsChecked);

        LayoutRect radioBounds = radio.ArrangedBounds;
        Click(root, radioBounds.X + 5, radioBounds.Y + 5);

        Assert.Equal(1, innerClicks);
        Assert.Equal(1, radioClicks);
        Assert.True(radio.IsChecked);
    }

    [Fact]
    public void ReleaseOnRadioButtonAfterPressOutsideLeavesItUnchecked()
    {
        RadioButton radio = new() { Content = "R", Width = 100, Height = 50 };
        UIRoot root = CreateCanvasRoot(radio);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(250, 150, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(50, 25, previousDown: true, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(50, 25, previousDown: true));
        root.ProcessFrame();

        Assert.False(radio.IsChecked);
    }

    private static (Button Button, DrawCommand Text) RenderStringButton(
        string label,
        float width,
        float height,
        HorizontalAlignment? horizontal = null,
        VerticalAlignment? vertical = null)
    {
        UIRoot root = new(400, 200);
        Button button = new() { Width = width, Height = height, Content = label, Foreground = new Cerneala.UI.Media.SolidColorBrush(Color.Black) };
        if (float.IsNaN(height))
        {
            // The root stretches a child without a size; a top-left Button gets exactly its desired size.
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
        }

        if (horizontal is { } h)
        {
            button.HorizontalContentAlignment = h;
        }

        if (vertical is { } v)
        {
            button.VerticalContentAlignment = v;
        }

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        return (button, FirstDrawText(root.RetainedRenderer.Commit(root)));
    }

    private static DrawCommand FirstDrawText(DrawCommandList commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Kind == DrawCommandKind.DrawText)
            {
                return commands[i];
            }
        }

        throw new InvalidOperationException("The Button drew no text.");
    }

    private static Button CreateTemplatedButton(
        string template,
        object content,
        HorizontalAlignment? horizontal = null,
        VerticalAlignment? vertical = null)
    {
        UIRoot root = new(400, 200);
        Button button = new()
        {
            Width = 300,
            Height = 70,
            Content = content,
            ComponentTemplate = template == "Modern" ? ButtonTemplates.Modern : DefaultTheme.CreateButtonTemplate()
        };
        if (horizontal is { } h)
        {
            button.HorizontalContentAlignment = h;
        }

        if (vertical is { } v)
        {
            button.VerticalContentAlignment = v;
        }

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        return button;
    }

    private static ContentPresenter FindPresenter(UIElement element)
    {
        return TryFindPresenter(element) ?? throw new InvalidOperationException("No ContentPresenter was found.");
    }

    private static ContentPresenter? TryFindPresenter(UIElement element)
    {
        if (element is ContentPresenter presenter)
        {
            return presenter;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            if (TryFindPresenter(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static LayoutRect Deflate(LayoutRect bounds, Control control)
    {
        return new LayoutRect(
            bounds.X + control.BorderThickness.Left + control.Padding.Left,
            bounds.Y + control.BorderThickness.Top + control.Padding.Top,
            bounds.Width - control.BorderThickness.Horizontal - control.Padding.Horizontal,
            bounds.Height - control.BorderThickness.Vertical - control.Padding.Vertical);
    }

    private static void AssertCenteredAtDesiredSize(UIElement element, LayoutRect inner)
    {
        LayoutRect arranged = element.ArrangedBounds;
        Assert.True(element.DesiredSize.Width < inner.Width && element.DesiredSize.Height < inner.Height);
        Assert.Equal(element.DesiredSize.Width, arranged.Width);
        Assert.Equal(element.DesiredSize.Height, arranged.Height);
        Assert.Equal(inner.X + (inner.Width / 2), arranged.X + (arranged.Width / 2), 0.5f);
        Assert.Equal(inner.Y + (inner.Height / 2), arranged.Y + (arranged.Height / 2), 0.5f);
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
        return CreateCanvasRoot(new Cerneala.UI.Layout.Panels.Canvas(), child);
    }

    private static UIRoot CreateCanvasRoot(Cerneala.UI.Layout.Panels.Canvas canvas, UIElement child)
    {
        UIRoot root = new(300, 200);
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

    private static InputFrame MoveFrame(float fromX, float fromY, float toX, float toY)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(fromX, fromY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(toX, toY).WithButton(InputMouseButton.Left, true);
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
