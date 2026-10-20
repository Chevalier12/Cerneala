using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Controls;

public sealed class RadioButtonTests
{
    [Fact]
    public void ChildOfStackPanelAndContentOfContentControlAreReachableAndLogicallyParented()
    {
        StackPanel panel = new();
        RadioButton inPanel = new();
        panel.Children.Add(inPanel);
        RadioButton inContent = new();
        ContentControl host = new() { Content = inContent };
        panel.Children.Add(host);
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(panel);

        UIElement[] preOrder = ElementTreeWalker.PreOrder(root).ToArray();

        Assert.Contains(inPanel, preOrder);
        Assert.Contains(inContent, preOrder);
        Assert.Same(panel, inPanel.LogicalParent);
        Assert.Same(host, inContent.LogicalParent);
    }

    [Fact]
    public async Task ClickingAnotherRadioButtonOfTheSamePanelUnchecksTheFirst()
    {
        (UiHost host, StackPanel panel) = CreateHost(out RadioButton[] radios, "small", "large");
        ServoApi servo = new(host);
        RadioButton small = radios[0];
        RadioButton large = radios[1];

        await servo.ClickAsync(ServoTarget.ById("small"));
        Assert.True(small.IsChecked);
        Assert.False(large.IsChecked);

        await servo.ClickAsync(ServoTarget.ById("large"));

        Assert.False(small.IsChecked);
        Assert.True(large.IsChecked);
        GC.KeepAlive(panel);
    }

    [Fact]
    public async Task ClickingACheckedRadioButtonKeepsItChecked()
    {
        (UiHost host, _) = CreateHost(out RadioButton[] radios, "small", "large");
        ServoApi servo = new(host);

        await servo.ClickAsync(ServoTarget.ById("small"));
        await servo.ClickAsync(ServoTarget.ById("small"));

        Assert.True(radios[0].IsChecked);
        Assert.False(radios[1].IsChecked);
    }

    [Fact]
    public async Task RadioButtonsWithoutGroupNameFormOneGroupPerParent()
    {
        RadioButton p1a = Radio("p1a");
        RadioButton p1b = Radio("p1b");
        RadioButton p2a = Radio("p2a");
        StackPanel panel1 = new();
        panel1.Children.Add(p1a);
        panel1.Children.Add(p1b);
        StackPanel panel2 = new();
        panel2.Children.Add(p2a);
        StackPanel outer = new();
        outer.Children.Add(panel1);
        outer.Children.Add(panel2);
        ServoApi servo = new(CreateHost(outer));

        await servo.ClickAsync(ServoTarget.ById("p1a"));
        await servo.ClickAsync(ServoTarget.ById("p2a"));
        await servo.ClickAsync(ServoTarget.ById("p1b"));

        Assert.False(p1a.IsChecked);
        Assert.True(p1b.IsChecked);
        Assert.True(p2a.IsChecked);
    }

    [Fact]
    public async Task ArrowKeysMoveFocusAndCheckTheNextAndPreviousRadioButtonAndWrap()
    {
        (UiHost host, _) = CreateHost(out RadioButton[] radios, "small", "medium", "large");
        ServoApi servo = new(host);
        RadioButton small = radios[0];
        RadioButton medium = radios[1];
        RadioButton large = radios[2];
        await servo.ClickAsync(ServoTarget.ById("medium"));
        Assert.True(medium.IsKeyboardFocused);
        Assert.True(medium.IsChecked);

        await servo.PressKeyAsync(InputKey.Down);
        AssertOnlyCheckedAndFocused(large, small, medium, large);

        await servo.PressKeyAsync(InputKey.Down);
        AssertOnlyCheckedAndFocused(small, small, medium, large);

        await servo.PressKeyAsync(InputKey.Up);
        AssertOnlyCheckedAndFocused(large, small, medium, large);

        await servo.PressKeyAsync(InputKey.Up);
        AssertOnlyCheckedAndFocused(medium, small, medium, large);

        await servo.PressKeyAsync(InputKey.Right);
        AssertOnlyCheckedAndFocused(large, small, medium, large);

        await servo.PressKeyAsync(InputKey.Left);
        AssertOnlyCheckedAndFocused(medium, small, medium, large);
    }

