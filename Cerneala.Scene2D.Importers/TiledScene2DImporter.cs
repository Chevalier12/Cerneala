using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Scene2D.Importers;

public static class TiledScene2DImporter
{
    public static Scene2DImportResult Import(string filePath, Scene2DImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using ImportContext context = new(options ?? new());
        Scene2DDocument? document = null;
        try
        {
            string file = context.Initialize(filePath);
            document = new Parser(context).Parse(context.Load(file));
        }
        catch (Exception error) when (error is ImportFailure or JsonException or IOException or UnauthorizedAccessException or ArgumentException or OverflowException)
        { context.Record(error); }
        return new(document, context.Diagnostics.Complete());
    }

    private sealed class Parser(ImportContext context)
    {
        private readonly List<TileSet2D> sets = new();
        private readonly List<TileLayer2DModel> layers = new();
        private readonly List<Scene2DEntity> entities = new();
        private readonly List<TilePromotion2D> promotions = new();
        private readonly Dictionary<string, Scene2DAsset> assets = new(StringComparer.Ordinal);
        private readonly HashSet<int> layerIds = new();
        private int tileWidth, tileHeight;
        private long definitions;
        private readonly ImportConventions conventions = new(context);
        private bool infinite;

        internal Scene2DDocument Parse(JsonElement map)
        {
            context.Object(map);
            Version(map);
            Dictionary<string, object?> properties = Properties(map);
            context.Fields(map, "height infinite layers orientation parallaxoriginx parallaxoriginy properties renderorder tileheight tilesets tilewidth type version width",
                "backgroundcolor class tiledversion", "compressionlevel editorsettings nextlayerid nextobjectid", properties);
            context.Expect(map, "type", "map");
            _ = context.Required(map, "orientation");
            context.Expect(map, "orientation", "orthogonal");
            context.Expect(map, "renderorder", "right-down");
            Preserve(map, properties, "parallaxoriginx", "parallaxoriginy");
            tileWidth = Positive(map, "tilewidth");
            tileHeight = Positive(map, "tileheight");
            infinite = context.Boolean(map, "infinite");
            TileMapBounds2D? bounds = infinite ? null : new(0, 0, Positive(map, "width"), Positive(map, "height"));
            int index = 0;
            foreach (JsonElement set in context.Array(context.Required(map, "tilesets")))
            {
                context.Path = $"$.tilesets[{index++}]";
                if (sets.Count >= 4096) { context.Fail("SCN2D013", "Tileset count exceeds the core limit."); }
                ParseSet(set);
            }
            context.Path = "$.layers";
            ParseLayers(context.Required(map, "layers"), default, 1, Color.White, true, Array.Empty<object>());
            context.Path = "$";
            TileMap2DModel model = new(new(tileWidth, tileHeight), sets, layers, bounds, properties: properties);
            Scene2DLevel level = new(context.Relative(context.File), model, entities: entities, promotions: promotions, properties: properties);
            return new([level], assets.Values, properties: new Dictionary<string, object?>
            { ["$Format"] = "Tiled", ["$Version"] = "1.11" }, validationOptions: context.ValidationOptions);
        }

        private void Version(JsonElement value)
        {
            context.Object(value);
            if (!value.TryGetProperty("version", out JsonElement version) || version.ValueKind != JsonValueKind.String || version.GetString() != "1.11")
            { context.Path += ".version"; context.Fail("SCN2D003", "Only Tiled JSON format version 1.11 is supported."); }
        }

        private int Positive(JsonElement value, string name, string code = "SCN2D005")
        {
            int number = context.Int(context.Required(value, name), code);
            if (number <= 0) { context.Path += "." + name; context.Fail(code, $"'{name}' must be positive."); }
            return number;
        }

