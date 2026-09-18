using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Scene2D.Packages;

// Closed, explicit wire types. No CLR type names, member discovery, activators,
// arbitrary-object JSON serialization or importer dependency belongs on this path.
internal static class PackageValueCodec
{
    internal const int MaximumDepth = 128;
    private const int Magic = 0x32565043; // CPV2, little endian; entity spatial headers replace the ID-only index.

    internal static byte[] Encode(object? value) => Encode(value, out _);

    internal static byte[] Encode(object? value, out long decodedDataCharge)
    {
        using MemoryStream stream = new();
        using BinaryWriter binary = new(stream, Encoding.UTF8, leaveOpen: true);
        binary.Write(Magic);
        Writer writer = new(binary);
        writer.Value(value, 0);
        decodedDataCharge = writer.DecodedDataCharge;
        return stream.ToArray();
    }

    internal static object? Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using MemoryStream stream = new(bytes, writable: false);
        using BinaryReader binary = new(stream, Encoding.UTF8, leaveOpen: true);
        try
        {
            if (binary.ReadInt32() != Magic) { throw Invalid("Unsupported payload format."); }
            object? result = new Reader(binary).Value(0);
            if (stream.Position != stream.Length) { throw Invalid("Trailing payload data."); }
            return result;
        }
        catch (Exception error) when (error is ArgumentException or OverflowException or EndOfStreamException or JsonException)
        {
            throw new InvalidDataException("Invalid scene payload.", error);
        }
    }

    private static InvalidDataException Invalid(string message) => new(message);

    private enum Tag : byte
    {
        Null, Boolean, Int32, Int64, UInt32, UInt64, Single, Double, String,
        Color, Point, Json, Reference, Properties, Bytes, Integers, IntegerList,
        Objects, ObjectList, Definition, TileSet, Collider, Grid, Placement,
        GridData, PlacementData, Entity, Promotion, Metadata, Block, Map, Level,
        EntityIndex, PromotionIndex, Index, Asset, Catalog, ChunkInfo, Spatial, EntityInfo,
        MetadataWithGridChunks, GridChunkMetadata
    }

    private sealed class Writer(BinaryWriter output)
    {
        private readonly Dictionary<object, int> references = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> pending = new(ReferenceEqualityComparer.Instance);

        // Admission accounting for this closed decoder, not a process/GC census.
        // Allowances include object/wrapper headers, eight-byte references,
        // spare hash capacity and the model constructors' retained copies.
        // Encoded buffers and temporary validation/parser work are not residency.
        // Keep these constructor charges and their ownership tests in sync when
        // extending the schema; do not substitute serialized byte lengths.
        private const long ObjectCharge = 256;
        internal long DecodedDataCharge { get; private set; }

        private void Charge(long bytes) => DecodedDataCharge = checked(DecodedDataCharge + bytes);
        private static long BufferCharge(long count, int elementBytes) => checked(64 + count * elementBytes);
        private static long TableCharge(long count) => checked(256 + count * 128);

        internal void Value(object? value, int depth)
        {
            if (depth > MaximumDepth) { throw new NotSupportedException("Payload nesting exceeds 128 levels."); }
            switch (value)
            {
                case null: Mark(Tag.Null); return;
                case bool item: Mark(Tag.Boolean); output.Write(item); return;
                case int item: Mark(Tag.Int32); output.Write(item); return;
                case long item: Mark(Tag.Int64); output.Write(item); return;
                case uint item: Mark(Tag.UInt32); output.Write(item); return;
                case ulong item: Mark(Tag.UInt64); output.Write(item); return;
                case float item: Mark(Tag.Single); output.Write(item); return;
                case double item: Mark(Tag.Double); output.Write(item); return;
                case string item: Mark(Tag.String); Text(item); return;
                case Color item: Mark(Tag.Color); Color(item); return;
                case DrawPoint item: Mark(Tag.Point); Point(item); return;
            }
            if (references.TryGetValue(value, out int id)) { Mark(Tag.Reference); output.Write(id); return; }
            if (!pending.Add(value)) { throw new NotSupportedException("Cyclic payload metadata is not supported."); }
            switch (value)
            {
                case SceneJsonValue2D item:
                    long rows = CountJsonRows(item.Value, 0);
                    string json = item.Value.GetRawText();
                    Mark(Tag.Json);
                    // The detached JSON owns UTF-8 content and a row database,
                    // not the temporary UTF-16 string used by Reader.Json.
                    Charge(BufferCharge(Encoding.UTF8.GetByteCount(json), 1) + BufferCharge(rows, 32));
                    Text(json, retained: false); break;
                case IReadOnlyDictionary<string, object?> items:
                    Mark(Tag.Properties); output.Write(items.Count);
                    Charge(TableCharge(items.Count));
                    foreach ((string key, object? item) in items) { Text(key); Value(item, depth + 1); }
                    break;
                case byte[] items:
                    Mark(Tag.Bytes); Charge(BufferCharge(items.Length, 1));
                    output.Write(items.Length); output.Write(items); break;
                case int[] items:
                    Mark(Tag.Integers); Integers(items); break;
                case IReadOnlyList<int> items:
                    Mark(Tag.IntegerList); Integers(items); break;
                case object?[] items:
                    Mark(Tag.Objects); Objects(items, depth); break;
                case IReadOnlyList<object?> items:
                    Mark(Tag.ObjectList); Objects(items, depth); break;
                case TileDefinition2D item:
                    Mark(Tag.Definition); output.Write(item.Id); Rect(item.SourceRect);
                    Charge(TableCharge(item.Properties.Count));
                    Value(item.Properties, depth + 1); Value(item.Collider, depth + 1); break;
                case TileSet2D item:
                    Mark(Tag.TileSet); Text(item.Id); Text(item.AtlasResourceId.Key); output.Write(item.Version);
                    Charge(TableCharge(item.Properties.Count) + BufferCharge(item.Tiles.Count, 8));
                    Value(item.Properties, depth + 1); Objects(item.Tiles, depth); break;
                case TileColliderDescriptor2D item:
                    Mark(Tag.Collider); output.Write((byte)item.Shape); Matrix(item.LocalTransform);
                    Charge(TableCharge(item.Properties.Count) + BufferCharge(2L * item.Vertices.Count, 8));
                    output.Write(item.Width); output.Write(item.Height); output.Write(item.Radius); Text(item.Points);
                    output.Write(item.OffsetX); output.Write(item.OffsetY); output.Write(item.CollisionLayer);
                    output.Write(item.CollisionMask); output.Write(item.IsTrigger);
                    Value(item.DebugIdentity, depth + 1); Value(item.Properties, depth + 1); break;
                case TileChunk2D item:
                    Mark(Tag.Grid); output.Write(item.Origin.X); output.Write(item.Origin.Y);
                    Charge(TableCharge(item.Properties.Count) + BufferCharge(item.Tiles.Count, 8));
                    output.Write(item.Width); output.Write(item.Height); output.Write(item.Version);
                    Value(item.Properties, depth + 1); output.Write(item.Tiles.Count);
                    foreach (TileCell2D cell in item.Tiles) { output.Write(cell.TileId); output.Write((int)cell.Flip); }
                    break;
                case Tile item:
                    Mark(Tag.Placement);
                    if (item.Image.ResourceId is not { } resource) { throw new NotSupportedException("Package images require resource IDs, not live image objects."); }
                    Text(resource.Key); output.Write(item.X); output.Write(item.Y);
                    output.Write(item.Width); output.Write(item.Height); Value(item.Collider, depth + 1); break;
                case TileMapChunkData2D item:
                    if (item.Grid is not null)
                    {
                        Mark(Tag.GridData); Value(item.Grid, depth + 1); Objects(item.TileSets, depth);
                        long definitions = 0;
                        foreach (TileSet2D set in item.TileSets) { definitions = checked(definitions + set.Tiles.Count); }
                        Charge(TableCharge(definitions) + TableCharge(item.TileSets.Count) +
                            BufferCharge(item.TileSets.Count, 8) + ObjectCharge * item.TileSets.Count);
                    }
                    else
                    {
                        Mark(Tag.PlacementData); Objects(item.Placements, depth);
                        Charge(2 * TableCharge(0) + BufferCharge(item.Placements.Count, 8));
                    }
                    break;
                case Scene2DEntity item:
                    Mark(Tag.Entity); Text(item.Id); Text(item.MapId); Point(item.Position);
                    Charge(TableCharge(item.Properties.Count) + BufferCharge(2L * item.Vertices.Count, 8));
                    output.Write(item.Size.Width); output.Write(item.Size.Height); Text(item.Shape); Text(item.Points);
                    output.Write(item.Rotation); Point(item.Pivot); Text(item.Role); Value(item.Collider, depth + 1);
                    output.Write(item.Order); output.Write(item.IsVisible); output.Write(item.Opacity);
                    Value(item.Properties, depth + 1); break;
                case TilePromotion2D item:
                    Mark(Tag.Promotion); Text(item.Cell.MapId); output.Write(item.Cell.Coordinate.X); output.Write(item.Cell.Coordinate.Y);
                    Charge(TableCharge(item.Properties.Count));
                    Value(item.TileId, depth + 1); Value(item.Properties, depth + 1); break;
                case Scene2DPackageMetadata item:
                    Mark(item.GridChunks.Count == 0 ? Tag.Metadata : Tag.MetadataWithGridChunks);
                    Charge(TableCharge(item.Properties.Count) + BufferCharge(item.TileSets.Count, 8) + BufferCharge(item.GridChunks.Count, 8));
                    Value(item.Properties, depth + 1); Objects(item.TileSets, depth);
                    if (item.GridChunks.Count != 0) { Objects(item.GridChunks, depth); }
                    break;
                case Scene2DPackageGridChunkMetadata item:
                    Mark(Tag.GridChunkMetadata); Bounds(item.Cells); output.Write(item.Version);
                    Charge(TableCharge(item.Properties.Count)); Value(item.Properties, depth + 1); break;
                case PackageBlock item:
                    Mark(Tag.Block); output.Write(item.Offset); output.Write(item.Length); Value(item.Hash, depth + 1); break;
                case PackageMap item:
                    Mark(Tag.Map); Value(item.Catalog, depth + 1); Value(item.Metadata, depth + 1); Objects(item.Chunks, depth); break;
                case PackageLevel item:
                    Mark(Tag.Level); Text(item.Id); Point(item.WorldOffset); Size(item.TileSize); Bounds(item.Bounds);
                    Value(item.Metadata, depth + 1); Objects(item.Maps, depth); Objects(item.Entities, depth); Objects(item.Promotions, depth); break;
                case PackageEntity item:
                    Mark(Tag.EntityIndex); Value(item.Info, depth + 1); Value(item.Data, depth + 1); break;
                case Scene2DPackageEntityInfo item:
                    Mark(Tag.EntityInfo); Text(item.Id); Text(item.MapId); Text(item.Role); Rect(item.AuthoringBounds);
                    output.Write(item.CollisionBounds.HasValue);
                    if (item.CollisionBounds is DrawRect collisionBounds) { Rect(collisionBounds); }
                    break;
                case PackagePromotion item:
                    Mark(Tag.PromotionIndex); Text(item.Cell.MapId); output.Write(item.Cell.Coordinate.X); output.Write(item.Cell.Coordinate.Y);
                    Value(item.Data, depth + 1); break;
                case PackageIndex item:
                    Mark(Tag.Index); Objects(item.Assets, depth); Objects(item.Files, depth);
                    Value(item.Metadata, depth + 1); Objects(item.Levels, depth); output.Write(item.DataLength); break;
                case Scene2DAsset item:
                    Mark(Tag.Asset); Text(item.ResourceId.Key); Text(item.Path); output.Write(item.Size.Width); output.Write(item.Size.Height); break;
                case TileMapCatalog2D item:
                    Mark(Tag.Catalog); Text(item.Id); Size(item.IsFreePlacement ? null : item.TileSize); Bounds(item.Bounds);
                    Charge(TableCharge(item.Chunks.Count) + TableCharge(0) + 2 * TableCharge(item.ImageSizes.Count) +
                        2 * BufferCharge(item.Chunks.Count, 8));
                    output.Write(item.Order); output.Write(item.IsVisible); Point(item.Offset); output.Write(item.Opacity);
                    Color(item.Tint); output.Write(item.Version); output.Write(item.ImageSizes.Count);
                    foreach ((string key, DrawSize size) in item.ImageSizes) { Text(key); output.Write(size.Width); output.Write(size.Height); }
                    Objects(item.Chunks, depth); break;
                case TileMapChunkInfo2D item:
                    Mark(Tag.ChunkInfo); Value(item.Spatial, depth + 1); Bounds(item.Cells); output.Write(item.TileCount);
                    Charge(TableCharge(item.TileIds.Count) + TableCharge(item.Images.Count) +
                        BufferCharge(item.TileIds.Count, 4) + BufferCharge(2L * item.Images.Count, 8) + ObjectCharge * item.Images.Count);
                    output.Write(item.ExpandedColliderCount); Value(item.DataResidencyBytes, depth + 1); Integers(item.TileIds);
                    output.Write(item.Images.Count);
                    foreach (ImageReference image in item.Images)
                    {
                        if (image.ResourceId is not { } imageId) { throw new NotSupportedException("Package catalogs require resource IDs."); }
                        Text(imageId.Key);
                    }
                    break;
                case SceneSpatialEntry2D item:
                    Mark(Tag.Spatial); Text(item.Id); Rect(item.Bounds); output.Write(item.CollisionBounds.HasValue);
                    if (item.CollisionBounds is DrawRect collision) { Rect(collision); }
                    output.Write(item.IsSimulated); output.Write(item.Version); break;
                default:
                    throw new NotSupportedException("The payload contains a value outside the explicit scene package wire types.");
            }
            pending.Remove(value);
            int reference = references.Count;
            references.Add(value, reference);
        }

        private static long CountJsonRows(JsonElement value, int depth)
        {
            if (value.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object)) { return 1; }
            if (depth >= MaximumDepth) { throw new NotSupportedException("JSON payload nesting exceeds 128 levels."); }
            long rows = 2; // Container start/end; properties also have a name row.
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray()) { rows = checked(rows + CountJsonRows(item, depth + 1)); }
            }
            else
            {
                foreach (JsonProperty item in value.EnumerateObject()) { rows = checked(rows + 1 + CountJsonRows(item.Value, depth + 1)); }
            }
            return rows;
        }

        private void Mark(Tag tag)
        {
            output.Write((byte)tag);
            Charge(tag switch
            {
                Tag.Null or Tag.Reference or Tag.String => 0,
                Tag.Boolean or Tag.Int32 or Tag.Int64 or Tag.UInt32 or Tag.UInt64 or
                    Tag.Single or Tag.Double or Tag.Color or Tag.Point => 32,
                _ => ObjectCharge
            });
        }

        private void Text(string text, bool retained = true)
        {
            if (retained) { Charge(BufferCharge(text.Length, 2)); }
            // UTF-16 code units preserve every .NET string exactly, including
            // unpaired surrogates. No encoding replacement is allowed.
            output.Write(text.Length);
            foreach (char character in text) { output.Write((ushort)character); }
        }

        private void Integers(IReadOnlyList<int> items)
        {
            Charge(BufferCharge(items.Count, 4));
            output.Write(items.Count);
            foreach (int item in items) { output.Write(item); }
        }

        private void Objects(IReadOnlyList<object?> items, int depth)
        {
            Charge(BufferCharge(items.Count, 8));
            output.Write(items.Count);
            foreach (object? item in items) { Value(item, depth + 1); }
        }

        private void Color(Color color) { output.Write(color.R); output.Write(color.G); output.Write(color.B); output.Write(color.A); }
        private void Size(DrawSize? size)
        {
            output.Write(size.HasValue);
            if (size is DrawSize value) { output.Write(value.Width); output.Write(value.Height); }
        }
        private void Bounds(TileMapBounds2D? bounds)
        {
            output.Write(bounds.HasValue);
            if (bounds is TileMapBounds2D value) { output.Write(value.X); output.Write(value.Y); output.Write(value.Width); output.Write(value.Height); }
        }
        private void Point(DrawPoint point) { output.Write(point.X); output.Write(point.Y); }
        private void Rect(DrawRect rect) { output.Write(rect.X); output.Write(rect.Y); output.Write(rect.Width); output.Write(rect.Height); }
        private void Matrix(Matrix3x2 matrix)
        {
            output.Write(matrix.M11); output.Write(matrix.M12); output.Write(matrix.M21);
            output.Write(matrix.M22); output.Write(matrix.M31); output.Write(matrix.M32);
        }
    }

    private sealed class Reader(BinaryReader input)
    {
        private readonly List<object> references = [];

        internal object? Value(int depth)
        {
            if (depth > MaximumDepth) { throw Invalid("Payload nesting exceeds 128 levels."); }
            Tag tag = (Tag)input.ReadByte();
            switch (tag)
            {
                case Tag.Null: return null;
                case Tag.Boolean: return Boolean();
                case Tag.Int32: return input.ReadInt32();
                case Tag.Int64: return input.ReadInt64();
                case Tag.UInt32: return input.ReadUInt32();
                case Tag.UInt64: return input.ReadUInt64();
                case Tag.Single: return input.ReadSingle();
                case Tag.Double: return input.ReadDouble();
                case Tag.String: return Text();
                case Tag.Color: return new Color(input.ReadByte(), input.ReadByte(), input.ReadByte(), input.ReadByte());
                case Tag.Point: return Point();
                case Tag.Reference:
                    int id = input.ReadInt32();
                    if ((uint)id >= (uint)references.Count) { throw Invalid("Invalid payload reference."); }
                    return references[id];
            }
            object result = tag switch
            {
                Tag.Json => Json(),
                Tag.Properties => Properties(depth),
                Tag.Bytes => input.ReadBytes(Count(1)),
                Tag.Integers => Integers(),
                Tag.IntegerList => Array.AsReadOnly(Integers()),
                Tag.Objects => Objects<object>(depth, allowNull: true),
                Tag.ObjectList => Array.AsReadOnly(Objects<object>(depth, allowNull: true)),
                Tag.Definition => Definition(depth),
                Tag.TileSet => TileSet(depth),
                Tag.Collider => Collider(depth),
                Tag.Grid => Grid(depth),
                Tag.Placement => Placement(depth),
                Tag.GridData => new TileMapChunkData2D(Required<TileChunk2D>(depth), Objects<TileSet2D>(depth)),
                Tag.PlacementData => new TileMapChunkData2D(Objects<Tile>(depth)),
                Tag.Entity => Entity(depth),
                Tag.Promotion => Promotion(depth),
                Tag.Metadata => new Scene2DPackageMetadata(Bag(depth), Objects<TileSet2D>(depth)),
                Tag.MetadataWithGridChunks => new Scene2DPackageMetadata(Bag(depth), Objects<TileSet2D>(depth), Objects<Scene2DPackageGridChunkMetadata>(depth)),
                Tag.GridChunkMetadata => new Scene2DPackageGridChunkMetadata(Bounds() ?? throw Invalid("Authored grid bounds are required."), input.ReadInt64(), Bag(depth)),
                Tag.Block => new PackageBlock(input.ReadInt64(), input.ReadInt32(), Required<byte[]>(depth)),
                Tag.Map => new PackageMap(Required<TileMapCatalog2D>(depth), Required<PackageBlock>(depth), Objects<PackageBlock>(depth)),
                Tag.Level => new PackageLevel(Text(), Point(), Size(), Bounds(), Required<PackageBlock>(depth),
                    Objects<PackageMap>(depth), Objects<PackageEntity>(depth), Objects<PackagePromotion>(depth)),
                Tag.EntityIndex => new PackageEntity(Required<Scene2DPackageEntityInfo>(depth), Required<PackageBlock>(depth)),
                Tag.EntityInfo => new Scene2DPackageEntityInfo(Text(), Text(), Text(), Rect(), Boolean() ? Rect() : null),
                Tag.PromotionIndex => new PackagePromotion(new(Text(), input.ReadInt32(), input.ReadInt32()), Required<PackageBlock>(depth)),
                Tag.Index => new PackageIndex(Objects<Scene2DAsset>(depth), Objects<string>(depth), Required<PackageBlock>(depth),
                    Objects<PackageLevel>(depth), input.ReadInt64()),
                Tag.Asset => new Scene2DAsset(new(Text()), Text(), new(input.ReadSingle(), input.ReadSingle())),
                Tag.Catalog => Catalog(depth),
                Tag.ChunkInfo => ChunkInfo(depth),
                Tag.Spatial => new SceneSpatialEntry2D(Text(), Rect(), Boolean() ? Rect() : null, Boolean(), input.ReadInt64()),
                _ => throw Invalid("Unknown payload value tag.")
            };
            references.Add(result);
            return result;
        }

        private T Required<T>(int depth) where T : class => Value(depth + 1) as T ?? throw Invalid("Unexpected payload value type.");
        private IReadOnlyDictionary<string, object?> Bag(int depth) => Required<IReadOnlyDictionary<string, object?>>(depth);

        private bool Boolean() => input.ReadByte() switch { 0 => false, 1 => true, _ => throw Invalid("Invalid Boolean.") };

        private int Count(int minimumBytes)
        {
            int count = input.ReadInt32();
            if (count < 0 || count > (input.BaseStream.Length - input.BaseStream.Position) / minimumBytes)
            {
                throw Invalid("Payload length exceeds the available bytes.");
            }
            return count;
        }

        private string Text()
        {
            int count = Count(2);
            return string.Create(count, input, static (characters, reader) =>
            {
                for (int index = 0; index < characters.Length; index++) { characters[index] = (char)reader.ReadUInt16(); }
            });
        }

        private object Json()
        {
            using JsonDocument document = JsonDocument.Parse(Text(), new JsonDocumentOptions { MaxDepth = MaximumDepth });
            return new SceneJsonValue2D(document.RootElement);
        }

        private IReadOnlyDictionary<string, object?> Properties(int depth)
        {
            int count = Count(5);
            Dictionary<string, object?> items = new(count, StringComparer.Ordinal);
            for (int index = 0; index < count; index++) { items.Add(Text(), Value(depth + 1)); }
            return new ReadOnlyDictionary<string, object?>(items);
        }

        private int[] Integers()
        {
            int[] items = new int[Count(4)];
            for (int index = 0; index < items.Length; index++) { items[index] = input.ReadInt32(); }
            return items;
        }

        private T[] Objects<T>(int depth, bool allowNull = false) where T : class
        {
            T[] items = new T[Count(1)];
            for (int index = 0; index < items.Length; index++)
            {
                object? value = Value(depth + 1);
                // Only opaque object lists may contain null. Model lists may not.
                if (value is T typed) { items[index] = typed; }
                else if (value is null && allowNull) { items[index] = null!; }
                else { throw Invalid("Unexpected collection item type."); }
            }
            return items;
        }

        private TileDefinition2D Definition(int depth) => new(input.ReadInt32(), Rect(), Bag(depth), OptionalCollider(depth));

        private TileSet2D TileSet(int depth)
        {
            string id = Text(), atlas = Text();
            long version = input.ReadInt64();
            var properties = Bag(depth);
            return new(id, new ResourceId<ImageResource>(atlas), Objects<TileDefinition2D>(depth), version, properties);
        }

        private TileColliderDescriptor2D? OptionalCollider(int depth) => Value(depth + 1) switch
        {
            null => null, TileColliderDescriptor2D item => item, _ => throw Invalid("Expected a collider or null.")
        };

        private TileColliderDescriptor2D Collider(int depth)
        {
            TileColliderShape2D shape = (TileColliderShape2D)input.ReadByte();
            Matrix3x2 matrix = new(input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
            float width = input.ReadSingle(), height = input.ReadSingle(), radius = input.ReadSingle();
            string points = Text();
            float x = input.ReadSingle(), y = input.ReadSingle();
            uint layer = input.ReadUInt32(), mask = input.ReadUInt32();
            bool trigger = Boolean();
            string? identity = Value(depth + 1) switch { null => null, string item => item, _ => throw Invalid("Expected an identity or null.") };
            return new(shape, matrix, width, height, radius, points, x, y, layer, mask, trigger, identity, Bag(depth));
        }

        private TileChunk2D Grid(int depth)
        {
            TileCoordinate2D origin = new(input.ReadInt32(), input.ReadInt32());
            int width = input.ReadInt32(), height = input.ReadInt32();
            long version = input.ReadInt64();
            var properties = Bag(depth);
            int count = Count(8);
            if (width <= 0 || height <= 0 || (long)width * height != count) { throw Invalid("Invalid grid dimensions."); }
            TileCell2D[] cells = new TileCell2D[count];
            for (int index = 0; index < cells.Length; index++) { cells[index] = new(input.ReadInt32(), (TileFlip2D)input.ReadInt32()); }
            return new(origin, width, height, cells, version, properties);
        }

        private Tile Placement(int depth)
        {
            ImageReference image = new(new ResourceId<ImageResource>(Text()));
            float x = input.ReadSingle(), y = input.ReadSingle(), width = input.ReadSingle(), height = input.ReadSingle();
            return new(image, OptionalCollider(depth), x, y, width, height);
        }

        private Scene2DEntity Entity(int depth)
        {
            string id = Text(), map = Text();
            DrawPoint position = Point();
            DrawSize size = new(input.ReadSingle(), input.ReadSingle());
            string shape = Text(), points = Text();
            float rotation = input.ReadSingle();
            DrawPoint pivot = Point();
            string role = Text();
            TileColliderDescriptor2D? collider = OptionalCollider(depth);
            int order = input.ReadInt32();
            bool visible = Boolean();
            float opacity = input.ReadSingle();
            return new(id, map, position, size, shape, points, rotation, pivot, role, collider, order, visible, opacity, Bag(depth));
        }

        private TilePromotion2D Promotion(int depth)
        {
            TileCellKey2D cell = new(Text(), input.ReadInt32(), input.ReadInt32());
            int? tileId = Value(depth + 1) switch { null => null, int item => item, _ => throw Invalid("Expected a tile ID or null.") };
            return new(cell, tileId, Bag(depth));
        }

        private DrawPoint Point() => new(input.ReadSingle(), input.ReadSingle());
        private DrawRect Rect() => new(input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
        private DrawSize? Size() => Boolean() ? new(input.ReadSingle(), input.ReadSingle()) : null;
        private TileMapBounds2D? Bounds() => Boolean() ? new(input.ReadInt32(), input.ReadInt32(), input.ReadInt32(), input.ReadInt32()) : null;

        private TileMapCatalog2D Catalog(int depth)
        {
            string id = Text();
            DrawSize? tileSize = Size();
            TileMapBounds2D? bounds = Bounds();
            int order = input.ReadInt32();
            bool visible = Boolean();
            DrawPoint offset = Point();
            float opacity = input.ReadSingle();
            Color tint = new(input.ReadByte(), input.ReadByte(), input.ReadByte(), input.ReadByte());
            long version = input.ReadInt64();
            int count = Count(12);
            Dictionary<string, DrawSize> sizes = new(count, StringComparer.Ordinal);
            for (int index = 0; index < count; index++) { sizes.Add(Text(), new(input.ReadSingle(), input.ReadSingle())); }
            return new(id, Objects<TileMapChunkInfo2D>(depth), tileSize, bounds, order, visible, offset, opacity, tint, version, sizes);
        }

        private TileMapChunkInfo2D ChunkInfo(int depth)
        {
            SceneSpatialEntry2D spatial = Required<SceneSpatialEntry2D>(depth);
            TileMapBounds2D? cells = Bounds();
            int count = input.ReadInt32(), colliders = input.ReadInt32();
            long? charge = Value(depth + 1) switch { null => null, long value => value, _ => throw Invalid("Invalid residency charge.") };
            int[] ids = Integers();
            ImageReference[] images = new ImageReference[Count(4)];
            for (int index = 0; index < images.Length; index++) { images[index] = new(new ResourceId<ImageResource>(Text())); }
            if (cells is TileMapBounds2D grid)
            {
                if ((long)grid.Width * grid.Height != count) { throw Invalid("Inconsistent chunk cell count."); }
                return new(spatial, grid, ids, images, colliders, charge);
            }
            if (ids.Length != 0) { throw Invalid("Placement chunks cannot declare grid tile IDs."); }
            return new(spatial, count, images, colliders, charge);
        }
    }
}
