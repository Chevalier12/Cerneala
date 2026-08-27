using System.Reflection;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.UI.Hosting;

public sealed class ApplicationBackendAttributeTests
{
    [Fact]
    public void ConstructorStoresTheSelectedBackendType()
    {
        ApplicationBackendAttribute attribute = new(typeof(FakeBackend));

        Assert.Equal(typeof(FakeBackend), attribute.BackendType);
    }

    [Fact]
    public void ConstructorRejectsNullBackendType()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new ApplicationBackendAttribute(null!));

        Assert.Equal("backendType", exception.ParamName);
    }

    [Fact]
    public void AttributeCanBeDeclaredOnceAtAssemblyScope()
    {
        AttributeUsageAttribute usage = typeof(ApplicationBackendAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        Assert.Equal(AttributeTargets.Assembly, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
    }

    private static class FakeBackend
    {
    }
}
