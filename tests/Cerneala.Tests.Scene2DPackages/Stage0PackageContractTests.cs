using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Scene2DPackages;

public sealed partial class Scene2DPackageTests
{
    private static readonly TimeSpan Stage0Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Stage0_PreCutoverCpv2BytesRemainReadableWithoutWireChange()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        Assert.Equal("A417E679D0ACE8599A59FCF31738C725B137A3ECD5491ADA1FB38F15A4B8815E",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fixture.CatalogPath))));
        Assert.Equal("F6A5EA008D9AEF3C88C4212F3FF93B32D1AFE2BBAB2F8FE5BE5D5665878A326E",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fixture.DataPath))));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Assert.Equal("level", Assert.Single(package.Levels).Id);
        Scene2DPackageMetadata metadata = await InvokeValueAsync<Scene2DPackageMetadata>(
            package, "LoadMetadataAsync", CancellationToken.None);
        Assert.Equal("document", metadata.Properties["scope"]);
    }

    [Fact]
    public async Task Stage0_RangeReaderHandlesShortConcurrentReadsAndDirectValues()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        fake.MaximumReadBytes = 11;
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            Assert.Equal("level", Assert.Single(package.Levels).Id);
            Task<Scene2DPackageMetadata>[] reads = Enumerable.Range(0, 24)
                .Select(index => index % 2 == 0
                    ? InvokeValueAsync<Scene2DPackageMetadata>(package, "LoadMetadataAsync", CancellationToken.None)
                    : InvokeValueAsync<Scene2DPackageMetadata>(package.Levels[0], "LoadMapMetadataAsync", "map", CancellationToken.None))
                .ToArray();
            Scene2DPackageMetadata[] values = await Task.WhenAll(reads).WaitAsync(Stage0Timeout);
            for (int index = 0; index < values.Length; index++)
            {
                Assert.Equal(index % 2 == 0 ? "document" : "map", values[index].Properties["scope"]);
            }
            Assert.True(fake.ReadCalls > reads.Length, "Short reads must be completed instead of treated as whole blocks.");
            Assert.Equal(0, fake.DisposeCalls);
            Assert.Throws<NotSupportedException>(() => package.GetFilePath("nested/atlas.bin"));
        }
        finally { await DisposePackageAsync(package); }
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_DirectValuesAndCompleteMapModelRemainCallerOwned()
    {
        using Fixture fixture = new();
        Scene2DDocument authored = fixture.Document();
        await fixture.WriteAsync(authored);
        Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        try
        {
            Scene2DPackageLevel level = Assert.Single(package.Levels);
            Scene2DPackageMetadata documentMetadata = await InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", CancellationToken.None);
            Scene2DPackageMetadata levelMetadata = await InvokeValueAsync<Scene2DPackageMetadata>(
                level, "LoadMetadataAsync", CancellationToken.None);
            Scene2DPackageMetadata mapMetadata = await InvokeValueAsync<Scene2DPackageMetadata>(
                level, "LoadMapMetadataAsync", "map", CancellationToken.None);
            Scene2DEntity entity = await InvokeValueAsync<Scene2DEntity>(
                level, "LoadEntityAsync", Assert.Single(level.EntityIds), CancellationToken.None);
            TilePromotion2D promotion = await InvokeValueAsync<TilePromotion2D>(
                level, "LoadPromotionAsync", Assert.Single(level.PromotionCells), CancellationToken.None);
            TileMap2DModel model = await InvokeValueAsync<TileMap2DModel>(
                level, "LoadMapModelAsync", "map", CancellationToken.None);

            Assert.Equal("map", model.Id);
            Assert.Equal(authored.Levels[0].TileMaps[0].TileSize, model.TileSize);
            Assert.Equal(authored.Levels[0].TileMaps[0].Offset, model.Offset);
            Assert.Equal(authored.Levels[0].TileMaps[0].Version, model.Version);
            Assert.Equal(2, model.Chunks.Count);
            Assert.Equal(2, Assert.Single(model.TileSets).Tiles.Count); // Includes the unused authored definition.
            for (int index = 0; index < model.Chunks.Count; index++)
            {
                TileChunk2D expected = authored.Levels[0].TileMaps[0].Chunks[index];
                TileChunk2D actual = model.Chunks[index];
                Assert.Equal(expected.Origin, actual.Origin);
                Assert.Equal(expected.Version, actual.Version);
                Assert.Equal(expected.Tiles, actual.Tiles);
            }

            package.Dispose();
            Assert.Equal("document", documentMetadata.Properties["scope"]);
            Assert.Equal("level", levelMetadata.Properties["scope"]);
            Assert.Equal("map", mapMetadata.Properties["scope"]);
            Assert.Equal(authored.Levels[0].Entities[0].Id, entity.Id);
            Assert.Equal(authored.Levels[0].Promotions[0].Cell, promotion.Cell);
            Assert.Equal(65_536, Assert.IsType<byte[]>(model.Chunks[0].Properties["bulk"]).Length);
        }
        finally
        {
            package.Dispose();
            if ((object)package is IAsyncDisposable asynchronous) { await asynchronous.DisposeAsync(); }
        }
    }

    [Fact]
    public async Task Stage0_LoadMapModelReassemblesSubdividedAuthoredGridAndEmptyCells()
    {
        using Fixture fixture = new();
        Dictionary<string, object?> properties = new()
        {
            ["origin"] = "authored",
            ["binary"] = new byte[] { 0, 1, 2, 255 }
        };
        TileSet2D set = new("set", new("atlas"),
        [
            new(1, new(0, 0, 10, 10)),
            new(2, new(10, 0, 10, 10)),
            new(3, new(0, 0, 10, 10), new Dictionary<string, object?> { ["unused"] = true })
        ], version: 4, properties: properties);
        TileCell2D[] cells = Enumerable.Range(0, 33 * 19)
            .Select(index => new TileCell2D(index % 9 == 0 ? 0 : index % 2 + 1,
                (TileFlip2D)(index % 8))).ToArray();
        TileChunk2D large = new(new(-19, -9), 33, 19, cells, 7, properties);
        TileChunk2D small = new(new(80, 5), 2, 3, Enumerable.Repeat(new TileCell2D(2), 6), 11, properties);
        TileChunk2D empty = new(new(90, -9), 17, 1, new TileCell2D[17], 13, properties);
        TileMap2DModel authored = new("map", new(10, 10), [set], [large, small, empty],
            new(-20, -10, 130, 80), order: 3, offset: new(2, 3), opacity: .75f,
            tint: new Color(100, 130, 160, 190), version: 17, properties: properties);
        Scene2DDocument document = new([new Scene2DLevel("level", [authored])],
            [new(new("atlas"), "nested/atlas.bin", new(20, 10))]);
        await fixture.WriteAsync(document);
        Assert.True(fixture.ReadIndex().Levels[0].Maps[0].Chunks.Length > authored.Chunks.Count,
            "The fixture must contain prepared subdivisions, not only authored chunks.");

        Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        TileMap2DModel restored;
        try
        {
            restored = await InvokeValueAsync<TileMap2DModel>(Assert.Single(package.Levels),
                "LoadMapModelAsync", "map", CancellationToken.None);
        }
        finally
        {
            package.Dispose();
            if ((object)package is IAsyncDisposable asynchronous) { await asynchronous.DisposeAsync(); }
        }
        Assert.Equal(authored.Id, restored.Id);
        Assert.Equal(authored.TileSize, restored.TileSize);
        Assert.Equal(authored.Bounds, restored.Bounds);
        Assert.Equal(authored.Order, restored.Order);
        Assert.Equal(authored.IsVisible, restored.IsVisible);
        Assert.Equal(authored.Offset, restored.Offset);
        Assert.Equal(authored.Opacity, restored.Opacity);
        Assert.Equal(authored.Tint, restored.Tint);
        Assert.Equal(authored.Version, restored.Version);
        Assert.Equal(PackageValueCodec.Encode(authored.Properties), PackageValueCodec.Encode(restored.Properties));
        Assert.Empty(restored.Tiles);
        Assert.Equal(3, restored.Chunks.Count); // Not the nine prepared package pieces.
        TileSet2D restoredSet = Assert.Single(restored.TileSets);
        Assert.Equal(set.Id, restoredSet.Id);
        Assert.Equal(set.AtlasResourceId, restoredSet.AtlasResourceId);
        Assert.Equal(set.Version, restoredSet.Version);
        Assert.Equal(PackageValueCodec.Encode(set.Properties), PackageValueCodec.Encode(restoredSet.Properties));
        Assert.Equal(set.Tiles.Count, restoredSet.Tiles.Count); // Includes the unused authored tile.
        for (int index = 0; index < set.Tiles.Count; index++)
        {
            Assert.Equal(PackageValueCodec.Encode(set.Tiles[index]),
                PackageValueCodec.Encode(restoredSet.Tiles[index]));
        }
        for (int index = 0; index < authored.Chunks.Count; index++)
        {
            TileChunk2D expected = authored.Chunks[index];
            TileChunk2D actual = restored.Chunks[index];
            Assert.Equal(expected.Origin, actual.Origin);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Tiles, actual.Tiles); // Includes zero cells and flip bits.
            Assert.Equal(PackageValueCodec.Encode(expected.Properties),
                PackageValueCodec.Encode(actual.Properties));
        }
        Assert.All(restored.Chunks[2].Tiles, static cell => Assert.Equal(0, cell.TileId));
    }

    [Fact]
    public async Task Stage0_LoadMapModelPreservesOrderedFreePlacementsAcrossPackageGroups()
    {
        using Fixture fixture = new();
        ImageReference atlas = new(new ResourceId<ImageResource>("atlas"));
        Tile[] placements = Enumerable.Range(0, 257)
            .Select(index => new Tile(atlas, x: 300 - index, y: index % 5, width: 10, height: 10))
            .ToArray();
        TileMap2DModel authored = new(placements, version: 7, id: "map");
        Scene2DDocument document = new([new Scene2DLevel("level", [authored])],
            [new(new("atlas"), "nested/atlas.bin", new(20, 10))]);
        await fixture.WriteAsync(document);
        Assert.Equal(2, fixture.ReadIndex().Levels[0].Maps[0].Chunks.Length);

        Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        TileMap2DModel restored;
        try
        {
            restored = await InvokeValueAsync<TileMap2DModel>(Assert.Single(package.Levels),
                "LoadMapModelAsync", "map", CancellationToken.None);
        }
        finally
        {
            package.Dispose();
            if ((object)package is IAsyncDisposable asynchronous) { await asynchronous.DisposeAsync(); }
        }
        Assert.Equal(authored.Id, restored.Id);
        Assert.Equal(authored.Version, restored.Version);
        Assert.Empty(restored.Chunks);
        Assert.Empty(restored.TileSets);
        Assert.Equal(placements.Length, restored.Tiles.Count);
        for (int index = 0; index < placements.Length; index++)
        {
            Assert.Equal(PackageValueCodec.Encode(placements[index]),
                PackageValueCodec.Encode(restored.Tiles[index]));
        }
    }

    [Fact]
    public async Task Stage0_RangeReaderCancellationIsPerCallAndRetryIsNewRead()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageBlock metadata = fixture.ReadIndex().Metadata;
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fake.OnRead = async (part, offset, destination, token) =>
        {
            if (part == "Payloads" && offset == metadata.Offset)
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            return fake.ReadDefault(part, offset, destination, token);
        };
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            using CancellationTokenSource cancellation = new();
            Task<Scene2DPackageMetadata> canceled = InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", cancellation.Token);
            await entered.Task.WaitAsync(Stage0Timeout);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.WaitAsync(Stage0Timeout));
            fake.OnRead = null;
            Scene2DPackageMetadata retry = await InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", CancellationToken.None).WaitAsync(Stage0Timeout);
            Assert.Equal("document", retry.Properties["scope"]);
        }
        finally { await DisposePackageAsync(package); }
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task Stage0_RangeReaderRejectsZeroAndOutOfRangeReadCounts(int mode)
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        fake.OnRead = (_, _, destination, _) => Task.FromResult(mode == 1 ? destination.Length + 1 : mode);
        await Assert.ThrowsAsync<InvalidDataException>(() => OpenReaderAsync(reader));
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_RangeReaderChecksCatalogAndPayloadHashes()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object corruptCatalogReader = CreateRangeReader(fixture, out Stage0RangeReaderProxy corruptCatalog);
        corruptCatalog.CatalogBytes[10] ^= 1;
        await Assert.ThrowsAsync<InvalidDataException>(() => OpenReaderAsync(corruptCatalogReader));
        Assert.Equal(1, corruptCatalog.DisposeCalls);

        object corruptPayloadReader = CreateRangeReader(fixture, out Stage0RangeReaderProxy corruptPayload);
        PackageBlock metadata = fixture.ReadIndex().Metadata;
        corruptPayload.PayloadBytes[checked((int)metadata.Offset)] ^= 1;
        Scene2DPackage package = await OpenReaderAsync(corruptPayloadReader);
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", CancellationToken.None));
        }
        finally { await DisposePackageAsync(package); }
        Assert.Equal(1, corruptPayload.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_RangeReaderRejectsPayloadLengthThatDisagreesWithCatalog()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        fake.LengthOverride = (part, length) => part == "Payloads" ? length - 1 : length;
        await Assert.ThrowsAsync<InvalidDataException>(() => OpenReaderAsync(reader));
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_FailedOpenTransfersReaderOwnershipAndAwaitsCleanup()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fake.DisposeGate = release.Task;
        Task<Scene2DPackage> opening = OpenReaderAsync(reader, new() { MaxCatalogBytes = 0 });
        await fake.DisposeEntered.Task.WaitAsync(Stage0Timeout);
        Assert.False(opening.IsCompleted);
        release.TrySetResult(true);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => opening.WaitAsync(Stage0Timeout));
        Assert.Equal(1, fake.DisposeCalls);

        object canceledReader = CreateRangeReader(fixture, out Stage0RangeReaderProxy canceledFake);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OpenReaderAsync(canceledReader,
            cancellationToken: cancellation.Token));
        Assert.Equal(1, canceledFake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_FailedOpenPreservesPrimaryAndReaderCleanupErrors()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        fake.DisposeFailure = new IOException("reader cleanup failed");
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() =>
            OpenReaderAsync(reader, new() { MaxCatalogBytes = 0 }));
        Exception[] errors = failure.Flatten().InnerExceptions.ToArray();
        Assert.Contains(errors, static error => error is ArgumentOutOfRangeException);
        Assert.Contains(errors, static error => error is IOException { Message: "reader cleanup failed" });
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_DisposeRejectsNewReadsAndDisposeAsyncDrainsAdmittedReadOnce()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageBlock metadata = fixture.ReadIndex().Metadata;
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? drain = null;
        Task? repeatedDrain = null;
        fake.OnRead = async (part, offset, destination, token) =>
        {
            if (part == "Payloads" && offset == metadata.Offset)
            {
                entered.TrySetResult(true);
                await release.Task;
                // This callback is still inside the admitted read. Terminal completion or
                // reader release before it returns would violate the drain ordering.
                Assert.NotNull(drain);
                Assert.NotNull(repeatedDrain);
                Assert.False(drain.IsCompleted);
                Assert.False(repeatedDrain.IsCompleted);
                Assert.Equal(0, fake.DisposeCalls);
            }
            return fake.ReadDefault(part, offset, destination, token);
        };
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            Task<Scene2DPackageMetadata> admitted = InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", CancellationToken.None);
            await entered.Task.WaitAsync(Stage0Timeout);
            package.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => InvokeValueAsync<Scene2DPackageMetadata>(
                package, "LoadMetadataAsync", CancellationToken.None));
            drain = DisposePackageAsync(package);
            repeatedDrain = DisposePackageAsync(package);
            release.TrySetResult(true);
            Scene2DPackageMetadata value = await admitted.WaitAsync(Stage0Timeout);
            Assert.Equal("document", value.Properties["scope"]);
            await drain.WaitAsync(Stage0Timeout);
            await repeatedDrain.WaitAsync(Stage0Timeout);
        }
        finally
        {
            release.TrySetResult(true);
            await DisposePackageAsync(package).WaitAsync(Stage0Timeout);
        }
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_DisposeAsyncReportsReaderCleanupFailureOnRepeatedAwait()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        fake.DisposeFailure = new IOException("reader close failed");
        Scene2DPackage package = await OpenReaderAsync(reader);
        Task first = DisposePackageAsync(package);
        Task repeated = DisposePackageAsync(package);
        Exception? firstFailure = await Record.ExceptionAsync(() => first.WaitAsync(Stage0Timeout));
        Exception? repeatedFailure = await Record.ExceptionAsync(() => repeated.WaitAsync(Stage0Timeout));
        Assert.NotNull(firstFailure);
        Assert.NotNull(repeatedFailure);
        Assert.True(ContainsCleanupError(firstFailure));
        Assert.True(ContainsCleanupError(repeatedFailure));
        Assert.Equal(1, fake.DisposeCalls);

        static bool ContainsCleanupError(Exception error) => error is IOException { Message: "reader close failed" } ||
            error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(
                static inner => inner is IOException { Message: "reader close failed" });
    }

    [Fact]
    public async Task Stage0_CreateTileMapOwnsIndependentInstancesAndMapDisposeIsTerminal()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            Scene2DPackageLevel level = Assert.Single(package.Levels);
            Assert.Equal(new[] { "map" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(RequiredProperty(level, "TileMapIds")));
            TileMap2D first = CreateTileMap(level, "map");
            TileMap2D second = CreateTileMap(level, "map");
            Assert.NotSame(first, second);
            Task firstDrain = DisposeMapAsync(first);
            Task secondDrain = DisposeMapAsync(second);
            Task repeatedDrain = DisposeMapAsync(first);
            var scene = new global::Cerneala.UI.Controls.Scene2D();
            TileMap2D stillAttachable = CreateTileMap(level, "map");
            scene.Children.Add(stillAttachable); // Establish that this scene accepts a fresh package map.
            Assert.True(scene.Children.Remove(stillAttachable));
            Task freshDrain = DisposeMapAsync(stillAttachable);
            Assert.Throws<ObjectDisposedException>(() => { scene.Children.Add(first); });
            await Task.WhenAll(firstDrain, secondDrain, repeatedDrain, freshDrain);
        }
        finally { await DisposePackageAsync(package); }
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_AttachedMapRejectsDisposeWithoutConsumingItsTerminal()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            TileMap2D map = CreateTileMap(Assert.Single(package.Levels), "map");
            var scene = new global::Cerneala.UI.Controls.Scene2D();
            scene.Children.Add(map);
            SceneSimulationContext2D context = new(scene);
            Task? attempted = null;
            Exception? rejection = null;
            bool removed = false;
            try
            {
                rejection = Record.Exception(() => { attempted = DisposeMapAsync(map); });
                rejection ??= attempted?.Exception?.GetBaseException();
                removed = scene.Children.Remove(map);
            }
            finally
            {
                if (!removed) { scene.Children.Remove(map); }
                context.Dispose(); // No scene-owner operation occurs after the first await below.
            }
            Assert.IsType<InvalidOperationException>(rejection);
            Assert.True(removed);
            await DisposeMapAsync(map);
        }
        finally
        {
            await DisposePackageAsync(package);
        }
        Assert.Equal(1, fake.DisposeCalls);
    }

    [Fact]
    public async Task Stage0_MapDisposeAsyncWaitsOldGenerationAfterDetachAndReattach()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageMap index = fixture.ReadIndex().Levels[0].Maps[0];
        object reader = CreateRangeReader(fixture, out Stage0RangeReaderProxy fake);
        TaskCompletionSource<bool> oldEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseOld = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? drain = null;
        Task? repeatedDrain = null;
        int oldCalls = 0;
        fake.OnRead = async (part, offset, destination, token) =>
        {
            if (part == "Payloads" && offset == index.Chunks[0].Offset && Interlocked.Increment(ref oldCalls) == 1)
            {
                oldEntered.TrySetResult(true);
                await releaseOld.Task; // Deliberately ignores canceled interest: late completion is the regression.
                Assert.NotNull(drain);
                Assert.NotNull(repeatedDrain);
                Assert.False(drain.IsCompleted);
                Assert.False(repeatedDrain.IsCompleted);
                Assert.Equal(0, fake.DisposeCalls);
            }
            return fake.ReadDefault(part, offset, destination, token, ignoreCancellation: true);
        };
        Scene2DPackage package = await OpenReaderAsync(reader);
        try
        {
            TileMap2D map = CreateTileMap(Assert.Single(package.Levels), "map");
            var scene = new global::Cerneala.UI.Controls.Scene2D();
            scene.Children.Add(map);
            SceneSimulationContext2D context = new(scene);
            using CancellationTokenSource oldInterest = new();
            Task<SceneCollisionRegion2D>? oldRegion = null;
            bool attached = true;
            try
            {
                oldRegion = scene.CollisionWorld.PrepareRegionAsync(
                    new(-5, -10, 30, 40), oldInterest.Token).AsTask();
                Assert.True(SpinWait.SpinUntil(() => { context.Update(); return oldEntered.Task.IsCompleted; }, Stage0Timeout));
                oldInterest.Cancel();
                Assert.True(SpinWait.SpinUntil(() => oldRegion.IsCompleted, Stage0Timeout));
                Assert.True(oldRegion.IsCanceled);
                context.Update(); // Drain the canceled region's posted release on the owner thread.
                Assert.True(scene.Children.Remove(map));
                attached = false;
                scene.Children.Add(map);
                attached = true;
                Task<SceneCollisionRegion2D> newRegion = scene.CollisionWorld.PrepareRegionAsync(new(95, -10, 30, 40)).AsTask();
                Assert.True(SpinWait.SpinUntil(() => { context.Update(); return newRegion.IsCompleted; }, Stage0Timeout));
                Assert.True(newRegion.IsCompletedSuccessfully);
                // The completed result must be released on this scene-owner thread before detach.
#pragma warning disable xUnit1031
                using (SceneCollisionRegion2D region = newRegion.GetAwaiter().GetResult()) { }
#pragma warning restore xUnit1031
                context.Update();
                Assert.True(scene.Children.Remove(map));
                attached = false;
                drain = DisposeMapAsync(map);
                repeatedDrain = DisposeMapAsync(map);
            }
            finally
            {
                try
                {
                    if (attached) { scene.Children.Remove(map); }
                    oldInterest.Cancel();
                    context.Dispose(); // Scene and region state retired on the owner thread.
                }
                finally { releaseOld.TrySetResult(true); }
            }
            Assert.NotNull(drain);
            Assert.NotNull(repeatedDrain);
            await drain.WaitAsync(Stage0Timeout);
            await repeatedDrain.WaitAsync(Stage0Timeout);
            if (oldRegion?.IsFaulted == true) { _ = oldRegion.Exception; }
        }
        finally
        {
            releaseOld.TrySetResult(true);
            await DisposePackageAsync(package).WaitAsync(Stage0Timeout);
        }
        Assert.Equal(1, fake.DisposeCalls);
    }

    private static object CreateRangeReader(Fixture fixture, out Stage0RangeReaderProxy fake)
    {
        Type readerType = typeof(Scene2DPackage).Assembly.GetType(
            "Cerneala.Scene2D.Packages.IScene2DPackageRangeReader", throwOnError: false)
            ?? throw new Xunit.Sdk.XunitException("Missing selected public IScene2DPackageRangeReader contract.");
        object reader = DispatchProxy.Create(readerType, typeof(Stage0RangeReaderProxy));
        fake = (Stage0RangeReaderProxy)reader;
        fake.CatalogBytes = File.ReadAllBytes(fixture.CatalogPath);
        fake.PayloadBytes = File.ReadAllBytes(fixture.DataPath);
        return reader;
    }

    private static async Task<Scene2DPackage> OpenReaderAsync(object reader,
        Scene2DPackageReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        Type readerType = reader.GetType().GetInterfaces().Single(type => type.FullName ==
            "Cerneala.Scene2D.Packages.IScene2DPackageRangeReader");
        MethodInfo method = typeof(Scene2DPackage).GetMethod("OpenAsync", BindingFlags.Public | BindingFlags.Static,
            binder: null, types: [readerType, typeof(Scene2DPackageReadOptions), typeof(CancellationToken)], modifiers: null)
            ?? throw new Xunit.Sdk.XunitException("Missing selected Scene2DPackage.OpenAsync(rangeReader, options, token).");
        return await ((Task<Scene2DPackage>)Invoke(method, null, reader, options, cancellationToken)!).ConfigureAwait(false);
    }

    private static async Task<T> InvokeValueAsync<T>(object owner, string name, params object?[] arguments)
    {
        MethodInfo method = owner.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length)
            ?? throw new Xunit.Sdk.XunitException($"Missing selected {owner.GetType().Name}.{name} API.");
        Assert.Equal(typeof(ValueTask<T>), method.ReturnType);
        object result = Invoke(method, owner, arguments)!;
        return await ((ValueTask<T>)result).ConfigureAwait(false);
    }

    private static object? RequiredProperty(object owner, string name) =>
        owner.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(owner)
        ?? throw new Xunit.Sdk.XunitException($"Missing selected {owner.GetType().Name}.{name} API.");

    private static TileMap2D CreateTileMap(Scene2DPackageLevel level, string id)
    {
        MethodInfo method = typeof(Scene2DPackageLevel).GetMethod("CreateTileMap", [typeof(string)])
            ?? throw new Xunit.Sdk.XunitException("Missing selected Scene2DPackageLevel.CreateTileMap API.");
        return Assert.IsType<TileMap2D>(Invoke(method, level, id));
    }

    private static Task DisposePackageAsync(Scene2DPackage package) =>
        ((IAsyncDisposable)(object)package).DisposeAsync().AsTask();

    private static Task DisposeMapAsync(TileMap2D map) =>
        ((IAsyncDisposable)(object)map).DisposeAsync().AsTask();

    private static object? Invoke(MethodInfo method, object? owner, params object?[] arguments)
    {
        try { return method.Invoke(owner, arguments); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}

public class Stage0RangeReaderProxy : DispatchProxy
{
    public byte[] CatalogBytes { get; set; } = [];
    public byte[] PayloadBytes { get; set; } = [];
    public int MaximumReadBytes { get; set; } = int.MaxValue;
    public int ReadCalls => Volatile.Read(ref readCalls);
    public int DisposeCalls => Volatile.Read(ref disposeCalls);
    public TaskCompletionSource<bool> DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task? DisposeGate { get; set; }
    public Exception? DisposeFailure { get; set; }
    public Func<string, long, long>? LengthOverride { get; set; }
    public Func<string, long, Memory<byte>, CancellationToken, Task<int>>? OnRead { get; set; }
    private int readCalls;
    private int disposeCalls;

    protected override object? Invoke(MethodInfo? method, object?[]? arguments)
    {
        object?[] args = arguments ?? [];
        return method?.Name switch
        {
            "GetLengthAsync" => GetLength(args),
            "ReadAsync" => new ValueTask<int>(ReadAsync(args)),
            "DisposeAsync" => new ValueTask(DisposeAsync()),
            _ => throw new NotSupportedException($"Unexpected range-reader call: {method?.Name}.")
        };
    }

    public int ReadDefault(string part, long offset, Memory<byte> destination, CancellationToken token,
        bool ignoreCancellation = false)
    {
        if (!ignoreCancellation) { token.ThrowIfCancellationRequested(); }
        byte[] bytes = PartBytes(part);
        if (offset < 0 || offset >= bytes.Length) { return 0; }
        int count = Math.Min(Math.Min(destination.Length, MaximumReadBytes), bytes.Length - checked((int)offset));
        bytes.AsMemory(checked((int)offset), count).CopyTo(destination);
        return count;
    }

    private ValueTask<long> GetLength(object?[] args)
    {
        CancellationToken token = (CancellationToken)args[1]!;
        token.ThrowIfCancellationRequested();
        string part = args[0]!.ToString()!;
        long length = PartBytes(part).Length;
        return ValueTask.FromResult(LengthOverride?.Invoke(part, length) ?? length);
    }

    private async Task<int> ReadAsync(object?[] args)
    {
        Interlocked.Increment(ref readCalls);
        await Task.Yield();
        string part = args[0]!.ToString()!;
        long offset = (long)args[1]!;
        Memory<byte> destination = (Memory<byte>)args[2]!;
        CancellationToken token = (CancellationToken)args[3]!;
        Func<string, long, Memory<byte>, CancellationToken, Task<int>>? callback = OnRead;
        return callback is null ? ReadDefault(part, offset, destination, token)
            : await callback(part, offset, destination, token).ConfigureAwait(false);
    }

    private async Task DisposeAsync()
    {
        Interlocked.Increment(ref disposeCalls);
        DisposeEntered.TrySetResult(true);
        if (DisposeGate is not null) { await DisposeGate.ConfigureAwait(false); }
        if (DisposeFailure is not null) { throw DisposeFailure; }
    }

    private byte[] PartBytes(string part) => part switch
    {
        "Catalog" => CatalogBytes,
        "Payloads" => PayloadBytes,
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown CPV2 package part.")
    };
}
