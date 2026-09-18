using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;
using Xunit.Abstractions;

namespace Cerneala.Tests.Controls;

public sealed class TileMapIdentityValidationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(1, 2, 2, 1)]
    [InlineData(2, 1, 1, 2)]
    [InlineData(3, 3, 1, 1)]
    public void TilesetReportsTheDuplicateWhoseFirstOccurrenceIsEarliest(int first, int second, int third, int fourth)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Set("Atlas", first, second, third, fourth));
        Assert.StartsWith($"Tile id {first} is duplicated in tileset 'Atlas'.", error.Message);
        Assert.Equal("tiles", error.ParamName);
        Assert.Equal("SCN2D015", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }

    [Fact]
    public void DuplicateTilesetNamesKeepTheirPriorityAndFirstOccurrenceOrder()
    {
        TileSet2D[] sets = [Set("First", 1), Set("Second", 1), Set("Second", 2), Set("First", 2)];
        ArgumentException error = Assert.Throws<ArgumentException>(() => TileMap2DModel.ValidateUniqueIds(sets));
        Assert.StartsWith("Tileset id 'First' is duplicated.", error.Message);
        Assert.Equal("tileSets", error.ParamName);
        Assert.Equal("SCN2D015", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }

    [Fact]
    public void CrossSetDuplicatesKeepTheirFirstOccurrenceOrder()
    {
        TileSet2D[] sets = [Set("First", 1, 2), Set("Second", 2, 1)];
        ArgumentException error = Assert.Throws<ArgumentException>(() => TileMap2DModel.ValidateUniqueIds(sets));
        Assert.StartsWith("Tile id 1 is defined by multiple tilesets.", error.Message);
        Assert.Equal("tileSets", error.ParamName);
        Assert.Equal("SCN2D015", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }

    [Fact]
    public void TilesetNamesRemainOrdinalAndEmptyMapsRemainValid()
    {
        TileMap2DModel.ValidateUniqueIds([Set("atlas", 1), Set("Atlas", 2)]);
        TileMap2DModel.ValidateUniqueIds([]);
    }

    [Fact]
    public void NullDefinitionsStillPrecedeDuplicateDiagnostics()
    {
        TileDefinition2D tile = new(1, new DrawRect(0, 0, 1, 1));
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            new TileSet2D("Atlas", new ResourceId<ImageResource>("Atlas"), [tile, tile, null!]));
        Assert.Equal("SCN2D006", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }

    [Fact]
    public void PaletteConstructionUsesBoundedValidationScratch()
    {
        TileDefinition2D[] definitions = Enumerable.Range(1, 256)
            .Select(id => new TileDefinition2D(id, new DrawRect(0, 0, 1, 1))).ToArray();
        ResourceId<ImageResource> atlas = new("Atlas");
        _ = new TileSet2D("Palette", atlas, definitions);

        long before = GC.GetAllocatedBytesForCurrentThread();
        TileSet2D set = new("Palette", atlas, definitions);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        long maximum = 64L * definitions.Length + 1024;

        output.WriteLine($"Definitions={definitions.Length}; allocated={allocated}; maximum={maximum}");
        Assert.Equal(definitions, set.Tiles);
        Assert.True(allocated <= maximum, $"Palette construction allocated {allocated} bytes; maximum {maximum}.");
    }

    private static TileSet2D Set(string name, params int[] ids) =>
        new(name, new ResourceId<ImageResource>("Atlas"), ids.Select(id => new TileDefinition2D(id, new DrawRect(0, 0, 1, 1))));
}
