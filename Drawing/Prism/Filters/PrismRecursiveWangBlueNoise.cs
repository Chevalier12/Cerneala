using System.Buffers.Binary;
using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal readonly record struct PrismRecursiveWangPoint(
    Vector2 Position,
    float Rank);

internal sealed class PrismSpatterPointField
{
    public PrismSpatterPointField(
        int gridSize,
        int layerCount,
        int pointCount,
        Vector4[] packedPoints)
    {
        GridSize = gridSize;
        LayerCount = layerCount;
        PointCount = pointCount;
        PackedPoints = packedPoints;
    }

    public int GridSize { get; }

    public int LayerCount { get; }

    public int PointCount { get; }

    public int TextureWidth => GridSize * LayerCount;

    public Vector4[] PackedPoints { get; }

    public Vector4 GetPoint(int cellX, int cellY, int layer)
    {
        int wrappedX = Wrap(cellX, GridSize);
        int wrappedY = Wrap(cellY, GridSize);
        int index =
            (wrappedY * TextureWidth) +
            (layer * GridSize) +
            wrappedX;
        return PackedPoints[index];
    }

    private static int Wrap(int value, int size)
    {
        int wrapped = value % size;
        return wrapped < 0 ? wrapped + size : wrapped;
    }
}



internal static class PrismRecursiveWangBlueNoise
{
    public const int GridSize = 512;
    public const int LayerCount = 2;
    public const int PointCount = 65_536;

    private const string TilesetResourceName =
        "Cerneala.Drawing.Prism.Filters.Assets.bluenoise.bin";

    private static readonly Lazy<PrismSpatterPointField> CachedField =
        new(CreateField, LazyThreadSafetyMode.ExecutionAndPublication);

    public static PrismSpatterPointField PointField =>
        CachedField.Value;

    internal static PrismSpatterPointField CreateField(
        ReadOnlySpan<byte> tileset)
    {
        TileSet parsed = Parse(tileset);
        Tile root = parsed.Tiles
            .Where(tile => tile.North == tile.South &&
                tile.East == tile.West)
            .OrderByDescending(tile => tile.SubPoints.Length)
            .FirstOrDefault() ??
            throw new InvalidDataException(
                "The Recursive Wang tileset has no toroidal root tile.");
        List<PrismRecursiveWangPoint> points = Generate(
            parsed,
            root,
            PointCount);
        return Pack(points);
    }

