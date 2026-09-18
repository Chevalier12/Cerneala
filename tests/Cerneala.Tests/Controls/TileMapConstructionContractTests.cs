using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

public sealed class TileMapConstructionContractTests
{
    [Fact]
    public void ResolutionKeepsOriginalDefinitionsIncludingUnusedOnesAndCopiesThePalette()
    {
        TileDefinition2D used = new(7, new DrawRect(0, 0, 16, 16));
        TileDefinition2D unused = new(int.MaxValue, new DrawRect(16, 0, 16, 16));
        TileSet2D first = new("first", new ResourceId<ImageResource>("atlas"), [used]);
        TileSet2D second = new("second", new ResourceId<ImageResource>("atlas"), [unused]);
        TileSet2D[] palette = [first, second];
        TileChunk2D chunk = new(default, 2, 1, [new TileCell2D(7), default]);

        TileMap2DModel model = new("map", new DrawSize(16, 16), palette, [chunk]);
        palette[0] = second;

        Assert.Same(first, model.TileSets[0]);
        Assert.True(model.TryResolveTile(7, out TileSet2D? actualSet, out TileDefinition2D? actualTile));
        Assert.Same(first, actualSet);
        Assert.Same(used, actualTile);
        Assert.True(model.TryResolveTile(int.MaxValue, out actualSet, out actualTile));
        Assert.Same(second, actualSet);
        Assert.Same(unused, actualTile);
        foreach (int missing in new[] { 0, -1, 8 })
        {
            Assert.False(model.TryResolveTile(missing, out actualSet, out actualTile));
            Assert.Null(actualSet);
            Assert.Null(actualTile);
        }
    }

    [Fact]
    public void DuplicatePaletteDiagnosticStillPrecedesUnresolvedCellDiagnostic()
    {
        TileSet2D first = new("first", new ResourceId<ImageResource>("atlas"), [new TileDefinition2D(7, new DrawRect(0, 0, 16, 16))]);
        TileSet2D second = new("second", new ResourceId<ImageResource>("atlas"), [new TileDefinition2D(7, new DrawRect(16, 0, 16, 16))]);
        TileChunk2D chunk = new(default, 1, 1, [new TileCell2D(99)]);

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            new TileMap2DModel("map", new DrawSize(16, 16), [first, second], [chunk]));

        Assert.Equal("tileSets", error.ParamName);
        Assert.Equal("SCN2D015", Scene2DModelValidator.GetDiagnostic(error)!.Code);
        Assert.Contains("Tile id 7 is defined by multiple tilesets.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownCellIsRejectedWithoutPublishingAnIncompleteLookup()
    {
        TileSet2D set = new("first", new ResourceId<ImageResource>("atlas"), [new TileDefinition2D(7, new DrawRect(0, 0, 16, 16))]);
        TileChunk2D chunk = new(default, 2, 1, [new TileCell2D(7), new TileCell2D(99)]);

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            new TileMap2DModel("map", new DrawSize(16, 16), [set], [chunk]));

        Assert.Equal("chunks", error.ParamName);
        Assert.Equal("SCN2D006", Scene2DModelValidator.GetDiagnostic(error)!.Code);
        Assert.Contains("Tile id 99 in map 'map' has no tileset definition.", error.Message, StringComparison.Ordinal);
    }
}
