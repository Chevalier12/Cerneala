using System.Reflection;
using System.Text.Json.Nodes;
using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class LdtkImporterTests
{
    [Theory]
    [InlineData("version", "SCN2D003")]
    [InlineData("unknown", "SCN2D004")]
    [InlineData("duplicateUid", "SCN2D015")]
    [InlineData("duplicateIid", "SCN2D015")]
    [InlineData("caseAliasIid", "SCN2D015")]
    [InlineData("missingAlpha", "SCN2D002")]
    [InlineData("externalUnknown", "SCN2D004")]
    [InlineData("externalField", "SCN2D004")]
    [InlineData("missingSet", "SCN2D006")]
    [InlineData("source", "SCN2D007")]
    [InlineData("tile", "SCN2D006")]
    [InlineData("flip", "SCN2D004")]
    [InlineData("alpha", "SCN2D004")]
    [InlineData("stack", "SCN2D004")]
    [InlineData("unsnapped", "SCN2D004")]
    [InlineData("grid", "SCN2D004")]
    [InlineData("parallax", "SCN2D004")]
    [InlineData("background", "SCN2D004")]
    [InlineData("layout", "SCN2D004")]
    [InlineData("missingLevel", "SCN2D001")]
    [InlineData("cycle", "SCN2D010")]
    [InlineData("escape", "SCN2D010")]
    [InlineData("externalIdentity", "SCN2D015")]
    [InlineData("layerLevel", "SCN2D015")]
    [InlineData("layerName", "SCN2D015")]
    [InlineData("intGridLength", "SCN2D005")]
    [InlineData("intGridValue", "SCN2D016")]
    [InlineData("fieldKind", "SCN2D004")]
    [InlineData("fieldType", "SCN2D016")]
    [InlineData("mask", "SCN2D009")]
    [InlineData("promotion", "SCN2D012")]
    [InlineData("degenerate", "SCN2D008")]
    public void InvalidSourcesAreLocatedAndNeverPublishPartially(string mutation, string code)
    {
        using Fixture fixture = new();
        JsonObject root = fixture.Root;
        JsonObject layer = Layer(root), tile = layer["gridTiles"]![0]!.AsObject();
        switch (mutation)
        {
            case "version": root["jsonVersion"] = "1.5.4"; break;
            case "unknown": root["futureGameplay"] = true; break;
            case "duplicateUid": root["defs"]!["layers"]![0]!["uid"] = 3; break;
            case "duplicateIid": layer["iid"] = root["iid"]!.DeepClone(); break;
            case "caseAliasIid": root["iid"] = "abcdefab-abcd-abcd-abcd-abcdefabcdef"; layer["iid"] = "ABCDEFAB-ABCD-ABCD-ABCD-ABCDEFABCDEF"; break;
            case "missingAlpha": tile.Remove("a"); break;
            case "externalUnknown": fixture.Separate("level.ldtkl", true, false); Level(root)["futureGameplay"] = true; break;
            case "externalField": fixture.Separate("level.ldtkl", true, false); Level(root)["fieldInstances"]!.AsArray().Add(new JsonObject { ["__type"] = "Array<Point>" }); break;
            case "missingSet": layer["__tilesetDefUid"] = 99; break;
            case "source": tile["src"] = new JsonArray(999, 0); break;
            case "tile": tile["t"] = 99; break;
            case "flip": tile["f"] = 4; break;
            case "alpha": tile["a"] = 0.5; break;
            case "stack": layer["gridTiles"]!.AsArray().Add(tile.DeepClone()); break;
            case "unsnapped": tile["px"] = new JsonArray(1, 0); break;
            case "grid": layer["__gridSize"] = 8; break;
            case "parallax": root["defs"]!["layers"]![1]!["parallaxFactorX"] = 0.5; break;
            case "background": Level(root)["bgRelPath"] = "atlas.svg"; break;
            case "layout": root["worldLayout"] = "Spiral"; break;
            case "missingLevel": fixture.Separate("missing.ldtkl"); break;
            case "cycle": fixture.Separate("project.ldtk"); break;
            case "escape": fixture.Separate("../outside.ldtkl"); break;
            case "externalIdentity": fixture.Separate("level.ldtkl", true); break;
            case "layerLevel": layer["levelId"] = 99; break;
            case "layerName": layer["__identifier"] = "Changed"; break;
            case "intGridLength": IntGrid(root); layer["intGridCsv"] = new JsonArray(1); break;
            case "intGridValue": IntGrid(root); layer["intGridCsv"] = new JsonArray(2, 0, 0, 0); break;
            case "fieldKind": Entity(root, "Spawn", "Point", fieldKind: "Array<Point>"); break;
            case "fieldType": Entity(root, "Spawn", "Point")["fieldInstances"]![0]!["__value"] = 4; break;
            case "mask": Entity(root, "Collider", "Box", mask: "-1"); break;
            case "promotion": Entity(root, "Promote", "Box", x: 99); break;
            case "degenerate": Entity(root, "Collider", "Polyline", points: "0,0 0,0"); break;
        }
        Scene2DImportResult result = fixture.Import();
        Assert.False(result.Success);
        Assert.Null(result.Document);
        Assert.Contains(result.Diagnostics, item => item.Code == code && !string.IsNullOrWhiteSpace(item.FilePath) && item.JsonPath!.StartsWith('$'));
    }

    [Fact]
    public void BakedAutoTilesAndIntGridRemainDataWithTotalOffsetsOnce()
    {
        using Fixture fixture = new();
        IntGrid(fixture.Root);
        JsonObject layer = Layer(fixture.Root);
        layer["autoLayerTiles"] = layer["gridTiles"]!.DeepClone(); layer["gridTiles"] = new JsonArray();
        layer["intGridCsv"] = new JsonArray(1, 0, 0, 1);
        Scene2DLevel level = Assert.Single(Success(fixture.Import()).Levels);
        TileLayer2DModel result = level.TileMap.Layers[0];
        Assert.Equal(new DrawPoint(2, 3), result.Offset);
        Assert.Equal(new[] { 1, 0, 0, 1 }, Assert.IsAssignableFrom<IReadOnlyList<int>>(result.Properties["$IntGrid"]));
        Assert.True(result.TryGetCell(new(1, 0), out TileCell2D cell));
        Assert.Equal(TileFlip2D.Horizontal, cell.Flip);
        Assert.Empty(level.Entities);
        Assert.All(level.TileMap.TileSets.SelectMany(set => set.Tiles), definition => Assert.Empty(definition.Colliders));
    }

    [Theory]
    [InlineData("Free", 90, -30)]
    [InlineData("GridVania", 90, -30)]
    [InlineData("LinearHorizontal", 32, 0)]
    [InlineData("LinearVertical", 0, 32)]
    public void WorldLayoutsHaveExplicitPlacement(string layout, int x, int y)
    {
        using Fixture fixture = new();
        JsonObject second = Level(fixture.Root).DeepClone().AsObject();
        second["uid"] = 8; second["iid"] = Guid.NewGuid().ToString(); second["worldX"] = 90; second["worldY"] = -30;
        foreach (JsonNode? layer in second["layerInstances"]!.AsArray()) { layer!["iid"] = Guid.NewGuid().ToString(); layer["levelId"] = 8; }
        fixture.Root["levels"]!.AsArray().Add(second);
        fixture.Root["worldLayout"] = layout;
        Scene2DDocument document = Success(fixture.Import());
        Assert.Equal(new DrawPoint(x, y), document.Levels[1].WorldOffset);
    }

    [Theory]
    [InlineData("Box", "", 1)]
    [InlineData("Ellipse", "", 1)]
    [InlineData("Polygon", "0,0 16,0 0,8", 1)]
    [InlineData("Polyline", "0,0 16,0 16,8", 2)]
    public void EntityGeometryPreservesPivotAndCollisionFields(string shape, string points, int count)
    {
        using Fixture fixture = new();
        Entity(fixture.Root, "Collider", shape, points);
        Scene2DEntity entity = Assert.Single(Assert.Single(Success(fixture.Import()).Levels).Entities);
        Assert.Equal(new DrawPoint(8, 12), entity.Position);
        Assert.Equal(new DrawPoint(0.5f, 0.5f), entity.Pivot);
        Assert.Equal(new DrawSize(16, 8), entity.Size);
        Assert.Equal(count, entity.Colliders.Count);
        Assert.All(entity.Colliders, collider => { Assert.Equal(0u, collider.CollisionLayer); Assert.Equal(0xffffffffu, collider.CollisionMask); Assert.True(collider.IsTrigger); });
        Assert.Equal("Closed", entity.Properties["InitialState"]);
    }

    [Fact]
    public void PromotionReferencesStableDefinitionLayerWithoutMaterializingNodes()
    {
        using Fixture fixture = new();
        Entity(fixture.Root, "Promote", "Box");
        TilePromotion2D promotion = Assert.Single(Assert.Single(Success(fixture.Import()).Levels).Promotions);
        Assert.Equal(new TileCellKey2D("1", 1, 0), promotion.Cell);
        Assert.Equal("Closed", promotion.Properties["InitialState"]);
    }

    [Fact]
    public void ResourceBudgetsApplyBeforeAllocation()
    {
        using Fixture fixture = new();
        Scene2DImportResult result = fixture.Import(new() { MaxCells = 3 });
        Assert.Null(result.Document); Assert.Contains(result.Diagnostics, item => item.Code == "SCN2D013");
    }

    [Fact]
    public void FieldTileMetadataAndDefinitionsRetainTheirOwner()
    {
        using Fixture fixture = new();
        JsonObject source = Entity(fixture.Root, "Spawn", "Box");
        for (int index = 0; index < 2; index++)
        {
            source["fieldInstances"]![index]!["__tile"] = new JsonObject { ["tilesetUid"] = 3, ["x"] = index * 16, ["y"] = 0, ["w"] = 16, ["h"] = 16 };
        }
        Scene2DEntity entity = Assert.Single(Assert.Single(Success(fixture.Import()).Levels).Entities);
        Assert.True(entity.Properties.TryGetValue("$FieldMetadata", out object? fields));
        var metadata = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(fields);
        foreach (string name in new[] { "CernealaRole", "ColliderShape" })
        {
            var field = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(metadata[name]);
            Assert.True(field.ContainsKey("$__tile"));
        }
        var definition = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(entity.Properties["$Definition"]);
        Assert.True(definition.ContainsKey("$FieldDefinitions"));
    }

    [Theory]
    [InlineData("Float", "F_Float", "1.25")]
    [InlineData("Multilines", "F_Text", "\"first\\nsecond\"")]
    [InlineData("Color", "F_Color", "\"#123456\"")]
    [InlineData("FilePath", "F_Path", "\"atlas.svg\"")]
    [InlineData("String", "F_String", "null")]
    public void AdditionalPrimitiveFieldsArePreserved(string kind, string internalKind, string json)
    {
        using Fixture fixture = new();
        JsonObject source = Entity(fixture.Root, "Spawn", "Box");
        JsonNode field = source["fieldInstances"]![2]!;
        field["__identifier"] = "Custom"; field["__type"] = kind; field["__value"] = JsonNode.Parse(json);
        JsonNode definition = fixture.Root["defs"]!["entities"]![0]!["fieldDefs"]![2]!;
        definition["identifier"] = "Custom"; definition["__type"] = kind; definition["type"] = internalKind; definition["canBeNull"] = true;
        object? value = Assert.Single(Assert.Single(Success(fixture.Import()).Levels).Entities).Properties["Custom"];
        switch (kind)
        {
            case "Float": Assert.Equal(1.25d, value); break;
            case "Multilines": Assert.Equal("first\nsecond", value); break;
            case "Color": Assert.True(Color.TryParse("#123456", out Color color)); Assert.Equal(color, value); break;
            case "FilePath": Assert.Equal("atlas.svg", value); break;
            default: Assert.Null(value); break;
        }
    }

    [Theory]
    [InlineData("Root")]
    [InlineData("Definitions")]
    [InlineData("World")]
    [InlineData("Level")]
    [InlineData("LayerInstance")]
    [InlineData("LayerDef")]
    [InlineData("Tile")]
    [InlineData("TilesetDef")]
    [InlineData("EntityInstance")]
    [InlineData("FieldInstance")]
    [InlineData("EntityDef")]
    [InlineData("FieldDef")]
    [InlineData("IntGridValueDef")]
    [InlineData("IntGridValueGroupDef")]
    [InlineData("TileCustomMetadata")]
    [InlineData("TilesetRect")]
    public void EveryInventoryScopeRejectsUnknownGameplay(string scope)
    {
        using Fixture fixture = new(); JsonObject root = fixture.Root;
        IntGrid(root); JsonObject ground = Layer(root);
        ground["autoLayerTiles"] = ground["gridTiles"]!.DeepClone(); ground["gridTiles"] = new JsonArray();
        ground["intGridCsv"] = new JsonArray(1, 0, 0, 1);
        JsonObject entity = Entity(root, "Spawn", "Box");
        JsonNode layerDef = root["defs"]!["layers"]![2]!;
        layerDef["intGridValuesGroups"]!.AsArray().Add(new JsonObject { ["uid"] = 1 });
        JsonObject rectangle = new() { ["tilesetUid"] = 3, ["x"] = 0, ["y"] = 0, ["w"] = 16, ["h"] = 16 };
        entity["__tile"] = rectangle;
        JsonNode set = root["defs"]!["tilesets"]![0]!;
        set["customData"]!.AsArray().Add(new JsonObject { ["data"] = "custom", ["tileId"] = 0 });
        JsonObject world = new() { ["identifier"] = "World", ["iid"] = Guid.NewGuid().ToString(), ["worldLayout"] = "Free", ["levels"] = root["levels"]!.DeepClone() };
        JsonNode target = scope switch
        {
            "Root" => root, "Definitions" => root["defs"]!, "World" => world, "Level" => Level(root),
            "LayerInstance" => ground, "LayerDef" => layerDef, "Tile" => ground["autoLayerTiles"]![0]!,
            "TilesetDef" => set, "EntityInstance" => entity, "FieldInstance" => entity["fieldInstances"]![0]!,
            "EntityDef" => root["defs"]!["entities"]![0]!, "FieldDef" => root["defs"]!["entities"]![0]!["fieldDefs"]![0]!,
            "IntGridValueDef" => layerDef["intGridValues"]![0]!, "IntGridValueGroupDef" => layerDef["intGridValuesGroups"]![0]!,
            "TileCustomMetadata" => set["customData"]![0]!, _ => rectangle
        };
        target["futureGameplay"] = true;
        if (scope == "World") { root["levels"] = new JsonArray(); root["worlds"] = new JsonArray(world); root["worldLayout"] = null; }
        Scene2DImportResult result = fixture.Import(); Assert.Null(result.Document);
        Assert.Contains(result.Diagnostics, item => item.Code == "SCN2D004" && item.JsonPath!.EndsWith(".futureGameplay", StringComparison.Ordinal));
    }

    [Fact]
    public void MultiWorldBakedAutoLayerKeepsWorldIdentity()
    {
        using Fixture fixture = new(); JsonObject root = fixture.Root;
        JsonObject ground = Layer(root); JsonNode definition = root["defs"]!["layers"]![1]!;
        definition["type"] = "AutoLayer"; definition["__type"] = "AutoLayer"; definition["autoTilesetDefUid"] = 3;
        ground["__type"] = "AutoLayer"; ground["autoLayerTiles"] = ground["gridTiles"]!.DeepClone(); ground["gridTiles"] = new JsonArray();
        string iid = Guid.NewGuid().ToString();
        root["worlds"] = new JsonArray(new JsonObject { ["identifier"] = "World", ["iid"] = iid, ["worldLayout"] = "Free", ["levels"] = root["levels"]!.DeepClone() });
        root["levels"] = new JsonArray(); root["worldLayout"] = null;
        Scene2DLevel level = Assert.Single(Success(fixture.Import()).Levels);
        Assert.Equal(iid, Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(level.Properties["$World"])["$WorldIid"]);
        Assert.True(level.TileMap.Layers[0].TryGetCell(new(1, 0), out TileCell2D cell)); Assert.Equal(2, cell.TileId);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"uid\":")]
    public void MalformedExternalPayloadFailsWithoutLeakingExceptions(string json)
    {
        using Fixture fixture = new(); fixture.Separate("level.ldtkl"); fixture.Write("level.ldtkl", json);
        Scene2DImportResult result = fixture.Import(); Assert.Null(result.Document);
        Assert.Contains(result.Diagnostics, item => item.Code == "SCN2D002");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyLevelAndUnalignedBoundsAreExplicit(bool unaligned)
    {
        using Fixture fixture = new();
        if (unaligned) { Level(fixture.Root)["pxWid"] = 31; }
        else { Level(fixture.Root)["layerInstances"] = new JsonArray(); }
        Scene2DImportResult result = fixture.Import();
        if (unaligned) { Assert.Null(result.Document); Assert.Contains(result.Diagnostics, item => item.Code == "SCN2D004"); }
        else { Assert.Equal(new DrawSize(1, 1), Assert.Single(Success(result).Levels).TileMap.TileSize); }
    }

    private static Scene2DDocument Success(Scene2DImportResult result)
    { Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(item => item.Code + ": " + item.Message))); return Assert.IsType<Scene2DDocument>(result.Document); }
    private static JsonObject Level(JsonObject root) => root["levels"]![0]!.AsObject();
    private static JsonObject Layer(JsonObject root) => Level(root)["layerInstances"]![1]!.AsObject();
    private static void IntGrid(JsonObject root)
    {
        JsonNode definition = root["defs"]!["layers"]![1]!;
        definition["type"] = "IntGrid"; definition["__type"] = "IntGrid"; definition["autoTilesetDefUid"] = 3;
        definition["intGridValues"] = new JsonArray(new JsonObject { ["value"] = 1, ["color"] = "#123456", ["identifier"] = "Wall", ["groupUid"] = 0 });
        Layer(root)["__type"] = "IntGrid";
    }

    private static JsonObject Entity(JsonObject root, string role, string shape, string points = "", string mask = "0xffffffff", int x = 1, string fieldKind = "String")
    {
        JsonObject layerDef = root["defs"]!["layers"]![0]!.DeepClone().AsObject();
        layerDef["uid"] = 20; layerDef["identifier"] = "Actors"; layerDef["type"] = "Entities"; layerDef["__type"] = "Entities"; layerDef["tilesetDefUid"] = null;
        root["defs"]!["layers"]!.AsArray().Insert(0, layerDef);
        JsonArray fields = new(), definitions = new();
        void Field(string name, string type, JsonNode? value)
        {
            int id = 30 + fields.Count;
            fields.Add(new JsonObject { ["__identifier"] = name, ["__type"] = type, ["__value"] = value, ["defUid"] = id, ["realEditorValues"] = new JsonArray() });
            definitions.Add(new JsonObject { ["identifier"] = name, ["__type"] = type, ["type"] = type == "Bool" ? "F_Bool" : type == "Int" ? "F_Int" : "F_String", ["uid"] = id, ["isArray"] = false, ["canBeNull"] = false });
        }
        Field("CernealaRole", fieldKind, JsonValue.Create(role)); Field("ColliderShape", "String", JsonValue.Create(shape));
        Field("ColliderPoints", "String", JsonValue.Create(points)); Field("CollisionLayer", "Int", JsonValue.Create(0));
        Field("CollisionMask", "String", JsonValue.Create(mask)); Field("IsTrigger", "Bool", JsonValue.Create(true));
        Field("InitialState", "String", JsonValue.Create("Closed")); Field("TileLayer", "Int", JsonValue.Create(1));
        Field("TileX", "Int", JsonValue.Create(x)); Field("TileY", "Int", JsonValue.Create(0));
        root["defs"]!["entities"]!.AsArray().Add(new JsonObject { ["uid"] = 21, ["identifier"] = "Door", ["fieldDefs"] = definitions, ["width"] = 16, ["height"] = 8, ["pivotX"] = 0.5, ["pivotY"] = 0.5 });
        JsonObject entity = new() { ["iid"] = Guid.NewGuid().ToString(), ["defUid"] = 21, ["__identifier"] = "Door", ["px"] = new JsonArray(16, 16), ["__pivot"] = new JsonArray(0.5, 0.5), ["width"] = 16, ["height"] = 8, ["fieldInstances"] = fields, ["__grid"] = new JsonArray(1, 1), ["__tags"] = new JsonArray(), ["__smartColor"] = "#123456" };
        JsonObject layer = Level(root)["layerInstances"]![0]!.DeepClone().AsObject();
        layer["layerDefUid"] = 20; layer["iid"] = Guid.NewGuid().ToString(); layer["__identifier"] = "Actors"; layer["__type"] = "Entities";
        layer["__tilesetDefUid"] = null; layer["__tilesetRelPath"] = null; layer["gridTiles"] = new JsonArray(); layer["entityInstances"] = new JsonArray(entity);
        Level(root)["layerInstances"]!.AsArray().Insert(0, layer);
        return entity;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string directory;
        internal JsonObject Root { get; }
        internal Fixture()
        {
            DirectoryInfo? repo = new(AppContext.BaseDirectory);
            while (repo is not null && !File.Exists(Path.Combine(repo.FullName, "Cerneala.slnx"))) { repo = repo.Parent; }
            Assert.NotNull(repo);
            string fixtures = Path.Combine(repo.FullName, "tests", "Fixtures", "Scene2DImport");
            directory = Path.Combine(repo.FullName, ".artifacts", "scene-import-stage3", "fixtures", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Root = JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures, "ldtk-inline.ldtk")))!.AsObject();
            File.Copy(Path.Combine(fixtures, "atlas.svg"), Path.Combine(directory, "atlas.svg"));
        }
        internal void Separate(string path, bool write = false, bool changeIdentity = true)
        {
            if (write) { JsonObject external = Level(Root).DeepClone().AsObject(); if (changeIdentity) { external["iid"] = Guid.NewGuid().ToString(); } File.WriteAllText(Path.Combine(directory, path), external.ToJsonString()); }
            Root["externalLevels"] = true; Level(Root)["layerInstances"] = null; Level(Root)["externalRelPath"] = path;
        }
        internal void Write(string path, string text) => File.WriteAllText(Path.Combine(directory, path), text);
        internal Scene2DImportResult Import(Scene2DImportOptions? options = null)
        {
            string file = Path.Combine(directory, "project.ldtk"); File.WriteAllText(file, Root.ToJsonString());
            Type? type = typeof(TiledScene2DImporter).Assembly.GetType("Cerneala.Scene2D.Importers.LdtkScene2DImporter");
            Assert.NotNull(type); // RED is a missing parser, not an invalid fixture exception.
            try { return (Scene2DImportResult)type.GetMethod("Import")!.Invoke(null, [file, options])!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        public void Dispose() => Directory.Delete(directory, true);
    }
}
