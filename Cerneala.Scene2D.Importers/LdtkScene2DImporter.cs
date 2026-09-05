using System.Globalization;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Scene2D.Importers;

public static class LdtkScene2DImporter
{
    public static Scene2DImportResult Import(string filePath, Scene2DImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using ImportContext context = new(options ?? new());
        Scene2DDocument? document = null;
        try
        {
            string file = context.Initialize(filePath);
            document = new Parser(context, file).Parse(context.Load(file));
        }
        catch (Exception error) when (error is ImportFailure or JsonException or IOException or UnauthorizedAccessException or ArgumentException or OverflowException)
        { context.Record(error); }
        return new(document, context.Diagnostics.Complete());
    }

    private sealed class Parser(ImportContext context, string projectFile)
    {
        private readonly ImportConventions conventions = new(context);
        private readonly HashSet<int> uids = new();
        private readonly HashSet<Guid> iids = new();
        private readonly HashSet<string> externalFiles = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly Dictionary<int, Atlas> atlases = new();
        private readonly Dictionary<int, LayerDefinition> layerDefinitions = new();
        private readonly Dictionary<int, EntityDefinition> entityDefinitions = new();
        private readonly Dictionary<int, FieldDefinition> levelFields = new();
        private readonly Dictionary<string, Scene2DAsset> assets = new(StringComparer.Ordinal);
        private readonly List<Scene2DLevel> levels = new();
        private int definitionCount;
        private bool externalLevels;

        internal Scene2DDocument Parse(JsonElement root)
        {
            context.Object(root);
            if (!root.TryGetProperty("jsonVersion", out JsonElement version) || version.ValueKind != JsonValueKind.String || version.GetString() != "1.5.3")
            { context.Path = "$.jsonVersion"; context.Fail("SCN2D003", "Only LDtk JSON version 1.5.3 is supported."); }
            Dictionary<string, object?> properties = new(StringComparer.Ordinal);
            context.Fields(root, "__header__ defs externalLevels iid jsonVersion levels worldGridHeight worldGridWidth worldLayout worlds",
                "bgColor defaultLevelBgColor",
                "__FORCED_REFS appBuildId backupLimit backupOnSave backupRelPath customCommands defaultEntityHeight defaultEntityWidth defaultGridSize defaultLevelHeight defaultLevelWidth defaultPivotX defaultPivotY dummyWorldIid exportLevelBg exportPng exportTiled flags identifierStyle imageExportMode levelNamePattern minifyJson nextUid pngFilePattern simplifiedExport toc tutorialDesc", properties);
            Header(root);
            properties["$Iid"] = Iid(root);
            properties["$Format"] = "LDtk"; properties["$Version"] = "1.5.3";
            externalLevels = context.Boolean(context.Required(root, "externalLevels"));
            externalFiles.Add(projectFile);
            context.Path = "$.defs";
            Definitions(context.Required(root, "defs"), properties);
            context.Path = "$";
            JsonElement legacy = context.Required(root, "levels"), worlds = context.Required(root, "worlds");
            _ = context.Array(legacy); _ = context.Array(worlds);
            if (legacy.GetArrayLength() > 0 && worlds.GetArrayLength() > 0)
            { context.Fail("SCN2D015", "Legacy levels and multi-world levels cannot both be nonempty."); }
            if (worlds.GetArrayLength() == 0)
            {
                Preserve(root, properties, "worldGridHeight", "worldGridWidth", "worldLayout");
                ParseLevels(legacy, Layout(root), properties, "$.levels");
            }
            else
            {
                int index = 0;
                foreach (JsonElement world in worlds.EnumerateArray())
                {
                    context.Path = $"$.worlds[{index++}]";
                    Dictionary<string, object?> worldProperties = new(StringComparer.Ordinal);
                    context.Fields(world, "identifier iid levels worldLayout", "worldGridHeight worldGridWidth", "defaultLevelHeight defaultLevelWidth", worldProperties);
                    worldProperties["$WorldIid"] = Iid(world);
                    worldProperties["$WorldIdentifier"] = RequiredText(world, "identifier");
                    worldProperties["$WorldLayout"] = Layout(world);
                    ParseLevels(context.Required(world, "levels"), Layout(world), worldProperties, context.Path + ".levels");
                }
            }
            context.File = projectFile; context.Path = "$";
            return new(levels, assets.Values, properties: properties, validationOptions: context.ValidationOptions);
        }

