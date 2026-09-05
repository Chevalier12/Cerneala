using System.Diagnostics;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Xunit.Abstractions;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneDocumentContractTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DocumentSnapshotsMetadataAndSparsePromotionsWithoutCreatingNodesOrOpeningFiles()
    {
        Dictionary<string, object?> properties = new() { ["InitialState"] = "Closed" };
        TilePromotion2D promotion = new(new TileCellKey2D("Layer", 1, 0), 1, properties);
        Scene2DEntity entity = new("Spawn", "Layer", new DrawPoint(2, 3), default, role: "Spawn");
        TilePromotion2D[] promotions = [promotion];
        Scene2DEntity[] entities = [entity];
        Scene2DLevel level = new("Level", Model(), entities: entities, promotions: promotions);
        Scene2DAsset asset = new(new ResourceId<ImageResource>("Atlas"), "not-loaded\\atlas.png", new DrawSize(2, 1));
        Scene2DDocument document = new([level], [asset]);
        properties["InitialState"] = "Open";
        promotions[0] = null!;
        entities[0] = null!;
        Assert.Same(entity, Assert.Single(level.Entities));
        Assert.Same(promotion, Assert.Single(level.Promotions));
        Assert.Equal("Closed", promotion.Properties["InitialState"]);
        Assert.Equal("not-loaded/atlas.png", asset.Path);
        Assert.True(Scene2DModelValidator.Validate(document).Success);
        Assert.False(Scene2DModelValidator.Validate(document, new Scene2DValidationOptions { MaxCells = 1 }).Success);
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("/absolute.png")]
    [InlineData("C:\\absolute.png")]
    [InlineData("//server/share/image.png")]
    [InlineData("https://host/image.png")]
    [InlineData("image.png:stream")]
    [InlineData("a//b.png")]
    [InlineData("a/./b.png")]
    [InlineData("a/../b.png")]
    [Trait("SceneImportStage", "1")]
    public void AssetDeclarationsRejectNonNormalizedOrNonLocalPaths(string path)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Scene2DAsset(
            new ResourceId<ImageResource>("Atlas"), path, new DrawSize(1, 1)));
        Assert.Equal("SCN2D010", Scene2DModelValidator.GetDiagnostic(exception)!.Code);
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void IdentityAndSchemaFailuresStayControlled()
    {
        Scene2DLevel level = new("Level", Model());
        Scene2DAsset asset = new(new ResourceId<ImageResource>("Atlas"), "atlas.png", new DrawSize(2, 1));
        AssertCode("SCN2D015", () => new Scene2DDocument([level, level], [asset]));
        AssertCode("SCN2D015", () => new Scene2DDocument([level], [asset, asset]));
        AssertCode("SCN2D003", () => new Scene2DDocument([level], [asset], schemaVersion: 0));
        Scene2DEntity missingLayer = new("Entity", "Missing", default, default);
        AssertCode("SCN2D015", () => new Scene2DLevel("Level", Model(), entities: [missingLayer]));
        Scene2DEntity entity = new("Entity", "Layer", default, default);
        AssertCode("SCN2D015", () => new Scene2DLevel("Level", Model(), entities: [entity, entity]));
        AssertCode("SCN2D008", () => new Scene2DLevel("Level", Model(), entities: [null!]));
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DiagnosticTruncationCannotTurnALaterErrorIntoSuccessOrAnUnrelatedException()
    {
        Scene2DDiagnosticCollector collector = new(new Scene2DValidationOptions { MaxDiagnostics = 1 });
        collector.Add(new Scene2DDiagnostic("SCN2D017", Scene2DDiagnosticSeverity.Warning, "Known editor metadata."));
        collector.Error("SCN2D008", "Invalid collider.", "$.collider");
        Scene2DValidationResult result = collector.Complete();
        Assert.False(result.Success);
        Assert.True(result.DiagnosticsTruncated);
        Assert.Equal(Scene2DDiagnosticSeverity.Warning, Assert.Single(result.Diagnostics).Severity);
        ArgumentException error = Assert.Throws<ArgumentException>(() => Scene2DModelValidator.ThrowIfInvalid(result, "model"));
        Assert.Equal("SCN2D008", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DeterministicHostileCoreMatrixRejectsWithoutArithmeticOrTailEnumerationFailures()
    {
        Random random = new(0x51CE);
        int rejected = 0;
        Stopwatch watch = Stopwatch.StartNew();
        for (int index = 0; index < 256; index++)
        {
            int width = index % 2 == 0 ? int.MaxValue - random.Next(100) : -random.Next(1, 100);
            Exception? error = Record.Exception(() => new TileChunk2D(default, width, int.MaxValue, []));
            Assert.IsAssignableFrom<ArgumentException>(error);
            Assert.NotNull(Scene2DModelValidator.GetDiagnostic(error!));
            rejected++;
        }
        AssertCode("SCN2D013", () => new TileColliderDescriptor2D(TileColliderShape2D.Polygon,
            points: new string(' ', 393_217) + "0,0 1,0 0,1"));
        output.WriteLine($"seed=0x51CE iterations=256 rejected={rejected} elapsedMs={watch.Elapsed.TotalMilliseconds:F3}");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void MaximumChunkCountCompletesAndOneMoreFailsBeforeTheUnboundedTail()
    {
        TileChunk2D[] chunks = Enumerable.Range(0, 65_536)
            .Select(index => new TileChunk2D(new TileCoordinate2D(index * 2, 0), 1, 1, [default])).ToArray();
        Stopwatch watch = Stopwatch.StartNew();
        TileLayer2DModel layer = new("Sparse", chunks);
        output.WriteLine($"chunks={layer.Chunks.Count} constructionMs={watch.Elapsed.TotalMilliseconds:F3}");
        IEnumerable<TileChunk2D> Hostile()
        {
            foreach (TileChunk2D chunk in chunks) { yield return chunk; }
            yield return chunks[0];
            throw new InvalidOperationException("The unbounded tail must not be visited.");
        }
        AssertCode("SCN2D013", () => new TileLayer2DModel("Sparse", Hostile()));
    }

    [Theory]
    [InlineData(-1f, false)]
    [InlineData(1f, false)]
    [InlineData(-1f, true)]
    [InlineData(1f, true)]
    [Trait("SceneImportStage", "1")]
    public void ContinuousSegmentContactHasOpposingNormalAndStopsCirclesAndBoxes(float direction, bool box)
    {
        SegmentCollider2D segment = new() { EndY = 20, EndX = 0, TranslateY = -10 };
        Collider2D mover = box ? new BoxCollider2D { Width = 2, Height = 2, OffsetX = -1, OffsetY = -1 } : new CircleCollider2D { Radius = 1 };
        mover.TranslateX = -20 * direction;
        Scene2D scene = new();
        scene.Children.Add(segment);
        scene.Children.Add(mover);
        MoveCollisionResult2D move = scene.CollisionWorld.MoveAndCollide(mover, new Vector2(1000 * direction, 0));
        Assert.NotNull(move.Collision);
        Assert.InRange(MathF.Abs(move.Travel.X), 18.98f, 19.02f);
        Assert.True(Vector2.Dot(move.Collision.Normal, new Vector2(direction, 0)) < -0.99f);
        Assert.InRange(MathF.Abs(move.Collision.Normal.Y), 0, 0.01f);
    }

    private static TileMap2DModel Model() => new(new DrawSize(1, 1),
        [new TileSet2D("Atlas", new ResourceId<ImageResource>("Atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1))])],
        [new TileLayer2DModel("Layer", [new TileChunk2D(default, 2, 1, [new TileCell2D(1), default])])]);

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void DiagnosticTextCannotMultiplyUntrustedStringsAcrossTheRetentionBudget()
    {
        string hostile = new('x', 10_000);
        Scene2DDiagnosticCollector collector = new(null);
        collector.Add(new Scene2DDiagnostic("SCN2D008", Scene2DDiagnosticSeverity.Error, hostile, hostile, "$" + hostile));
        Scene2DDiagnostic diagnostic = Assert.Single(collector.Complete().Diagnostics);
        Assert.InRange(diagnostic.Message.Length, 1, 4096);
        Assert.InRange(diagnostic.FilePath.Length, 1, 4096);
        Assert.InRange(diagnostic.JsonPath.Length, 1, 4096);
        Assert.EndsWith("...", diagnostic.Message);
        Assert.StartsWith("$", diagnostic.JsonPath);
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ChunkSweepMatchesIndependentExhaustiveOverlapOracle()
    {
        Random random = new(0xC11);
        for (int trial = 0; trial < 512; trial++)
        {
            TileChunk2D[] chunks = Enumerable.Range(0, 24).Select(_ =>
            {
                int width = random.Next(1, 5), height = random.Next(1, 5);
                return new TileChunk2D(new TileCoordinate2D(random.Next(-40, 41), random.Next(-40, 41)),
                    width, height, Enumerable.Repeat(default(TileCell2D), width * height));
            }).ToArray();
            bool overlaps = false;
            for (int first = 0; first < chunks.Length; first++)
            {
                for (int second = first + 1; second < chunks.Length; second++)
                {
                    TileChunk2D a = chunks[first], b = chunks[second];
                    overlaps |= Math.Max(a.Origin.X, b.Origin.X) < Math.Min(a.Origin.X + a.Width, b.Origin.X + b.Width) &&
                        Math.Max(a.Origin.Y, b.Origin.Y) < Math.Min(a.Origin.Y + a.Height, b.Origin.Y + b.Height);
                }
            }
            Exception? error = Record.Exception(() => new TileLayer2DModel("Layer", chunks));
            Assert.Equal(overlaps, error is not null);
            if (error is not null) { Assert.Equal("SCN2D011", Scene2DModelValidator.GetDiagnostic(error)!.Code); }
        }
        output.WriteLine("seed=0xC11 trials=512 rectanglesPerTrial=24 exhaustiveOracle=matched");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ChunkSweepHandlesTheMaximumDenseActiveIntervalSet()
    {
        TileChunk2D[] chunks = Enumerable.Range(0, 65_536).Select(index =>
            new TileChunk2D(new TileCoordinate2D(0, index * 2), 1, 1, [default])).Reverse().ToArray();
        Stopwatch watch = Stopwatch.StartNew();
        TileLayer2DModel layer = new("Vertical", chunks);
        Assert.Equal(chunks, layer.Chunks);
        output.WriteLine($"denseActiveChunks=65536 constructionMs={watch.Elapsed.TotalMilliseconds:F3}");
        AssertCode("SCN2D011", () => new TileLayer2DModel("Overlap", chunks.Take(65_535).Append(chunks[0])));
    }

    private static void AssertCode(string code, Action action)
    {
        Exception? error = Record.Exception(action);
        Assert.IsAssignableFrom<ArgumentException>(error);
        Assert.Equal(code, Scene2DModelValidator.GetDiagnostic(error!)!.Code);
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void SparsePromotionValidationDoesNotRescanEveryChunkForEveryAddress()
    {
        const int count = 16_384;
        TileChunk2D[] chunks = Enumerable.Range(0, count).Select(index =>
            new TileChunk2D(new TileCoordinate2D(index * 2, 0), 1, 1, [new TileCell2D(1)])).ToArray();
        TileMap2DModel map = new(new DrawSize(1, 1),
            [new TileSet2D("Atlas", new ResourceId<ImageResource>("Atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1))])],
            [new TileLayer2DModel("Sparse", chunks)]);
        TilePromotion2D[] promotions = Enumerable.Range(0, count).Reverse()
            .Select(index => new TilePromotion2D(new TileCellKey2D("Sparse", index * 2, 0))).ToArray();
        Stopwatch watch = Stopwatch.StartNew();
        Scene2DLevel level = new("Level", map, promotions: promotions);
        Assert.Equal(count, level.Promotions.Count);
        output.WriteLine($"sparsePromotions={count} chunks={count} validationMs={watch.Elapsed.TotalMilliseconds:F3}");
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void ProgrammaticGraphCannotAmplifySharedChunksPastTheMapBudget()
    {
        TileChunk2D chunk = new(default, 524_288, 1, Enumerable.Repeat(default(TileCell2D), 524_288));
        TileLayer2DModel[] layers = Enumerable.Range(0, 3).Select(index => new TileLayer2DModel(index.ToString(), [chunk])).ToArray();
        AssertCode("SCN2D013", () => new TileMap2DModel(new DrawSize(1, 1), [], layers));
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void OptionalParsersCanReuseTheCoreDiagnosticCollectorInsteadOfDuplicatingItsRules()
    {
        Assert.True(typeof(Scene2DDiagnosticCollector).IsPublic);
        Assert.NotNull(typeof(Scene2DDiagnosticCollector).GetConstructor([typeof(int)]));
        Assert.NotNull(typeof(Scene2DDiagnosticCollector).GetMethod(nameof(Scene2DDiagnosticCollector.Add)));
        Assert.NotNull(typeof(Scene2DDiagnosticCollector).GetMethod(nameof(Scene2DDiagnosticCollector.Complete)));
        Scene2DDiagnosticCollector collector = new(null);
        Assert.Throws<ArgumentNullException>(() => collector.Add(null!));
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void InvalidResolvedAtlasBoundsCannotLeavePartiallyBuiltChunkCaches()
    {
        ResourceId<ImageResource> resourceId = new("Atlas");
        TileSet2D set = new("Atlas", resourceId,
            [new TileDefinition2D(1, new DrawRect(0, 0, 1, 1)), new TileDefinition2D(2, new DrawRect(1, 0, 2, 1))]);
        TileMap2D map = new() { Model = new(new DrawSize(1, 1), [set],
            [new TileLayer2DModel("Layer", [new TileChunk2D(default, 1, 1, [new TileCell2D(1)]),
                new TileChunk2D(new TileCoordinate2D(1, 0), 1, 1, [new TileCell2D(2)])])]) };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        surface.Resources.SetResource(resourceId, new ImageResource("atlas"));
        UIRoot root = new();
        root.SetImageLoader(new AtlasLoader());
        root.VisualChildren.Add(surface);
        DrawCommandList commands = new();
        Assert.ThrowsAny<ArgumentException>(() => ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 10, 10)));
        Assert.Equal(0, map.GetDiagnosticsSnapshot().BatchesBuilt);
        Assert.DoesNotContain(commands, command => command.Kind is DrawCommandKind.DrawImage or DrawCommandKind.DrawSpriteBatch);
    }

    private sealed class AtlasImage : IDrawImage
    {
        public int Width => 2;
        public int Height => 1;
    }

    private sealed class AtlasLoader : IImageLoader
    {
        public IDrawImage Load(string path) => new AtlasImage();
    }
}
