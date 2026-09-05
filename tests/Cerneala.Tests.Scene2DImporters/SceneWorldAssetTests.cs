using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class SceneWorldAssetTests
{
    [Fact]
    public void OriginalVillageFilesDescribeEquivalentCellsCollidersSpawnAndDoor()
    {
        string assets = Assets();
        Scene2DImportResult tiled = TiledScene2DImporter.Import(Path.Combine(assets, "village.tmj"));
        Scene2DImportResult ldtk = LdtkScene2DImporter.Import(Path.Combine(assets, "village.ldtk"));
        Assert.True(tiled.Success, string.Join(Environment.NewLine, tiled.Diagnostics));
        Assert.True(ldtk.Success, string.Join(Environment.NewLine, ldtk.Diagnostics));
        Scene2DLevel left = Assert.Single(tiled.Document!.Levels);
        Scene2DLevel right = Assert.Single(ldtk.Document!.Levels);
        Assert.Equal(new DrawSize(16, 16), left.TileMap.TileSize);
        Assert.Equal(left.TileMap.TileSize, right.TileMap.TileSize);
        Assert.Equal(3, left.TileMap.Layers.Count);
        Assert.Equal(3, right.TileMap.Layers.Count);
        foreach (string layerId in new[] { "1", "2" })
        {
            Assert.True(left.TileMap.TryGetLayer(layerId, out TileLayer2DModel? a));
            Assert.True(right.TileMap.TryGetLayer(layerId, out TileLayer2DModel? b));
            Assert.Equal(a!.Order, b!.Order);
            Assert.Equal(a.Offset, b.Offset);
            // Tiled preserves its 8x8 chunks; LDtk exports one finite grid per layer.
            Assert.Equal(32, a.Chunks.Count);
            Assert.Single(b.Chunks);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 64; x++)
            {
                Assert.True(a.TryGetCell(new TileCoordinate2D(x, y), out TileCell2D cellA));
                Assert.True(b.TryGetCell(new TileCoordinate2D(x, y), out TileCell2D cellB));
                Assert.Equal(cellA, cellB);
            }
        }
        Assert.Equal(8, left.Entities.Count);
        Assert.Equal(8, right.Entities.Count);
        foreach (Scene2DEntity entity in left.Entities)
        {
            Scene2DEntity other = Assert.Single(right.Entities.Where(e => Equals(e.Properties["Label"], entity.Properties["Label"])));
            Assert.Equal(entity.Role, other.Role);
            Assert.Equal(entity.Position, other.Position);
            Assert.Equal(entity.Size, other.Size);
            Assert.Equal(entity.Colliders.Count, other.Colliders.Count);
            if (entity.Role == "Collider")
            {
                Assert.Equal("Box", entity.Shape);
                Assert.Equal(entity.Shape, other.Shape);
            }
        }
        Assert.Equal(new DrawPoint(226, 192), Assert.Single(left.Entities.Where(e => e.Role == "Spawn")).Position);
        Assert.Equal(new TileCellKey2D("2", 14, 9), Assert.Single(left.Promotions).Cell);
        Assert.Equal(Assert.Single(left.Promotions).Cell, Assert.Single(right.Promotions).Cell);
        Assert.Equal("Closed", Assert.Single(left.Promotions).Properties["InitialState"]);
        Assert.Equal("world-atlas.png", Assert.Single(tiled.Document.Assets).ResourceId.Key);
        Assert.Equal(Assert.Single(tiled.Document.Assets).Size, Assert.Single(ldtk.Document.Assets).Size);
    }

    private static string Assets()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "Playground", "Cerneala.Playground", "SceneWorldAssets");
    }
}
