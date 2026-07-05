using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class FocusPolicyTests
{
    [Fact]
    public void PlainUiElementIsNotFocusableByDefault()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
        Assert.False(element.IsKeyboardFocused);
    }

    [Fact]
    public void ExplicitFocusableElementCanReceiveFocus()
    {
        UIRoot root = new();
        UIElement element = new() { Focusable = true };
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.True(changed);
        Assert.Same(element, manager.FocusedElement);
        Assert.True(element.IsKeyboardFocused);
    }

    [Theory]
    [InlineData(false, true, Visibility.Visible)]
    [InlineData(true, false, Visibility.Visible)]
    [InlineData(true, true, Visibility.Hidden)]
    [InlineData(true, true, Visibility.Collapsed)]
    public void InvalidFocusTargetsAreRejected(bool isEnabled, bool isVisible, Visibility visibility)
    {
        UIRoot root = new();
        UIElement element = new()
        {
            Focusable = true,
            IsEnabled = isEnabled,
            IsVisible = isVisible,
            Visibility = visibility
        };
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
        Assert.False(element.IsKeyboardFocused);
    }

    [Fact]
    public void DetachedElementCannotReceiveFocus()
    {
        UIElement element = new() { Focusable = true };
        ElementInputRouteMap map = new();
        FocusManager manager = new();

        bool changed = manager.Focus(element, map);

        Assert.False(changed);
        Assert.Null(manager.FocusedElement);
    }

    [Fact]
    public void ButtonAndTextBoxAreFocusableByDefault()
    {
        Assert.True(new Button().Focusable);
        Assert.True(new Button().IsTabStop);
        Assert.True(new TextBox().Focusable);
        Assert.True(new TextBox().IsTabStop);
    }

    [Fact]
    public void FocusedElementIsClearedWhenItNoLongerSatisfiesFocusPolicy()
    {
        UIRoot root = new();
        UIElement element = new() { Focusable = true };
        root.VisualChildren.Add(element);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        FocusManager manager = new();
        manager.Focus(element, map);

        element.Visibility = Visibility.Hidden;
        ElementInputRouteMap updatedMap = new ElementInputRouteBuilder().Build(root);
        manager.DispatchKeyboard(FocusManagerTests.FrameWithKey(InputKey.A), updatedMap);

        Assert.Null(manager.FocusedElement);
        Assert.False(element.IsKeyboardFocused);
        Assert.False(root.IsKeyboardFocusWithin);
    }
}
