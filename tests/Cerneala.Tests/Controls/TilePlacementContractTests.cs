using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TilePlacementContractTests
{
    [Fact]
    public void ModelCopiesPlacementsWithoutInventingGridCells()
    {
        ImageReference image = new(new TestImage(32, 48));
        Tile first = new(image, -2.5f, 1.25f);
        Tile[] input = [first];
        TileMap2DModel model = new(input);
        input[0] = new Tile(image, 99);
        Assert.Same(first, Assert.Single(model.Tiles));
        Assert.Empty(model.TileSets);
        Assert.Equal(default, model.TileSize);
        Assert.Null(model.Bounds);
        Assert.Empty(Assert.Single(model.Layers).Chunks);
        Assert.False(model.TryResolveTile(1, out _, out _));
        Assert.False(Assert.Single(model.Layers).TryGetCell(default, out _));
        Assert.Throws<ArgumentNullException>(() => new Tile(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(image, float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(image, width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(image, height: float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(image, 2_000_000_000, width: 1_000));
        Assert.Throws<ArgumentException>(() => new TileMap2DModel(new Tile[] { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileMap2DModel([], version: 0));
    }

    [Fact]
    public void OverlapOrderAndIndependentNaturalDimensionsSurviveRetainedReuse()
    {
        TestImage a = new(32, 48);
        TestImage b = new(64, 12);
        Fixture fixture = Create([
            new Tile(new ImageReference(a), -2.5f, 1.25f),
            new Tile(new ImageReference(a), 10, 8, width: 7),
            new Tile(new ImageReference(b), 12, 9, height: 9),
            new Tile(new ImageReference(a), 13, 10, 4, 5)]);
        DrawSpriteBatch[] first = Batches(Record(fixture.Surface));
        Assert.Equal(new IDrawImage[] { a, b, a }, first.Select(batch => batch.Image));
        Assert.Equal(new[] { new DrawRect(-2.5f, 1.25f, 32, 48), new DrawRect(10, 8, 7, 48),
            new DrawRect(12, 9, 64, 9), new DrawRect(13, 10, 4, 5) },
            first.SelectMany(batch => batch.Sprites).Select(sprite => sprite.Destination));
        DrawSpriteBatch[] second = Batches(Record(fixture.Surface));
        for (int index = 0; index < first.Length; index++) { Assert.Same(first[index], second[index]); }
        Assert.Equal(3, fixture.Map.GetDiagnosticsSnapshot().BatchesReused);
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Empty(Assert.Single(fixture.Map.Layers).LogicalChildren);
    }

    [Fact]
    public void PartiallyVisibleNegativePlacementsUseActualExtentsAndHalfOpenCulling()
    {
        TestImage image = new(100, 30);
        Fixture fixture = Create([new Tile(new ImageReference(image), -90.5f, -20.25f)]);
        fixture.Surface.ViewBox = new DrawRect(0, 0, 10, 10);
        Assert.Single(Batches(Record(fixture.Surface, new DrawSize(10, 10))));
        fixture.Surface.ViewBox = new DrawRect(9.5f, 0, 10, 10);
        Assert.Empty(Batches(Record(fixture.Surface, new DrawSize(10, 10))));
        fixture.Surface.ViewBox = new DrawRect(0, 9.75f, 10, 10);
        Assert.Empty(Batches(Record(fixture.Surface, new DrawSize(10, 10))));
        fixture.Surface.ViewBox = new DrawRect(-1, -1, 10, 10);
        Assert.Single(Batches(Record(fixture.Surface, new DrawSize(10, 10))));
    }

    [Fact]
    public void MissingImageAndReplacementDimensionsRefreshBoundsAndDependentBatches()
    {
        ResourceId<ImageResource> id = new("Art");
        Fixture fixture = Create([new Tile(new ImageReference(id), -40, 0)]);
        fixture.Surface.ViewBox = new DrawRect(0, 0, 20, 20);
        Assert.Empty(Batches(Record(fixture.Surface)));
        TestImage small = new(32, 16);
        TestImage large = new(64, 24);
        fixture.Root.SetImageLoader(new TestLoader(new Dictionary<string, IDrawImage>
        {
            ["small.png"] = small, ["large.png"] = large
        }));
        fixture.Surface.Resources.SetResource(id, new ImageResource("small.png"));
        Assert.Empty(Batches(Record(fixture.Surface)));
        fixture.Surface.Resources.SetResource(id, new ImageResource("large.png"));
        DrawSpriteBatch batch = Assert.Single(Batches(Record(fixture.Surface)));
        Assert.Same(large, batch.Image);
        Assert.Equal(new DrawRect(-40, 0, 64, 24), Assert.Single(batch.Sprites).Destination);
        fixture.Surface.Resources.SetResource(id, new ImageResource("small.png"));
        Assert.Empty(Batches(Record(fixture.Surface)));
    }

    [Fact]
    public void DeclarationOrderCrossesInternalChunkBoundariesWithoutPerPlacementNodes()
    {
        TestImage a = new(32, 32);
        TestImage b = new(32, 32);
        Tile[] placements = Enumerable.Range(0, 513).Select(index =>
            new Tile(new ImageReference(index % 2 == 0 ? a : b), index % 3, index % 5)).ToArray();
        Fixture fixture = Create(placements);
        DrawSpriteBatch[] batches = Batches(Record(fixture.Surface));
        Assert.Equal(513, batches.Length);
        Assert.Equal(placements.Select(tile => tile.Image.DirectImage), batches.Select(batch => batch.Image));
        Assert.Equal(3, fixture.Map.GetDiagnosticsSnapshot().VisibleChunks);
        Assert.Single(fixture.Map.LogicalChildren);
        Assert.Empty(Assert.Single(fixture.Map.Layers).LogicalChildren);
    }

    [Fact]
    public void ReplacingImmutableModelChangesGeometryEvenAtDefaultVersion()
    {
        ImageReference image = new(new TestImage(16, 16));
        Fixture fixture = Create([new Tile(image, 10)]);
        DrawSpriteBatch first = Assert.Single(Batches(Record(fixture.Surface)));
        fixture.Map.Model = new TileMap2DModel([new Tile(image, 20)]);
        DrawSpriteBatch second = Assert.Single(Batches(Record(fixture.Surface)));
        Assert.NotSame(first, second);
        Assert.Equal(20, Assert.Single(second.Sprites).Destination.X);
    }

    [Fact]
    public void ValidatorCountsPlacementsAndChecksResolvedImageGeometry()
    {
        ResourceId<ImageResource> id = new("Art");
        TileMap2DModel model = new([new Tile(new ImageReference(id), 1_999_999_872)]);
        Assert.False(Scene2DModelValidator.Validate(model, new Dictionary<string, DrawSize>()).Success);
        Assert.False(Scene2DModelValidator.Validate(model, new Dictionary<string, DrawSize> { ["Art"] = new(256, 16) }).Success);
        TileMap2DModel overBudget = new([model.Tiles[0], model.Tiles[0]]);
        Assert.False(Scene2DModelValidator.Validate(overBudget, new Dictionary<string, DrawSize> { ["Art"] = new(16, 16) },
            new Scene2DValidationOptions { MaxCells = 1 }).Success);
        Assert.True(Scene2DModelValidator.Validate(model, new Dictionary<string, DrawSize> { ["Art"] = new(16, 16) }).Success);
    }

    [Fact]
    public void LevelAndDocumentValidateFreePlacementWorldExtents()
    {
        ResourceId<ImageResource> id = new("Art");
        ImageReference resource = new(id);
        TileMap2DModel explicitSize = new([new Tile(resource, width: 256, height: 16)]);
        ArgumentException explicitError = Assert.Throws<ArgumentException>(() =>
            new Scene2DLevel("World", explicitSize, new DrawPoint(1_999_999_872, 0)));
        Assert.Equal("SCN2D014", Scene2DModelValidator.GetDiagnostic(explicitError)?.Code);

        TileMap2DModel naturalSize = new([new Tile(resource)]);
        Scene2DLevel level = new("World", naturalSize, new DrawPoint(1_999_999_872, 0));
        ArgumentException naturalError = Assert.Throws<ArgumentException>(() =>
            new Scene2DDocument([level], [new Scene2DAsset(id, "art.png", new DrawSize(256, 16))]));
        Assert.Equal("SCN2D014", Scene2DModelValidator.GetDiagnostic(naturalError)?.Code);
        _ = new Scene2DDocument([level], [new Scene2DAsset(id, "art.png", new DrawSize(16, 16))]);
    }

    [Fact]
    public void WarmAllocationsDoNotScaleWithPlacementsInsideAChunk()
    {
        ImageReference image = new(new TestImage(16, 16));
        Fixture small = Create(Enumerable.Range(0, 8).Select(_ => new Tile(image)));
        Fixture large = Create(Enumerable.Range(0, 256).Select(_ => new Tile(image)));
        static long Measure(RenderSurface2D surface)
        {
            for (int index = 0; index < 64; index++) { _ = Record(surface); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 256; index++) { _ = Record(surface); }
            return (GC.GetAllocatedBytesForCurrentThread() - before) / 256;
        }
        long smallBytes = Measure(small.Surface);
        long largeBytes = Measure(large.Surface);
        Assert.InRange(largeBytes - smallBytes, -256, 256);
        Assert.Equal(0, large.Map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.Equal(1, large.Map.GetDiagnosticsSnapshot().BatchesReused);
    }

    [Fact]
    public void TransformAndCameraReuseBatchesAndReparentResolvesNewResourceScope()
    {
        ResourceId<ImageResource> id = new("Art");
        Fixture fixture = Create([new Tile(new ImageReference(id), 10, 10)]);
        TestImage firstImage = new(16, 16);
        TestImage secondImage = new(32, 32);
        fixture.Root.SetImageLoader(new TestLoader(new Dictionary<string, IDrawImage> { ["a.png"] = firstImage, ["b.png"] = secondImage }));
        fixture.Surface.Resources.SetResource(id, new ImageResource("a.png"));
        DrawSpriteBatch first = Assert.Single(Batches(Record(fixture.Surface)));
        fixture.Map.TranslateX = 5;
        fixture.Surface.ViewBox = new DrawRect(1, 1, 64, 32);
        Assert.Same(first, Assert.Single(Batches(Record(fixture.Surface))));

        Scene2D nextScene = new();
        RenderSurface2D nextSurface = new() { Scene = nextScene };
        nextSurface.Resources.SetResource(id, new ImageResource("b.png"));
        fixture.Root.VisualChildren.Add(nextSurface);
        fixture.Surface.Scene!.Children.Remove(fixture.Map);
        nextScene.Children.Add(fixture.Map);
        DrawSpriteBatch second = Assert.Single(Batches(Record(nextSurface)));
        Assert.Same(secondImage, second.Image);
        Assert.Equal(new DrawRect(10, 10, 32, 32), Assert.Single(second.Sprites).Destination);
    }

    private static Fixture Create(IEnumerable<Tile> placements)
    {
        TileMap2D map = new() { Model = new TileMap2DModel(placements) };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        return new(root, surface, map);
    }

    private static DrawCommandList Record(RenderSurface2D surface, DrawSize? size = null)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, size?.Width ?? 128, size?.Height ?? 64));
        return commands;
    }

    private static DrawSpriteBatch[] Batches(DrawCommandList commands) => commands
        .Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch)
        .Select(command => command.SpriteBatch!).ToArray();

    private sealed record Fixture(UIRoot Root, RenderSurface2D Surface, TileMap2D Map);
    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width => width;
        public int Height => height;
    }
    private sealed class TestLoader(IReadOnlyDictionary<string, IDrawImage> images) : IImageLoader
    {
        public IDrawImage Load(string path) => images[path];
    }
}
