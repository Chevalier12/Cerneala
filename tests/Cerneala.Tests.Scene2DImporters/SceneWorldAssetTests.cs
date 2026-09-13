using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DImporters;

public sealed class SceneWorldAssetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoorIsAuthoredAboveTheFacadeWithoutDuplicatingItsCell(bool useLdtk)
    {
        Scene2DImportResult result = useLdtk
            ? LdtkScene2DImporter.Import(Path.Combine(Assets(), "village.ldtk"))
            : TiledScene2DImporter.Import(Path.Combine(Assets(), "village.tmj"));
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Scene2DLevel level = Assert.Single(result.Document!.Levels);
        TileCellKey2D door = Assert.Single(level.Promotions).Cell;
        Assert.NotEqual("2", door.MapId);
        TileMap2DModel facade = Assert.Single(level.TileMaps.Where(map => map.Id == "2"));
        TileMap2DModel foreground = Assert.Single(level.TileMaps.Where(map => map.Id == door.MapId));
        Assert.True(foreground!.Order > facade!.Order);
        Assert.True(facade.TryGetCell(new(14, 9), out TileCell2D oldCell));
        Assert.Equal(0, oldCell.TileId);
        Assert.True(foreground.TryGetCell(new(14, 9), out TileCell2D doorCell));
        Assert.Equal(7, doorCell.TileId);
    }

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
        Assert.Equal(new DrawSize(16, 16), left.TileSize);
        Assert.Equal(left.TileSize, right.TileSize);
        Assert.Equal(4, left.TileMaps.Count);
        Assert.Equal(4, right.TileMaps.Count);
        foreach (string layerId in new[] { "1", "2" })
        {
            TileMap2DModel a = Assert.Single(left.TileMaps.Where(map => map.Id == layerId));
            TileMap2DModel b = Assert.Single(right.TileMaps.Where(map => map.Id == layerId));
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
            Assert.Equal(entity.Collider is not null, other.Collider is not null);
            if (entity.Role == "Collider")
            {
                Assert.Equal("Box", entity.Shape);
                Assert.Equal(entity.Shape, other.Shape);
            }
        }
        Assert.Equal(new DrawPoint(226, 192), Assert.Single(left.Entities.Where(e => e.Role == "Spawn")).Position);
        Assert.Equal(new TileCellKey2D("4", 14, 9), Assert.Single(left.Promotions).Cell);
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
