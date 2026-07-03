using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class SetterTests
{
    [Fact]
    public void SetterStoresTypedPropertyAndValue()
    {
        Setter<DrawColor> setter = new(Control.BackgroundProperty, DrawColor.White);

        Assert.Same(Control.BackgroundProperty, setter.Property);
        Assert.Equal(typeof(DrawColor), setter.ValueType);
        Assert.Equal(DrawColor.White, setter.GetValue());
    }

    [Fact]
    public void SetterAppliesThroughRequestedValueSource()
    {
        Button button = new();
        Setter<DrawColor> setter = new(Control.BackgroundProperty, DrawColor.White);

        setter.Apply(button, UiPropertyValueSource.StyleBase, null);

        Assert.Equal(DrawColor.White, button.Background);
        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void ThemeBackedSetterRequiresThemeProvider()
    {
        ThemeKey<DrawColor> key = new("Accent");
        Setter<DrawColor> setter = new(Control.BackgroundProperty, new ThemeResource<DrawColor>(key));

        Assert.True(setter.IsThemeBacked);
        Assert.Throws<InvalidOperationException>(() => setter.GetValue());
    }

    [Fact]
    public void UntypedSetterFactoryRejectsMismatchedValues()
    {
        Assert.Throws<ArgumentException>(() => Setter.Create(Control.BackgroundProperty, "not a color"));
    }

    [Fact]
    public void UntypedSetterFactoryRejectsNullForValueTypeProperties()
    {
        Assert.Throws<ArgumentException>(() => Setter.Create(Control.FontSizeProperty, null));
    }

    [Fact]
    public void UntypedSetterFactoryAcceptsAssignableValues()
    {
        Setter setter = Setter.Create(Control.BackgroundProperty, DrawColor.White);

        Assert.Equal(Control.BackgroundProperty, setter.Property);
        Assert.Equal(DrawColor.White, setter.GetValue());
    }

    [Fact]
    public void SetterUsesPropertyValidation()
    {
        Setter<float> setter = new(Control.FontSizeProperty, -1);
        Button button = new();

        Assert.Throws<ArgumentException>(() => setter.Apply(button, UiPropertyValueSource.StyleBase, null));
    }
}
