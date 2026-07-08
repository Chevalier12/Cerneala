using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class InheritedUiPropertyTests
{
    [Fact]
    public void InheritedValueWinsOverDefault()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(InheritedUiPropertyTests),
            new UiPropertyMetadata<string>("default", UiPropertyOptions.Inherits));
        UiObject owner = new();

        owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);

        Assert.Equal("inherited", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Inherited, owner.GetValueSource(property));
    }

    [Fact]
    public void AspectAndLocalValuesWinOverInheritedValue()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(InheritedUiPropertyTests),
            new UiPropertyMetadata<string>("default", UiPropertyOptions.Inherits));
        UiObject owner = new();

        owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);
        owner.SetValue(property, "aspect", UiPropertyValueSource.AspectBase);

        Assert.Equal("aspect", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.AspectBase, owner.GetValueSource(property));

        owner.SetValue(property, "local");

        Assert.Equal("local", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, owner.GetValueSource(property));
    }

    private static string UniqueName()
    {
        return $"{nameof(InheritedUiPropertyTests)}_{Guid.NewGuid():N}";
    }
}
