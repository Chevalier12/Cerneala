using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyStoreTests
{
    [Fact]
    public void EffectiveValueUsesExplicitPrecedence()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();

        owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);
        owner.SetValue(property, "aspect-base", UiPropertyValueSource.AspectBase);
        owner.SetValue(property, "aspect-state", UiPropertyValueSource.AspectVisualState);
        owner.SetValue(property, "animation", UiPropertyValueSource.Animation);
        owner.SetValue(property, "local", UiPropertyValueSource.Local);

        Assert.Equal("local", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, owner.GetValueSource(property));
    }

    [Fact]
    public void ClearingHigherSourceRevealsNextEffectiveValue()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();
        owner.SetValue(property, "aspect", UiPropertyValueSource.AspectBase);
        owner.SetValue(property, "local");

        string previous = owner.ClearValue(property);

        Assert.Equal("local", previous);
        Assert.Equal("aspect", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.AspectBase, owner.GetValueSource(property));
    }

    [Fact]
    public void StoreRejectsDefaultAsStoredSource()
    {
        UiPropertyStore store = new();
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<int>(0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => store.SetValue(property, UiPropertyValueSource.Default, 1));
    }

    [Fact]
    public void PublicClearRejectsReadOnlyProperty()
    {
        UiPropertyKey<int> key = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<int>(0));
        UiObject owner = new();
        owner.SetValue(key, 7);

        Assert.Throws<InvalidOperationException>(() => owner.ClearValue(key.Property));
        Assert.Equal(7, owner.GetValue(key.Property));
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyStoreTests)}_{Guid.NewGuid():N}";
    }
}
