using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class TileMapSingleLayerContractTests
{
    [Theory]
    [InlineData("Cerneala.UI.Controls.TileLayer2D")]
    [InlineData("Cerneala.UI.Controls.TileLayer2DModel")]
    [InlineData("Cerneala.UI.Controls.TileInstance2D")]
    public void PublicApiHasNoSeparateLayerOrPromotedTileNode(string name)
    {
        Assert.Null(typeof(TileMap2D).Assembly.GetType(name));
    }

    [Fact]
    public void MapHasNoLiveTileChildren()
    {
        Assert.Null(typeof(TileMap2D).GetProperty("Layers"));
        Assert.Null(typeof(TileMap2D).GetProperty("PromotedTiles"));
        Assert.Null(typeof(TileMap2D).GetMethod("Promote"));
        Assert.Empty(new TileMap2D().LogicalChildren);
    }

    [Fact]
    public void ModelContainsChunksAndLevelContainsIndependentMaps()
    {
        Assert.Null(typeof(TileMap2DModel).GetProperty("Layers"));
        Assert.Equal(typeof(IReadOnlyList<TileChunk2D>),
            typeof(TileMap2DModel).GetProperty("Chunks")?.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<Tile>),
            typeof(TileMap2DModel).GetProperty("Tiles")?.PropertyType);
        Assert.Null(typeof(Scene2DLevel).GetProperty("TileMap"));
        Assert.Equal(typeof(IReadOnlyList<TileMap2DModel>),
            typeof(Scene2DLevel).GetProperty("TileMaps")?.PropertyType);
    }
}
