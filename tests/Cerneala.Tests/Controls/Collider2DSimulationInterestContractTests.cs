using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class Collider2DSimulationInterestContractTests
{
    [Fact]
    public void IsSimulatedDefaultsFalseAndMarksOnlyTheChosenCollider()
    {
        BoxCollider2D first = new() { Width = 10, Height = 10 };
        BoxCollider2D second = new() { Width = 10, Height = 10 };
        Assert.False(GetSimulated(first));
        Assert.False(GetSimulated(second));

        SetSimulated(first, true);
        Assert.True(GetSimulated(first));
        Assert.False(GetSimulated(second));
    }

    [Fact]
    public void MarkedColliderContributesOnlyItsOwnActiveGeometryNotUiEnabledState()
    {
        BoxCollider2D collider = new() { Width = 10, Height = 10, OffsetX = 3, CollisionMask = 0, IsTrigger = true };
        Sprite2D actor = new() { X = 2000, Collider = collider, IsEnabled = false };
        Sprite2D unmarkedSibling = new() { X = 10000, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        Scene2D scene = new();
        scene.Children.Add(actor);
        scene.Children.Add(unmarkedSibling);
        using SceneSimulationContext2D context = new(scene);
        context.Update();
        Assert.Empty(scene.CollisionWorld.GetSpatialCollisionInterest());
        SetSimulated(collider, true);
        context.Update();

        SceneBounds2D interest = Assert.Single(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.Equal(SceneBoundsKind.Known, interest.Kind);
        Assert.True(interest.Bounds.X > 1900 && interest.Bounds.X <= 2003);
        Assert.True(interest.Bounds.X + interest.Bounds.Width >= 2013);
        Assert.True(HasActiveGeometry(collider));

        actor.X = 5000;
        context.Update();
        SceneBounds2D movedInterest = Assert.Single(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.True(movedInterest.Bounds.X > interest.Bounds.X + 2000);

        collider.Enabled = false;
        context.Update();
        Assert.Empty(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.False(HasActiveGeometry(collider));
        collider.Enabled = true;
        context.Update();
        Assert.Single(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.True(HasActiveGeometry(collider));
        collider.CollisionLayer = 0;
        context.Update();
        Assert.Empty(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.False(HasActiveGeometry(collider));
        collider.CollisionLayer = 1;
        context.Update();
        Assert.Single(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.True(HasActiveGeometry(collider));
        actor.IsVisible = false;
        context.Update();
        Assert.Empty(scene.CollisionWorld.GetSpatialCollisionInterest());
        Assert.False(HasActiveGeometry(collider));
    }

    [Fact]
    public void MarkerAndGeometryMutationsInvalidateExistingCollisionRevisionButIdleUpdatesDoNot()
    {
        BoxCollider2D collider = new() { Width = 10, Height = 10 };
        Sprite2D actor = new() { X = 2000, Collider = collider };
        Scene2D scene = new();
        scene.Children.Add(actor);
        using SceneSimulationContext2D context = new(scene);
        long initial = GetCollisionMutationVersion(scene);

        SetSimulated(collider, true);
        long afterMarker = GetCollisionMutationVersion(scene);
        Assert.True(afterMarker > initial);
        context.Update();
        context.Update();
        Assert.Equal(afterMarker, GetCollisionMutationVersion(scene));

        actor.X = 2020;
        long afterMove = GetCollisionMutationVersion(scene);
        Assert.True(afterMove > afterMarker);
        collider.CollisionMask = 0;
        long afterFilter = GetCollisionMutationVersion(scene);
        Assert.True(afterFilter > afterMove);
        collider.Width = 12;
        long afterShape = GetCollisionMutationVersion(scene);
        Assert.True(afterShape > afterFilter);
        actor.IsVisible = false;
        long afterVisibility = GetCollisionMutationVersion(scene);
        Assert.True(afterVisibility > afterShape);
        actor.Collider = new BoxCollider2D { Width = 10, Height = 10 };
        Assert.True(GetCollisionMutationVersion(scene) > afterVisibility);
    }

    [Fact]
    public void MarkedActorRetainsNearbyTerrainOnlyWhileItsOwnGeometryIsNear()
    {
        TileColliderDescriptor2D tileCollider = new(TileColliderShape2D.Box, width: 10, height: 10);
        TileMap2DModel model = new("terrain", new DrawSize(10, 10),
            [new TileSet2D("ground", new("GroundAtlas"),
                [new TileDefinition2D(1, new(0, 0, 10, 10), collider: tileCollider)])],
            [new TileChunk2D(new(200, 0), 1, 1, [new TileCell2D(1)])]);
        TileMap2D map = TileMap2D.FromModel(model);

        BoxCollider2D collider = new() { Width = 10, Height = 10 };
        Sprite2D actor = new() { X = 1995, Collider = collider };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(actor);
        using SceneSimulationContext2D context = new(scene);
        context.Update();
        Assert.Equal(0, map.GetDiagnosticsSnapshot().ResidentDataChunks);
        Assert.Empty(map.LogicalChildren);
        int baselineColliderEntries = scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount;

        SetSimulated(collider, true);
        Assert.True(SpinWait.SpinUntil(() =>
        {
            context.Update();
            return map.GetDiagnosticsSnapshot().ResidentDataChunks == 1;
        }, TimeSpan.FromSeconds(5)), "Marked actor did not retain nearby terrain.");
        Collider2D terrainCollider = Assert.IsAssignableFrom<Collider2D>(Assert.Single(map.LogicalChildren));
        Assert.False(GetSimulated(terrainCollider));
        Assert.True(scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount > baselineColliderEntries);

        actor.X = 4000;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            context.Update();
            return map.GetDiagnosticsSnapshot().ResidentDataChunks == 0;
        }, TimeSpan.FromSeconds(5)), "Terrain remained pinned after the marked actor moved away.");
        Assert.Empty(map.LogicalChildren);
        Assert.Equal(baselineColliderEntries, scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        Assert.Empty(scene.CollisionWorld.GetSpatialCollisionInterest().Where(interest =>
            interest.Kind == SceneBoundsKind.Known && interest.Bounds.X < 3000));
    }

    private static PropertyInfo GetMarkerProperty()
    {
        PropertyInfo? property = typeof(Collider2D).GetProperty("IsSimulated", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.True(property.CanRead && property.CanWrite);
        return property;
    }

    private static bool GetSimulated(Collider2D collider) => (bool)GetMarkerProperty().GetValue(collider)!;

    private static void SetSimulated(Collider2D collider, bool value) => GetMarkerProperty().SetValue(collider, value);

    private static bool HasActiveGeometry(Collider2D collider)
    {
        MethodInfo? method = typeof(Collider2D).GetMethod("TryGetActiveSceneGeometry",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        object?[] arguments = [null];
        return (bool)method.Invoke(collider, arguments)!;
    }

    private static long GetCollisionMutationVersion(Scene2D scene)
    {
        PropertyInfo? property = typeof(Scene2D).GetProperty("CollisionMutationVersion",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        return Convert.ToInt64(property.GetValue(scene));
    }
}
