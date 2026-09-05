using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Scene2DImporters;

/// <summary>
/// Executable Stage 0 contracts. Reflection allows the missing API to be the RED
/// assertion rather than a compilation failure. Import cases move to the optional
/// importer test project when that project is introduced in Stage 2.
/// </summary>
public sealed class ImportStageZeroContractTests
{
    [Theory]
    [InlineData("Tiled", "tiled-finite.tmj")]
    [InlineData("Tiled", "tiled-external.tmj")]
    [InlineData("Tiled", "tiled-raw.tmj")]
    [InlineData("Tiled", "tiled-zlib.tmj")]
    [InlineData("Tiled", "tiled-gzip.tmj")]
    [Trait("SceneImportStage", "0")]
    public void ImportProducesIndependentGoldenIncludingAtlasFlipOffsetsAndLayerOrder(string format, string file)
    {
        object result = Import(format, file);
        Assert.True(Value<bool>(result, "Success"), DiagnosticsText(result));
        object document = RequiredValue(result, "Document");
        TileMap2DModel model = Assert.IsType<TileMap2DModel>(RequiredValue(Assert.Single(Items(document, "Levels")), "TileMap"));
        using JsonDocument golden = JsonDocument.Parse(File.ReadAllText(Fixture("common.golden.json")));
        JsonElement expected = golden.RootElement;
        Assert.Equal(new DrawSize(16, 16), model.TileSize);
        Assert.Equal(new TileMapBounds2D(0, 0, 2, 2), model.Bounds);
        Assert.Equal(2, model.Layers.Count);
        Assert.Equal(2, Assert.Single(model.TileSets).Tiles.Count);
        JsonElement.ArrayEnumerator layers = expected.GetProperty("layers").EnumerateArray();
        foreach (JsonElement expectedLayer in layers)
        {
            int order = expectedLayer.GetProperty("order").GetInt32();
            TileLayer2DModel layer = model.Layers[order];
            Assert.Equal(order, layer.Order);
            Assert.Equal(expectedLayer.GetProperty("name").GetString(), Assert.IsType<string>(layer.Properties["$SourceName"]));
            Assert.Equal(new DrawPoint(2, 3), layer.Offset);
            Assert.Equal(expectedLayer.GetProperty("opacity").GetSingle(), layer.Opacity);
            int expectedNonEmpty = expectedLayer.GetProperty("cells").GetArrayLength();
            Assert.Equal(expectedNonEmpty, layer.Chunks.Sum(static chunk => chunk.Tiles.Count(static cell => cell.TileId != 0)));
            foreach (JsonElement expectedCell in expectedLayer.GetProperty("cells").EnumerateArray())
            {
                JsonElement coordinate = expectedCell.GetProperty("coordinate");
                Assert.True(layer.TryGetCell(new TileCoordinate2D(coordinate[0].GetInt32(), coordinate[1].GetInt32()), out TileCell2D cell));
                Assert.Equal(expectedCell.GetProperty("flip").GetInt32(), (int)cell.Flip);
                Assert.True(model.TryResolveTile(cell.TileId, out _, out TileDefinition2D? definition));
                JsonElement rect = expectedCell.GetProperty("sourceRect");
                Assert.Equal(new DrawRect(rect[0].GetSingle(), rect[1].GetSingle(), rect[2].GetSingle(), rect[3].GetSingle()), definition!.SourceRect);
            }
        }
        object asset = Assert.Single(Items(document, "Assets"));
        Assert.Equal("atlas.svg", Value<string>(asset, "Path"));
        Assert.Equal(new DrawSize(32, 16), Value<DrawSize>(asset, "Size"));
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void InfiniteMapKeepsNegativeAndRemoteChunksWithoutFillingTheGap()
    {
        object result = Import("Tiled", "tiled-infinite.tmj");
        Assert.True(Value<bool>(result, "Success"), DiagnosticsText(result));
        TileMap2DModel model = ImportedMap(result);
        Assert.Null(model.Bounds);
        TileLayer2DModel layer = Assert.Single(model.Layers);
        Assert.Equal(2, layer.Chunks.Count);
        Assert.Equal([new TileCoordinate2D(-2, -1), new TileCoordinate2D(14, -1)], layer.Chunks.Select(static chunk => chunk.Origin));
        Assert.Equal(8, layer.Chunks.Sum(static chunk => chunk.Tiles.Count));
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void AllEightTiledFlipCombinationsSurviveAsCoreData()
    {
        object result = Import("Tiled", "tiled-flips.tmj");
        Assert.True(Value<bool>(result, "Success"), DiagnosticsText(result));
        TileCell2D[] cells = Assert.Single(ImportedMap(result).Layers).Chunks.SelectMany(static chunk => chunk.Tiles).ToArray();
        Assert.Equal(Enumerable.Range(0, 8), cells.Select(static cell => (int)cell.Flip));
        Assert.All(cells, static cell => Assert.Equal(1, cell.TileId));
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void GroupOffsetsOpacityAndTintComposeWithoutChangingLayerIdentity()
    {
        object result = Import("Tiled", "tiled-group.tmj");
        Assert.True(Value<bool>(result, "Success"), DiagnosticsText(result));
        TileMap2DModel model = ImportedMap(result);
        Assert.Equal(["1", "2"], model.Layers.Select(static layer => layer.Id));
        Assert.All(model.Layers, static layer => Assert.Equal(new DrawPoint(12, -1), layer.Offset));
        Assert.Equal(0.5f, model.Layers[0].Opacity);
        Assert.Equal(0.25f, model.Layers[1].Opacity);
        Assert.Equal((byte)128, model.Layers[0].Tint.A);
    }

    [Theory]
    [MemberData(nameof(TiledDiagnosticCases))]
    [Trait("SceneImportStage", "0")]
    public void InvalidAndUnsupportedDataHasStableLocatedDiagnosticsAndNoPartialPublication(
        string file, string format, string code, string category)
    {
        object result = Import(format, file);
        object diagnostic = Assert.Single(Items(result, "Diagnostics").Where(item => Value<string>(item, "Code") == code));
        Assert.Equal(category, RequiredValue(diagnostic, "Severity").ToString());
        Assert.False(string.IsNullOrWhiteSpace(Value<string>(diagnostic, "Message")));
        Assert.False(string.IsNullOrWhiteSpace(Value<string>(diagnostic, "FilePath")));
        Assert.StartsWith("$", Value<string>(diagnostic, "JsonPath"));
        Assert.Equal(category == "Warning", Value<bool>(result, "Success"));
        if (category != "Warning") { Assert.Null(Property(result, "Document")); }
    }

    [Theory]
    [InlineData("tiled-objects.tmj", 1, 0)]
    [InlineData("tiled-empty-promotion.tmj", 1, 1)]
    [Trait("SceneImportStage", "0")]
    public void SparsePromotionKeepsStableAddressAndTemplatePropertiesWithoutCreatingUiNodes(string file, int x, int y)
    {
        object result = Import("Tiled", file);
        Assert.True(Value<bool>(result, "Success"), DiagnosticsText(result));
        object document = RequiredValue(result, "Document");
        object level = Assert.Single(Items(document, "Levels"));
        object promotion = Assert.Single(Items(level, "Promotions"));
        Assert.Equal(new TileCellKey2D("1", x, y), Value<TileCellKey2D>(promotion, "Cell"));
        IReadOnlyDictionary<string, object?> properties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(RequiredValue(promotion, "Properties"));
        Assert.Equal("Closed", properties["InitialState"]);
        Assert.False(promotion is UIElement);
        Assert.False(document is UIElement);
        TileMap2DModel model = Assert.IsType<TileMap2DModel>(RequiredValue(level, "TileMap"));
        TileMap2D map = new() { Model = model };
        Assert.Empty(map.Layers.SelectMany(layer => layer.PromotedTiles));
        int? tileId = Property(promotion, "TileId") is int overrideId ? overrideId : null;
        TileInstance2D node = map.Promote(Value<TileCellKey2D>(promotion, "Cell"), tileId);
        Assert.Same(node, map.Promote(Value<TileCellKey2D>(promotion, "Cell")));
        Assert.Single(map.Layers.SelectMany(layer => layer.PromotedTiles));
        Assert.True(map.Demote(Value<TileCellKey2D>(promotion, "Cell")));
    }

    [Theory]
    [InlineData("ldtk-inline.ldtk")]
    [InlineData("ldtk-separate.ldtk")]
    [Trait("SceneImportStage", "3")]
    public void LdtkImportProducesIndependentGolden(string file) =>
        ImportProducesIndependentGoldenIncludingAtlasFlipOffsetsAndLayerOrder("LDtk", file);

    [Theory]
    [MemberData(nameof(LdtkDiagnosticCases))]
    [Trait("SceneImportStage", "3")]
    public void LdtkDiagnosticsRemainLocatedAndAtomic(string file, string format, string code, string category) =>
        InvalidAndUnsupportedDataHasStableLocatedDiagnosticsAndNoPartialPublication(file, format, code, category);

    public static IEnumerable<object[]> TiledDiagnosticCases() => DiagnosticCases().Where(item => (string)item[1] == "Tiled");
    public static IEnumerable<object[]> LdtkDiagnosticCases() => DiagnosticCases().Where(item => (string)item[1] == "LDtk");

    public static IEnumerable<object[]> DiagnosticCases()
    {
        using JsonDocument cases = JsonDocument.Parse(File.ReadAllText(Fixture("diagnostic-cases.json")));
        return cases.RootElement.EnumerateArray().Select(static item => new object[]
        {
            item.GetProperty("file").GetString()!, item.GetProperty("format").GetString()!,
            item.GetProperty("code").GetString()!, item.GetProperty("category").GetString()!
        }).ToArray();
    }

    private static object Import(string format, string file)
    {
        Assembly? importerAssembly = null;
        try { importerAssembly = Assembly.Load("Cerneala.Scene2D.Importers"); }
        catch (FileNotFoundException) { }
        Assert.True(importerAssembly is not null, "RED: optional Cerneala.Scene2D.Importers parser assembly is absent.");
        Assert.All(importerAssembly.GetExportedTypes(), static type => Assert.False(typeof(UIElement).IsAssignableFrom(type)));
        Assert.DoesNotContain(importerAssembly.GetReferencedAssemblies(), static assembly =>
            assembly.Name!.StartsWith("Cerneala.Backends.", StringComparison.Ordinal) ||
            assembly.Name.StartsWith("Cerneala.Platforms.", StringComparison.Ordinal));
        Type? optionsType = importerAssembly.GetType("Cerneala.Scene2D.Importers.Scene2DImportOptions");
        Assert.NotNull(optionsType);
        object options = Activator.CreateInstance(optionsType)!;
        Set(options, "AssetRootDirectory", Fixture(string.Empty));
        Type? importerType = importerAssembly.GetType($"Cerneala.Scene2D.Importers.{(format == "Tiled" ? "Tiled" : "Ldtk")}Scene2DImporter");
        Assert.NotNull(importerType);
        return InvokeStatic(importerType, "Import", Fixture(file), options);
    }

    private static TileMap2DModel ImportedMap(object result) => Assert.IsType<TileMap2DModel>(
        RequiredValue(Assert.Single(Items(RequiredValue(result, "Document"), "Levels")), "TileMap"));

    private static object InvokeStatic(Type type, string name, params object[] arguments)
    {
        MethodInfo? method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == name && candidate.GetParameters().Length >= arguments.Length)
            .FirstOrDefault(candidate => arguments.Select((argument, index) => candidate.GetParameters()[index].ParameterType.IsInstanceOfType(argument)).All(static valid => valid));
        Assert.NotNull(method);
        object?[] invocation = method.GetParameters().Select(static parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
        Array.Copy(arguments, invocation, arguments.Length);
        try { return method.Invoke(null, invocation)!; }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
    }

    private static object? Property(object value, string name)
    {
        PropertyInfo? property = value.GetType().GetProperty(name);
        Assert.NotNull(property);
        return property.GetValue(value);
    }

    private static object RequiredValue(object value, string name)
    {
        object? result = Property(value, name);
        Assert.NotNull(result);
        return result;
    }

    private static T Value<T>(object value, string name) => Assert.IsType<T>(RequiredValue(value, name));
    private static object[] Items(object value, string name) => Assert.IsAssignableFrom<IEnumerable>(RequiredValue(value, name)).Cast<object>().ToArray();
    private static string DiagnosticsText(object result) => string.Join("; ", Items(result, "Diagnostics").Select(static item => Value<string>(item, "Message")));

    private static void Set(object target, string propertyName, object value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        property.SetValue(target, value);
    }

    private static string Fixture(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "tests", "Fixtures", "Scene2DImport", name.Replace('/', Path.DirectorySeparatorChar));
    }
}
