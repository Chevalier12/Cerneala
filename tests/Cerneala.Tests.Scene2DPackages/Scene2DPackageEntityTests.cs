using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.Tests.Scene2DPackages;

public sealed partial class Scene2DPackageTests
{
    [Fact]
    public async Task ResidentEntityGeometryIsMapLocalAndReadableWithoutItsPayload()
    {
        using Fixture fixture = new();
        Scene2DEntity entity = new("box", "map", new(10, 20), new(2, 4), shape: "Box",
            rotation: MathF.PI / 2, pivot: new(1, 1), role: "Collider",
            collider: new(TileColliderShape2D.Box, width: 1, height: 2, offsetX: 8));
        await fixture.WriteAsync(fixture.Document([entity]));
        fixture.Corrupt(fixture.ReadIndex().Levels[0].Entities[0].Data);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = Assert.Single(package.Levels);
        Scene2DPackageEntityInfo info = Assert.Single(level.Entities);
        Assert.Equal("box", info.Id);
        Assert.Equal("map", info.MapId);
        Assert.Equal("Collider", info.Role);
        Assert.Equal(entity.GetAuthoringBounds(), info.AuthoringBounds);
        Assert.Equal(entity.GetCollisionBounds(), info.CollisionBounds);
        Assert.InRange(info.AuthoringBounds.X, 5.9999f, 6.0001f);
        Assert.InRange(info.AuthoringBounds.Y, 19.9999f, 20.0001f);
        Assert.InRange(info.CollisionBounds!.Value.Y, 27.9999f, 28.0001f);
        Assert.Equal(new DrawPoint(200, -300), level.WorldOffset); // Not folded into either envelope.
        Assert.Equal(new DrawPoint(2, 3), level.TileMaps[0].Catalog.Offset);
        Assert.Equal(info.Id, Assert.Single(level.EntityIds));
        await Assert.ThrowsAsync<InvalidDataException>(async () => await level.LoadEntityAsync(info.Id));
    }

