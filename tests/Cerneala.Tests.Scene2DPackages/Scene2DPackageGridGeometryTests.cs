using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Scene2DPackages;

public sealed partial class Scene2DPackageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubdivisionPreservesNearestCollisionGeometryAndReleasesAdapters(bool patterned)
    {
        using Fixture fixture = new();
        TileSet2D set = new("set", new("atlas"),
        [
            new(1, new(0, 0, 10, 10), collider: new(TileColliderShape2D.Box, width: 10, height: 10)),
            new(2, new(10, 0, 10, 10), collider: new(TileColliderShape2D.Polygon, points: "0,0 10,0 0,10", offsetX: -2, offsetY: 3))
        ]);
        TileCell2D[] cells = Enumerable.Range(0, 33 * 19).Select(index => patterned
            ? new TileCell2D(index % 9 == 0 ? 0 : index % 3 == 0 ? 2 : 1, (TileFlip2D)(index % 8)) : new TileCell2D(1)).ToArray();
        TileMap2DModel model = new("map", new(10, 10), [set], [new(new(-17, -9), 33, 19, cells)], offset: new(2, 3));
        await fixture.WriteAsync(new([new Scene2DLevel("level", [model])], [new(new("atlas"), "nested/atlas.bin", new(20, 10))]));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        TileMap2D expected = new() { Source = TileMapSource2D.FromModel(model) };
        TileMap2D actual = new() { Source = package.Levels[0].TileMaps[0] };
        var before = new global::Cerneala.UI.Controls.Scene2D();
        var after = new global::Cerneala.UI.Controls.Scene2D();
        before.Children.Add(expected);
        after.Children.Add(actual);
        using SceneSimulationContext2D firstContext = new(before), secondContext = new(after);
        Task<SceneCollisionRegion2D> firstRequest = before.CollisionWorld.PrepareRegionAsync(new(-200, -120, 400, 260)).AsTask();
        Task<SceneCollisionRegion2D> secondRequest = after.CollisionWorld.PrepareRegionAsync(new(-200, -120, 400, 260)).AsTask();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            firstContext.Update(); secondContext.Update(); return firstRequest.IsCompleted && secondRequest.IsCompleted;
        }, TimeSpan.FromSeconds(5)));
        using SceneCollisionRegion2D firstRegion = await firstRequest, secondRegion = await secondRequest;
        // Full-cell boxes coalesce horizontally, not between authored rows.
        if (!patterned) { Assert.Equal(19, expected.LogicalChildren.Count); Assert.Equal(57, actual.LogicalChildren.Count); }
        for (int step = 0; step <= 64; step++)
        {
            float x = -174.5f + step * 5.25f;
            CompareRay(new(x, -115), Vector2.UnitY, 250);
            CompareRay(new(x, 135), -Vector2.UnitY, 250);
        }
        for (int step = 0; step <= 40; step++)
        {
            float y = -92.5f + step * 5.25f;
            CompareRay(new(-195, y), Vector2.UnitX, 390);
            CompareRay(new(195, y), -Vector2.UnitX, 390);
        }
        firstRegion.Dispose(); secondRegion.Dispose();
        firstContext.Update(); secondContext.Update();
        Assert.Empty(expected.LogicalChildren);
        Assert.Empty(actual.LogicalChildren);
        Assert.Throws<SceneCollisionRegionNotReadyException>(() => after.CollisionWorld.Raycast(new(-165, -115), Vector2.UnitY, 250));

        void CompareRay(Vector2 origin, Vector2 direction, float distance)
        {
            CollisionHit2D? reference = before.CollisionWorld.Raycast(origin, direction, distance).FirstOrDefault();
            CollisionHit2D? result = after.CollisionWorld.Raycast(origin, direction, distance).FirstOrDefault();
            Assert.Equal(reference is null, result is null);
            if (reference is null) { return; }
            // Grouping and hit multiplicity may differ; the first exposed surface must not.
            Assert.Equal(reference.Distance, result!.Distance, precision: 4);
            Assert.Equal(reference.Normal, result.Normal);
            Assert.Equal(reference.IsTrigger, result.IsTrigger);
        }
    }

    [Theory]
    [InlineData(0)] // Disk package.
    [InlineData(1)] // Independently authored small grid chunks, without package I/O.
    [InlineData(2)] // Ordinary live boxes, without TileMap or spatial source on the tested side.
    public async Task MovementAcrossPreparedBoundariesKeepsTravelAndContactNormals(int targetKind)
    {
        using Fixture fixture = new();
        TileSet2D set = new("set", new("atlas"), [new(1, new(0, 0, 10, 10),
            collider: new(TileColliderShape2D.Box, width: 10, height: 10))]);
        TileMap2DModel model = new("map", new(10, 10), [set], [new(new(-32, 0), 64, 1, Enumerable.Repeat(new TileCell2D(1), 64))]);
        if (targetKind == 0)
        {
            await fixture.WriteAsync(new([new Scene2DLevel("level", [model])], [new(new("atlas"), "nested/atlas.bin", new(20, 10))]));
        }
        using Scene2DPackage? package = targetKind == 0 ? await Scene2DPackage.OpenAsync(fixture.Output) : null;
        var before = new global::Cerneala.UI.Controls.Scene2D();
        var after = new global::Cerneala.UI.Controls.Scene2D();
        before.Children.Add(new TileMap2D { Source = TileMapSource2D.FromModel(model) });
        if (targetKind == 0) { after.Children.Add(new TileMap2D { Source = package!.Levels[0].TileMaps[0] }); }
        else if (targetKind == 1)
        {
            TileChunk2D[] independent = Enumerable.Range(0, 4).Select(index => new TileChunk2D(new(-32 + index * 16, 0),
                16, 1, Enumerable.Repeat(new TileCell2D(1), 16))).ToArray();
            after.Children.Add(new TileMap2D { Source = TileMapSource2D.FromModel(new("map", new(10, 10), [set], independent)) });
        }
        else
        {
            for (int index = 0; index < 4; index++)
            {
                after.Children.Add(new Sprite2D { X = -320 + index * 160, Collider = new BoxCollider2D { Width = 160, Height = 10 } });
            }
        }
        Sprite2D first = new() { Width = 4, Height = 4 }, second = new() { Width = 4, Height = 4 };
        BoxCollider2D firstCollider = new() { Width = 4, Height = 4 }, secondCollider = new() { Width = 4, Height = 4 };
        first.Collider = firstCollider; second.Collider = secondCollider;
        before.Children.Add(first); after.Children.Add(second);
        using SceneSimulationContext2D firstContext = new(before), secondContext = new(after);
        Task<SceneCollisionRegion2D> firstRequest = before.CollisionWorld.PrepareRegionAsync(new(-330, -40, 660, 100)).AsTask();
        Task<SceneCollisionRegion2D> secondRequest = after.CollisionWorld.PrepareRegionAsync(new(-330, -40, 660, 100)).AsTask();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            firstContext.Update(); secondContext.Update(); return firstRequest.IsCompleted && secondRequest.IsCompleted;
        }, TimeSpan.FromSeconds(5)));
        using SceneCollisionRegion2D firstRegion = await firstRequest, secondRegion = await secondRequest;
        List<string> differences = [];
        foreach (float x in new[] { -165, -162, -160, -155, -5, -2, 0, 5, 155, 158, 160, 165 })
        {
            first.X = second.X = x;
            first.Y = second.Y = -30;
            MoveCollisionResult2D reference = before.CollisionWorld.MoveAndCollide(firstCollider, new(0, 60));
            MoveCollisionResult2D result = after.CollisionWorld.MoveAndCollide(secondCollider, new(0, 60));
            // Conservative advancement has a 1e-5 contact epsilon plus float
            // rounding; changing a coalesced box width need not preserve bits.
            Assert.Equal(26, reference.Travel.Y, precision: 4);
            Assert.Equal(26, result.Travel.Y, precision: 4);
            Assert.Equal(reference.Travel.X, result.Travel.X, precision: 4);
            Assert.Equal(reference.Travel.Y, result.Travel.Y, precision: 4);
            Assert.Equal(reference.Remainder.X, result.Remainder.X, precision: 4);
            Assert.Equal(reference.Remainder.Y, result.Remainder.Y, precision: 4);
            Assert.NotNull(reference.Collision); Assert.NotNull(result.Collision);
            if (reference.Collision.Normal != result.Collision.Normal)
            {
                differences.Add(FormattableString.Invariant($"kind={targetKind} x={x} authoredTravel={reference.Travel} preparedTravel={result.Travel} authoredNormal={reference.Collision.Normal} preparedNormal={result.Collision.Normal}"));
            }
        }
        first.X = second.X = -200;
        first.Y = second.Y = -5;
        MoveCollisionResult2D clear = after.CollisionWorld.MoveAndCollide(secondCollider, new(400, 0));
        Assert.Equal(new Vector2(400, 0), clear.Travel);
        Assert.Equal(before.CollisionWorld.MoveAndCollide(firstCollider, new(400, 0)).Travel, clear.Travel);
        Assert.Null(clear.Collision);
        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MaintainedWorldPackagesPreserveSmallTiledChunksAndSubdivideLdtkLayers(bool ldtk)
    {
        using Fixture fixture = new();
        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Cerneala.slnx"))) { repository = repository.Parent; }
        Assert.NotNull(repository);
        string assets = Path.Combine(repository.FullName, "Playground", "Cerneala.Playground", "SceneWorldAssets");
        Scene2DImportOptions options = new() { AssetRootDirectory = assets };
        Scene2DImportResult imported = ldtk ? LdtkScene2DImporter.Import(Path.Combine(assets, "village.ldtk"), options)
            : TiledScene2DImporter.Import(Path.Combine(assets, "village.tmj"), options);
        Assert.True(imported.Success, string.Join(Environment.NewLine, imported.Diagnostics.Select(item => item.Message)));
        await Scene2DPackageWriter.WriteAsync(fixture.Output, imported.Document!, assets, imported.ReferencedFiles);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DLevel original = Assert.Single(imported.Document!.Levels);
        Scene2DPackageLevel prepared = Assert.Single(package.Levels);
        Assert.Equal(ldtk ? 3 : 65, original.TileMaps.Sum(map => map.Chunks.Count));
        Assert.Equal(ldtk ? 24 : 65, prepared.TileMaps.Sum(map => map.Entries.Count));
        Assert.Equal(original.TileMaps.Count, prepared.TileMaps.Count);
        foreach (TileMapSource2D source in prepared.TileMaps)
        {
            TileMap2DModel model = Assert.Single(original.TileMaps.Where(map => map.Id == source.Catalog.Id));
            using var metadata = await prepared.LoadMapMetadataAsync(model.Id);
            Assert.Equal(model.Chunks.Count, metadata.Value.GridChunks.Count);
            Assert.Equal(PackageValueCodec.Encode(model.Properties), PackageValueCodec.Encode(metadata.Value.Properties));
            HashSet<TileCoordinate2D> visited = [];
            foreach (SceneSpatialEntry2D entry in source.Entries)
            {
                using var lease = await source.LoadAsync(entry);
                TileChunk2D chunk = lease.Value.Grid!;
                Assert.InRange(chunk.Width, 1, ldtk ? 16 : 8);
                Assert.InRange(chunk.Height, 1, ldtk ? 16 : 8);
                for (int y = 0; y < chunk.Height; y++)
                for (int x = 0; x < chunk.Width; x++)
                {
                    TileCoordinate2D position = new(chunk.Origin.X + x, chunk.Origin.Y + y);
                    Assert.True(visited.Add(position));
                    Assert.True(model.TryGetCell(position, out TileCell2D cell));
                    Assert.Equal(cell, chunk.GetCell(position));
                }
            }
            Assert.Equal(model.Chunks.Sum(chunk => chunk.Tiles.Count), visited.Count);
        }
    }
}
