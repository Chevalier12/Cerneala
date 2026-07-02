using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyRegistryTests
{
    [Fact]
    public void RegisterAssignsStableUniqueIdentity()
    {
        UiProperty<int> first = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0));
        UiProperty<int> second = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyRegistryTests),
            new UiPropertyMetadata<int>(0));

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.DiagnosticName, second.DiagnosticName);
    }

    [Fact]
    public void RegisterRejectsDuplicateOwnerAndName()
    {
        string name = UniqueName();

        UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(0));

        Assert.Throws<InvalidOperationException>(
            () => UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(
            () => UiProperty<int>.Register(name, typeof(UiPropertyRegistryTests), new UiPropertyMetadata<int>(0)));
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyRegistryTests)}_{Guid.NewGuid():N}";
    }
}
