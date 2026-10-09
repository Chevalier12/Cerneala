using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectCheckedExpandedTests
{
    [Theory]
    [InlineData(typeof(ToggleButton))]
    [InlineData(typeof(CheckBox))]
    [InlineData(typeof(RadioButton))]
    [InlineData(typeof(MenuItem))]
    [InlineData(typeof(ComboBox))]
    public void ProjectionTracksCurrentControlStateWithoutLosingExistingStates(Type controlType)
    {
        (Control control, UiProperty<bool> property, AspectState state) = CreateControl(controlType);
        control.IsPointerOver = true;
        control.IsEnabled = false;
        // A submenu can only open on an enabled MenuItem.
        if (control is MenuItem)
        {
            control.IsEnabled = true;
        }

        Assert.False(AspectStateSet.FromElement(control).Contains(state));
        control.SetValue(property, true);
        Assert.True(control.GetValue(property));

        AspectStateSet active = AspectStateSet.FromElement(control);
        Assert.True(active.Contains(state));
        Assert.True(active.Contains(AspectState.Hover));
        Assert.Equal(!control.IsEnabled, active.Contains(AspectState.Disabled));
        Assert.False(active.Contains(state.Equals(AspectState.Checked) ? AspectState.Expanded : AspectState.Checked));

        control.SetValue(property, false);
        AspectStateSet inactive = AspectStateSet.FromElement(control);
        Assert.False(inactive.Contains(state));
        Assert.True(inactive.Contains(AspectState.Hover));
        Assert.Equal(!control.IsEnabled, inactive.Contains(AspectState.Disabled));
    }

    [Fact]
    public void OrdinaryButtonDoesNotProjectCheckedOrExpanded()
    {
        AspectStateSet states = AspectStateSet.FromElement(new Button());

        Assert.False(states.Contains(AspectState.Checked));
        Assert.False(states.Contains(AspectState.Expanded));
    }

    [Theory]
    [InlineData(typeof(ToggleButton))]
    [InlineData(typeof(RadioButton))]
    [InlineData(typeof(MenuItem))]
    [InlineData(typeof(ComboBox))]
    public void SourcePropertyInvalidatesAspect(Type controlType)
    {
        (_, UiProperty<bool> property, _) = CreateControl(controlType);

        Assert.True(property.Options.HasFlag(UiPropertyOptions.AffectsAspect));
    }

    [Theory]
    [InlineData(typeof(ToggleButton))]
    [InlineData(typeof(CheckBox))]
    [InlineData(typeof(RadioButton))]
    [InlineData(typeof(MenuItem))]
    [InlineData(typeof(ComboBox))]
    public void PropertyChangeReappliesTrackedStateRuleInBothDirections(Type controlType)
    {
        (Control control, UiProperty<bool> property, AspectState state) = CreateControl(controlType);
        AspectCatalog catalog = AspectCatalog.FromPackages([CreatePackage(controlType, state)], version: 1);
        using AspectInvalidation invalidation = new(new AspectEngine(), catalog, new AspectEnvironment("state-test"));
        invalidation.Track(control);
        Assert.Equal(1f, control.Opacity);

        control.SetValue(property, true);

        Assert.True(control.GetValue(property));
        Assert.Equal(0.5f, control.Opacity);
        control.SetValue(property, false);
        Assert.Equal(1f, control.Opacity);
    }

    [Theory]
    [InlineData(typeof(ToggleButton))]
    [InlineData(typeof(CheckBox))]
    [InlineData(typeof(RadioButton))]
    [InlineData(typeof(MenuItem))]
    [InlineData(typeof(ComboBox))]
    public void PropertyChangeQueuesStateRuleForNextFrameInBothDirections(Type controlType)
    {
        (Control control, UiProperty<bool> property, AspectState state) = CreateControl(controlType);
        UIRoot root = AttachWithRule(control, state);

        control.SetValue(property, true);

        Assert.True(control.GetValue(property));
        Assert.Contains(control, root.AspectQueue.Snapshot());
        Assert.True(root.ProcessFrame().AspectElements > 0);
        Assert.Equal(0.5f, control.Opacity);

        control.SetValue(property, false);

        Assert.Contains(control, root.AspectQueue.Snapshot());
        Assert.True(root.ProcessFrame().AspectElements > 0);
        Assert.Equal(1f, control.Opacity);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void ClickingCheckBoxActivatesAndDeactivatesCheckedRule()
    {
        CheckBox checkBox = new() { Content = "Agree", Width = 120, Height = 40 };
        UIRoot root = AttachWithRule(checkBox, AspectState.Checked);
        ElementInputBridge bridge = new();

        Click(bridge, root, checkBox);
        Assert.True(checkBox.IsChecked);
        root.ProcessFrame();
        Assert.Equal(0.5f, checkBox.Opacity);

        Click(bridge, root, checkBox);
        Assert.False(checkBox.IsChecked);
        root.ProcessFrame();
        Assert.Equal(1f, checkBox.Opacity);
    }

    [Fact]
    public void ClickingRadioButtonActivatesCheckedRule()
    {
        RadioButton radioButton = new() { Content = "Choice", Width = 120, Height = 40 };
        UIRoot root = AttachWithRule(radioButton, AspectState.Checked);
        ElementInputBridge bridge = new();

        Click(bridge, root, radioButton);

        Assert.True(radioButton.IsChecked);
        root.ProcessFrame();
        Assert.Equal(0.5f, radioButton.Opacity);
    }

    [Fact]
    public void ClickingMenuItemThenLeafActivatesAndDeactivatesExpandedRule()
    {
        MenuItem menuItem = new() { Header = "File", Width = 120, Height = 40 };
        MenuItem leaf = new() { Header = "Open" };
        menuItem.Items.Add(leaf);
        MenuBar menuBar = new();
        menuBar.Items.Add(menuItem);
        UIRoot root = AttachWithRule(menuItem, AspectState.Expanded, menuBar);
        ElementInputBridge bridge = new();

        Click(bridge, root, menuItem);
        Assert.True(menuItem.IsSubmenuOpen);
        root.ProcessFrame();
        Assert.Equal(0.5f, menuItem.Opacity);

        Click(bridge, root, leaf);
        Assert.False(menuItem.IsSubmenuOpen);
        root.ProcessFrame();
        Assert.Equal(1f, menuItem.Opacity);
    }

    [Fact]
    public void ClickingComboBoxToggleAndPressingEscapeUpdatesExpandedRule()
    {
        ComboBox comboBox = new() { Width = 160, Height = 40, IsEditable = false };
        comboBox.Items.Add("One");
        UIRoot root = AttachWithRule(comboBox, AspectState.Expanded);
        ToggleButton toggle = Assert.IsType<ToggleButton>(comboBox.ComponentTemplateInstance!.Parts["PART_DropDownToggle"]);
        ElementInputBridge bridge = new();

        Click(bridge, root, toggle);
        Assert.True(comboBox.IsDropDownOpen);
        root.ProcessFrame();
        Assert.Equal(0.5f, comboBox.Opacity);

        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.Escape]), []));
        bridge.Dispatch(root, new InputFrame(
            PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys([InputKey.Escape]), KeyboardSnapshot.Empty, []));
        Assert.False(comboBox.IsDropDownOpen);
        root.ProcessFrame();
        Assert.Equal(1f, comboBox.Opacity);
    }

    private static (Control Control, UiProperty<bool> Property, AspectState State) CreateControl(Type controlType)
    {
        Control control = (Control)Activator.CreateInstance(controlType)!;
        if (control is MenuItem menuItem)
        {
            menuItem.Items.Add("Child");
        }

        return control switch
        {
            ToggleButton => (control, ToggleButton.IsCheckedProperty, AspectState.Checked),
            RadioButton => (control, RadioButton.IsCheckedProperty, AspectState.Checked),
            MenuItem => (control, MenuItem.IsSubmenuOpenProperty, AspectState.Expanded),
            ComboBox => (control, ComboBox.IsDropDownOpenProperty, AspectState.Expanded),
            _ => throw new ArgumentException("Unsupported test control.", nameof(controlType))
        };
    }

    private static AspectPackage CreatePackage(Type controlType, AspectState state)
    {
        return AspectPackage.Create("checked-expanded-test")
            .Components(components => components.AddRule(new AspectRuleSet(
                "active-state", AspectLayer.App,
                new AspectTarget(controlType, conditions: [AspectCondition.State(state)]),
                [new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.5f))],
                declarationOrder: 0)));
    }

    private static UIRoot AttachWithRule(Control control, AspectState state, Control? visualOwner = null)
    {
        UIRoot root = new(320, 240);
        root.AspectRegistry.Register(CreatePackage(control.GetType(), state));
        root.VisualChildren.Add(visualOwner ?? control);
        root.ProcessFrame();
        Assert.Equal(1f, control.Opacity);
        return root;
    }

    private static void Click(ElementInputBridge bridge, UIRoot root, UIElement element)
    {
        float x = element.ArrangedBounds.X + element.ArrangedBounds.Width / 2;
        float y = element.ArrangedBounds.Y + element.ArrangedBounds.Height / 2;
        Assert.True(element.ArrangedBounds.Width > 0 && element.ArrangedBounds.Height > 0);
        PointerSnapshot pointer = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot pressed = pointer.WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(pointer, pressed, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(pressed, pointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }
}
