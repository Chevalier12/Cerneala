using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DPackages;

public sealed partial class Scene2DPackageTests
{
    [Theory]
    [InlineData(64, 32)]
    [InlineData(33, 19)]
    [InlineData(1, 33)]
    [InlineData(33, 1)]
    [InlineData(16, 16)]
    [InlineData(8, 8)]
    public async Task GridPreparationSubdividesOnlyOversizedChunksWithoutChangingCellsOrMetadata(int width, int height)
    {
        using Fixture fixture = new();
        using JsonDocument json = JsonDocument.Parse("{\"source\":1.0}");
        SceneJsonValue2D provenance = new(json.RootElement);
        Dictionary<string, object?> properties = new() { ["original"] = provenance, ["bulk"] = new byte[1024] };
        TileSet2D set = new("set", new("atlas"),
        [
            new(1, new(0, 0, 10, 10), properties, new(TileColliderShape2D.Box, width: 10, height: 10, properties: properties)),
            new(2, new(10, 0, 10, 10), properties, new(TileColliderShape2D.Polygon, points: "0,0 10,0 0,10", offsetX: -2, offsetY: 3, properties: properties)),
            new(3, new(0, 0, 10, 10), new Dictionary<string, object?> { ["unused"] = true })
        ], version: 4, properties: properties);
        TileCell2D[] cells = Enumerable.Range(0, width * height)
            .Select(index => new TileCell2D(index % 9 == 0 ? 0 : index % width < 16 ? 1 : 2, (TileFlip2D)(index % 8))).ToArray();
        TileChunk2D original = new(new(-19, -9), width, height, cells, 7, properties);
        TileChunk2D small = new(new(80, 5), 2, 3, Enumerable.Repeat(new TileCell2D(2), 6), 11, properties);
        TileChunk2D empty = new(new(90, -9), 17, 1, new TileCell2D[17], 13, properties);
        TileMap2DModel model = new("map", new(10, 10), [set], [original, small, empty],
            new(-20, -10, 130, 80), order: 3, offset: new(2, 3), opacity: .75f, tint: new Color(100, 130, 160, 190),
            version: 17, properties: properties);
        Scene2DDocument document = new([new Scene2DLevel("level", [model])], [new(new("atlas"), "nested/atlas.bin", new(20, 10))]);
        await fixture.WriteAsync(document);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        PackageMap map = fixture.ReadIndex().Levels[0].Maps[0];
        TileMapCatalog2D catalog = map.Catalog;
        Assert.Equal(new[] { model.Id }, package.Levels[0].TileMapIds);

        Assert.All(catalog.Chunks, info =>
        {
            Assert.InRange(info.Cells!.Value.Width, 1, 16);
            Assert.InRange(info.Cells.Value.Height, 1, 16);
            Assert.InRange(info.TileCount, 1, 256);
            Assert.True(info.DataResidencyBytes > 0);
        });
        Assert.Equal(((width + 15) / 16) * ((height + 15) / 16) + 3, catalog.Chunks.Count);
        Assert.Equal(3, TileMapSource2D.FromModel(model).Entries.Count); // Preparation must not mutate the importer/model adapter.
        Assert.Same(original, model.Chunks[0]);
        Assert.Equal(cells, original.Tiles);
        Assert.Equal(model.Bounds, catalog.Bounds);
        Assert.Equal(model.Offset, catalog.Offset);
        Assert.Equal(model.Opacity, catalog.Opacity);
        Assert.Equal(model.Tint, catalog.Tint);
        Assert.Equal(model.Order, catalog.Order);
        Assert.Equal(model.Version, catalog.Version);
        HashSet<TileCoordinate2D> visited = [];
        for (int chunkIndex = 0; chunkIndex < catalog.Chunks.Count; chunkIndex++)
        {
            TileMapChunkInfo2D info = catalog.Chunks[chunkIndex];
            TileMapChunkData2D data = await package.LoadAsync<TileMapChunkData2D>(map.Chunks[chunkIndex], CancellationToken.None);
            TileChunk2D piece = data.Grid!;
            TileChunk2D authored = Assert.Single(model.Chunks.Where(chunk => chunk.Contains(piece.Origin)));
            Assert.Equal(authored.Version, piece.Version);
            Assert.Equal(PackageValueCodec.Encode(authored.Properties), PackageValueCodec.Encode(piece.Properties));
            Assert.Equal(FormattableString.Invariant($"grid:{piece.Origin.X}:{piece.Origin.Y}:{piece.Width}:{piece.Height}"), info.Spatial.Id);
            int[] used = piece.Tiles.Where(cell => cell.TileId != 0).Select(cell => cell.TileId).Distinct().Order().ToArray();
            Assert.Equal(used, info.TileIds.Order());
            Assert.Equal(used, data.TileSets.SelectMany(palette => palette.Tiles).Select(tile => tile.Id).Order());
            Assert.Equal(piece.Tiles.Count(cell => cell.TileId != 0), info.ExpandedColliderCount);
            foreach (TileSet2D palette in data.TileSets)
            {
                Assert.Equal(set.Version, palette.Version);
                Assert.Same(piece.Properties["original"], palette.Properties["original"]);
                foreach (TileDefinition2D tile in palette.Tiles)
                {
                    Assert.True(model.TryResolveTile(tile.Id, out _, out TileDefinition2D? definition));
                    Assert.Equal(PackageValueCodec.Encode(definition), PackageValueCodec.Encode(tile));
                    Assert.Same(piece.Properties["original"], tile.Properties["original"]);
                    Assert.Same(tile.Properties["original"], tile.Collider!.Properties["original"]);
                }
            }
            for (int y = 0; y < piece.Height; y++)
            for (int x = 0; x < piece.Width; x++)
            {
                TileCoordinate2D coordinate = new(piece.Origin.X + x, piece.Origin.Y + y);
                Assert.True(visited.Add(coordinate), "Prepared pieces must not overlap.");
                Assert.Equal(authored.GetCell(coordinate), piece.GetCell(coordinate));
            }
            if (authored == small) { Assert.Equal(PackageValueCodec.Encode(small), PackageValueCodec.Encode(piece)); }
            if (authored == empty) { Assert.Empty(data.TileSets); Assert.Empty(info.Images); Assert.Null(info.Spatial.CollisionBounds); }
        }
        Assert.Equal(width * height + 6 + 17, visited.Count); // Includes every empty cell/extent.
        Scene2DPackageMetadata metadata = await package.Levels[0].LoadMapMetadataAsync(model.Id);
        Assert.Equal(PackageValueCodec.Encode(model.Properties), PackageValueCodec.Encode(metadata.Properties));
        Assert.Equal(3, Assert.Single(metadata.TileSets).Tiles.Count); // Unused definitions remain explicit metadata.
        Assert.Equal(model.Chunks.Count, metadata.GridChunks.Count);
        for (int index = 0; index < model.Chunks.Count; index++)
        {
            TileChunk2D authored = model.Chunks[index];
            Scene2DPackageGridChunkMetadata stored = metadata.GridChunks[index];
            Assert.Equal(new TileMapBounds2D(authored.Origin.X, authored.Origin.Y, authored.Width, authored.Height), stored.Cells);
            Assert.Equal(authored.Version, stored.Version);
            Assert.Equal(PackageValueCodec.Encode(authored.Properties), PackageValueCodec.Encode(stored.Properties));
            Assert.Same(metadata.Properties["original"], stored.Properties["original"]);
        }
        string second = Path.Combine(fixture.Root, "second");
        await Scene2DPackageWriter.WriteAsync(second, document, fixture.Input, ["scripts/quest.txt"]);
        Assert.Equal(await File.ReadAllBytesAsync(fixture.CatalogPath), await File.ReadAllBytesAsync(Path.Combine(second, PackageFiles.CatalogName)));
        Assert.Equal(await File.ReadAllBytesAsync(fixture.DataPath), await File.ReadAllBytesAsync(Path.Combine(second, PackageFiles.DataName)));
    }