        private void ParseSet(JsonElement reference)
        {
            context.Object(reference);
            string mapFile = context.File, referencePath = context.Path;
            int first = Positive(reference, "firstgid", "SCN2D006");
            JsonElement set = reference;
            if (reference.TryGetProperty("source", out JsonElement source))
            {
                context.Fields(reference, "firstgid source");
                string external = context.Resolve(mapFile, context.Text(source));
                set = context.Load(external);
                Version(set);
                if (set.TryGetProperty("source", out _)) { context.Fail("SCN2D010", "External tilesets cannot chain or cycle source references."); }
            }
            else if (set.TryGetProperty("version", out _)) { Version(set); }
            Dictionary<string, object?> properties = Properties(set);
            context.Fields(set, "columns fillmode firstgid grid image imageheight imagewidth margin name objectalignment properties spacing tilecount tileheight tileoffset tilerendersize tiles tilewidth type version",
                "backgroundcolor class tiledversion", "editorsettings terrains transformations wangsets", properties);
            context.Expect(set, "type", "tileset");
            context.Expect(set, "fillmode", "stretch");
            context.Expect(set, "tilerendersize", "tile");
            if (context.Text(set, "objectalignment", "unspecified") is not
                ("unspecified" or "topleft" or "top" or "topright" or "left" or "center" or "right" or "bottomleft" or "bottom" or "bottomright"))
            { context.Fail("SCN2D004", "Unknown tile object alignment."); }
            Preserve(set, properties, "objectalignment"); // Tile objects are rejected; alignment is provenance only.
            if (set.TryGetProperty("tileoffset", out JsonElement offset))
            {
                context.Fields(offset, "x y"); context.Expect(offset, "x", 0); context.Expect(offset, "y", 0);
            }
            if (set.TryGetProperty("grid", out JsonElement grid))
            {
                context.Fields(grid, "orientation", editor: "height width"); context.Expect(grid, "orientation", "orthogonal");
            }
            if (Positive(set, "tilewidth") != tileWidth || Positive(set, "tileheight") != tileHeight)
            { context.Fail("SCN2D004", "Atlas tiles must match the map's destination grid."); }
            int count = Positive(set, "tilecount", "SCN2D007"), columns = Positive(set, "columns", "SCN2D007");
            definitions += count;
            if (definitions > Math.Min(context.Options.MaxCells, 1_048_576)) { context.Fail("SCN2D013", "Tile definitions exceed the import/core budget."); }
            if ((long)first + count - 1 > 0x0fffffff) { context.Fail("SCN2D006", "Tile IDs overlap Tiled's reserved flag bits."); }
            int margin = context.Int(set, "margin", 0, "SCN2D007"), spacing = context.Int(set, "spacing", 0, "SCN2D007");
            if (margin < 0 || spacing < 0) { context.Fail("SCN2D007", "Atlas margin and spacing cannot be negative."); }
            DrawSize size = new(Positive(set, "imagewidth", "SCN2D007"), Positive(set, "imageheight", "SCN2D007"));
            long atlasColumns = ((long)size.Width - 2L * margin + spacing) / (tileWidth + (long)spacing);
            long atlasRows = ((long)size.Height - 2L * margin + spacing) / (tileHeight + (long)spacing);
            if (atlasColumns != columns || atlasRows <= 0 || count > atlasColumns * atlasRows)
            { context.Fail("SCN2D007", "Declared atlas columns/tile count do not fit the image grid."); }
            string atlasFile = context.Resolve(context.File, context.Text(context.Required(set, "image")));
            context.RequireFile(atlasFile);
            string atlasPath = context.Relative(atlasFile);
            ResourceId<ImageResource> resource = new(atlasPath);
            if (assets.TryGetValue(atlasPath, out Scene2DAsset? existing) && existing.Size != size)
            { context.Fail("SCN2D007", "The same atlas has conflicting declared dimensions."); }
            assets.TryAdd(atlasPath, new(resource, atlasPath, size));
            Dictionary<int, (Dictionary<string, object?> Properties, List<TileColliderDescriptor2D> Colliders)> overrides = new();
            string setPath = context.Path;
            if (set.TryGetProperty("tiles", out JsonElement tiles))
            {
                int index = 0;
                foreach (JsonElement tile in context.Array(tiles))
                {
                    context.Path = $"{setPath}.tiles[{index++}]";
                    Dictionary<string, object?> tileProperties = Properties(tile);
                    context.Fields(tile, "id objectgroup properties", "class type", "probability terrain", tileProperties);
                    int id = context.Int(context.Required(tile, "id"), "SCN2D006");
                    if (id < 0 || id >= count) { context.Fail("SCN2D006", "Local tile ID is outside the atlas tile count."); }
                    if (overrides.ContainsKey(id)) { context.Fail("SCN2D015", "Duplicate local tile ID."); }
                    List<TileColliderDescriptor2D> colliders = new();
                    if (tile.TryGetProperty("objectgroup", out JsonElement group))
                    {
                        Dictionary<string, object?> groupProperties = Properties(group);
                        LayerFields(group, groupProperties);
                        context.Expect(group, "type", "objectgroup");
                        DrawPoint groupOffset = new(context.Number(group, "offsetx"), context.Number(group, "offsety"));
                        int objectIndex = 0;
                        HashSet<string> objectIds = new(StringComparer.Ordinal);
                        foreach (JsonElement item in context.Array(context.Required(group, "objects")))
                        {
                            context.Path = $"{setPath}.tiles[{index - 1}].objectgroup.objects[{objectIndex++}]";
                            Scene2DEntity entity = ParseObject(item, "tile", objectIndex - 1, "Collider", 4096 - colliders.Count);
                            if (!objectIds.Add(entity.Id)) { context.Fail("SCN2D015", "Tile collision object IDs must be unique within their object group."); }
                            Matrix3x2 placement = Matrix3x2.CreateRotation(entity.Rotation) * Matrix3x2.CreateTranslation(
                                entity.Position.X + groupOffset.X, entity.Position.Y + groupOffset.Y);
                            foreach (TileColliderDescriptor2D descriptor in entity.Colliders)
                            {
                                colliders.Add(new(descriptor.Shape, descriptor.LocalTransform * placement, descriptor.Width,
                                    descriptor.Height, descriptor.Radius, descriptor.Points, descriptor.OffsetX, descriptor.OffsetY,
                                    descriptor.CollisionLayer, descriptor.CollisionMask, descriptor.IsTrigger, descriptor.DebugIdentity, descriptor.Properties));
                            }
                            tileProperties["$Object" + entity.Id] = entity;
                        }
                        tileProperties["$ObjectGroup"] = groupProperties;
                    }
                    overrides.Add(id, (tileProperties, colliders));
                }
            }
            context.Path = setPath;
            TileDefinition2D[] result = new TileDefinition2D[count];
            for (int local = 0; local < count; local++)
            {
                float x = checked(margin + (long)(local % columns) * (tileWidth + (long)spacing));
                float y = checked(margin + (long)(local / columns) * (tileHeight + (long)spacing));
                overrides.TryGetValue(local, out var data);
                result[local] = new(first + local, new(x, y, tileWidth, tileHeight), data.Properties, data.Colliders);
            }
            properties["$SourceName"] = context.Text(set, "name");
            properties["$SourceFile"] = context.Relative(context.File);
            sets.Add(new(first.ToString(CultureInfo.InvariantCulture), resource, result, properties: properties));
            context.File = mapFile;
            context.Path = referencePath;
        }

