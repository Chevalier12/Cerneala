using System.Reflection;
using System.Security.Cryptography;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class TileMap2DStageZeroContractTests
{
    [Fact]
    [Trait("TileMapStage", "0")]
    public void DeterministicVillageFixtureCoversFiniteSparseAtlasFlipAndVisibilityCases()
    {
        TileMapVillageFixture first = TileMapVillageFixture.Create();
        TileMapVillageFixture second = TileMapVillageFixture.Create();

        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal("AB31673783B86A0D9EA7789AB0F133B0FB2AEC270DAFD9D4A6EECF7B317416E5", first.Fingerprint);
        Assert.Equal(2, first.Atlases.Count);
        Assert.Contains(first.FiniteChunks.SelectMany(static chunk => chunk.Cells), static cell => cell.TileId == 0);
        Assert.Contains(first.FiniteChunks.SelectMany(static chunk => chunk.Cells), static cell => cell.Flip != FixtureTileFlip.None);
        Assert.Contains(first.FiniteChunks, static chunk => chunk.IsVisible);
        Assert.Contains(first.FiniteChunks, static chunk => !chunk.IsVisible);
        Assert.True(first.FiniteChunks.Count(static chunk => !chunk.IsVisible) > first.FiniteChunks.Count(static chunk => chunk.IsVisible));
        Assert.Contains(first.SparseChunks, static chunk => chunk.OriginX < 0 || chunk.OriginY < 0);
        Assert.Contains(first.SparseChunks, static chunk => chunk.OriginX >= 100 || chunk.OriginY >= 50);
        Assert.All(first.Properties, static pair => Assert.IsType<string>(pair.Value));
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void PublicModelAndSparsePresentationContractExistsWithoutPerTileMaterialization()
    {
        string[] expectedTypes =
        [
            "TileMap2DModel",
            "TileSet2D",
            "TileDefinition2D",
            "TileLayer2DModel",
            "TileChunk2D",
            "TileCell2D",
            "TileCoordinate2D",
            "TileCellKey2D",
            "TileMapBounds2D",
            "TileFlip2D",
            "TileMap2D",
            "TileLayer2D",
            "TileInstance2D"
        ];

        IReadOnlyDictionary<string, Type?> resolved = expectedTypes.ToDictionary(
            static name => name,
            static name => typeof(SceneNode2D).Assembly.GetType($"Cerneala.UI.Controls.{name}"));
        string[] missing = resolved
            .Where(static pair => pair.Value is null)
            .Select(static pair => pair.Key)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "RED: the approved backend-neutral tilemap API is absent: " + string.Join(", ", missing));

        Assert.True(typeof(SceneNode2D).IsAssignableFrom(resolved["TileMap2D"]));
        Assert.True(typeof(SceneNode2D).IsAssignableFrom(resolved["TileLayer2D"]));
        Assert.True(typeof(SceneNode2D).IsAssignableFrom(resolved["TileInstance2D"]));
        RequireProperties(resolved["TileMap2DModel"]!, "TileSize", "Bounds", "TileSets", "Layers", "Version", "Properties");
        RequireProperties(resolved["TileSet2D"]!, "Id", "AtlasResourceId", "Tiles", "Version", "Properties");
        RequireProperties(resolved["TileDefinition2D"]!, "Id", "SourceRect", "Properties");
        RequireProperties(resolved["TileLayer2DModel"]!, "Id", "IsVisible", "Offset", "Opacity", "Tint", "Order", "Chunks", "Version", "Properties");
        RequireProperties(resolved["TileChunk2D"]!, "Origin", "Width", "Height", "Tiles", "Version", "Properties");
        RequireProperties(resolved["TileCell2D"]!, "TileId", "Flip");
        RequireProperties(resolved["TileMap2D"]!, "Model", "Layers");
        RequireProperties(resolved["TileLayer2D"]!, "LayerId", "PromotedTiles");
        RequireProperties(resolved["TileInstance2D"]!, "X", "Y", "TileId", "SourceRect", "Tint", "Flip");
        Assert.NotNull(resolved["TileMap2D"]!.GetMethod("Promote", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(resolved["TileMap2D"]!.GetMethod("Demote", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(resolved["TileMap2D"]!.GetMethod("TryGetPromoted", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void AtlasResolutionLayerOrderAndSourceRectRecordingHaveAnObservableContract()
    {
        Type? mapType = Resolve("TileMap2D");
        Assert.True(mapType is not null, "RED: TileMap2D is missing, so atlas/source-rect/layer-order recording cannot run.");
        Assert.NotNull(mapType.GetMethod("GetDiagnosticsSnapshot", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void PromotionAndDemotionHaveStableCoordinatesOneSemanticSlotAndNoDoubleDraw()
    {
        Type? mapType = Resolve("TileMap2D");
        Assert.True(mapType is not null, "RED: TileMap2D is missing, so promotion/demotion lifecycle cannot run.");
        RequireMethods(mapType, "Promote", "Demote", "TryGetPromoted");
        Type? snapshot = Resolve("TileMap2DDiagnosticsSnapshot");
        Assert.True(snapshot is not null, "RED: promoted/demoted draw ownership has no diagnostics snapshot.");
        RequireProperties(snapshot, "PromotedInstancesVisible", "PromotedInstancesCulled", "Promotions", "Demotions", "BatchSplits");
    }

    [Theory]
    [InlineData("MonoGame")]
    [InlineData("SDL_GPU")]
    [Trait("TileMapStage", "0")]
    public void NestedPrismMapLayerPromotedTileHasABackendConformancePath(string backend)
    {
        Type? mapType = Resolve("TileMap2D");
        Type? layerType = Resolve("TileLayer2D");
        Type? tileType = Resolve("TileInstance2D");
        Assert.True(
            mapType is not null && layerType is not null && tileType is not null,
            $"RED: {backend} cannot exercise map -> layer -> promoted tile Prism scopes before the three scene nodes exist.");
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void CullingCountersProveInvisibleChunksAreRejectedBeforeTileEnumeration()
    {
        Type? snapshot = Resolve("TileMap2DDiagnosticsSnapshot");
        Assert.True(snapshot is not null, "RED: culling work is not observable because TileMap2DDiagnosticsSnapshot is missing.");
        RequireProperties(snapshot, "TotalChunks", "CandidateChunks", "VisibleChunks", "CandidateTiles", "DrawnTiles");
    }

    [Fact]
    [Trait("TileMapStage", "0")]
    public void LocalMutationCountersProveOnlyTheDependentChunkSegmentRebuilds()
    {
        Type? snapshot = Resolve("TileMap2DDiagnosticsSnapshot");
        Assert.True(snapshot is not null, "RED: local invalidation is not observable because TileMap2DDiagnosticsSnapshot is missing.");
        RequireProperties(snapshot, "BatchesBuilt", "BatchesRebuilt", "BatchesReused", "RetainedBytes", "RetainedObjects", "TileInvalidations");
    }

    private static Type? Resolve(string name) =>
        typeof(SceneNode2D).Assembly.GetType($"Cerneala.UI.Controls.{name}");

    private static void RequireProperties(Type type, params string[] names)
    {
        string[] missing = names
            .Where(name => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is null)
            .ToArray();
        Assert.True(missing.Length == 0, $"{type.Name} is missing properties: {string.Join(", ", missing)}");
    }

    private static void RequireMethods(Type type, params string[] names)
    {
        string[] missing = names
            .Where(name => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(method => method.Name != name))
            .ToArray();
        Assert.True(missing.Length == 0, $"{type.Name} is missing methods: {string.Join(", ", missing)}");
    }
}

internal sealed record TileMapVillageFixture(
    IReadOnlyList<FixtureAtlas> Atlases,
    IReadOnlyList<FixtureChunk> FiniteChunks,
    IReadOnlyList<FixtureChunk> SparseChunks,
    IReadOnlyDictionary<string, object?> Properties,
    string Fingerprint)
{
    internal const int ChunkSize = 16;
    internal const int LayerCount = 3;
    internal const int WidthInTiles = 128;
    internal const int HeightInTiles = 96;

    internal static TileMapVillageFixture Create()
    {
        FixtureAtlas[] atlases =
        [
            new("Terrain", "VillageTerrain", 1, 12),
            new("Structures", "VillageStructures", 100, 8)
        ];
        List<FixtureChunk> finite = [];
        for (int layer = 0; layer < LayerCount; layer++)
        {
            for (int chunkY = 0; chunkY < HeightInTiles / ChunkSize; chunkY++)
            {
                for (int chunkX = 0; chunkX < WidthInTiles / ChunkSize; chunkX++)
                {
                    finite.Add(CreateChunk(layer, chunkX, chunkY, isVisible: chunkX is >= 2 and <= 4 && chunkY is >= 2 and <= 3));
                }
            }
        }

        FixtureChunk[] sparse =
        [
            CreateChunk(0, -4, -3, false),
            CreateChunk(1, 0, 0, true),
            CreateChunk(2, 100, 50, false)
        ];
        Dictionary<string, object?> properties = new(StringComparer.Ordinal)
        {
            ["importer:biome"] = "temperate",
            ["importer:source"] = "deterministic-stage-zero"
        };
        string fingerprint = ComputeFingerprint(atlases, finite, sparse, properties);
        return new TileMapVillageFixture(atlases, finite, sparse, properties, fingerprint);
    }

    private static FixtureChunk CreateChunk(int layer, int chunkX, int chunkY, bool isVisible)
    {
        FixtureCell[] cells = new FixtureCell[ChunkSize * ChunkSize];
        for (int localY = 0; localY < ChunkSize; localY++)
        {
            for (int localX = 0; localX < ChunkSize; localX++)
            {
                int worldX = (chunkX * ChunkSize) + localX;
                int worldY = (chunkY * ChunkSize) + localY;
                int selector = Math.Abs(unchecked((worldX * 31) + (worldY * 17) + (layer * 13)));
                int tileId = selector % 11 == 0
                    ? 0
                    : layer == 2 && selector % 5 == 0
                        ? 100 + (selector % 8)
                        : 1 + (selector % 12);
                FixtureTileFlip flip = selector % 29 == 0
                    ? FixtureTileFlip.Horizontal
                    : selector % 31 == 0
                        ? FixtureTileFlip.Vertical
                        : FixtureTileFlip.None;
                cells[(localY * ChunkSize) + localX] = new FixtureCell(tileId, flip);
            }
        }

        return new FixtureChunk(layer, chunkX, chunkY, isVisible, cells);
    }

    private static string ComputeFingerprint(
        IReadOnlyList<FixtureAtlas> atlases,
        IReadOnlyList<FixtureChunk> finite,
        IReadOnlyList<FixtureChunk> sparse,
        IReadOnlyDictionary<string, object?> properties)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void AddInt(int value) => hash.AppendData(BitConverter.GetBytes(value));
        void AddString(string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            AddInt(bytes.Length);
            hash.AppendData(bytes);
        }

        foreach (FixtureAtlas atlas in atlases)
        {
            AddString(atlas.Id);
            AddString(atlas.ResourceId);
            AddInt(atlas.FirstTileId);
            AddInt(atlas.TileCount);
        }
        foreach (FixtureChunk chunk in finite.Concat(sparse))
        {
            AddInt(chunk.Layer);
            AddInt(chunk.OriginX);
            AddInt(chunk.OriginY);
            AddInt(chunk.IsVisible ? 1 : 0);
            foreach (FixtureCell cell in chunk.Cells)
            {
                AddInt(cell.TileId);
                AddInt((int)cell.Flip);
            }
        }
        foreach ((string key, object? value) in properties.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            AddString(key);
            AddString(value?.ToString() ?? "<null>");
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}

internal sealed record FixtureAtlas(string Id, string ResourceId, int FirstTileId, int TileCount);

internal sealed record FixtureChunk(
    int Layer,
    int OriginX,
    int OriginY,
    bool IsVisible,
    IReadOnlyList<FixtureCell> Cells);

internal readonly record struct FixtureCell(int TileId, FixtureTileFlip Flip);

[Flags]
internal enum FixtureTileFlip
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}