    [Fact]
    public async Task ArrowKeysSkipADisabledRadioButton()
    {
        (UiHost host, _) = CreateHost(out RadioButton[] radios, "small", "medium", "large");
        ServoApi servo = new(host);
        RadioButton small = radios[0];
        RadioButton medium = radios[1];
        RadioButton large = radios[2];
        await servo.ClickAsync(ServoTarget.ById("small"));
        medium.IsEnabled = false;

        await servo.PressKeyAsync(InputKey.Down);
        AssertOnlyCheckedAndFocused(large, small, medium, large);

        await servo.PressKeyAsync(InputKey.Up);
        AssertOnlyCheckedAndFocused(small, small, medium, large);
    }

    [Fact]
    public async Task ArrowKeyWithNoOtherFocusableMemberChangesNothingAndStaysUnhandled()
    {
        (UiHost host, _) = CreateHost(out RadioButton[] radios, "only");
        ServoApi servo = new(host);
        RadioButton only = radios[0];
        await servo.ClickAsync(ServoTarget.ById("only"));
        bool handled = false;
        only.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => handled = args.Handled);

        await servo.PressKeyAsync(InputKey.Down);

        Assert.True(only.IsChecked);
        Assert.True(only.IsKeyboardFocused);
        Assert.False(handled);
    }

    [Fact]
    public async Task ClickingAnotherRadioButtonRaisesUncheckedOfTheOldOneBeforeCheckedOfTheNewOne()
    {
        (UiHost host, _) = CreateHost(out RadioButton[] radios, "small", "large");
        ServoApi servo = new(host);
        RadioButton small = radios[0];
        RadioButton large = radios[1];
        List<string> log = [];
        small.Handlers.AddHandler(ToggleButton.CheckedEvent, (_, _) => log.Add("small checked"));
        small.Handlers.AddHandler(ToggleButton.UncheckedEvent, (_, _) => log.Add("small unchecked"));
        large.Handlers.AddHandler(ToggleButton.CheckedEvent, (_, _) => log.Add("large checked"));
        large.Handlers.AddHandler(ToggleButton.UncheckedEvent, (_, _) => log.Add("large unchecked"));

        await servo.ClickAsync(ServoTarget.ById("small"));
        await servo.ClickAsync(ServoTarget.ById("large"));

        Assert.Equal(["small checked", "small unchecked", "large checked"], log);

        await servo.ClickAsync(ServoTarget.ById("large"));

        Assert.Equal(["small checked", "small unchecked", "large checked"], log);
    }

    [Fact]
    public void AssigningIsCheckedInCodeUnchecksTheOtherRadioButtonAndRaisesOneUncheckedAndOneChecked()
    {
        StackPanel panel = new();
        RadioButton small = new();
        RadioButton large = new();
        panel.Children.Add(small);
        panel.Children.Add(large);
        small.IsChecked = true;
        List<string> log = [];
        small.Handlers.AddHandler(ToggleButton.CheckedEvent, (_, _) => log.Add("small checked"));
        small.Handlers.AddHandler(ToggleButton.UncheckedEvent, (_, _) => log.Add("small unchecked"));
        large.Handlers.AddHandler(ToggleButton.CheckedEvent, (_, _) => log.Add("large checked"));
        large.Handlers.AddHandler(ToggleButton.UncheckedEvent, (_, _) => log.Add("large unchecked"));

        large.IsChecked = true;

        Assert.False(small.IsChecked);
        Assert.True(large.IsChecked);
        Assert.Equal(["small unchecked", "large checked"], log);
    }

    [Fact]
    public void GroupNameIsEmptyByDefault()
    {
        Assert.Equal(string.Empty, new RadioButton().GroupName);
    }

    [Fact]
    public async Task RadioButtonsOfDifferentGroupNamesInOnePanelFormSeparateGroups()
    {
        RadioButton a1 = Radio("a1", "A");
        RadioButton a2 = Radio("a2", "A");
        RadioButton b1 = Radio("b1", "B");
        RadioButton b2 = Radio("b2", "B");
        StackPanel panel = new();
        panel.Children.Add(a1);
        panel.Children.Add(b1);
        panel.Children.Add(a2);
        panel.Children.Add(b2);
        ServoApi servo = new(CreateHost(panel));

        await servo.ClickAsync(ServoTarget.ById("a1"));
        await servo.ClickAsync(ServoTarget.ById("b1"));
        await servo.ClickAsync(ServoTarget.ById("a2"));

        Assert.False(a1.IsChecked);
        Assert.True(a2.IsChecked);
        Assert.True(b1.IsChecked);
        Assert.False(b2.IsChecked);
    }

    [Fact]
    public async Task RadioButtonsWithTheSameGroupNameInDifferentParentsFormOneGroup()
    {
        RadioButton first = Radio("first", "G");
        RadioButton second = Radio("second", "G");
        RadioButton unnamed = Radio("unnamed");
        StackPanel panel1 = new();
        panel1.Children.Add(first);
        panel1.Children.Add(unnamed);
        StackPanel panel2 = new();
        panel2.Children.Add(second);
        StackPanel outer = new();
        outer.Children.Add(panel1);
        outer.Children.Add(panel2);
        ServoApi servo = new(CreateHost(outer));

        await servo.ClickAsync(ServoTarget.ById("unnamed"));
        await servo.ClickAsync(ServoTarget.ById("first"));
        await servo.ClickAsync(ServoTarget.ById("second"));

        Assert.False(first.IsChecked);
        Assert.True(second.IsChecked);
        Assert.True(unnamed.IsChecked);
    }

    [Fact]
    public void RadioButtonsWithTheSameGroupNameUnderDifferentRootsDoNotAffectEachOther()
    {
        RadioButton inFirstRoot = Radio("inFirstRoot", "G");
        RadioButton inSecondRoot = Radio("inSecondRoot", "G");
        new UIRoot(200, 200).VisualChildren.Add(inFirstRoot);
        new UIRoot(200, 200).VisualChildren.Add(inSecondRoot);

        inFirstRoot.IsChecked = true;
        inSecondRoot.IsChecked = true;

        Assert.True(inFirstRoot.IsChecked);
        Assert.True(inSecondRoot.IsChecked);
    }

    [Fact]
    public async Task ArrowKeysMoveWithinAGroupNameAcrossParents()
    {
        RadioButton first = Radio("first", "G");
        RadioButton second = Radio("second", "G");
        StackPanel panel1 = new();
        panel1.Children.Add(first);
        StackPanel panel2 = new();
        panel2.Children.Add(second);
        StackPanel outer = new();
        outer.Children.Add(panel1);
        outer.Children.Add(panel2);
        ServoApi servo = new(CreateHost(outer));
        await servo.ClickAsync(ServoTarget.ById("first"));

        await servo.PressKeyAsync(InputKey.Down);

        AssertOnlyCheckedAndFocused(second, first, second);
    }

    private static void AssertOnlyCheckedAndFocused(RadioButton expected, params RadioButton[] all)
    {
        foreach (RadioButton radio in all)
        {
            Assert.Equal(ReferenceEquals(radio, expected), radio.IsChecked);
        }

        Assert.True(expected.IsKeyboardFocused);
    }

    private static RadioButton Radio(string id) => Radio(id, string.Empty);

    private static RadioButton Radio(string id, string groupName)
    {
        RadioButton radio = new() { Content = id, Width = 120, Height = 24, GroupName = groupName };
        ServoApi.SetId(radio, id);
        return radio;
    }

    private static (UiHost Host, StackPanel Panel) CreateHost(out RadioButton[] radios, params string[] ids)
    {
        StackPanel panel = new();
        radios = ids.Select(id => Radio(id)).ToArray();
        foreach (RadioButton radio in radios)
        {
            panel.Children.Add(radio);
        }

        return (CreateHost(panel), panel);
    }

    private static UiHost CreateHost(UIElement content, float width = 320, float height = 240)
    {
        UIRoot root = new(width, height);
        root.VisualChildren.Add(content);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(width, height)
        });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        return host;
    }

    private static InputFrame EmptyInput() =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
}