        private void LayerFields(JsonElement layer, Dictionary<string, object?> properties)
        {
            string specific = context.Text(layer, "type", "objectgroup") switch
            {
                "tilelayer" => "chunks compression data encoding ",
                "objectgroup" => "draworder objects ",
                "group" => "layers ",
                _ => ""
            };
            context.Fields(layer, specific + "height id mode name offsetx offsety opacity parallaxx parallaxy properties tintcolor type visible width x y",
                "class", "locked startx starty", properties);
            context.Expect(layer, "x", 0); context.Expect(layer, "y", 0);
            context.Expect(layer, "parallaxx", 1); context.Expect(layer, "parallaxy", 1);
            context.Expect(layer, "mode", "normal");
        }

        private void ParseLayers(JsonElement values, DrawPoint parentOffset, float parentOpacity, Color parentTint, bool parentVisible, object[] ancestors)
        {
            string path = context.Path;
            int index = 0;
            foreach (JsonElement layer in context.Array(values))
            {
                context.Path = $"{path}[{index++}]";
                context.CountLayer();
                Dictionary<string, object?> properties = Properties(layer);
                LayerFields(layer, properties);
                int numericId = Positive(layer, "id", "SCN2D015");
                if (!layerIds.Add(numericId)) { context.Fail("SCN2D015", "Layer IDs, including groups, must be unique."); }
                string id = numericId.ToString(CultureInfo.InvariantCulture), type = context.Text(context.Required(layer, "type"));
                properties["$SourceName"] = context.Text(layer, "name");
                properties["$GroupAncestors"] = Array.AsReadOnly(ancestors);
                properties["$LayerType"] = type;
                DrawPoint offset = new(parentOffset.X + context.Number(layer, "offsetx"), parentOffset.Y + context.Number(layer, "offsety"));
                float opacity = Opacity(layer) * parentOpacity;
                bool visible = parentVisible & context.Boolean(layer, "visible", true);
                Color tint = Multiply(parentTint, ParseColor(context.Text(layer, "tintcolor", "#ffffff")));
                string layerPath = context.Path;
                if (type == "group")
                {
                    properties["$Id"] = id;
                    context.Path = layerPath + ".layers";
                    ParseLayers(context.Required(layer, "layers"), offset, opacity, tint, visible, [.. ancestors, properties]);
                    continue;
                }
                List<TileChunk2D> chunks = new();
                if (type == "tilelayer")
                {
                    if (infinite)
                    {
                        if (layer.TryGetProperty("data", out _)) { context.Fail("SCN2D002", "Infinite tile layers require chunks, not data."); }
                        int chunkIndex = 0;
                        foreach (JsonElement chunk in context.Array(context.Required(layer, "chunks")))
                        {
                            context.Path = $"{layerPath}.chunks[{chunkIndex++}]";
                            context.Fields(chunk, "data height width x y");
                            chunks.Add(ParseChunk(chunk, layer, new(context.Int(context.Required(chunk, "x")), context.Int(context.Required(chunk, "y")))));
                        }
                    }
                    else
                    {
                        if (layer.TryGetProperty("chunks", out _)) { context.Fail("SCN2D002", "Finite tile layers cannot declare chunks."); }
                        chunks.Add(ParseChunk(layer, layer, default));
                    }
                }
                else if (type == "objectgroup")
                {
                    string drawOrder = context.Text(layer, "draworder", "topdown");
                    if (drawOrder is not ("topdown" or "index")) { context.Fail("SCN2D004", "Unsupported object draw order."); }
                    properties["$DrawOrder"] = drawOrder;
                    List<Scene2DEntity> objects = new();
                    int objectIndex = 0;
                    foreach (JsonElement item in context.Array(context.Required(layer, "objects")))
                    {
                        context.Path = $"{layerPath}.objects[{objectIndex++}]";
                        objects.Add(ParseObject(item, id, objectIndex - 1, "Metadata"));
                    }
                    IEnumerable<Scene2DEntity> ordered = drawOrder == "topdown" ? objects.OrderBy(item => item.Position.Y) : objects;
                    int order = 0;
                    foreach (Scene2DEntity item in ordered)
                    {
                        entities.Add(new(item.Id, item.LayerId, item.Position, item.Size, item.Shape, item.Points, item.Rotation,
                            item.Pivot, item.Role, item.Colliders, order++, item.IsVisible, item.Opacity, item.Properties));
                        if (item.Role == "Promote") { promotions.Add(conventions.Promotion(item.Properties)); }
                    }
                }
                else { context.Fail("SCN2D004", $"Layer type '{type}' is outside v1."); }
                context.Path = layerPath;
                layers.Add(new(id, chunks, layers.Count, visible, offset, opacity, tint, properties: properties));
            }
            context.Path = path;
        }

