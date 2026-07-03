using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class PseudoClassTests
{
    [Fact]
    public void RegistryReportsBuiltInPseudoClassProperties()
    {
        PseudoClassRegistry registry = PseudoClassRegistry.Default;

        Assert.True(registry.AffectsPseudoClass(UIElement.IsPointerOverProperty));
        Assert.True(registry.AffectsPseudoClass(UIElement.IsKeyboardFocusedProperty));
        Assert.True(registry.AffectsPseudoClass(UIElement.IsKeyboardFocusWithinProperty));
        Assert.True(registry.AffectsPseudoClass(UIElement.IsEnabledProperty));
        Assert.True(registry.AffectsPseudoClass(ButtonBase.IsPressedProperty));
        Assert.True(registry.AffectsPseudoClass(ListBoxItem.IsSelectedProperty));
        Assert.True(registry.AffectsPseudoClass(TabItem.IsSelectedProperty));
    }

    [Fact]
    public void RegistryCanRegisterCustomPseudoClassProperty()
    {
        UiProperty<bool> selectedProperty = UiProperty<bool>.Register(
            $"{nameof(PseudoClassTests)}_{Guid.NewGuid():N}",
            typeof(PseudoClassTests),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsStyle));
        PseudoClassRegistry registry = new();

        registry.Register(selectedProperty, PseudoClass.Selected);

        Assert.True(registry.AffectsPseudoClass(selectedProperty));
        Assert.Equal(PseudoClass.Selected, registry.GetPseudoClasses(selectedProperty));
    }

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
