using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class CollisionStageThreeTileMapTests
{
    [Fact]
    [Trait("CollisionStage", "3")]
    public void VillageFenceCrossesChunkBoundaryAndNegativeCoordinatesWithoutCullingCollision()
    {
        TileColliderDescriptor2D fence = FullCellBox();
        TileDefinition2D definition = Definition(1, fence);
        TileChunk2D left = Chunk(-2, 0, 2, 1, 1, 1);
        TileChunk2D right = Chunk(0, 0, 2, 1, 1, 1);
        TileChunk2D farCulled = Chunk(100, 0, 1, 1, 1);
        TileMap2D map = Map(definition, left, right, farCulled);
        CircleCollider2D player = new() { Radius = 2, TranslateX = -50, TranslateY = 8 };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(player);
        RenderSurface2D surface = new() { Scene = scene };
        DrawCommandList commands = [];

        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, default);

        CollisionWorld2DDiagnosticsSnapshot snapshot = scene.CollisionWorld.GetDiagnosticsSnapshot();
        MoveCollisionResult2D movement = scene.CollisionWorld.MoveAndCollide(
            player,
            new Vector2(100, 0));
        CollisionHit2D farHit = Assert.Single(
            scene.CollisionWorld.Raycast(new Vector2(1590, 8), Vector2.UnitX, 30));

        Assert.Empty(commands);
        Assert.Equal(0, map.GetDiagnosticsSnapshot().VisibleChunks);
        Assert.Equal(4, snapshot.EntryCount); // one coalesced collider per chunk plus player
        Assert.NotNull(movement.Collision);
        Assert.InRange(movement.Collision.Point.X, -32.01f, -31.99f);
        Assert.InRange(farHit.Point.X, 1599.99f, 1600.01f);
    }

    [Fact]
    [Trait("CollisionStage", "3")]
    public void CoalescingRequiresIdenticalFilterTriggerPropertiesAndDebugIdentity()
    {
        IReadOnlyDictionary<string, object?> wood = new Dictionary<string, object?>
        {
            ["material"] = "wood"
        };
        TileColliderDescriptor2D baseline = FullCellBox(
            debugIdentity: "fence",
            properties: wood);
        TileColliderDescriptor2D[] incompatible =
        [
            FullCellBox(collisionLayer: 2, debugIdentity: "fence", properties: wood),
            FullCellBox(collisionMask: 2, debugIdentity: "fence", properties: wood),
            FullCellBox(isTrigger: true, debugIdentity: "fence", properties: wood),
            FullCellBox(debugIdentity: "other", properties: wood),
            FullCellBox(
                debugIdentity: "fence",
                properties: new Dictionary<string, object?> { ["material"] = "stone" })
        ];

        foreach (TileColliderDescriptor2D different in incompatible)
        {
            TileMap2D split = Map(
                [Definition(1, baseline), Definition(2, different)],
                Chunk(0, 0, 2, 1, 1, 2));
            Scene2D splitScene = new();
            splitScene.Children.Add(split);

            Assert.Equal(2, splitScene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        }

        TileMap2D merged = Map(
            [
                Definition(1, FullCellBox(
                    debugIdentity: "same",
                    properties: new Dictionary<string, object?> { ["material"] = "wood" })),
                Definition(2, FullCellBox(
                    debugIdentity: "same",
                    properties: new Dictionary<string, object?> { ["material"] = "wood" }))
            ],
            Chunk(0, 0, 2, 1, 1, 2));
        Scene2D mergedScene = new();
        mergedScene.Children.Add(merged);

        Assert.Equal(1, mergedScene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
    }

    [Fact]
    [Trait("CollisionStage", "3")]
    public void PromotedTileExplicitlyComposesOrReplacesImportedColliderWithoutDuplication()
    {
        TileMap2D map = Map(Definition(1, FullCellBox()), Chunk(2, 0, 1, 1, 1));
        Scene2D scene = new();
        scene.Children.Add(map);
        TileCellKey2D key = new("Structures", 2, 0);
        TileInstance2D tile = map.Promote(key);
        BoxCollider2D custom = new() { Width = 16, Height = 16 };
        tile.Colliders.Add(custom);

        CollisionHit2D[] composed = scene.CollisionWorld.Raycast(
            new Vector2(24, 8),
            Vector2.UnitX,
            32);
        Assert.Equal(2, composed.Length);

        tile.ReplacesImportedColliders = true;
        CollisionHit2D replaced = Assert.Single(scene.CollisionWorld.Raycast(
            new Vector2(24, 8),
            Vector2.UnitX,
            32));
        Assert.Same(custom, replaced.Collider);
        Assert.Same(tile, replaced.Entity);

        Assert.True(map.Demote(key));
        CollisionHit2D restored = Assert.Single(scene.CollisionWorld.Raycast(
            new Vector2(24, 8),
            Vector2.UnitX,
            32));
        Assert.NotSame(custom, restored.Collider);
    }

    [Fact]
    [Trait("CollisionStage", "3")]
    public void ReplacingOneChunkUpdatesOnlyItsCollisionEntries()
    {
        TileDefinition2D definition = Definition(1, FullCellBox());
        TileChunk2D changed = Chunk(0, 0, 1, 1, 1);
        TileChunk2D unchanged = Chunk(10, 0, 1, 1, 1);
        TileMap2D map = Map(definition, changed, unchanged);
        Scene2D scene = new();
        scene.Children.Add(map);
        CollisionWorld2D world = scene.CollisionWorld;
        CollisionWorld2DDiagnosticsSnapshot before = world.GetDiagnosticsSnapshot();
        Collider2D unchangedCollider = Assert.Single(
            world.Raycast(new Vector2(150, 8), Vector2.UnitX, 30)).Collider;

        TileChunk2D replacement = Chunk(0, 0, 1, 1, 0, version: 2);
        map.Model = Model(definition, replacement, unchanged, version: 2);
        CollisionWorld2DDiagnosticsSnapshot after = world.GetDiagnosticsSnapshot();
        Collider2D sameUnchangedCollider = Assert.Single(
            world.Raycast(new Vector2(150, 8), Vector2.UnitX, 30)).Collider;

        Assert.Equal(1, after.EntryCount);
        Assert.Equal(before.RebuildCount, after.RebuildCount);
        Assert.Equal(before.IncrementalUpdateCount + 1, after.IncrementalUpdateCount);
        Assert.Equal(before.UpdatedEntryCount + 1, after.UpdatedEntryCount);
        Assert.Same(unchangedCollider, sameUnchangedCollider);
    }

    [Fact]
    [Trait("CollisionStage", "3")]
    public void DescriptorValidationUsesTheSameConvexShapeContract()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TileColliderDescriptor2D(
            TileColliderShape2D.Circle,
            radius: 0));
        Assert.Throws<ArgumentException>(() => new TileColliderDescriptor2D(
            TileColliderShape2D.Polygon,
            points: "0,0 1,1 2,2"));

        TileColliderDescriptor2D polygon = new(
            TileColliderShape2D.Polygon,
            points: "0,0 8,0 8,8 0,8",
            collisionLayer: 4,
            collisionMask: 2,
            isTrigger: true,
            debugIdentity: "door-trigger");
        Assert.Equal(4, polygon.Vertices.Count);
        Assert.Equal(4u, polygon.CollisionLayer);
        Assert.Equal(2u, polygon.CollisionMask);
        Assert.True(polygon.IsTrigger);
    }

    private static TileColliderDescriptor2D FullCellBox(
        uint collisionLayer = 1,
        uint collisionMask = uint.MaxValue,
        bool isTrigger = false,
        string? debugIdentity = null,
        IReadOnlyDictionary<string, object?>? properties = null) => new(
            TileColliderShape2D.Box,
            width: 16,
            height: 16,
            collisionLayer: collisionLayer,
            collisionMask: collisionMask,
            isTrigger: isTrigger,
            debugIdentity: debugIdentity,
            properties: properties);

    private static TileDefinition2D Definition(
        int id,
        params TileColliderDescriptor2D[] colliders) =>
        new(id, new DrawRect((id - 1) * 16, 0, 16, 16), colliders: colliders);

    private static TileChunk2D Chunk(
        int x,
        int y,
        int width,
        int height,
        params int[] tileIds) =>
        Chunk(x, y, width, height, tileIds, version: 1);

    private static TileChunk2D Chunk(
        int x,
        int y,
        int width,
        int height,
        int tileId,
        long version) =>
        Chunk(x, y, width, height, [tileId], version);

    private static TileChunk2D Chunk(
        int x,
        int y,
        int width,
        int height,
        IReadOnlyList<int> tileIds,
        long version) =>
        new(
            new TileCoordinate2D(x, y),
            width,
            height,
            tileIds.Select(static id => new TileCell2D(id)),
            version);

    private static TileMap2D Map(
        TileDefinition2D definition,
        params TileChunk2D[] chunks) =>
        Map([definition], chunks);

    private static TileMap2D Map(
        IReadOnlyList<TileDefinition2D> definitions,
        params TileChunk2D[] chunks) => new()
        {
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [new TileSet2D("Village", new ResourceId<ImageResource>("VillageAtlas"), definitions)],
                [new TileLayer2DModel("Structures", chunks)])
        };

    private static TileMap2DModel Model(
        TileDefinition2D definition,
        TileChunk2D first,
        TileChunk2D second,
        long version) => new(
            new DrawSize(16, 16),
            [new TileSet2D("Village", new ResourceId<ImageResource>("VillageAtlas"), [definition])],
            [new TileLayer2DModel("Structures", [first, second], version: version)],
            version: version);
}