        private TileChunk2D ParseChunk(JsonElement chunk, JsonElement layer, TileCoordinate2D origin)
        {
            context.CountChunk();
            int width = Positive(chunk, "width"), height = Positive(chunk, "height");
            long count = (long)width * height;
            context.CountCells(count);
            string encoding = context.Text(layer, "encoding"), compression = context.Text(layer, "compression");
            if (encoding is not ("" or "base64") || compression is not ("" or "zlib" or "gzip") || (encoding == "" && compression != ""))
            { context.Fail("SCN2D004", "Only numeric arrays and raw/zlib/gzip base64 tile data are supported."); }
            JsonElement data = context.Required(chunk, "data");
            TileCell2D[] cells = new TileCell2D[(int)count];
            if (encoding == "")
            {
                if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() != count)
                { context.Fail("SCN2D005", "Tile data length must equal width times height."); }
                int index = 0;
                foreach (JsonElement gid in data.EnumerateArray())
                {
                    if (gid.ValueKind != JsonValueKind.Number || !gid.TryGetUInt32(out _)) { context.Fail("SCN2D006", "GIDs must be unsigned 32-bit integers."); }
                    cells[index++] = Cell(gid.GetUInt32());
                }
            }
            else
            {
                byte[] encoded;
                try { encoded = Convert.FromBase64String(context.Text(data)); }
                catch (FormatException) { context.Fail("SCN2D002", "Invalid base64 tile data."); throw; }
                byte[] bytes = TileDataDecoder.Decode(context, encoded, compression, checked((int)count * 4));
                for (int index = 0; index < cells.Length; index++) { cells[index] = Cell(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * 4, 4))); }
            }
            return new(origin, width, height, cells);
        }

        private static TileCell2D Cell(uint gid) => new((int)(gid & 0x0fffffff),
            (TileFlip2D)((gid >> 31) | ((gid >> 29) & 2) | ((gid >> 27) & 4)));

        private Scene2DEntity ParseObject(JsonElement value, string layerId, int order, string defaultRole, int maxColliders = 4096)
        {
            context.CountEntity();
            Dictionary<string, object?> properties = Properties(value);
            context.Fields(value, "class ellipse height id name opacity point polygon polyline properties rotation type visible width x y");
            string id = Positive(value, "id", "SCN2D015").ToString(CultureInfo.InvariantCulture);
            string type = context.Text(value, "type"), legacyClass = context.Text(value, "class");
            if (type.Length > 0 && legacyClass.Length > 0 && type != legacyClass) { context.Fail("SCN2D015", "Object class and type conflict."); }
            properties["$SourceName"] = context.Text(value, "name");
            properties["$Class"] = type.Length > 0 ? type : legacyClass;
            properties["$SourceOrder"] = order;
            string role = conventions.Text(properties, "CernealaRole", defaultRole);
            DrawPoint position = new(context.Number(value, "x"), context.Number(value, "y"));
            DrawSize size = new(context.Number(value, "width"), context.Number(value, "height"));
            float rotation = context.Number(value, "rotation") * (MathF.PI / 180);
            bool ellipse = context.Boolean(value, "ellipse"), point = context.Boolean(value, "point");
            bool polygon = value.TryGetProperty("polygon", out JsonElement polygonPoints), polyline = value.TryGetProperty("polyline", out JsonElement polylinePoints);
            if ((ellipse ? 1 : 0) + (point ? 1 : 0) + (polygon ? 1 : 0) + (polyline ? 1 : 0) > 1)
            { context.Fail("SCN2D008", "An object cannot declare multiple geometries."); }
            string shape = ellipse ? "Ellipse" : point ? "Point" : polygon ? "Polygon" : polyline ? "Polyline" : "Box";
            string[] points = [];
            if (polygon || polyline)
            {
                List<string> tokens = new();
                foreach (JsonElement vertex in context.Array(polygon ? polygonPoints : polylinePoints))
                {
                    if (tokens.Count >= Math.Min(context.Options.MaxPoints, 4096)) { context.Fail("SCN2D013", "Shape point count exceeds its import/core budget."); }
                    context.Fields(vertex, "x y");
                    tokens.Add(context.Number(context.Required(vertex, "x")).ToString("R", CultureInfo.InvariantCulture) + "," +
                        context.Number(context.Required(vertex, "y")).ToString("R", CultureInfo.InvariantCulture));
                }
                points = tokens.ToArray();
            }
            string pointText = string.Join(' ', points);
            List<TileColliderDescriptor2D> colliders = conventions.Colliders(id, role, shape, size, pointText, properties, maxColliders);
            return new(id, layerId, position, size, shape, pointText, rotation, role: role, colliders: colliders,
                order: order, isVisible: context.Boolean(value, "visible", true), opacity: Opacity(value), properties: properties);
        }

        private Dictionary<string, object?> Properties(JsonElement owner)
        {
            context.Object(owner);
            Dictionary<string, object?> result = new(StringComparer.Ordinal);
            if (!owner.TryGetProperty("properties", out JsonElement properties)) { return result; }
            string path = context.Path;
            int index = 0;
            foreach (JsonElement property in context.Array(properties))
            {
                context.Path = $"{path}.properties[{index++}]";
                context.Fields(property, "name propertytype type value");
                string name = context.Text(context.Required(property, "name"));
                if (name.Length == 0 || name.StartsWith('$') || result.ContainsKey(name)) { context.Fail("SCN2D015", "Property names must be unique and cannot use reserved '$' provenance names."); }
                if (context.Text(property, "propertytype").Length != 0) { context.Fail("SCN2D004", "Custom property types are outside v1."); }
                JsonElement value = context.Required(property, "value");
                string kind = context.Text(property, "type", "string");
                object? mapped;
                switch (kind)
                {
                    case "string": mapped = PropertyString(value); break;
                    case "color": mapped = ParseColor(PropertyString(value)); break;
                    case "file":
                        string file = PropertyString(value);
                        if (file.Length > 0)
                        { string full = context.Resolve(context.File, file); context.RequireFile(full); file = context.Relative(full); }
                        mapped = file; break;
                    case "bool": mapped = context.Boolean(value); break;
                    case "int":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _)) { context.Fail("SCN2D016", "Integer properties require an Int64 integer."); }
                        mapped = value.GetInt64(); break;
                    case "float":
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) || !double.IsFinite(number)) { context.Fail("SCN2D016", "Float properties require a finite number."); }
                        mapped = value.GetDouble(); break;
                    default: context.Fail("SCN2D004", $"Property type '{kind}' is outside v1."); return result;
                }
                result.Add(name, mapped);
            }
            conventions.Validate(result);
            context.Path = path;
            return result;
        }

        private string PropertyString(JsonElement value)
        { if (value.ValueKind != JsonValueKind.String) { context.Fail("SCN2D016", "The property requires a string value."); } return value.GetString()!; }
        private float Opacity(JsonElement value)
        { float opacity = context.Number(value, "opacity", 1); if (opacity < 0 || opacity > 1) { context.Fail("SCN2D014", "Opacity must be in [0,1]."); } return opacity; }
        private Color ParseColor(string value)
        { if (value.Length is not (7 or 9) || !value.StartsWith('#') || !Color.TryParse(value, out Color color)) { context.Fail("SCN2D016", "Tiled colors require #RRGGBB or #AARRGGBB."); } Color.TryParse(value, out Color parsed); return parsed; }
        private static Color Multiply(Color left, Color right) => new(
            (byte)((left.R * right.R + 127) / 255), (byte)((left.G * right.G + 127) / 255),
            (byte)((left.B * right.B + 127) / 255), (byte)((left.A * right.A + 127) / 255));
        private static void Preserve(JsonElement owner, Dictionary<string, object?> properties, params string[] names)
        { foreach (string name in names) { if (owner.TryGetProperty(name, out JsonElement value)) { properties["$" + name] = value.Clone(); } } }
    }
}
