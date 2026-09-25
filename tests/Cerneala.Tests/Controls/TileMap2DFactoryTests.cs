using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

public sealed class TileMap2DFactoryTests
{
    [Theory]
    [InlineData(-17)]
    [InlineData(23)]
    public async Task FromModelCreatesDetachedNodeWithModelOrderAndAdaptedGridData(int order)
    {
        TileMap2DModel model = GridModel(order);

        TileMap2D map = TileMap2D.FromModel(model);

        Assert.Null(map.LogicalParent);
        Assert.Equal(order, map.Layer);
        TileMapSource2D source = Assert.IsType<TileMapSource2D>(map.Source);
        Assert.Equal(model.Id, source.Catalog.Id);
        Assert.Equal(model.Order, source.Catalog.Order);
        TileMapChunkInfo2D chunk = Assert.Single(source.Catalog.Chunks);
        using SceneSpatialLease2D<TileMapChunkData2D> lease = await source.LoadAsync(chunk.Spatial);
        Assert.Same(model.Chunks[0], lease.Value.Grid);
    }

    [Fact]
    public void FromModelForwardsImageMetadataAndSourceValidation()
    {
        ImageReference picture = new(new ResourceId<ImageResource>("Picture"));
        TileMap2DModel model = new([new Tile(picture)]);
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TileMap2D.FromModel(null!)).ParamName);
        Assert.Throws<ArgumentException>(() => TileMap2D.FromModel(model));

        Dictionary<string, DrawSize> imageSizes = new() { ["Picture"] = new(20, 30) };
        TileMap2D map = TileMap2D.FromModel(model, imageSizes);
        TileMapSource2D source = Assert.IsType<TileMapSource2D>(map.Source);
        Assert.Equal(new DrawRect(0, 0, 20, 30), Assert.Single(source.Catalog.Entries).Bounds);
        Assert.True(source.Catalog.TryGetImageSize(picture, out DrawSize size));
        Assert.Equal(new DrawSize(20, 30), size);
    }

    [Fact]
    public void AssigningSourceDirectlyDoesNotCopyCatalogOrderToLayer()
    {
        TileMap2DModel model = GridModel(order: 11);

        TileMap2D map = new() { Source = TileMapSource2D.FromModel(model) };

        Assert.Equal(11, map.Source!.Catalog.Order);
        Assert.Equal(0, map.Layer);
    }

    private static TileMap2DModel GridModel(int order) => new(
        "Ground", new DrawSize(16, 16),
        [new TileSet2D("Terrain", new ResourceId<ImageResource>("TerrainAtlas"),
            [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
        [new TileChunk2D(default, 1, 1, [new TileCell2D(1)])],
        order: order);
}
