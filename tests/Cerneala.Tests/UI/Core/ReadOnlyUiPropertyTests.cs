using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class ReadOnlyUiPropertyTests
{
    [Fact]
    public void PublicSetRejectsReadOnlyProperty()
    {
        UiPropertyKey<int> key = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(ReadOnlyUiPropertyTests),
            new UiPropertyMetadata<int>(0));
        UiObject owner = new();

        Assert.True(key.Property.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => owner.SetValue(key.Property, 1));
    }

    [Fact]
    public void KeySetUpdatesReadOnlyProperty()
    {
        UiPropertyKey<int> key = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(ReadOnlyUiPropertyTests),
            new UiPropertyMetadata<int>(0));
        UiObject owner = new();

        int previous = owner.SetValue(key, 3);

        Assert.Equal(0, previous);
        Assert.Equal(3, owner.GetValue(key.Property));
    }

    [Fact]
    public void PublicClearRejectsReadOnlyProperty()
    {
        UiPropertyKey<int> key = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(ReadOnlyUiPropertyTests),
            new UiPropertyMetadata<int>(0));
        UiObject owner = new();
        owner.SetValue(key, 7);

        Assert.Throws<InvalidOperationException>(() => owner.ClearValue(key.Property));
        Assert.Equal(7, owner.GetValue(key.Property));
    }

    private static string UniqueName()
    {
        return $"{nameof(ReadOnlyUiPropertyTests)}_{Guid.NewGuid():N}";
    }
}