    [Fact]
    public async Task AdapterSelectsBoundsAndSimulationExplicitlyWithoutReadingOrSpawning()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync(fixture.Document([
            new("npc", "map", new(1000, 10), default, role: "Spawn"),
            new("static-spawn", "map", new(100, 10), default, role: "Spawn"),
            new("annotation", "map", default, default)]));
        foreach (PackageEntity entity in fixture.ReadIndex().Levels[0].Entities) { fixture.Corrupt(entity.Data); }
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        List<string> described = [];
        int ownerThread = Environment.CurrentManagedThreadId;
        SceneSpatialSource2D<object> source = package.Levels[0].CreateEntitySource(info =>
        {
            Assert.Equal(ownerThread, Environment.CurrentManagedThreadId);
            described.Add(info.Id);
            if (info.Id == "annotation") { return null; }
            DrawRect bounds = new(info.AuthoringBounds.X, info.AuthoringBounds.Y, 8, 12);
            return new(info.Id, bounds, collisionBounds: info.Id == "npc" ? bounds : null, isSimulated: info.Id == "npc");
        });
        Assert.Equal(new[] { "npc", "static-spawn", "annotation" }, described);
        Assert.Equal(2, source.Entries.Count);
        Assert.True(source.Entries[0].IsSimulated);
        Assert.False(source.Entries[1].IsSimulated); // Same authored role is not an automatic policy.
        Assert.Null(source.Entries[1].CollisionBounds);
        Assert.Equal(new DrawRect(1000, 10, 8, 12), source.Entries[0].Bounds);
        await Assert.ThrowsAsync<InvalidDataException>(async () => await source.LoadAsync(source.Entries[0]));
    }

    [Fact]
    public async Task AdapterPreservesIdentityAndPreparedRevisionButAllowsRuntimeEnvelopeChanges()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = package.Levels[0];
        Assert.Throws<ArgumentNullException>(() => level.CreateEntitySource(null!));
        Assert.Throws<ArgumentException>(() => level.CreateEntitySource(info => new("different", info.AuthoringBounds)));
        Assert.Throws<ArgumentException>(() => level.CreateEntitySource(info => new(info.Id, info.AuthoringBounds, version: 2)));
        SceneSpatialSource2D<object> source = level.CreateEntitySource(_ => null);
        Assert.Empty(source.Entries);
        int notifications = 0;
        source.Changed += (_, _) => notifications++;
        source.SetEntries([new("e", new(700, 800, 16, 16), collisionBounds: null, isSimulated: true)]);
        using (var lease = await source.LoadAsync(source.Entries[0]))
        {
            Scene2DEntity entity = Assert.IsType<Scene2DEntity>(lease.Value);
            Assert.Equal(new DrawPoint(10, 20), entity.Position); // Selection metadata does not rewrite the stored entity.
            Assert.True(source.Entries[0].IsSimulated);
        }
        Assert.Equal(1, notifications);
        source.SetEntries([new("unknown", default, collisionBounds: null)]);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await source.LoadAsync(source.Entries[0]));
        source.SetEntries([new("e", default, collisionBounds: null, version: 2)]);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await source.LoadAsync(source.Entries[0]));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("role")]
    [InlineData("authoring")]
    [InlineData("collision")]
    public async Task LoadedEntityMustMatchItsHeaderNotJustItsChecksum(string mismatch)
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageIndex index = fixture.ReadIndex();
        PackageEntity original = index.Levels[0].Entities[0];
        Scene2DPackageEntityInfo info = original.Info;
        Scene2DPackageEntityInfo changed = new(mismatch == "identity" ? "renamed" : info.Id,
            info.MapId, mismatch == "role" ? "Spawn" : info.Role,
            mismatch == "authoring" ? new(90, 90, 1, 1) : info.AuthoringBounds,
            mismatch == "collision" ? new DrawRect(1, 2, 3, 4) : info.CollisionBounds);
        index.Levels[0].Entities[0] = original with { Info = changed };
        await fixture.WriteIndexAsync(index);
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        using var unrelated = await package.LoadMetadataAsync();
        Assert.Equal("document", unrelated.Value.Properties["scope"]);
        await Assert.ThrowsAsync<InvalidDataException>(async () => await package.Levels[0].LoadEntityAsync(changed.Id));
    }

    [Fact]
    public async Task EntityHeaderRequiresAnExistingMapAtOpen()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        PackageIndex index = fixture.ReadIndex();
        PackageEntity original = index.Levels[0].Entities[0];
        index.Levels[0].Entities[0] = original with { Info = new(original.Id, "missing", original.Info.Role,
            original.Info.AuthoringBounds, original.Info.CollisionBounds) };
        await fixture.WriteIndexAsync(index);
        InvalidDataException error = await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output));
        Assert.Contains("unknown owning map", error.Message);
    }

    [Fact]
    public async Task EarlierPackageWireVersionIsRejectedInsteadOfReinterpreted()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        byte[] catalog = (await File.ReadAllBytesAsync(fixture.CatalogPath))[..^32];
        Assert.Equal(new byte[] { (byte)'C', (byte)'P', (byte)'V', (byte)'2' }, catalog[..4]);
        catalog[3] = (byte)'1';
        await File.WriteAllBytesAsync(fixture.CatalogPath, [.. catalog, .. SHA256.HashData(catalog)]);
        await Assert.ThrowsAsync<InvalidDataException>(() => Scene2DPackage.OpenAsync(fixture.Output));
    }

    [Fact]
    public async Task ClosedAdapterLeasesDoNotPinEntitiesOrPropertiesWithLiveSourceAndPackage()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        SceneSpatialSource2D<object> source = package.Levels[0].CreateEntitySource(
            info => new(info.Id, info.AuthoringBounds, info.CollisionBounds));
        List<SceneSpatialLease2D<object>> closed = [];
        List<WeakReference> references = [];
        for (int iteration = 0; iteration < 32; iteration++)
        {
            var acquisition = await AcquireEntityAndRelease(source);
            closed.Add(acquisition.Lease);
            references.AddRange(acquisition.References);
        }
        await Task.Yield(); // Leave the final producer's completion stack before measuring retention.
        for (int collection = 0; collection < 3; collection++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.All(references, reference => Assert.False(reference.IsAlive));
        Assert.All(closed, lease => Assert.Throws<ObjectDisposedException>(() => lease.Value));
        GC.KeepAlive(source);
        GC.KeepAlive(package);
        GC.KeepAlive(closed);
    }

    [Fact]
    public async Task EntityAdapterRetainsStartedAndExistingLeasesButRejectsReadsAfterPackageDisposal()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        SceneSpatialSource2D<object> source = package.Levels[0].CreateEntitySource(
            info => new(info.Id, info.AuthoringBounds, info.CollisionBounds));
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await source.LoadAsync(source.Entries[0], canceled.Token));
        var started = source.LoadAsync(source.Entries[0]);
        package.Dispose();
        using var acquired = await started;
        Assert.Equal("e", Assert.IsType<Scene2DEntity>(acquired.Value).Id);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await source.LoadAsync(source.Entries[0]));
        Assert.Single(package.Levels[0].Entities);
    }

    [Fact]
    public async Task HeadlessPackageEntitiesStreamStaticRegionsWhileNpcMovesAndCollidesWithoutAViewport()
    {
        using Fixture fixture = new();
        Scene2DEntity Box(string id, float x, float y, string role = "Collider") => new(id, "map", new(x, y), new(2, 2),
            shape: "Box", role: role, collider: new(TileColliderShape2D.Box, width: 2, height: 2));
        await fixture.WriteAsync(fixture.Document([Box("left", 0, 0), Box("right", 100, 0),
            Box("npc", 1000, 10, "Spawn"), Box("npc-wall", 1006, 10)]));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        SceneSpatialSource2D<object> source = package.Levels[0].CreateEntitySource(info => info.Id == "npc"
            ? new(info.Id, new(999, 9, 10, 4), new(999, 9, 10, 4), isSimulated: true)
            : new(info.Id, info.AuthoringBounds, info.CollisionBounds));
        SceneItems2D items = new() { ItemsSource = source };
        items.Templates.Add(new ContentTemplate<Scene2DEntity>("entity", null, 0, context =>
        {
            Scene2DEntity entity = Assert.IsType<Scene2DEntity>(context.Data);
            return new Sprite2D
            {
                X = entity.Position.X, Y = entity.Position.Y,
                Width = 2, Height = 2, Collider = new BoxCollider2D { Width = 2, Height = 2 }
            };
        }));
        var scene = new global::Cerneala.UI.Controls.Scene2D();
        scene.Children.Add(items);
        using SceneSimulationContext2D simulation = new(scene);
        Sprite2D? npc = null;
        for (int iteration = 0; iteration < 32; iteration++)
        {
            int x = iteration % 2 * 100;
            string id = iteration % 2 == 0 ? "left" : "right";
            Task<SceneCollisionRegion2D> request = scene.CollisionWorld.PrepareRegionAsync(new(x - 1, -1, 4, 4)).AsTask();
            Assert.True(SpinWait.SpinUntil(() => { simulation.Update(); return request.IsCompleted; }, TimeSpan.FromSeconds(5)));
            using SceneCollisionRegion2D region = await request;
            Assert.Equal(3, items.RealizedItemCount); // Region + NPC + only its nearby wall.
            Assert.True(items.TryGetRealizedNode(id, out SceneNode2D? current));
            Assert.False(items.TryGetRealizedNode(id == "left" ? "right" : "left", out _));
            Assert.True(items.TryGetRealizedNode("npc", out SceneNode2D? loadedNpc));
            if (npc is null) { npc = Assert.IsType<Sprite2D>(loadedNpc); }
            else { Assert.Same(npc, loadedNpc); }
            Assert.Null(npc.Root);
            Assert.False(npc.IsAttached);
            Assert.Same(current, Assert.Single(scene.CollisionWorld.Raycast(new(x + 1, -0.5f), Vector2.UnitY, 3)).Entity);
            MoveCollisionResult2D move = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(0.25f, 0));
            npc.X += move.Travel.X; // The game applies allowed movement; the query does not mutate it.
            if (iteration == 31)
            {
                Assert.True(items.TryGetRealizedNode("npc-wall", out SceneNode2D? wall));
                Assert.Same(wall, move.Collision!.Entity);
            }
            region.Dispose();
            simulation.Update();
            Assert.Equal(2, items.RealizedItemCount);
            Assert.False(items.TryGetRealizedNode(id, out _));
            Assert.Throws<SceneCollisionRegionNotReadyException>(() => scene.CollisionWorld.Raycast(new(x + 1, -0.5f), Vector2.UnitY, 3));
        }
        Assert.InRange(npc!.X, 1003.999f, 1004.001f);
        source.SetEntries([]);
        Assert.Equal(0, items.RealizedItemCount); // Catalog removals are immediately authoritative.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(SceneSpatialLease2D<object> Lease, WeakReference[] References)> AcquireEntityAndRelease(SceneSpatialSource2D<object> source)
    {
        SceneSpatialLease2D<object> lease = await source.LoadAsync(source.Entries[0]);
        Scene2DEntity entity = Assert.IsType<Scene2DEntity>(lease.Value);
        WeakReference[] references = [new(entity), new(entity.Properties), new(entity.Properties["json"]!)];
        lease.Dispose();
        lease.Dispose();
        return (lease, references);
    }
}
