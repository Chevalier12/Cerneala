using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class TiledContractCoverageTests : IDisposable
{
    private readonly string root;
    private readonly string fixtures;
    private readonly List<string> files = new();
    private readonly List<string> directories = new();

    public TiledContractCoverageTests()
    {
        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Cerneala.slnx"))) { repository = repository.Parent; }
        Assert.NotNull(repository);
        fixtures = Path.Combine(repository.FullName, "tests", "Fixtures", "Scene2DImport");
        root = Path.Combine(repository.FullName, ".artifacts", "scene-import-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Write("atlas.svg", File.ReadAllText(Path.Combine(fixtures, "atlas.svg")));
    }

    private JsonObject Map(string file = "tiled-finite.tmj") => JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures, file)))!.AsObject();
    private Scene2DImportResult Import(JsonObject map, Scene2DImportOptions? options = null) =>
        TiledScene2DImporter.Import(Write("map.tmj", map.ToJsonString()), options ?? new() { AssetRootDirectory = root });

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void ObjectsPreserveExactGeometryTransformsRolesAndBitsets()
    {
        Scene2DImportResult result = Import(Map("tiled-objects.tmj"));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        Scene2DLevel level = Assert.Single(result.Document!.Levels);
        Assert.Equal(["Spawn", "Collider", "Collider", "Promote"], level.Entities.Select(entity => entity.Role));
        Scene2DEntity ellipse = level.Entities[1];
        Assert.Equal(new DrawPoint(4, 4), ellipse.Position);
        Assert.Equal(new DrawSize(16, 8), ellipse.Size);
        Assert.Equal(MathF.PI / 6, ellipse.Rotation, 6);
        TileColliderDescriptor2D oval = Assert.Single(ellipse.Colliders);
        Assert.Equal(TileColliderShape2D.Circle, oval.Shape);
        Assert.Equal(new Matrix3x2(8, 0, 0, 4, 8, 4), oval.LocalTransform);
        Assert.Equal(2u, oval.CollisionLayer);
        Assert.Equal(1u, oval.CollisionMask);
        Assert.True(oval.IsTrigger);
        Scene2DEntity fence = level.Entities[2];
        Assert.Equal("0,0 16,0 16,-8", fence.Points);
        Assert.Equal(2, fence.Colliders.Count);
        Assert.All(fence.Colliders, collider => Assert.Equal(TileColliderShape2D.Segment, collider.Shape));
        Assert.Equal([new Vector2(16, 0), new Vector2(16, -8)], fence.Colliders[1].Vertices);
        Assert.Single(level.Promotions);
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void TileCollisionObjectsComposeTheirLocalRotationAndGroupOffsetExactlyOnce()
    {
        JsonObject map = Map("tiled-objects.tmj");
        JsonNode ellipse = map["layers"]![2]!["objects"]![1]!.DeepClone();
        JsonObject group = new()
        {
            ["type"] = "objectgroup", ["offsetx"] = 3, ["offsety"] = -2,
            ["objects"] = new JsonArray(ellipse)
        };
        map["tilesets"]![0]!["tiles"]![0]!["objectgroup"] = group;
        Scene2DImportResult result = Import(map);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        TileColliderDescriptor2D descriptor = Assert.Single(result.Document!.Levels[0].TileMap.TileSets[0].Tiles[1].Colliders);
        Matrix3x2 expected = new Matrix3x2(8, 0, 0, 4, 8, 4) * Matrix3x2.CreateRotation(MathF.PI / 6) * Matrix3x2.CreateTranslation(7, 2);
        Assert.Equal(expected.M31, descriptor.LocalTransform.M31, 5);
        Assert.Equal(expected.M32, descriptor.LocalTransform.M32, 5);
        Assert.Equal(expected.M11, descriptor.LocalTransform.M11, 5);
        Assert.Equal(expected.M12, descriptor.LocalTransform.M12, 5);
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void TopDownOrderingIsStableAndGroupVisibilityDoesNotDestroySourceMetadata()
    {
        JsonObject map = Map("tiled-objects.tmj");
        JsonNode objectLayer = map["layers"]![2]!.DeepClone();
        objectLayer["draworder"] = "topdown";
        objectLayer["objects"]![0]!["y"] = 4;
        JsonObject group = new()
        {
            ["id"] = 10, ["name"] = "Hidden", ["type"] = "group", ["visible"] = false,
            ["offsetx"] = 10, ["offsety"] = 20, ["layers"] = new JsonArray(objectLayer),
            ["properties"] = new JsonArray(new JsonObject { ["name"] = "State", ["type"] = "string", ["value"] = "Kept" })
        };
        map["layers"]![2] = group;
        Scene2DImportResult result = Import(map);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        Scene2DLevel level = result.Document!.Levels[0];
        Assert.Equal(["4", "1", "2", "3"], level.Entities.Select(entity => entity.Id));
        Assert.Equal([0, 1, 2, 3], level.Entities.Select(entity => entity.Order));
        TileLayer2DModel layer = level.TileMap.Layers[2];
        Assert.False(layer.IsVisible);
        Assert.Equal(new DrawPoint(10, 20), layer.Offset);
        object ancestor = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<object>>(layer.Properties["$GroupAncestors"]));
        Assert.Equal("Kept", Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(ancestor)["State"]);
    }

    [Theory]
    [InlineData("zlib")]
    [InlineData("gzip")]
    [Trait("SceneImportStage", "2")]
    public void TruncatedCompressionContainerDoesNotBecomeAValidMap(string compression)
    {
        JsonObject map = Map();
        byte[] raw = new byte[16];
        using MemoryStream encoded = new();
        using (Stream compressor = compression == "gzip" ? new GZipStream(encoded, CompressionLevel.SmallestSize, true)
            : new ZLibStream(encoded, CompressionLevel.SmallestSize, true)) { compressor.Write(raw); }
        byte[] complete = encoded.ToArray();
        for (int missing = 1; missing <= 4; missing++)
        {
            map["layers"]![0]!["encoding"] = "base64";
            map["layers"]![0]!["compression"] = compression;
            map["layers"]![0]!["data"] = Convert.ToBase64String(complete[..^missing]);
            Failure(Import(map), "SCN2D002");
        }
    }

    [Theory]
    [InlineData("group-data")]
    [InlineData("tile-objects")]
    [InlineData("object-data")]
    [InlineData("tile-nested-layers")]
    [Trait("SceneImportStage", "2")]
    public void FieldsForADifferentLayerKindAreNotSilentlyLost(string mutation)
    {
        JsonObject map = Map();
        JsonNode layer = map["layers"]![0]!;
        switch (mutation)
        {
            case "group-data": layer["type"] = "group"; layer["layers"] = new JsonArray(); break;
            case "tile-objects": layer["objects"] = new JsonArray(); break;
            case "object-data": layer["type"] = "objectgroup"; layer["objects"] = new JsonArray(); break;
            case "tile-nested-layers": layer["layers"] = new JsonArray(); break;
        }
        Failure(Import(map), "SCN2D004");
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void EveryFrozenUnsupportedTiledFieldAndEveryUnknownScopeFieldIsRejected()
    {
        using JsonDocument inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtures, "compatibility-matrix.json")));
        foreach (JsonProperty scope in inventory.RootElement.GetProperty("scopes").EnumerateObject().Where(scope => scope.Name.StartsWith("Tiled.")))
        {
            string[] rejected = scope.Value.EnumerateObject().Where(field => field.Value.GetString() == "unsupported")
                .Select(field => field.Name).Append("FutureGameplay").ToArray();
            foreach (string field in rejected)
            {
                JsonObject map = Map("tiled-objects.tmj");
                JsonNode owner = scope.Name switch
                {
                    "Tiled.Map" => map,
                    "Tiled.Layer" => map["layers"]![0]!,
                    "Tiled.Tileset" => map["tilesets"]![0]!,
                    "Tiled.Tile" => map["tilesets"]![0]!["tiles"]![0]!,
                    "Tiled.Object" => map["layers"]![2]!["objects"]![0]!,
                    "Tiled.Property" => map["layers"]![2]!["objects"]![0]!["properties"]![0]!,
                    "Tiled.Point" => map["layers"]![2]!["objects"]![2]!["polyline"]![0]!,
                    "Tiled.TileOffset" => Attach(map["tilesets"]![0]!, "tileoffset", new JsonObject { ["x"] = 0, ["y"] = 0 }),
                    "Tiled.Grid" => Attach(map["tilesets"]![0]!, "grid", new JsonObject { ["orientation"] = "orthogonal" }),
                    "Tiled.Chunk" => MakeInfinite(map),
                    _ => throw new InvalidOperationException(scope.Name)
                };
                owner[field] = true;
                Failure(Import(map), "SCN2D004");
            }
        }
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void ExternalTilesetAndFilePropertiesResolveRelativeToTheirOwnFileInsideRoot()
    {
        string folder = Path.Combine(root, "sets"); Directory.CreateDirectory(folder); directories.Add(folder);
        JsonObject map = Map();
        JsonNode set = map["tilesets"]![0]!.DeepClone();
        set.AsObject().Remove("firstgid"); set["version"] = "1.11"; set["type"] = "tileset"; set["image"] = "../atlas.svg";
        set["properties"] = new JsonArray(new JsonObject { ["name"] = "Info", ["type"] = "file", ["value"] = "../atlas.svg" });
        Write("sets/atlas.tsj", set.ToJsonString());
        map["tilesets"] = new JsonArray(new JsonObject { ["firstgid"] = 1, ["source"] = "sets\\atlas.tsj" });
        Scene2DImportResult result = Import(map);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        Assert.Equal("atlas.svg", result.Document!.Levels[0].TileMap.TileSets[0].Properties["Info"]);
        Failure(Import(map, new() { AssetRootDirectory = root, MaxFiles = 1 }), "SCN2D013");
        set["source"] = "atlas.tsj"; Write("sets/atlas.tsj", set.ToJsonString());
        Failure(Import(map), "SCN2D010");
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void ReparsePointBelowRootCannotEscapeTheAssetPolicy()
    {
        // Directory symlinks on non-Windows; a junction on Windows does not
        // require Developer Mode or elevated symbolic-link privilege.
        string outside = Path.Combine(Path.GetDirectoryName(root)!, "scene-import-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        string link = Path.Combine(root, "link");
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using System.Diagnostics.Process process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell", UseShellExecute = false, CreateNoWindow = true,
                    ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", "New-Item -ItemType Junction -Path '" + link.Replace("'", "''") + "' -Target '" + outside.Replace("'", "''") + "' | Out-Null" }
                })!;
                Assert.True(process.WaitForExit(10_000));
                Assert.Equal(0, process.ExitCode);
            }
            else { Directory.CreateSymbolicLink(link, outside); }
            JsonObject map = Map(); map["tilesets"]![0]!["image"] = "link/missing.svg";
            Failure(Import(map), "SCN2D010");
        }
        finally
        {
            if (Directory.Exists(link)) { Directory.Delete(link, recursive: false); }
            Directory.Delete(outside, recursive: false);
        }
    }

    [Theory]
    [InlineData("columns", "SCN2D007")]
    [InlineData("tile-object-id", "SCN2D015")]
    [InlineData("missing-orientation", "SCN2D002")]
    [InlineData("unknown-role", "SCN2D004")]
    [InlineData("unknown-alignment", "SCN2D004")]
    [Trait("SceneImportStage", "2")]
    public void StructuralReferencesAndReservedConventionsCannotBeSilentlyInvented(string mutation, string code)
    {
        JsonObject map = Map("tiled-objects.tmj");
        switch (mutation)
        {
            case "columns": map["tilesets"]![0]!["columns"] = 100; break;
            case "tile-object-id":
                JsonNode ellipse = map["layers"]![2]!["objects"]![1]!;
                map["tilesets"]![0]!["tiles"]![0]!["objectgroup"] = new JsonObject
                { ["objects"] = new JsonArray(ellipse.DeepClone(), ellipse.DeepClone()) };
                break;
            case "missing-orientation": map.Remove("orientation"); break;
            case "unknown-role": map["properties"] = new JsonArray(new JsonObject { ["name"] = "CernealaRole", ["type"] = "string", ["value"] = "FutureGameplay" }); break;
            case "unknown-alignment": map["tilesets"]![0]!["objectalignment"] = "FutureGameplay"; break;
        }
        Failure(Import(map), code);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("42")]
    [Trait("SceneImportStage", "2")]
    public void ExternalTilesetRequiresAnObjectRatherThanThrowingAnImplementationException(string payload)
    {
        JsonObject map = Map();
        Write("bad.tsj", payload);
        map["tilesets"] = new JsonArray(new JsonObject { ["firstgid"] = 1, ["source"] = "bad.tsj" });
        Failure(Import(map), "SCN2D002");
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void OversizedTileColliderCollectionStopsBeforeMaterializingEveryObject()
    {
        JsonObject map = Map("tiled-objects.tmj");
        JsonArray points = new();
        for (int index = 0; index < 4096; index++) { points.Add(new JsonObject { ["x"] = index, ["y"] = index % 2 }); }
        JsonArray objects = new();
        for (int index = 1; index <= 32; index++)
        { objects.Add(new JsonObject { ["id"] = index, ["polyline"] = points.DeepClone() }); }
        map["tilesets"]![0]!["tiles"]![0]!["objectgroup"] = new JsonObject { ["objects"] = objects };
        string file = Write("map.tmj", map.ToJsonString());
        // Warm runtime/code paths without processing the hostile shape corpus.
        Assert.True(TiledScene2DImporter.Import(Path.Combine(fixtures, "tiled-finite.tmj")).Success);
        long start = GC.GetAllocatedBytesForCurrentThread();
        Scene2DImportResult result = TiledScene2DImporter.Import(file);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Failure(result, "SCN2D013");
        Assert.True(allocated < 100_000_000, $"A rejected tile collider collection allocated {allocated:N0} bytes.");
    }

    [Theory]
    [InlineData("zlib")]
    [InlineData("gzip")]
    [Trait("SceneImportStage", "2")]
    public void CompressionTruncationAndCorruptionAreBoundedAndDeterministic(string compression)
    {
        JsonObject map = Map();
        using MemoryStream encoded = new();
        using (Stream compressor = compression == "gzip" ? new GZipStream(encoded, CompressionLevel.SmallestSize, true)
            : new ZLibStream(encoded, CompressionLevel.SmallestSize, true)) { compressor.Write(new byte[16]); }
        byte[] bytes = encoded.ToArray();
        map["layers"]![0]!["encoding"] = "base64";
        map["layers"]![0]!["compression"] = compression;
        for (int length = 0; length < bytes.Length; length++)
        {
            map["layers"]![0]!["data"] = Convert.ToBase64String(bytes[..length]);
            Failure(Import(map), "SCN2D002");
        }
        byte[] badChecksum = (byte[])bytes.Clone(); badChecksum[^1] ^= 0xff;
        map["layers"]![0]!["data"] = Convert.ToBase64String(badChecksum);
        Failure(Import(map), "SCN2D002");
        map["layers"]![0]!["data"] = Convert.ToBase64String([.. bytes, 0xff]);
        Failure(Import(map), "SCN2D002");
    }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void GzipOptionalHeadersHeaderChecksumAndConcatenatedMembersRemainSupported()
    {
        JsonObject map = Map();
        using MemoryStream encoded = new();
        using (GZipStream compressor = new(encoded, CompressionLevel.SmallestSize, true)) { compressor.Write(new byte[8]); }
        byte[] member = encoded.ToArray();
        byte[] header = [0x1f, 0x8b, 8, 0x1e, 0, 0, 0, 0, 0, 255, 2, 0, 1, 2, (byte)'N', 0, (byte)'C', 0];
        // Independent bitwise CRC oracle, rather than using the production checksum library.
        uint crc = uint.MaxValue;
        foreach (byte item in header)
        {
            crc ^= item;
            for (int bit = 0; bit < 8; bit++) { crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0); }
        }
        crc = ~crc;
        byte[] first = [.. header, (byte)crc, (byte)(crc >> 8), .. member[10..]];
        map["layers"]![0]!["encoding"] = "base64";
        map["layers"]![0]!["compression"] = "gzip";
        map["layers"]![0]!["data"] = Convert.ToBase64String([.. first, .. member]);
        Scene2DImportResult result = Import(map);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        first[header.Length] ^= 1;
        map["layers"]![0]!["data"] = Convert.ToBase64String([.. first, .. member]);
        Failure(Import(map), "SCN2D002");
    }

    private static JsonNode Attach(JsonNode owner, string name, JsonObject value) { owner[name] = value; return value; }

    [Fact]
    [Trait("SceneImportStage", "2")]
    public void ExplicitLocalDriveRootUsesTheSameContainmentRules()
    {
        Scene2DImportResult result = Import(Map(), new() { AssetRootDirectory = Path.GetPathRoot(root) });
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        Assert.EndsWith("atlas.svg", Assert.Single(result.Document!.Assets).Path);
    }

    private static JsonNode MakeInfinite(JsonObject map)
    {
        map["infinite"] = true;
        foreach (JsonNode? layer in map["layers"]!.AsArray())
        {
            if (layer!["type"]!.GetValue<string>() != "tilelayer") { continue; }
            JsonObject chunk = new() { ["x"] = 0, ["y"] = 0, ["width"] = 2, ["height"] = 2, ["data"] = layer["data"]!.DeepClone() };
            layer.AsObject().Remove("data"); layer["chunks"] = new JsonArray(chunk);
        }
        return map["layers"]![0]!["chunks"]![0]!;
    }
    private static void Failure(Scene2DImportResult result, string code)
    { Assert.False(result.Success); Assert.Null(result.Document); Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code); }
    private string Write(string name, string text)
    { string file = Path.Combine(root, name); if (!files.Contains(file)) { files.Add(file); } File.WriteAllText(file, text); return file; }
    public void Dispose()
    {
        foreach (string file in files) { File.Delete(file); }
        foreach (string directory in directories) { Directory.Delete(directory, recursive: false); }
        Directory.Delete(root, recursive: false);
    }
}
