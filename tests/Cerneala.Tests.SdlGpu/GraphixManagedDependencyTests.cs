using System.Text.Json;
using SDL3;

namespace Cerneala.Tests.SdlGpu;

public sealed class GraphixManagedDependencyTests
{
    [Fact]
    public void ManagedBindingComesFromGraphixPackageWithUnchangedClrIdentity()
    {
        using JsonDocument dependencies = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Cerneala.Tests.SdlGpu.deps.json")));
        JsonElement libraries = dependencies.RootElement.GetProperty("libraries");

        Assert.True(libraries.TryGetProperty("Graphix-CS/3.4.16.1", out JsonElement package),
            "The SDL adapter must resolve Graphix-CS 3.4.16.1 transitively.");
        Assert.Equal("package", package.GetProperty("type").GetString());
        Assert.DoesNotContain(libraries.EnumerateObject(),
            library => library.Name.StartsWith("SDL3-CS/", StringComparison.Ordinal));

        Assert.Equal("SDL3.SDL", typeof(SDL).FullName);
        Assert.Equal("SDL3-CS", typeof(SDL).Assembly.GetName().Name);
        Assert.Equal(new Version(3, 4, 16, 0), typeof(SDL).Assembly.GetName().Version);
    }
}