    [Fact]
    public async Task OpeningAndOneGridPieceDoNotReadTheRestOfTheAuthoredLayer()
    {
        using Fixture fixture = new();
        TileSet2D set = new("set", new("atlas"), [new(1, new(0, 0, 10, 10))]);
        TileChunk2D layer = new(default, 64, 32, Enumerable.Repeat(new TileCell2D(1), 2048));
        Scene2DDocument document = new([new Scene2DLevel("level", [new("map", new(10, 10), [set], [layer])])],
            [new(new("atlas"), "nested/atlas.bin", new(20, 10))]);
        await fixture.WriteAsync(document);
        PackageMap map = fixture.ReadIndex().Levels[0].Maps[0];
        Assert.Equal(8, map.Chunks.Length);
        fixture.Corrupt(map.Metadata);
        fixture.Corrupt(map.Chunks[7]);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output, new() { MaxPayloadBytes = 4096 });
        TileMapChunkData2D first = await package.LoadAsync<TileMapChunkData2D>(map.Chunks[0], CancellationToken.None);
        Assert.Equal(256, first.Grid!.Tiles.Count);
        Assert.Equal(default, first.Grid.Origin);
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.LoadAsync<TileMapChunkData2D>(map.Chunks[7], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.Levels[0].LoadMapMetadataAsync("map"));
    }
}