        private void Header(JsonElement value)
        {
            if (!value.TryGetProperty("__header__", out JsonElement header)) { return; }
            context.Fields(header, "app appVersion doc fileType schema url");
            if (RequiredText(header, "app") != "LDtk" || RequiredText(header, "appVersion") != "1.5.3" || RequiredText(header, "fileType") != "LDtk Project JSON")
            { context.Fail("SCN2D003", "The transport header must identify LDtk 1.5.3 project JSON."); }
        }

        private void Definitions(JsonElement value, Dictionary<string, object?> properties)
        {
            context.Fields(value, "entities layers tilesets", "enums externalEnums levelFields", properties: properties);
            string path = context.Path;
            int index = 0;
            foreach (JsonElement set in context.Array(context.Required(value, "tilesets")))
            { context.Path = $"{path}.tilesets[{index++}]"; ParseAtlas(set); }
            index = 0;
            foreach (JsonElement layer in context.Array(context.Required(value, "layers")))
            {
                context.Path = $"{path}.layers[{index++}]";
                if (layerDefinitions.Count >= 4096) { context.Fail("SCN2D013", "Layer definitions exceed the core limit."); }
                Dictionary<string, object?> metadata = new(StringComparer.Ordinal);
                context.Fields(layer, "__type autoTilesetDefUid gridSize identifier parallaxFactorX parallaxFactorY parallaxScaling pxOffsetX pxOffsetY tilesetDefUid type uid",
                    "intGridValues intGridValuesGroups",
                    "autoRuleGroups autoSourceLayerDefUid autoTilesKilledByOtherLayerUid biomeFieldUid canSelectWhenInactive displayOpacity doc excludedTags guideGridHei guideGridWid hideFieldsWhenInactive hideInList inactiveOpacity renderInWorldView requiredTags tilePivotX tilePivotY uiColor uiFilterTags useAsyncRender", metadata);
                int id = Uid(layer); string type = LayerType(RequiredText(layer, "type"));
                if (RequiredText(layer, "__type") != type) { context.Fail("SCN2D015", "Layer definition types conflict."); }
                context.Expect(layer, "parallaxFactorX", 0); context.Expect(layer, "parallaxFactorY", 0);
                // Scaling has no effect with zero parallax, but its exported value is retained.
                _ = context.Boolean(layer, "parallaxScaling", true);
                Preserve(layer, metadata, "parallaxScaling", "pxOffsetX", "pxOffsetY");
                int? tileSet = NullableInt(layer, type is "IntGrid" or "AutoLayer" ? "autoTilesetDefUid" : "tilesetDefUid");
                if (tileSet is not null && !atlases.ContainsKey(tileSet.Value)) { context.Fail("SCN2D006", "Layer definition references an absent tileset."); }
                HashSet<int> gridValues = IntGridDefinitions(layer);
                layerDefinitions.Add(id, new(RequiredText(layer, "identifier"), type, Positive(layer, "gridSize"), tileSet, metadata, gridValues));
            }
            index = 0;
            foreach (JsonElement entity in context.Array(context.Required(value, "entities")))
            {
                context.Path = $"{path}.entities[{index++}]";
                if (entityDefinitions.Count >= context.Options.MaxEntities) { context.Fail("SCN2D013", "Entity definitions exceed the import budget."); }
                Dictionary<string, object?> metadata = new(StringComparer.Ordinal);
                context.Fields(entity, "fieldDefs identifier uid", "color height pivotX pivotY tileRect tileRenderMode width",
                    "allowOutOfBounds doc exportToToc fillOpacity hollow keepAspectRatio limitBehavior limitScope lineOpacity maxCount maxHeight maxWidth minHeight minWidth nineSliceBorders renderMode resizableX resizableY showName tags tileId tileOpacity tilesetId uiTileRect", metadata);
                int id = Uid(entity);
                OptionalRect(entity, "tileRect");
                Dictionary<int, FieldDefinition> fields = ParseFieldDefinitions(context.Required(entity, "fieldDefs"));
                metadata["$FieldDefinitions"] = fields.ToDictionary(field => Id(field.Key), field => (object?)new Dictionary<string, object?>
                { ["Name"] = field.Value.Name, ["Kind"] = field.Value.Kind, ["Nullable"] = field.Value.Nullable });
                entityDefinitions.Add(id, new(RequiredText(entity, "identifier"), fields, metadata));
            }
            context.Path = path + ".levelFields";
            foreach (var field in ParseFieldDefinitions(context.Required(value, "levelFields"))) { levelFields.Add(field.Key, field.Value); }
            context.Path = path;
        }

