using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Pins down the existing behavior that Collider2D.Entered and Exited build on
// (docs/plans/2026-10-25-scene2d-contacte.md, stage 0).
public sealed class ColliderContactCharacterizationTests
{
    private static readonly KeyboardSnapshot NoKeys = KeyboardSnapshot.Empty;
    private static readonly KeyboardSnapshot HeldD = KeyboardSnapshot.FromDownKeys([InputKey.D]);

    [Fact]
    public void UiHostUpdateAdvancesTimeThenDrainsRelayThenDispatchesInputThenProcessesLayout()
    {
        List<string> order = [];
        UIRoot root = new(100, 100);
        OrderProbe probe = new(order) { Focusable = true };
        root.VisualChildren.Add(probe);
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.Zero);
        probe.InputHeld += (_, _) =>
        {
            order.Add("input");
            probe.Invalidate(InvalidationFlags.Measure, "Contact delivery characterization");
        };
        root.Relay.Post(() => order.Add("relay"));
        order.Clear();

        host.Update(KeyFrame(NoKeys, HeldD), new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(["time", "relay", "input", "processing"], order);
    }

    [Fact]
    public void IntersectsCountsEdgeTouchWithinEpsilonAndTriggers()
    {
        BoxCollider2D left = new() { Width = 10, Height = 10 };
        BoxCollider2D right = new() { Width = 10, Height = 10 };
        Sprite2D rightSprite = new() { X = 10, Collider = right }; // the boxes share the edge x = 10
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = left });
        scene.Children.Add(rightSprite);
        CollisionWorld2D world = scene.CollisionWorld;

        Assert.True(world.Intersects(left, right));
        rightSprite.X = 10 + CollisionNarrowPhase2D.Epsilon / 2;
        Assert.True(world.Intersects(left, right));
        rightSprite.X = 10 + CollisionNarrowPhase2D.Epsilon * 2;
        Assert.False(world.Intersects(left, right));

        rightSprite.X = 9;
        right.IsTrigger = true;
        Assert.True(world.Intersects(left, right));
        Assert.True(world.Intersects(right, left));
    }

    [Theory]
    [InlineData(1u, uint.MaxValue, 1u, uint.MaxValue, true)]
    [InlineData(1u, 2u, 1u, uint.MaxValue, false)] // left does not accept layer 1
    [InlineData(1u, uint.MaxValue, 1u, 2u, false)] // right does not accept layer 1
    [InlineData(1u, 2u, 2u, 1u, true)] // each accepts the other's layer
    [InlineData(1u, 0u, 1u, uint.MaxValue, false)]
    [InlineData(0u, uint.MaxValue, 1u, uint.MaxValue, false)]
    public void IntersectsAppliesTheLayerAndMaskFilterInBothDirections(
        uint leftLayer,
        uint leftMask,
        uint rightLayer,
        uint rightMask,
        bool expected)
    {
        BoxCollider2D left = new() { Width = 10, Height = 10, CollisionLayer = leftLayer, CollisionMask = leftMask };
        BoxCollider2D right = new() { Width = 10, Height = 10, CollisionLayer = rightLayer, CollisionMask = rightMask };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = left });
        scene.Children.Add(new Sprite2D { X = 5, Collider = right });

        Assert.Equal(expected, scene.CollisionWorld.Intersects(left, right));
        Assert.Equal(expected, scene.CollisionWorld.Intersects(right, left));
    }

    [Fact]
    public async Task OverlapReportsTheInternalTileColliderWithTheMapAsEntity()
    {
        TileColliderDescriptor2D fence = new(TileColliderShape2D.Box, width: 16, height: 16);
        TileMap2D map = new()
        {
            Source = TileMapTestSource.Create(new TileMap2DModel(
                "Structures",
                new DrawSize(16, 16),
                [new TileSet2D(
                    "Village",
                    new ResourceId<ImageResource>("VillageAtlas"),
                    [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16), collider: fence)])],
                [new TileChunk2D(new TileCoordinate2D(2, 0), 1, 1, [new TileCell2D(1)], version: 1)]))
        };
        Scene2D scene = new();
        scene.Children.Add(map);
        using SceneSimulationContext2D context = new(scene);
        using SceneCollisionRegion2D prepared = await scene.CollisionWorld.PrepareRegionAsync(new(-64, -64, 40_000, 128));
        BoxCollider2D fox = new() { Width = 16, Height = 16 };
        scene.Children.Add(new Sprite2D { X = 40, Collider = fox }); // tile cell (2, 0) covers x 32..48

        CollisionHit2D first = Assert.Single(scene.CollisionWorld.Overlap(fox));
        CollisionHit2D second = Assert.Single(scene.CollisionWorld.Overlap(fox));

        Assert.IsType<TileStaticCollider2D>(first.Collider);
        Assert.Same(map, first.Entity);
        Assert.Same(first.Collider, second.Collider);
    }

    private static InputFrame KeyFrame(KeyboardSnapshot previous, KeyboardSnapshot current) =>
        new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);

    private sealed class OrderProbe(List<string> order) : UIElement, ITimeSensitiveRenderElement
    {
        public bool UpdateRenderTime(TimeSpan frameTime)
        {
            order.Add("time");
            return false;
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            order.Add("processing");
            return new LayoutSize(10, 10);
        }
    }
}
