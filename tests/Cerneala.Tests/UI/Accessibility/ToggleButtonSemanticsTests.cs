using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class ToggleButtonSemanticsTests
{
    [Fact]
    public void CheckedCheckBoxExposesCheckBoxRoleNameAndCheckedState()
    {
        SemanticsNode node = BuildSingle(new CheckBox { Content = "Bold", IsChecked = true });

        Assert.Equal(SemanticsRole.CheckBox, node.Role);
        Assert.Equal("Bold", node.Name);
        Assert.True(node.GetProperty<bool>(SemanticsProperty.IsChecked));
    }

    [Fact]
    public void RadioButtonExposesRadioButtonRoleAndCheckedStateWhetherCheckedOrNot()
    {
        UIRoot root = new();
        StackPanel panel = new();
        panel.Children.Add(new RadioButton { Content = "Small" });
        panel.Children.Add(new RadioButton { Content = "Large", IsChecked = true });
        root.VisualChildren.Add(panel);

        IReadOnlyList<SemanticsNode> nodes = new SemanticsProvider().Build(root).Root.Children.Single().Children;

        Assert.All(nodes, node => Assert.Equal(SemanticsRole.RadioButton, node.Role));
        Assert.Equal(["Small", "Large"], nodes.Select(node => node.Name));
        Assert.Equal(false, nodes[0].Properties[SemanticsProperty.IsChecked]);
        Assert.Equal(true, nodes[1].Properties[SemanticsProperty.IsChecked]);
    }

    [Fact]
    public void PlainToggleButtonKeepsButtonRoleAndExposesCheckedState()
    {
        SemanticsNode node = BuildSingle(new ToggleButton { Content = "Wrap" });

        Assert.Equal(SemanticsRole.Button, node.Role);
        Assert.Equal("Wrap", node.Name);
        Assert.Equal(false, node.Properties[SemanticsProperty.IsChecked]);
    }

    [Fact]
    public void ButtonAndRepeatButtonKeepButtonRoleWithoutCheckedState()
    {
        SemanticsNode button = BuildSingle(new Button { Content = "Save" });
        SemanticsNode repeat = BuildSingle(new RepeatButton { Content = "+" });

        Assert.Equal(SemanticsRole.Button, button.Role);
        Assert.Equal(SemanticsRole.Button, repeat.Role);
        Assert.False(button.Properties.ContainsKey(SemanticsProperty.IsChecked));
        Assert.False(repeat.Properties.ContainsKey(SemanticsProperty.IsChecked));
    }

    [Fact]
    public void CreateReturnsAToggleButtonAutomationPeerWhoseInvokeTogglesTheCheckBox()
    {
        CheckBox checkBox = new() { Content = "Bold" };

        AutomationPeer peer = AutomationPeer.Create(checkBox);

        ToggleButtonAutomationPeer togglePeer = Assert.IsType<ToggleButtonAutomationPeer>(peer);
        Assert.IsAssignableFrom<ButtonAutomationPeer>(peer);
        Assert.True(togglePeer.Invoke());
        Assert.True(checkBox.IsChecked);
        Assert.True(togglePeer.GetProperties()[SemanticsProperty.IsChecked] is true);
    }

    [Fact]
    public async Task ServoClickOnCheckBoxIsReportedAsCheckedByTheNextQuery()
    {
        CheckBox checkBox = new() { Content = "Bold", Width = 120, Height = 30 };
        ServoApi.SetId(checkBox, "bold");
        (UiHost host, _) = CreateHost(checkBox);
        ServoApi servo = new(host);
        ServoElement before = await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.CheckBox));
        Assert.Equal(false, before.Properties[SemanticsProperty.IsChecked]);

        await servo.ClickAsync(ServoTarget.ById("bold"));

        ServoElement after = await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.CheckBox));
        Assert.True(checkBox.IsChecked);
        Assert.Equal(true, after.Properties[SemanticsProperty.IsChecked]);
    }

    [Fact]
    public async Task IsCheckedChangeIsVisibleToTheNextQueryWithoutAFrame()
    {
        // A plain ToggleButton has no template part whose visibility follows IsChecked, so only IsChecked's own
        // metadata can mark the cached semantics tree dirty.
        ToggleButton toggle = new() { Content = "Wrap", Width = 120, Height = 30 };
        ServoApi.SetId(toggle, "wrap");
        (UiHost host, UIRoot root) = CreateHost(toggle);
        ServoApi servo = new(host);
        Assert.Equal(false, (await servo.FindAsync(ServoTarget.ById("wrap"))).Properties[SemanticsProperty.IsChecked]);
        int buildsBefore = root.ServoSemanticsBuildCount;

        toggle.IsChecked = true;

        Assert.Equal(true, (await servo.FindAsync(ServoTarget.ById("wrap"))).Properties[SemanticsProperty.IsChecked]);
        Assert.Equal(1, root.ServoSemanticsBuildCount - buildsBefore);
    }

    private static SemanticsNode BuildSingle(UIElement element)
    {
        UIRoot root = new();
        root.VisualChildren.Add(element);
        return new SemanticsProvider().Build(root).Root.Children.Single();
    }

    private static (UiHost Host, UIRoot Root) CreateHost(UIElement content)
    {
        UIRoot root = new(240, 120);
        root.VisualChildren.Add(content);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        return (host, root);
    }

    private static InputFrame EmptyInput() =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
}