        private void ParseAtlas(JsonElement value)
        {
            Dictionary<string, object?> metadata = new(StringComparer.Ordinal);
            context.Fields(value, "__cHei __cWid embedAtlas identifier padding pxHei pxWid relPath spacing tileGridSize uid",
                "customData enumTags tags", "cachedPixelData savedSelections tagsSourceEnumUid", metadata);
            int uid = Uid(value);
            if (atlases.Count >= 4096) { context.Fail("SCN2D013", "Atlas count exceeds the core limit."); }
            NullOnly(value, "embedAtlas");
            int grid = Positive(value, "tileGridSize"), width = Positive(value, "pxWid", "SCN2D007"), height = Positive(value, "pxHei", "SCN2D007");
            int columns = Positive(value, "__cWid", "SCN2D007"), rows = Positive(value, "__cHei", "SCN2D007");
            int padding = context.Int(context.Required(value, "padding"), "SCN2D007"), spacing = context.Int(context.Required(value, "spacing"), "SCN2D007");
            if (padding < 0 || spacing < 0 || ((long)width - 2L * padding + spacing) / (grid + (long)spacing) != columns ||
                ((long)height - 2L * padding + spacing) / (grid + (long)spacing) != rows)
            { context.Fail("SCN2D007", "Atlas dimensions and exported grid disagree."); }
            long count = (long)columns * rows;
            if (count > Math.Min(context.Options.MaxCells, 1_048_576) - definitionCount) { context.Fail("SCN2D013", "Atlas definitions exceed the import/core budget."); }
            string file = context.Resolve(projectFile, RequiredText(value, "relPath")); context.RequireFile(file);
            string relative = context.Relative(file); DrawSize size = new(width, height);
            ResourceId<ImageResource> resource = new(relative);
            if (assets.TryGetValue(relative, out Scene2DAsset? existing) && existing.Size != size) { context.Fail("SCN2D007", "An atlas has conflicting dimensions."); }
            assets.TryAdd(relative, new(resource, relative, size));
            Dictionary<int, string> custom = new();
            foreach (JsonElement item in context.Array(context.Required(value, "customData")))
            {
                context.Fields(item, "data tileId");
                int tile = context.Int(context.Required(item, "tileId"), "SCN2D006");
                if (tile < 0 || tile >= count) { context.Fail("SCN2D006", "Custom metadata references an absent local tile."); }
                if (!custom.TryAdd(tile, RequiredText(item, "data"))) { context.Fail("SCN2D015", "Duplicate tile custom metadata."); }
            }
            int first = definitionCount + 1; TileDefinition2D[] tiles = new TileDefinition2D[(int)count];
            for (int index = 0; index < tiles.Length; index++)
            {
                Dictionary<string, object?> tileProperties = new() { ["$LocalTileId"] = index };
                if (custom.TryGetValue(index, out string? data)) { tileProperties["$CustomData"] = data; }
                tiles[index] = new(first + index, new(padding + (long)(index % columns) * (grid + (long)spacing),
                    padding + (long)(index / columns) * (grid + (long)spacing), grid, grid), tileProperties);
            }
            definitionCount += tiles.Length;
            metadata["$DefinitionUid"] = uid; metadata["$SourceName"] = RequiredText(value, "identifier");
            atlases.Add(uid, new(new(Id(uid), resource, tiles, properties: metadata), grid, columns, rows, padding, spacing, first, file, size));
        }

        private HashSet<int> IntGridDefinitions(JsonElement layer)
        {
            HashSet<int> groups = new() { 0 }, values = new();
            foreach (JsonElement group in context.Array(context.Required(layer, "intGridValuesGroups")))
            {
                Dictionary<string, object?> metadata = new(); context.Fields(group, "uid", "color identifier", properties: metadata);
                int uid = Positive(group, "uid", "SCN2D015");
                if (!groups.Add(uid)) { context.Fail("SCN2D015", "Duplicate IntGrid group UID."); }
            }
            foreach (JsonElement value in context.Array(context.Required(layer, "intGridValues")))
            {
                Dictionary<string, object?> metadata = new(); context.Fields(value, "value", "color groupUid identifier tile", properties: metadata);
                int id = Positive(value, "value", "SCN2D016");
                if (!values.Add(id)) { context.Fail("SCN2D015", "Duplicate IntGrid value."); }
                if (!groups.Contains(context.Int(value, "groupUid", 0))) { context.Fail("SCN2D015", "IntGrid value references an absent group."); }
                OptionalRect(value, "tile");
            }
            return values;
        }

