using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleRuleTests
{
    [Fact]
    public void TypeSelectorMatchesDerivedElements()
    {
        StyleSelector selector = StyleSelector.ForType<Control>();

        Assert.True(selector.Matches(new Button()));
        Assert.False(selector.Matches(new UIElement()));
    }

    [Fact]
    public void PredicateSelectorControlsMatch()
    {
        Button target = new();
        StyleSelector selector = StyleSelector.Where("target", element => ReferenceEquals(element, target));

        Assert.True(selector.Matches(target));
        Assert.False(selector.Matches(new Button()));
    }

    [Fact]
    public void StyleRuleReportsSourceFromVisualState()
    {
        StyleRule baseRule = new(StyleSelector.ForType<Button>());
        StyleRule visualRule = new(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover));

        Assert.Equal(UiPropertyValueSource.StyleBase, baseRule.Source);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, visualRule.Source);
    }

    [Fact]
    public void StyleSheetKeepsRuleOrder()
    {
        StyleRule first = new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White));
        StyleRule second = new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black));

        StyleSheet sheet = new StyleSheet().Add(first).Add(second);

        Assert.Equal(new[] { first, second }, sheet.Rules);
    }
}
