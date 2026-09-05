using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Nodes;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class TiledHostileInputTests : IDisposable
{
    private readonly string root;
    private readonly List<string> files = new();

    public TiledHostileInputTests()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        Assert.NotNull(directory);
        RepositoryRoot = directory.FullName;
        root = Path.Combine(RepositoryRoot, ".artifacts", "scene-import-hostile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Write("atlas.svg", File.ReadAllText(Path.Combine(RepositoryRoot, "tests", "Fixtures", "Scene2DImport", "atlas.svg")));
    }

    private string RepositoryRoot { get; }
    private JsonObject Map() => JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot,
        "tests", "Fixtures", "Scene2DImport", "tiled-finite.tmj")))!.AsObject();

    [Theory]
    [InlineData("dimensions", "SCN2D005")]
    [InlineData("fractional-gid", "SCN2D006")]
    [InlineData("long-gid", "SCN2D006")]
    [InlineData("duplicate-layer", "SCN2D015")]
    [InlineData("duplicate-tile", "SCN2D015")]
    [InlineData("nested-unknown", "SCN2D004")]
    [InlineData("parallax", "SCN2D004")]
    [InlineData("renderorder", "SCN2D004")]
    [InlineData("tileset-offset", "SCN2D004")]
    [InlineData("animation", "SCN2D004")]
    [InlineData("reserved-property", "SCN2D015")]
    [InlineData("malformed-property", "SCN2D016")]
    [InlineData("rooted-asset", "SCN2D010")]
    [InlineData("network-asset", "SCN2D010")]
    [InlineData("stream-asset", "SCN2D010")]
    [InlineData("missing-asset", "SCN2D001")]
    [Trait("SceneImportStage", "2")]
    public void HostileOrUnsupportedDataCannotPublishAPartialDocument(string mutation, string code)
    {
        JsonObject map = Map();
        JsonNode layer = map["layers"]![0]!;
        JsonNode set = map["tilesets"]![0]!;
        switch (mutation)
        {
            case "dimensions": layer["width"] = int.MaxValue; layer["height"] = int.MaxValue; break;
            case "fractional-gid": layer["data"]![0] = 1.5; break;
            case "long-gid": layer["data"]![0] = 4_294_967_296L; break;
            case "duplicate-layer": map["layers"]!.AsArray().Add(layer.DeepClone()); break;
            case "duplicate-tile": set["tiles"]!.AsArray().Add(set["tiles"]![0]!.DeepClone()); break;
            case "nested-unknown": set["tiles"]![0]!["FutureGameplay"] = true; break;
            case "parallax": layer["parallaxx"] = 0.5; break;
            case "renderorder": map["renderorder"] = "left-up"; break;
            case "tileset-offset": set["tileoffset"] = new JsonObject { ["x"] = 1, ["y"] = 0 }; break;
            case "animation": set["tiles"]![0]!["animation"] = new JsonArray(); break;
            case "reserved-property": map["properties"] = new JsonArray(new JsonObject { ["name"] = "$SourceName", ["type"] = "string", ["value"] = "spoofed" }); break;
            case "malformed-property": map["properties"] = new JsonArray(new JsonObject { ["name"] = "Value", ["type"] = "bool", ["value"] = "true" }); break;
            case "rooted-asset": set["image"] = Path.Combine(root, "atlas.svg"); break;
            case "network-asset": set["image"] = "//host/share/atlas.svg"; break;
            case "stream-asset": set["image"] = "atlas.svg:payload"; break;
            case "missing-asset": set["image"] = "missing.svg"; break;
        }
        AssertFailure(Import(Write("map.tmj", map.ToJsonString())), code);
    }

    [Theory]
    [InlineData("raw")]
    [InlineData("zlib")]
    [InlineData("gzip")]
    [Trait("SceneImportStage", "2")]
    public void DecodingRejectsMissingTrailingAndExplosivePayloads(string compression)
    {
        foreach (int bytes in new[] { 12, 20, 1_048_576 })
        {
            JsonObject map = Map();
            JsonNode layer = map["layers"]![0]!;
            byte[] raw = new byte[bytes];
            using MemoryStream encoded = new();
            if (compression == "raw") { encoded.Write(raw); }
            else
            {
                using Stream compressor = compression == "gzip"
                    ? new GZipStream(encoded, CompressionLevel.SmallestSize, leaveOpen: true)
                    : new ZLibStream(encoded, CompressionLevel.SmallestSize, leaveOpen: true);
                compressor.Write(raw);
            }
            layer["encoding"] = "base64";
            if (compression != "raw") { layer["compression"] = compression; }
            layer["data"] = Convert.ToBase64String(encoded.ToArray());
            AssertFailure(Import(Write("map.tmj", map.ToJsonString())), "SCN2D005");
        }
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void TruncatedJsonAndDuplicateMembersProduceControlledLocatedDiagnostics()
    {
        string original = Map().ToJsonString();
        Random random = new(0x712ED);
        for (int index = 0; index < 128; index++)
        {
            int length = random.Next(1, original.Length);
            AssertFailure(Import(Write("map.tmj", original[..length])), "SCN2D002");
        }
        AssertFailure(Import(Write("map.tmj", "{\"version\":\"1.11\"," + original[1..])), "SCN2D015");
    }

    [Theory]
    [InlineData("MaxFileBytes", 32)]
    [InlineData("MaxTotalBytes", 32)]
    [InlineData("MaxJsonDepth", 2)]
    [InlineData("MaxCells", 3)]
    [InlineData("MaxLayers", 1)]
    [Trait("SceneImportStage", "2")]
    public void ExplicitBudgetsFailBeforePublishing(string option, int value)
    {
        AssertFailure(Import(Write("map.tmj", Map().ToJsonString()), option, value), "SCN2D013");
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void DiagnosticRetentionCannotHideAnErrorBehindEditorWarnings()
    {
        JsonObject map = Map();
        map["layers"]![0]!["FutureGameplay"] = true;
        object result = Import(Write("map.tmj", map.ToJsonString()), "MaxDiagnostics", 1);
        Assert.False(Property<bool>(result, "Success"));
        Assert.Null(result.GetType().GetProperty("Document")!.GetValue(result));
        Assert.Single(Property<IReadOnlyList<Scene2DDiagnostic>>(result, "Diagnostics"));
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void NormalizationInsideRootAndHistoricalHexBitPreserveTheMap()
    {
        JsonObject map = Map();
        map["tilesets"]![0]!["image"] = "./unused/../atlas.svg";
        map["layers"]![0]!["data"]![0] = 0x10000001;
        object result = Import(Write("map.tmj", map.ToJsonString()));
        Assert.True(Property<bool>(result, "Success"));
        Scene2DDocument document = Property<Scene2DDocument>(result, "Document");
        Assert.Equal("atlas.svg", Assert.Single(document.Assets).Path);
        TileCell2D cell = document.Levels[0].TileMap.Layers[0].Chunks[0].Tiles[0];
        Assert.Equal(1, cell.TileId);
        Assert.Equal(TileFlip2D.None, cell.Flip);
    }

    private object Import(string file, string? optionName = null, int value = 0)
    {
        Assembly assembly = Assembly.Load("Cerneala.Scene2D.Importers");
        Type? optionsType = assembly.GetType("Cerneala.Scene2D.Importers.Scene2DImportOptions");
        Assert.True(optionsType is not null, "RED: importer options are not implemented.");
        object options = Activator.CreateInstance(optionsType)!;
        optionsType.GetProperty("AssetRootDirectory")!.SetValue(options, root);
        if (optionName is not null)
        {
            PropertyInfo? property = optionsType.GetProperty(optionName);
            Assert.NotNull(property);
            property.SetValue(options, Convert.ChangeType(value, property.PropertyType));
        }
        Type? importer = assembly.GetType("Cerneala.Scene2D.Importers.TiledScene2DImporter");
        Assert.True(importer is not null, "RED: Tiled parser is not implemented.");
        try { return importer.GetMethod("Import")!.Invoke(null, [file, options])!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }

    private static void AssertFailure(object result, string code)
    {
        Assert.False(Property<bool>(result, "Success"));
        Assert.Null(result.GetType().GetProperty("Document")!.GetValue(result));
        Assert.Contains(Property<IReadOnlyList<Scene2DDiagnostic>>(result, "Diagnostics"), diagnostic =>
            diagnostic.Code == code && diagnostic.FilePath.Length > 0 && diagnostic.JsonPath.StartsWith('$'));
    }

    private static T Property<T>(object target, string name) => Assert.IsAssignableFrom<T>(target.GetType().GetProperty(name)!.GetValue(target));

    private string Write(string name, string content)
    {
        string path = Path.Combine(root, name);
        if (!files.Contains(path)) { files.Add(path); }
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        foreach (string file in files) { File.Delete(file); }
        Directory.Delete(root, recursive: false);
    }
}