        private Dictionary<int, FieldDefinition> ParseFieldDefinitions(JsonElement values)
        {
            Dictionary<int, FieldDefinition> fields = new(); HashSet<string> names = new(StringComparer.Ordinal);
            string path = context.Path; int index = 0;
            foreach (JsonElement value in context.Array(values))
            {
                context.Path = $"{path}[{index++}]";
                context.Fields(value, "__type canBeNull identifier isArray type uid", editor:
                    "acceptFileTypes allowOutOfLevelRef allowedRefTags allowedRefs allowedRefsEntityUid arrayMaxLength arrayMinLength autoChainRef defaultOverride doc editorAlwaysShow editorCutLongValues editorDisplayColor editorDisplayMode editorDisplayPos editorDisplayScale editorLinkStyle editorShowInWorld editorTextPrefix editorTextSuffix exportToToc max min regex searchable symmetricalRef textLanguageMode tilesetUid useForSmartColor");
                string kind = FieldKind(RequiredText(value, "__type"));
                if (context.Boolean(context.Required(value, "isArray"))) { context.Fail("SCN2D004", "Array fields are outside v1."); }
                string internalType = kind switch { "Int" => "F_Int", "Float" => "F_Float", "Bool" => "F_Bool", "Color" => "F_Color", "FilePath" => "F_Path", "Multilines" => "F_Text", _ => "F_String" };
                if (RequiredText(value, "type") != internalType) { context.Fail("SCN2D015", "Field definition type caches conflict."); }
                int uid = Uid(value); string name = RequiredText(value, "identifier"); CheckName(name, names);
                fields.Add(uid, new(name, kind, context.Boolean(context.Required(value, "canBeNull"))));
            }
            context.Path = path; return fields;
        }

