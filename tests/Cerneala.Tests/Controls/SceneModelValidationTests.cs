using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneModelValidationTests
{
    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ChunkRejectsExcessCellsWithoutEnumeratingTheTail()
    {
        int visited = 0;
        IEnumerable<TileCell2D> Cells()
        {
            for (int index = 0; index < 2; index++) { visited++; yield return default; }
            throw new InvalidOperationException("The unbounded tail must not be visited.");
        }

        ArgumentException error = Assert.Throws<ArgumentException>(() => new TileChunk2D(default, 1, 1, Cells()));
        Assert.Equal(2, visited);
        AssertDiagnostic(error, "SCN2D005");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void HugeChunkRejectsDimensionsBeforeEnumerating()
    {
        IEnumerable<TileCell2D> Cells()
        {
            throw new InvalidOperationException("Dimensions must be checked before enumeration.");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
        Exception? error = Record.Exception(() => new TileChunk2D(default, int.MaxValue, int.MaxValue, Cells()));
        Assert.IsAssignableFrom<ArgumentException>(error);
        AssertDiagnostic(error!, "SCN2D013");
    }

    [Theory]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MaxValue)]
    [Trait("SceneImportStage", "1")]
    public void OverflowingBoundsAreRejectedAtConstruction(int x, int y)
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new TileMapBounds2D(x, y, 1, 1));
        AssertDiagnostic(error, "SCN2D005");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DefaultBoundsCannotEscapeTheMapConstructor()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new TileMap2DModel(new DrawSize(16, 16), [], [], default(TileMapBounds2D)));
        AssertDiagnostic(error, "SCN2D005");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ConvexityRejectsSelfIntersectingStarNotOnlyLocalTurnSigns()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new TileColliderDescriptor2D(
            TileColliderShape2D.Polygon, points: "0,-10 6,8 -10,-3 10,-3 -6,8"));
        AssertDiagnostic(error, "SCN2D008");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ExtremePolygonFailsControlledInsteadOfOverflowingCrossProducts()
    {
        Exception? error = Record.Exception(() => new TileColliderDescriptor2D(
            TileColliderShape2D.Polygon, points: "3e38,3e38 -3e38,-3e38 3e38,-3e38"));
        Assert.IsAssignableFrom<ArgumentException>(error);
        AssertDiagnostic(error!, "SCN2D008");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ExistingConstructorsExposeTheSameStableDiagnosticCategories()
    {
        AssertDiagnostic(Assert.Throws<ArgumentOutOfRangeException>(() => new TileCell2D(-1)), "SCN2D006");
        AssertDiagnostic(Assert.Throws<ArgumentOutOfRangeException>(() => new TileColliderDescriptor2D(TileColliderShape2D.Circle, radius: 0)), "SCN2D008");
        AssertDiagnostic(Assert.Throws<ArgumentOutOfRangeException>(() => new TileLayer2DModel("Layer", [], opacity: float.NaN)), "SCN2D014");
        AssertDiagnostic(Assert.Throws<ArgumentOutOfRangeException>(() => new TileMap2DModel(new DrawSize(1, 1), [], [], version: 0)), "SCN2D003");
        AssertDiagnostic(Assert.Throws<ArgumentException>(() => new TileSet2D("Atlas", default, [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1))])), "SCN2D010");
        TileChunk2D chunk = new(default, 1, 1, [new TileCell2D(99)]);
        AssertDiagnostic(Assert.Throws<ArgumentException>(() => new TileMap2DModel(new DrawSize(1, 1), [], [new TileLayer2DModel("Layer", [chunk])])), "SCN2D006");
        AssertDiagnostic(Assert.Throws<ArgumentException>(() => new TileLayer2DModel("Layer", [chunk, chunk])), "SCN2D011");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DiagnosticRetentionIsBoundedAndDeterministic()
    {
        Type validator = CoreType("Scene2DModelValidator");
        object options = Activator.CreateInstance(CoreType("Scene2DValidationOptions"))!;
        options.GetType().GetProperty("MaxDiagnostics")!.SetValue(options, 2);
        TileDefinition2D[] definitions = Enumerable.Range(1, 8).Select(id => new TileDefinition2D(id, new DrawRect(id, 0, 2, 2))).ToArray();
        TileSet2D set = new("Atlas", new ResourceId<ImageResource>("Atlas"), definitions);
        TileMap2DModel model = new(new DrawSize(1, 1), [set], []);
        Dictionary<string, DrawSize> sizes = new() { ["Atlas"] = new DrawSize(1, 1) };
        MethodInfo method = validator.GetMethods().Single(candidate => candidate.Name == "Validate" && candidate.GetParameters()[0].ParameterType == typeof(TileMap2DModel));
        object first = method.Invoke(null, [model, sizes, options])!;
        object second = method.Invoke(null, [model, sizes, options])!;
        Assert.Equal(false, first.GetType().GetProperty("Success")!.GetValue(first));
        object[] Items(object result) => ((System.Collections.IEnumerable)result.GetType().GetProperty("Diagnostics")!.GetValue(result)!).Cast<object>().ToArray();
        Assert.Equal(2, Items(first).Length);
        Assert.Equal(Items(first), Items(second));
        Assert.Equal(true, first.GetType().GetProperty("DiagnosticsTruncated")!.GetValue(first));
    }

    private static void AssertDiagnostic(Exception error, string code)
    {
        MethodInfo? method = CoreType("Scene2DModelValidator").GetMethod("GetDiagnostic");
        Assert.NotNull(method);
        object? diagnostic = method.Invoke(null, [error, "programmatic", "$.test"]);
        Assert.NotNull(diagnostic);
        Assert.Equal(code, diagnostic.GetType().GetProperty("Code")!.GetValue(diagnostic));
        Assert.Equal("programmatic", diagnostic.GetType().GetProperty("FilePath")!.GetValue(diagnostic));
        Assert.Equal("$.test", diagnostic.GetType().GetProperty("JsonPath")!.GetValue(diagnostic));
    }

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("4294967295", uint.MaxValue)]
    [InlineData("0x80000000", 2147483648u)]
    [Trait("SceneImportStage", "1")]
    public void CollisionBitsetsUseTheWholeUnsignedRange(string text, uint expected)
    {
        MethodInfo? parser = CoreType("Scene2DModelValidator").GetMethod("ParseCollisionBits");
        Assert.NotNull(parser);
        Assert.Equal(expected, parser.Invoke(null, [text]));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("4294967296")]
    [InlineData("0x100000000")]
    [InlineData("1.5")]
    [InlineData("")]
    [Trait("SceneImportStage", "1")]
    public void InvalidBitsetsHaveStableDiagnostics(string text)
    {
        MethodInfo? parser = CoreType("Scene2DModelValidator").GetMethod("ParseCollisionBits");
        Assert.NotNull(parser);
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => parser.Invoke(null, [text]));
        AssertDiagnostic(exception.InnerException!, "SCN2D009");
    }

    [Theory]
    [InlineData(0, "SCN2D012")]
    [InlineData(1, "SCN2D012")]
    [InlineData(2, "SCN2D012")]
    [Trait("SceneImportStage", "1")]
    public void ProgrammaticPromotionReferencesRejectEmptyMissingAndDuplicateCells(int scenario, string code)
    {
        object promotion = Construct("TilePromotion2D", ("cell", new TileCellKey2D("Layer", scenario == 1 ? 5 : 0, 0)),
            ("tileId", scenario == 0 ? null : 1));
        Array promotions = Array.CreateInstance(CoreType("TilePromotion2D"), scenario == 2 ? 2 : 1);
        for (int index = 0; index < promotions.Length; index++) { promotions.SetValue(promotion, index); }
        TileMap2DModel map = new(new DrawSize(1, 1),
            [new TileSet2D("Atlas", new ResourceId<ImageResource>("Atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1))])],
            [new TileLayer2DModel("Layer", [new TileChunk2D(default, 1, 1, [default])])]);
        ArgumentException error = Assert.Throws<ArgumentException>(() => Construct("Scene2DLevel", ("id", "Level"), ("tileMap", map), ("promotions", promotions)));
        AssertDiagnostic(error, code);
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DocumentDoesNotPublishWithUnresolvedAtlas()
    {
        TileMap2DModel map = new(new DrawSize(1, 1),
            [new TileSet2D("Atlas", new ResourceId<ImageResource>("Atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1))])], []);
        object level = Construct("Scene2DLevel", ("id", "Level"), ("tileMap", map));
        Array levels = Array.CreateInstance(CoreType("Scene2DLevel"), 1);
        levels.SetValue(level, 0);
        Array assets = Array.CreateInstance(CoreType("Scene2DAsset"), 0);
        ArgumentException error = Assert.Throws<ArgumentException>(() => Construct("Scene2DDocument", ("levels", levels), ("assets", assets)));
        AssertDiagnostic(error, "SCN2D010");
    }

    private static object Construct(string name, params (string Name, object? Value)[] supplied)
    {
        ConstructorInfo constructor = Assert.Single(CoreType(name).GetConstructors());
        object?[] arguments = constructor.GetParameters().Select(parameter =>
            supplied.Any(item => item.Name == parameter.Name)
                ? supplied.Single(item => item.Name == parameter.Name).Value : parameter.DefaultValue).ToArray();
        try { return constructor.Invoke(arguments); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ModelRejectsSceneCoordinateOverflowBeforePresentationCanPublish()
    {
        TileLayer2DModel layer = new("Layer", [new TileChunk2D(new TileCoordinate2D(3000, 0), 1, 1, [default])]);
        Exception? error = Record.Exception(() => new TileMap2DModel(new DrawSize(1_000_000, 1), [], [layer]));
        Assert.IsAssignableFrom<ArgumentException>(error);
        AssertDiagnostic(error!, "SCN2D014");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void RepeatedTileCollidersCannotExpandIntoAnUnboundedSceneIndex()
    {
        TileColliderDescriptor2D descriptor = new(TileColliderShape2D.Circle);
        TileDefinition2D tile = new(1, new DrawRect(0, 0, 1, 1), colliders: Enumerable.Repeat(descriptor, 4096));
        TileSet2D set = new("Atlas", new ResourceId<ImageResource>("Atlas"), [tile]);
        TileLayer2DModel layer = new("Layer", [new TileChunk2D(default, 17, 1, Enumerable.Repeat(new TileCell2D(1), 17))]);
        Exception? error = Record.Exception(() => new TileMap2DModel(new DrawSize(1, 1), [set], [layer]));
        Assert.IsAssignableFrom<ArgumentException>(error);
        AssertDiagnostic(error!, "SCN2D013");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("SceneImportStage", "1")]
    public void ColliderDescriptorRejectsOutOfRangeGeometryBeforeAnAdapterIsCreated(bool affine)
    {
        Exception? error = Record.Exception(() => affine
            ? new TileColliderDescriptor2D(TileColliderShape2D.Circle, System.Numerics.Matrix3x2.CreateTranslation(3_000_000_000f, 0))
            : new TileColliderDescriptor2D(TileColliderShape2D.Box, width: 3_000_000_000f));
        Assert.IsAssignableFrom<ArgumentException>(error);
        AssertDiagnostic(error!, "SCN2D008");
    }

    private static Type CoreType(string name)
    {
        Type? type = typeof(Scene2D).Assembly.GetType("Cerneala.UI.Controls." + name);
        Assert.True(type is not null, "RED: missing core contract " + name);
        return type;
    }
}
