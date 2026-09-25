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
        Assert.Equal(new DrawPoint(2, 3), fixture.ReadIndex().Levels[0].Maps[0].Catalog.Offset);
        Assert.Equal(info.Id, Assert.Single(level.EntityIds));
        await Assert.ThrowsAsync<InvalidDataException>(async () => await level.LoadEntityAsync(info.Id));
    }

    [Fact]
    public async Task EntityHeadersAllowApplicationSelectionWithoutReadingOrSpawning()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync(fixture.Document([
            new("npc", "map", new(1000, 10), default, role: "Spawn"),
            new("static-spawn", "map", new(100, 10), default, role: "Spawn"),
            new("annotation", "map", default, default)]));
        foreach (PackageEntity entity in fixture.ReadIndex().Levels[0].Entities) { fixture.Corrupt(entity.Data); }
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = package.Levels[0];
        Assert.Equal(new[] { "npc", "static-spawn", "annotation" }, level.EntityIds);
        Scene2DPackageEntityInfo[] selected = level.Entities.Where(info => info.Id != "annotation").ToArray();
        Assert.Equal(2, selected.Length);
        Assert.All(selected, info => Assert.Equal("Spawn", info.Role));
        Assert.Equal(new DrawRect(1000, 10, 0, 0), selected[0].AuthoringBounds);
        Assert.Null(selected[0].CollisionBounds); // A role does not imply a collider or simulation policy.
        await Assert.ThrowsAsync<InvalidDataException>(async () => await level.LoadEntityAsync(selected[0].Id));
    }

    [Fact]
    public async Task DirectEntityReadsPreserveIdentityWithoutMutableRuntimeEnvelopes()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = package.Levels[0];
        Scene2DEntity first = await level.LoadEntityAsync("e");
        Scene2DEntity second = await level.LoadEntityAsync("e");
        Assert.Equal(new DrawPoint(10, 20), first.Position);
        Assert.Equal("e", first.Id);
        Assert.NotSame(first, second);
        Assert.Equal(PackageValueCodec.Encode(first), PackageValueCodec.Encode(second));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await level.LoadEntityAsync("unknown"));
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
        Scene2DPackageMetadata unrelated = await package.LoadMetadataAsync();
        Assert.Equal("document", unrelated.Properties["scope"]);
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
    public async Task ReleasedDirectEntityValuesDoNotStayPinnedByThePackage()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        List<WeakReference> references = [];
        for (int iteration = 0; iteration < 32; iteration++)
        {
            references.AddRange(await ReadEntityAndRelease(package.Levels[0]));
        }
        await Task.Yield(); // Leave the final producer's completion stack before measuring retention.
        for (int collection = 0; collection < 3; collection++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.All(references, reference => Assert.False(reference.IsAlive));
        GC.KeepAlive(package);
    }

    [Fact]
    public async Task DirectEntityValuesRemainCallerOwnedAfterPackageDisposal()
    {
        using Fixture fixture = new();
        await fixture.WriteAsync();
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = package.Levels[0];
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await level.LoadEntityAsync("e", canceled.Token));
        ValueTask<Scene2DEntity> started = level.LoadEntityAsync("e");
        package.Dispose();
        Scene2DEntity acquired = await started;
        Assert.Equal("e", acquired.Id);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await level.LoadEntityAsync("e"));
        Assert.Single(package.Levels[0].Entities);
        await package.DisposeAsync();
        Assert.Equal(new DrawPoint(10, 20), acquired.Position);
    }

    [Fact]
    public async Task HeadlessPackageEntitiesInAnOrdinaryCollectionKeepNpcAndCollisionsActive()
    {
        using Fixture fixture = new();
        Scene2DEntity Box(string id, float x, float y, string role = "Collider") => new(id, "map", new(x, y), new(2, 2),
            shape: "Box", role: role, collider: new(TileColliderShape2D.Box, width: 2, height: 2));
        await fixture.WriteAsync(fixture.Document([Box("left", 0, 0), Box("right", 100, 0),
            Box("npc", 1000, 10, "Spawn"), Box("npc-wall", 1006, 10)]));
        using Scene2DPackage package = await Scene2DPackage.OpenAsync(fixture.Output);
        Scene2DPackageLevel level = package.Levels[0];
        List<Scene2DEntity> entities = [];
        foreach (string id in level.EntityIds) { entities.Add(await level.LoadEntityAsync(id)); }
        Dictionary<string, Sprite2D> realized = new(StringComparer.Ordinal);
        SceneItems2D items = new() { ItemsSource = entities };
        items.Templates.Add(new ContentTemplate<Scene2DEntity>("entity", null, 0, context =>
        {
            Scene2DEntity entity = Assert.IsType<Scene2DEntity>(context.Data);
            Sprite2D sprite = new()
            {
                X = entity.Position.X, Y = entity.Position.Y,
                Width = 2, Height = 2, Collider = new BoxCollider2D { Width = 2, Height = 2, IsSimulated = entity.Id == "npc" }
            };
            realized.Add(entity.Id, sprite);
            return sprite;
        }));
        var scene = new global::Cerneala.UI.Controls.Scene2D();
        scene.Children.Add(items);
        using SceneSimulationContext2D simulation = new(scene);
        Assert.Equal(4, items.RealizedItemCount); // Eager collection: no pre-load viewport selection.
        Sprite2D npc = realized["npc"];
        for (int iteration = 0; iteration < 32; iteration++)
        {
            int x = iteration % 2 * 100;
            string id = iteration % 2 == 0 ? "left" : "right";
            Task<SceneCollisionRegion2D> request = scene.CollisionWorld.PrepareRegionAsync(new(x - 1, -1, 4, 4)).AsTask();
            Assert.True(SpinWait.SpinUntil(() => { simulation.Update(); return request.IsCompleted; }, TimeSpan.FromSeconds(5)));
            using SceneCollisionRegion2D region = await request;
            Assert.Equal(4, items.RealizedItemCount);
            Sprite2D current = realized[id];
            Assert.Same(npc, realized["npc"]);
            Assert.Null(npc.Root);
            Assert.False(npc.IsAttached);
            Assert.Same(current, Assert.Single(scene.CollisionWorld.Raycast(new(x + 1, -0.5f), Vector2.UnitY, 3)).Entity);
            MoveCollisionResult2D move = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(0.25f, 0));
            npc.X += move.Travel.X; // The game applies allowed movement; the query does not mutate it.
            if (iteration == 31)
            {
                Assert.Same(realized["npc-wall"], move.Collision!.Entity);
            }
            region.Dispose();
            simulation.Update();
            Assert.Equal(4, items.RealizedItemCount);
            Assert.Same(current, Assert.Single(scene.CollisionWorld.Raycast(new(x + 1, -0.5f), Vector2.UnitY, 3)).Entity);
        }
        Assert.InRange(npc.X, 1003.999f, 1004.001f);
        items.ItemsSource = Array.Empty<Scene2DEntity>();
        Assert.Equal(0, items.RealizedItemCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> ReadEntityAndRelease(Scene2DPackageLevel level)
    {
        Scene2DEntity entity = await level.LoadEntityAsync("e");
        WeakReference[] references = [new(entity), new(entity.Properties), new(entity.Properties["json"]!)];
        return references;
    }
}