    internal static List<PrismRecursiveWangPoint> Generate(
        ReadOnlySpan<byte> tileset,
        int pointCount)
    {
        if (pointCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointCount));
        }

        TileSet parsed = Parse(tileset);
        Tile root = parsed.Tiles
            .Where(tile => tile.North == tile.South &&
                tile.East == tile.West)
            .OrderByDescending(tile => tile.SubPoints.Length)
            .FirstOrDefault() ??
            throw new InvalidDataException(
                "The Recursive Wang tileset has no toroidal root tile.");
        return Generate(parsed, root, pointCount);
    }

    internal static uint Hash(uint value)
    {
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return value;
    }

    internal static Vector2 SeedOffset(uint seed) =>
        new(
            Hash(seed ^ 0x68bc21ebu) % GridSize,
            Hash(seed ^ 0x02e5be93u) % GridSize);

    private static PrismSpatterPointField CreateField()
    {
        using Stream stream = typeof(PrismRecursiveWangBlueNoise)
            .Assembly
            .GetManifestResourceStream(TilesetResourceName) ??
            throw new InvalidOperationException(
                $"Embedded Recursive Wang tileset '{TilesetResourceName}' is missing.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return CreateField(buffer.ToArray());
    }

    private static List<PrismRecursiveWangPoint> Generate(
        TileSet tileset,
        Tile root,
        int pointCount)
    {
        float density = pointCount;
        List<PrismRecursiveWangPoint> points = [];
        for (int attempt = 0; attempt < 8; attempt++)
        {
            points.Clear();
            AddRootPoints(root, density, points);
            RecurseTile(
                tileset,
                root,
                Vector2.Zero,
                level: 0,
                density,
                points);
            if (points.Count >= pointCount)
            {
                break;
            }

            density *= MathF.Max(
                1.1f,
                pointCount / (float)Math.Max(points.Count, 1));
        }

        if (points.Count < pointCount)
        {
            throw new InvalidDataException(
                $"Recursive Wang generation produced {points.Count} of " +
                $"the requested {pointCount} points.");
        }

        points.Sort(static (left, right) =>
            left.Rank.CompareTo(right.Rank));
        if (points.Count > pointCount)
        {
            points.RemoveRange(pointCount, points.Count - pointCount);
        }

        for (int index = 0; index < points.Count; index++)
        {
            PrismRecursiveWangPoint point = points[index];
            points[index] = point with
            {
                Rank = (index + 1f) / (pointCount + 1f)
            };
        }
        return points;
    }

    private static void AddRootPoints(
        Tile root,
        float density,
        List<PrismRecursiveWangPoint> output)
    {
        int testCount = Math.Min(
            root.Points.Length,
            Math.Max(0, (int)density));
        float factor = 1 / density;
        for (int index = 0; index < testCount; index++)
        {
            output.Add(new PrismRecursiveWangPoint(
                root.Points[index],
                index * factor));
        }
    }

    private static void RecurseTile(
        TileSet tileset,
        Tile tile,
        Vector2 origin,
        int level,
        float density,
        List<PrismRecursiveWangPoint> output)
    {
        float subdivisionScale = MathF.Pow(
            tileset.SubtileCount,
            level);
        float tileSize = 1 / subdivisionScale;
        float depth = subdivisionScale * subdivisionScale;
        float threshold =
            (density / depth) - tile.Points.Length;
        int testCount = Math.Min(
            tile.SubPoints.Length,
            Math.Max(0, (int)threshold));
        float factor = depth / density;
        for (int index = 0; index < testCount; index++)
        {
            output.Add(new PrismRecursiveWangPoint(
                origin + (tile.SubPoints[index] * tileSize),
                (level + 1) + (index * factor)));
        }

        if (threshold <= tile.SubPoints.Length)
        {
            return;
        }

        float childSize = tileSize / tileset.SubtileCount;
        int childLevel = level + 1;
        for (int childY = 0;
            childY < tileset.SubtileCount;
            childY++)
        {
            for (int childX = 0;
                childX < tileset.SubtileCount;
                childX++)
            {
                int childIndex = tile.Subdivision[
                    (childY * tileset.SubtileCount) + childX];
                if ((uint)childIndex >= (uint)tileset.Tiles.Length)
                {
                    throw new InvalidDataException(
                        "The Recursive Wang tileset references an invalid child tile.");
                }
                RecurseTile(
                    tileset,
                    tileset.Tiles[childIndex],
                    origin + new Vector2(
                        childX * childSize,
                        childY * childSize),
                    childLevel,
                    density,
                    output);
            }
        }
    }

    private static PrismSpatterPointField Pack(
        IReadOnlyList<PrismRecursiveWangPoint> points)
    {
        int textureWidth = GridSize * LayerCount;
        Vector4[] packed = new Vector4[
            textureWidth * GridSize];
        byte[] occupancy = new byte[GridSize * GridSize];
        foreach (PrismRecursiveWangPoint point in points)
        {
            float scaledX = Math.Clamp(
                point.Position.X * GridSize,
                0,
                MathF.BitDecrement(GridSize));
            float scaledY = Math.Clamp(
                point.Position.Y * GridSize,
                0,
                MathF.BitDecrement(GridSize));
            int cellX = (int)scaledX;
            int cellY = (int)scaledY;
            int occupancyIndex = (cellY * GridSize) + cellX;
            int layer = occupancy[occupancyIndex]++;
            if (layer >= LayerCount)
            {
                throw new InvalidDataException(
                    "Recursive Wang point packing exceeded the configured cell layers.");
            }

            int packedIndex =
                (cellY * textureWidth) +
                (layer * GridSize) +
                cellX;
            packed[packedIndex] = new Vector4(
                scaledX - cellX,
                scaledY - cellY,
                point.Rank,
                1);
        }

        return new PrismSpatterPointField(
            GridSize,
            LayerCount,
            points.Count,
            packed);
    }

    private static TileSet Parse(ReadOnlySpan<byte> bytes)
    {
        TilesetReader reader = new(bytes);
        int tileCount = reader.ReadPositiveInt("tile count");
        int subtileCount = reader.ReadPositiveInt("subtile count");
        int subdivisionCount = reader.ReadPositiveInt(
            "subdivision count");
        Tile[] tiles = new Tile[tileCount];
        int subdivisionLength = checked(subtileCount * subtileCount);
        for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            int north = reader.ReadInt();
            int east = reader.ReadInt();
            int south = reader.ReadInt();
            int west = reader.ReadInt();
            int[] subdivision = new int[subdivisionLength];
            for (int subdivisionIndex = 0;
                subdivisionIndex < subdivisionCount;
                subdivisionIndex++)
            {
                for (int index = 0;
                    index < subdivisionLength;
                    index++)
                {
                    int child = reader.ReadInt();
                    if (subdivisionIndex == 0)
                    {
                        subdivision[index] = child;
                    }
                }
            }

            Vector2[] points = reader.ReadPoints("point count");
            Vector2[] subPoints = reader.ReadPoints("subpoint count");
            tiles[tileIndex] = new Tile(
                north,
                east,
                south,
                west,
                subdivision,
                points,
                subPoints);
        }

        reader.EnsureComplete();
        return new TileSet(subtileCount, tiles);
    }

    private sealed record TileSet(int SubtileCount, Tile[] Tiles);

    private sealed record Tile(
        int North,
        int East,
        int South,
        int West,
        int[] Subdivision,
        Vector2[] Points,
        Vector2[] SubPoints);

    private ref struct TilesetReader
    {
        private readonly ReadOnlySpan<byte> bytes;
        private int offset;

        public TilesetReader(ReadOnlySpan<byte> bytes)
        {
            this.bytes = bytes;
            offset = 0;
        }

        public int ReadInt()
        {
            ReadOnlySpan<byte> value = Read(sizeof(int));
            return BinaryPrimitives.ReadInt32LittleEndian(value);
        }

        public int ReadPositiveInt(string name)
        {
            int value = ReadInt();
            if (value <= 0)
            {
                throw new InvalidDataException(
                    $"The Recursive Wang tileset has an invalid {name}.");
            }
            return value;
        }

        public Vector2[] ReadPoints(string countName)
        {
            int count = ReadPositiveInt(countName);
            Vector2[] points = new Vector2[count];
            for (int index = 0; index < count; index++)
            {
                points[index] = new Vector2(
                    ReadSingle(),
                    ReadSingle());
            }
            return points;
        }

        public void EnsureComplete()
        {
            if (offset != bytes.Length)
            {
                throw new InvalidDataException(
                    "The Recursive Wang tileset has trailing or unread data.");
            }
        }

        private float ReadSingle() =>
            BitConverter.Int32BitsToSingle(ReadInt());

        private ReadOnlySpan<byte> Read(int count)
        {
            if (count < 0 || offset > bytes.Length - count)
            {
                throw new InvalidDataException(
                    "The Recursive Wang tileset is truncated.");
            }
            ReadOnlySpan<byte> value = bytes.Slice(offset, count);
            offset += count;
            return value;
        }
    }
}