        private Dictionary<string, object?> Fields(JsonElement values, Dictionary<int, FieldDefinition> definitions)
        {
            Dictionary<string, object?> properties = new(StringComparer.Ordinal), fieldMetadata = new(StringComparer.Ordinal); HashSet<string> names = new(StringComparer.Ordinal);
            string path = context.Path; int index = 0;
            foreach (JsonElement field in context.Array(values))
            {
                context.Path = $"{path}.fieldInstances[{index++}]";
                Dictionary<string, object?> metadata = new(StringComparer.Ordinal);
                context.Fields(field, "__identifier __type __value defUid", "__tile", "realEditorValues", metadata);
                string kind = FieldKind(RequiredText(field, "__type")), name = RequiredText(field, "__identifier"); CheckName(name, names);
                int uid = context.Int(context.Required(field, "defUid"), "SCN2D015");
                if (!definitions.TryGetValue(uid, out FieldDefinition? definition) || definition.Name != name || definition.Kind != kind)
                { context.Fail("SCN2D015", "Field instance does not match its owning definition."); }
                OptionalRect(field, "__tile");
                JsonElement value = context.Required(field, "__value"); object? mapped;
                if (value.ValueKind == JsonValueKind.Null)
                {
                    if (!definition.Nullable) { context.Fail("SCN2D016", "A non-nullable field is null."); }
                    mapped = null;
                }
                else switch (kind)
                {
                    case "Int":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _)) { context.Fail("SCN2D016", "Int fields require an Int64 integer."); }
                        mapped = value.GetInt64(); break;
                    case "Float":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) || !double.IsFinite(number)) { context.Fail("SCN2D016", "Float fields require a finite number."); }
                        mapped = value.GetDouble(); break;
                    case "Bool": mapped = context.Boolean(value); break;
                    default:
                        if (value.ValueKind != JsonValueKind.String) { context.Fail("SCN2D016", "Text, path and color fields require a string."); }
                        string text = value.GetString()!;
                        if (kind == "Color")
                        {
                            if (text.Length != 7 || !text.StartsWith('#') || !Color.TryParse(text, out _)) { context.Fail("SCN2D016", "LDtk colors require #RRGGBB."); }
                            Color.TryParse(text, out Color color); mapped = color;
                        }
                        else if (kind == "FilePath" && text.Length > 0)
                        { string file = context.Resolve(projectFile, text); context.RequireFile(file); mapped = context.Relative(file); }
                        else { mapped = text; }
                        break;
                }
                properties.Add(name, mapped);
                if (metadata.Count > 0) { fieldMetadata.Add(name, metadata); }
            }
            if (fieldMetadata.Count > 0) { properties["$FieldMetadata"] = fieldMetadata; }
            conventions.Validate(properties); context.Path = path; return properties;
        }

        private void ParseLevels(JsonElement values, string layout, IReadOnlyDictionary<string, object?> world, string path)
        {
            int index = 0; float linearX = 0, linearY = 0;
            foreach (JsonElement reference in context.Array(values))
            {
                context.File = projectFile; context.Path = $"{path}[{index++}]";
                if (levels.Count >= 4096) { context.Fail("SCN2D013", "Level count exceeds the core limit."); }
                context.Object(reference);
                int uid = Uid(reference); string iid = Iid(reference); JsonElement level = reference;
                string referencePath = context.Path;
                Dictionary<string, object?>? referenceProperties = null;
                if (externalLevels)
                {
                    referenceProperties = LevelProperties(reference);
                    if (context.Required(reference, "layerInstances").ValueKind != JsonValueKind.Null) { context.Fail("SCN2D002", "External levels require null inline layerInstances."); }
                    string file = context.Resolve(projectFile, RequiredText(reference, "externalRelPath"));
                    if (!externalFiles.Add(file)) { context.Fail("SCN2D010", "External levels cannot cycle or share a payload file."); }
                    level = context.Load(file); context.Object(level); Header(level);
                    if (context.Int(context.Required(level, "uid")) != uid || RequiredText(level, "iid") != iid)
                    { context.Fail("SCN2D015", "External level identity differs from the project reference."); }
                    NullOnly(level, "externalRelPath", "SCN2D010");
                    foreach (string member in new[] { "identifier", "pxWid", "pxHei", "worldX", "worldY", "worldDepth" })
                    {
                        if (context.Required(reference, member).GetRawText() != context.Required(level, member).GetRawText())
                        { context.Fail("SCN2D015", "External level placement or dimensions conflict with its project reference."); }
                    }
                }
                else { NullOnly(level, "externalRelPath", "SCN2D010"); }
                string levelPath = context.Path;
                Dictionary<string, object?> properties = LevelProperties(level);
                if (referenceProperties is not null) { properties["$ProjectReference"] = referenceProperties; }
                int width = Positive(level, "pxWid"), height = Positive(level, "pxHei");
                DrawPoint placement = layout is "Free" or "GridVania" ? new(context.Number(context.Required(level, "worldX")), context.Number(context.Required(level, "worldY"))) : new(linearX, linearY);
                if (layout == "LinearHorizontal") { linearX += width; }
                if (layout == "LinearVertical") { linearY += height; }
                properties["$DefinitionUid"] = uid; properties["$Iid"] = iid; properties["$SourceName"] = RequiredText(level, "identifier");
                properties["$World"] = world; Preserve(level, properties, "worldDepth");
                List<TileLayer2DModel> layers = new(); List<Scene2DEntity> entities = new(); List<TilePromotion2D> promotions = new();
                JsonElement sourceLayers = context.Required(level, "layerInstances"); _ = context.Array(sourceLayers);
                int grid = sourceLayers.GetArrayLength() == 0 ? 1 : Positive(sourceLayers[0], "__gridSize");
                if (width % grid != 0 || height % grid != 0) { context.Fail("SCN2D004", "Level dimensions must align to its uniform grid."); }
                HashSet<int> layerIds = new();
                for (int layerIndex = sourceLayers.GetArrayLength() - 1; layerIndex >= 0; layerIndex--)
                {
                    context.Path = $"{levelPath}.layerInstances[{layerIndex}]";
                    layers.Add(ParseLayer(sourceLayers[layerIndex], uid, grid, layers.Count, layerIds, entities, promotions));
                }
                context.Path = levelPath;
                TileMap2DModel map = new(new(grid, grid), atlases.Values.Select(atlas => atlas.Set), layers, new(0, 0, width / grid, height / grid), properties: properties);
                levels.Add(new(iid, map, placement, entities, promotions, properties));
                context.File = projectFile; context.Path = referencePath;
            }
        }

        private Dictionary<string, object?> LevelProperties(JsonElement level)
        {
            Dictionary<string, object?> properties = Fields(context.Required(level, "fieldInstances"), levelFields);
            context.Fields(level, "__bgPos __header__ bgPos bgRelPath externalRelPath fieldInstances identifier iid layerInstances pxHei pxWid uid worldDepth worldX worldY",
                "__bgColor __neighbours __smartColor bgColor", "bgPivotX bgPivotY useAutoIdentifier", properties);
            Header(level); NullOnly(level, "bgRelPath"); NullOnly(level, "__bgPos"); NullOnly(level, "bgPos");
            return properties;
        }

        private TileLayer2DModel ParseLayer(JsonElement value, int levelUid, int grid, int order, HashSet<int> ids,
            List<Scene2DEntity> entities, List<TilePromotion2D> promotions)
        {
            context.CountLayer(); Dictionary<string, object?> properties = new(StringComparer.Ordinal);
            context.Fields(value, "__cHei __cWid __gridSize __identifier __opacity __pxTotalOffsetX __pxTotalOffsetY __tilesetDefUid __tilesetRelPath __type autoLayerTiles entityInstances gridTiles iid intGrid intGridCsv layerDefUid levelId overrideTilesetUid pxOffsetX pxOffsetY visible", editor: "optionalRules seed");
            int uid = context.Int(context.Required(value, "layerDefUid"), "SCN2D015");
            if (!layerDefinitions.TryGetValue(uid, out LayerDefinition? definition) || !ids.Add(uid)) { context.Fail("SCN2D015", "Layer UID is duplicate or unresolved within the level."); }
            string type = LayerType(RequiredText(value, "__type")), iid = Iid(value);
            if (context.Int(context.Required(value, "levelId")) != levelUid || type != definition.Type || RequiredText(value, "__identifier") != definition.Name)
            { context.Fail("SCN2D015", "Layer instance identity/type does not match its owner and definition."); }
            int sourceGrid = Positive(value, "__gridSize"), width = Positive(value, "__cWid"), height = Positive(value, "__cHei");
            if (sourceGrid != grid || definition.Grid != grid) { context.Fail("SCN2D004", "All layers within one level require the same grid."); }
            long count = (long)width * height; context.CountCells(count);
            float opacity = context.Number(context.Required(value, "__opacity"));
            if (opacity < 0 || opacity > 1) { context.Fail("SCN2D014", "Layer opacity must be in [0,1]."); }
            DrawPoint offset = new(context.Number(context.Required(value, "__pxTotalOffsetX")), context.Number(context.Required(value, "__pxTotalOffsetY")));
            properties["$SourceName"] = definition.Name; properties["$DefinitionUid"] = uid; properties["$Iid"] = iid; properties["$LayerType"] = type;
            properties["$Definition"] = definition.Properties; Preserve(value, properties, "pxOffsetX", "pxOffsetY");
            EmptyOrNull(value, "intGrid");
            JsonElement csv = context.Required(value, "intGridCsv"); _ = context.Array(csv);
            if (type == "IntGrid")
            {
                if (csv.GetArrayLength() != count) { context.Fail("SCN2D005", "IntGrid CSV length must equal the layer cell count."); }
                int[] data = new int[(int)count]; int index = 0;
                foreach (JsonElement number in csv.EnumerateArray())
                {
                    int item = context.Int(number, "SCN2D016");
                    if (item != 0 && !definition.IntGridValues.Contains(item)) { context.Fail("SCN2D016", "IntGrid references an undefined value."); }
                    data[index++] = item;
                }
                properties["$IntGrid"] = Array.AsReadOnly(data); properties["$IntGridDefinitions"] = definition.Properties;
            }
            else if (csv.GetArrayLength() != 0) { context.Fail("SCN2D002", "Only IntGrid layers may contain IntGrid CSV."); }
            int? setUid = NullableInt(value, "__tilesetDefUid"), overrideUid = NullableInt(value, "overrideTilesetUid");
            Atlas? atlas = null;
            if (setUid is not null && !atlases.TryGetValue(setUid.Value, out atlas)) { context.Fail("SCN2D006", "Layer instance references an absent tileset."); }
            if (setUid != (overrideUid ?? definition.TilesetUid)) { context.Fail("SCN2D015", "Layer tileset cache conflicts with its definition or override."); }
            if (atlas is not null)
            {
                string path = context.Resolve(projectFile, RequiredText(value, "__tilesetRelPath"));
                if (!string.Equals(path, atlas.File, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) { context.Fail("SCN2D010", "Layer atlas path conflicts with its tileset definition."); }
                if (atlas.Grid != grid) { context.Fail("SCN2D004", "Atlas tiles must match their destination layer grid."); }
            }
            else { NullOnly(value, "__tilesetRelPath", "SCN2D010"); }
            JsonElement tiles = context.Required(value, type == "Tiles" ? "gridTiles" : "autoLayerTiles"); _ = context.Array(tiles);
            EmptyOrNull(value, type == "Tiles" ? "autoLayerTiles" : "gridTiles", "SCN2D002");
            if (type == "Entities" && tiles.GetArrayLength() != 0) { context.Fail("SCN2D002", "Entity layers cannot contain tiles."); }
            List<TileChunk2D> chunks = new();
            if (type != "Entities")
            {
                context.CountChunk(); TileCell2D[] cells = new TileCell2D[(int)count]; string path = context.Path; int index = 0;
                foreach (JsonElement tile in tiles.EnumerateArray())
                {
                    context.Path = $"{path}.{(type == "Tiles" ? "gridTiles" : "autoLayerTiles")}[{index++}]";
                    context.Fields(tile, "a f px src t", editor: "d"); _ = context.Required(tile, "a"); context.Expect(tile, "a", 1);
                    if (atlas is null) { context.Fail("SCN2D006", "A tile requires an atlas."); }
                    int id = context.Int(context.Required(tile, "t"), "SCN2D006"), flip = context.Int(context.Required(tile, "f"));
                    if (id < 0 || id >= atlas.Set.Tiles.Count) { context.Fail("SCN2D006", "Tile ID is outside its tileset."); }
                    if (flip < 0 || flip > 3) { context.Fail("SCN2D004", "Only LDtk horizontal/vertical flip bits are supported."); }
                    (int x, int y) = IntPair(context.Required(tile, "px"));
                    if (x % grid != 0 || y % grid != 0) { context.Fail("SCN2D004", "Unsnapped tiles are outside the static cell contract."); }
                    x /= grid; y /= grid;
                    if (x < 0 || y < 0 || x >= width || y >= height) { context.Fail("SCN2D005", "Tile position is outside layer bounds."); }
                    (int sx, int sy) = IntPair(context.Required(tile, "src"));
                    DrawRect expected = atlas.Set.Tiles[id].SourceRect;
                    if (sx != expected.X || sy != expected.Y) { context.Fail("SCN2D007", "Tile source position does not match its local tile ID."); }
                    int cell = y * width + x;
                    if (cells[cell].TileId != 0) { context.Fail("SCN2D004", "Stacked tiles cannot be represented by one static cell."); }
                    cells[cell] = new(atlas.First + id, (TileFlip2D)flip);
                }
                context.Path = path; chunks.Add(new(default, width, height, cells));
            }
            JsonElement sourceEntities = context.Required(value, "entityInstances"); _ = context.Array(sourceEntities);
            if (type != "Entities" && sourceEntities.GetArrayLength() > 0) { context.Fail("SCN2D002", "Only Entity layers may contain entities."); }
            int entityIndex = 0; string layerPath = context.Path;
            foreach (JsonElement entity in sourceEntities.EnumerateArray())
            {
                context.Path = $"{layerPath}.entityInstances[{entityIndex}]";
                Scene2DEntity mapped = ParseEntity(entity, Id(uid), entityIndex++); entities.Add(mapped);
                if (mapped.Role == "Promote") { promotions.Add(conventions.Promotion(mapped.Properties)); }
            }
            context.Path = layerPath;
            return new(Id(uid), chunks, order, context.Boolean(context.Required(value, "visible")), offset, opacity, properties: properties);
        }

        private Scene2DEntity ParseEntity(JsonElement value, string layer, int order)
        {
            context.CountEntity(); context.Object(value);
            int uid = context.Int(context.Required(value, "defUid"), "SCN2D015");
            if (!entityDefinitions.TryGetValue(uid, out EntityDefinition? definition)) { context.Fail("SCN2D015", "Entity definition is absent."); }
            Dictionary<string, object?> properties = Fields(context.Required(value, "fieldInstances"), definition.Fields);
            context.Fields(value, "__identifier __pivot defUid fieldInstances height iid px width", "__grid __smartColor __tags __tile", "__worldX __worldY", properties);
            string iid = Iid(value);
            if (RequiredText(value, "__identifier") != definition.Name) { context.Fail("SCN2D015", "Entity identifier differs from its definition."); }
            OptionalRect(value, "__tile");
            (int x, int y) = IntPair(context.Required(value, "px"));
            JsonElement pivot = context.Required(value, "__pivot"); _ = context.Array(pivot);
            if (pivot.GetArrayLength() != 2) { context.Fail("SCN2D005", "Entity pivot requires two coordinates."); }
            float px = context.Number(pivot[0]), py = context.Number(pivot[1]);
            if (px < 0 || px > 1 || py < 0 || py > 1) { context.Fail("SCN2D014", "Entity pivot must be normalized."); }
            DrawSize size = new(Positive(value, "width"), Positive(value, "height"));
            string role = conventions.Text(properties, "CernealaRole", "Metadata");
            string shape = role == "Collider" ? conventions.Text(properties, "ColliderShape", "Box") : "Box";
            string points = shape is "Polygon" or "Polyline" ? conventions.Text(properties, "ColliderPoints", "") : "";
            if (points.Length > 393_216 || points.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > Math.Min(context.Options.MaxPoints, 4096))
            { context.Fail("SCN2D013", "Entity point text exceeds the import/core limit."); }
            properties["$DefinitionUid"] = uid; properties["$SourceName"] = definition.Name; properties["$Definition"] = definition.Properties;
            properties["$SourcePx"] = new DrawPoint(x, y);
            return new(iid, layer, new(x - size.Width * px, y - size.Height * py), size, shape, points,
                pivot: new(px, py), role: role, colliders: conventions.Colliders(iid, role, shape, size, points, properties), order: order, properties: properties);
        }

        private void OptionalRect(JsonElement owner, string name)
        {
            if (!owner.TryGetProperty(name, out JsonElement rect) || rect.ValueKind == JsonValueKind.Null) { return; }
            context.Fields(rect, "h tilesetUid w x y");
            int uid = context.Int(context.Required(rect, "tilesetUid"), "SCN2D006");
            if (!atlases.TryGetValue(uid, out Atlas? atlas)) { context.Fail("SCN2D006", "Tile rectangle references an absent tileset."); }
            int x = context.Int(context.Required(rect, "x")), y = context.Int(context.Required(rect, "y"));
            int w = Positive(rect, "w", "SCN2D007"), h = Positive(rect, "h", "SCN2D007");
            if (x < 0 || y < 0 || (long)x + w > atlas.Size.Width || (long)y + h > atlas.Size.Height) { context.Fail("SCN2D007", "Tile metadata rectangle is outside its atlas."); }
        }
        private int Uid(JsonElement value)
        { int uid = Positive(value, "uid", "SCN2D015"); if (!uids.Add(uid)) { context.Fail("SCN2D015", "Definition/level UIDs must be unique within the project."); } return uid; }
        private string Iid(JsonElement value)
        { string iid = RequiredText(value, "iid"); if (!Guid.TryParse(iid, out Guid identity) || !iids.Add(identity)) { context.Fail("SCN2D015", "Instance IIDs must be valid and globally unique."); } return iid; }
        private void CheckName(string name, HashSet<string> names)
        { if (name.Length == 0 || name.StartsWith('$') || !names.Add(name)) { context.Fail("SCN2D015", "Field names must be unique and outside reserved '$' provenance."); } }
        private string RequiredText(JsonElement value, string name) => context.Text(context.Required(value, name));
        private int Positive(JsonElement value, string name, string code = "SCN2D005")
        { int number = context.Int(context.Required(value, name), code); if (number <= 0) { context.Fail(code, $"'{name}' must be positive."); } return number; }
        private int? NullableInt(JsonElement value, string name) => !value.TryGetProperty(name, out JsonElement item) || item.ValueKind == JsonValueKind.Null ? null : context.Int(item);
        private (int X, int Y) IntPair(JsonElement value)
        { _ = context.Array(value); if (value.GetArrayLength() != 2) { context.Fail("SCN2D005", "A position requires two coordinates."); } return (context.Int(value[0]), context.Int(value[1])); }
        private string Layout(JsonElement value)
        { string layout = RequiredText(value, "worldLayout"); if (layout is not ("Free" or "GridVania" or "LinearHorizontal" or "LinearVertical")) { context.Fail("SCN2D004", "Unsupported world layout."); } return layout; }
        private string LayerType(string type)
        { if (type is not ("Tiles" or "AutoLayer" or "IntGrid" or "Entities")) { context.Fail("SCN2D004", "Unsupported LDtk layer type."); } return type; }
        private string FieldKind(string kind)
        { if (kind is not ("Int" or "Float" or "String" or "Multilines" or "Bool" or "Color" or "FilePath")) { context.Fail("SCN2D004", $"Field kind '{kind}' is outside v1."); } return kind; }
        private void NullOnly(JsonElement value, string name, string code = "SCN2D004")
        { if (value.TryGetProperty(name, out JsonElement item) && item.ValueKind != JsonValueKind.Null) { context.Fail(code, $"'{name}' must be absent or null in v1."); } }
        private void EmptyOrNull(JsonElement value, string name, string code = "SCN2D004")
        { if (value.TryGetProperty(name, out JsonElement item) && item.ValueKind != JsonValueKind.Null && (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() > 0)) { context.Fail(code, $"'{name}' must be empty in this layer/subset."); } }
        private static string Id(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Preserve(JsonElement value, Dictionary<string, object?> properties, params string[] names)
        { foreach (string name in names) { if (value.TryGetProperty(name, out JsonElement field)) { properties["$" + name] = field.Clone(); } } }
        private sealed record Atlas(TileSet2D Set, int Grid, int Columns, int Rows, int Padding, int Spacing, int First, string File, DrawSize Size);
        private sealed record LayerDefinition(string Name, string Type, int Grid, int? TilesetUid, Dictionary<string, object?> Properties, HashSet<int> IntGridValues);
        private sealed record EntityDefinition(string Name, Dictionary<int, FieldDefinition> Fields, Dictionary<string, object?> Properties);
        private sealed record FieldDefinition(string Name, string Kind, bool Nullable);
    }
}
