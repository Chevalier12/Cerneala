using System.Numerics;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneStreamingReflectionContractTests
{
    [Fact]
    public void SurfaceDrawingActivationDoesNotDiscoverRuntimeMembers()
    {
        string source = ReadSource("UI/Controls/RenderSurface2D.cs");
        Assert.DoesNotContain("System.Reflection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMethod(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetBaseDefinition(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OnDrawOverrideCache", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RoutedRegistrationDoesNotDiscoverEnumMetadata()
    {
        string source = ReadSource("UI/Input/RoutedEventRegistry.cs");
        Assert.DoesNotContain("Enum.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StreamedColliderDescriptorsDoNotDiscoverEnumMetadata()
    {
        Assert.DoesNotContain("Enum.", ReadSource("UI/Controls/TileColliderDescriptor2D.cs"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TileColliderShape2D.Box)]
    [InlineData(TileColliderShape2D.Circle)]
    [InlineData(TileColliderShape2D.Polygon)]
    [InlineData(TileColliderShape2D.Segment)]
    public void EveryDeclaredColliderShapeIsAccepted(TileColliderShape2D shape)
    {
        string points = shape == TileColliderShape2D.Segment ? "0,0 1,1" : "0,0 1,0 0,1";
        Assert.Equal(shape, new TileColliderDescriptor2D(shape, points: points).Shape);
        Assert.Equal(shape, new TileColliderDescriptor2D(shape, Matrix3x2.Identity, points: points).Shape);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void InvalidColliderShapesKeepValidationAndExceptionOrdering(int value)
    {
        TileColliderShape2D shape = (TileColliderShape2D)value;
        Assert.Equal("shape", Assert.Throws<ArgumentOutOfRangeException>(() => new TileColliderDescriptor2D(shape)).ParamName);
        Assert.Equal("shape", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TileColliderDescriptor2D(shape, Matrix3x2.Identity)).ParamName);
        Assert.Equal("localTransform", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TileColliderDescriptor2D(shape, default(Matrix3x2))).ParamName);
    }

    private static string ReadSource(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
