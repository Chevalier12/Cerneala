using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyTests
{
    [Fact]
    public void RegisteredPropertyExposesMetadataAndDefaultValue()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyTests),
            new UiPropertyMetadata<int>(42, UiPropertyOptions.AffectsRender));

        UiObject owner = new();

        Assert.Equal(42, owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Default, owner.GetValueSource(property));
        Assert.Equal(typeof(UiPropertyTests), property.OwnerType);
        Assert.Equal(typeof(int), property.ValueType);
        Assert.True(property.Options.HasFlag(UiPropertyOptions.AffectsRender));
        Assert.Contains(property.Name, property.DiagnosticName, StringComparison.Ordinal);
    }

    [Fact]
    public void SetValueReturnsPreviousTypedValue()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyTests),
            new UiPropertyMetadata<int>(10));
        UiObject owner = new();

        int previous = owner.SetValue(property, 20);

        Assert.Equal(10, previous);
        Assert.Equal(20, owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, owner.GetValueSource(property));
    }

    [Fact]
    public void ValidationRejectsInvalidValueAndDoesNotStoreIt()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyTests),
            new UiPropertyMetadata<int>(0, validateValue: value => value >= 0));
        UiObject owner = new();

        Assert.Throws<ArgumentException>(() => owner.SetValue(property, -1));
        Assert.Equal(0, owner.GetValue(property));
    }

    [Fact]
    public void CoercionRunsBeforeEffectiveValueComparison()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyTests),
            new UiPropertyMetadata<int>(10, coerceValue: (_, value) => Math.Clamp(value, 0, 10)));
        UiObject owner = new();
        int changes = 0;
        owner.PropertyChanged += (_, _) => changes++;

        int previous = owner.SetValue(property, 20);

        Assert.Equal(10, previous);
        Assert.Equal(10, owner.GetValue(property));
        Assert.Equal(0, changes);
    }

    [Fact]
    public void TypedPropertyChangedEventArgsExposeTypedValues()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyTests),
            new UiPropertyMetadata<int>(1));
        UiObject owner = new();
        UiPropertyChangedEventArgs<int>? captured = null;
        owner.PropertyChanged += (_, args) => captured = Assert.IsType<UiPropertyChangedEventArgs<int>>(args);

        owner.SetValue(property, 2);

        Assert.NotNull(captured);
        Assert.Same(property, captured.Property);
        Assert.Equal(1, captured.OldValue);
        Assert.Equal(2, captured.NewValue);
        Assert.Equal(UiPropertyValueSource.Local, captured.ValueSource);
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyTests)}_{Guid.NewGuid():N}";
    }
}
