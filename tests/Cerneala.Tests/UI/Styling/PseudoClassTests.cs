using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class PseudoClassTests
{
    [Fact]
    public void HoverMatchesPointerOver()
    {
        Button button = new() { IsPointerOver = true };

        Assert.True(PseudoClassMatcher.Matches(button, PseudoClass.Hover));
    }

    [Fact]
    public void PressedMatchesButtonBasePressed()
    {
        ButtonBase button = new Button { IsPressed = true };

        Assert.True(PseudoClassMatcher.Matches(button, PseudoClass.Pressed));
    }

    [Fact]
    public void FocusAndDisabledMatchElementState()
    {
        Button button = new()
        {
            IsKeyboardFocused = true,
            IsKeyboardFocusWithin = true,
            IsEnabled = false
        };

        Assert.True(PseudoClassMatcher.Matches(button, PseudoClass.Focus | PseudoClass.FocusWithin | PseudoClass.Disabled));
    }

    [Fact]
    public void SelectedDoesNotMatchWithoutSelectedStateSupport()
    {
        Assert.False(PseudoClassMatcher.Matches(new Button(), PseudoClass.Selected));
    }

    [Fact]
    public void SelectedCanMatchProviderSupportedState()
    {
        SelectableElement element = new(selected: true);

        Assert.True(PseudoClassMatcher.Matches(element, PseudoClass.Selected));
    }

    private sealed class SelectableElement(bool selected) : Button, IStylePseudoClassProvider
    {
        public bool IsPseudoClassActive(PseudoClass pseudoClass)
        {
            return pseudoClass switch
            {
                PseudoClass.Selected => selected,
                PseudoClass.Hover => IsPointerOver,
                PseudoClass.Pressed => IsPressed,
                PseudoClass.Focus => IsKeyboardFocused,
                PseudoClass.FocusWithin => IsKeyboardFocusWithin,
                PseudoClass.Disabled => !IsEnabled,
                _ => false
            };
        }
    }
}
