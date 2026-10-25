using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Contract for Collider2D.Entered and Exited (docs/plans/2026-10-25-scene2d-contacte.md).
// A headless scene delivers its contacts in SceneSimulationContext2D.Update().
public sealed class ColliderContactEventTests
{
    private static readonly KeyboardSnapshot NoKeys = KeyboardSnapshot.Empty;
    private static readonly KeyboardSnapshot HeldD = KeyboardSnapshot.FromDownKeys([InputKey.D]);

    [Fact]
    public void CoinExampleRemovesTheCoinWhenTheFoxTouchesIt()
    {
        Sprite2D coin = new() { Collider = new BoxCollider2D { Width = 10, Height = 10, IsTrigger = true } };
        Sprite2D fox = new() { X = 100, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        Scene2D scene = new();
        scene.Children.Add(coin);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);

        coin.Collider.Entered += (_, e) =>
        {
            if (e.Other == fox.Collider)
            {
                scene.Children.Remove(coin);   // moneda dispare când vulpea o atinge
            }
        };
        context.Update();
        Assert.Contains(coin, scene.Children);

        fox.X = 5;
        context.Update();

        Assert.DoesNotContain(coin, scene.Children);
    }

    [Fact]
    public void MovingASpriteOverATriggerRaisesOneEnteredWithTheOtherCollider()
    {
        using CoinAndFox game = new();
        Assert.Empty(game.Update());

        game.FoxSprite.X = 5;

        Assert.Equal(["coin entered fox"], game.Update());
    }

    [Fact]
    public void MovingAgainInsideRaisesNoSecondEntered()
    {
        using CoinAndFox game = new();
        game.FoxSprite.X = 5;
        Assert.Equal(["coin entered fox"], game.Update());

        game.FoxSprite.X = 6;

        Assert.Empty(game.Update());
    }

    [Fact]
    public void LeavingRaisesOneExited()
    {
        using CoinAndFox game = new();
        game.FoxSprite.X = 5;
        Assert.Equal(["coin entered fox"], game.Update());

        game.FoxSprite.X = 100;

        Assert.Equal(["coin exited fox"], game.Update());
        Assert.Empty(game.Update());
    }

    [Fact]
    public void BothSidesReceiveTheEventWhenBothHaveHandlers()
    {
        using CoinAndFox game = new(watchFox: true);
        game.FoxSprite.X = 5;

        Assert.Equal(["coin entered fox", "fox entered coin"], game.Update());

        game.FoxSprite.X = 100;

        Assert.Equal(["coin exited fox", "fox exited coin"], game.Update());
    }

    [Theory]
    [InlineData("Enabled")]
    [InlineData("CollisionLayer")]
    [InlineData("CollisionMask")]
    [InlineData("IsVisible")]
    [InlineData("RemovedFromChildren")]
    [InlineData("ColliderReplaced")]
    public void ContactEndsWithoutMovementWhenTheOtherColliderStopsParticipating(string change)
    {
        using CoinAndFox game = new(foxX: 5);
        Assert.Equal(["coin entered fox"], game.Update());

        switch (change)
        {
            case "Enabled": game.Fox.Enabled = false; break;
            case "CollisionLayer": game.Fox.CollisionLayer = 0; break;
            case "CollisionMask": game.Fox.CollisionMask = 2; break; // the fox stops accepting the coin's layer 1
            case "IsVisible": game.FoxSprite.IsVisible = false; break;
            case "RemovedFromChildren": Assert.True(game.Scene.Children.Remove(game.FoxSprite)); break;
            case "ColliderReplaced": game.FoxSprite.Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = 100 }; break;
        }

        Assert.Equal(["coin exited fox"], game.Update());
    }

    [Theory]
    [InlineData("Enabled")]
    [InlineData("CollisionLayer")]
    [InlineData("IsVisible")]
    [InlineData("RemovedFromChildren")]
    [InlineData("ColliderReplaced")]
    public void ContactEndsWithoutMovementWhenTheWatchedColliderStopsParticipating(string change)
    {
        using CoinAndFox game = new(foxX: 5);
        Assert.Equal(["coin entered fox"], game.Update());

        switch (change)
        {
            case "Enabled": game.Coin.Enabled = false; break;
            case "CollisionLayer": game.Coin.CollisionLayer = 0; break;
            case "IsVisible": game.CoinSprite.IsVisible = false; break;
            case "RemovedFromChildren": Assert.True(game.Scene.Children.Remove(game.CoinSprite)); break;
            case "ColliderReplaced": game.CoinSprite.Collider = new BoxCollider2D { Width = 10, Height = 10 }; break;
        }

        Assert.Equal(["coin exited fox"], game.Update());
    }

    [Theory]
    [InlineData(4u, uint.MaxValue, 1u)] // the fox is on layer 4, the coin accepts only layer 1
    [InlineData(1u, 2u, uint.MaxValue)] // the fox accepts only layer 2, the coin is on layer 1
    public void MismatchedLayerAndMaskRaiseNoEvents(uint foxLayer, uint foxMask, uint coinMask)
    {
        using CoinAndFox game = new();
        game.Fox.CollisionLayer = foxLayer;
        game.Fox.CollisionMask = foxMask;
        game.Coin.CollisionMask = coinMask;
        game.FoxSprite.X = 5;

        Assert.Empty(game.Update());
    }

    [Fact]
    public void AHandlerAddedOverAnExistingOverlapReceivesEnteredAtTheNextDelivery()
    {
        using CoinAndFox game = new(foxX: 5, watchCoin: false);
        Assert.Empty(game.Update());

        game.Recorder.Watch(game.Coin, "coin");
        Assert.Empty(game.Recorder.Take()); // nothing is raised from inside +=

        Assert.Equal(["coin entered fox"], game.Update());
        Assert.Empty(game.Update());
    }

    [Fact]
    public void ASecondHandlerDoesNotReplayAContactTheColliderAlreadyReported()
    {
        using CoinAndFox game = new(foxX: 5);
        Assert.Equal(["coin entered fox"], game.Update());
        List<Collider2D> late = [];

        game.Coin.Entered += (_, e) => late.Add(e.Other);

        Assert.Empty(game.Update());
        Assert.Empty(late);
    }

    [Fact]
    public void RemovingTheLastHandlerForgetsContactsWithoutExitedAndTheNextFirstHandlerGetsEntered()
    {
        using CoinAndFox game = new(foxX: 5);
        Assert.Equal(["coin entered fox"], game.Update());

        game.Recorder.Unwatch(game.Coin);
        game.FoxSprite.X = 100;
        Assert.Empty(game.Update());
        game.FoxSprite.X = 5;
        Assert.Empty(game.Update());

        game.Recorder.Watch(game.Coin, "coin");

        Assert.Equal(["coin entered fox"], game.Update());
    }

    [Fact]
    public void AHandlerThatMovesAnotherSpriteGetsNoRecursiveEventsInTheSameDelivery()
    {
        using CoinAndFox game = new();
        BoxCollider2D bee = new() { Width = 10, Height = 10 };
        Sprite2D beeSprite = new() { X = 200, Collider = bee };
        game.Scene.Children.Add(beeSprite);
        game.Recorder.Name(bee, "bee");
        game.Coin.Entered += (_, e) =>
        {
            if (e.Other == game.Fox)
            {
                beeSprite.X = 2; // the bee now overlaps the coin
            }
        };
        game.FoxSprite.X = 5;

        Assert.Equal(["coin entered fox"], game.Update());
        Assert.Equal(["coin entered bee"], game.Update());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingAParentScene2DMovesTheContactsOfItsChildren(bool watchedColliderInsideGroup)
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = new() { Width = 10, Height = 10 };
        Scene2D group = new() { X = 100 };
        group.Children.Add(new Sprite2D { X = 5, Collider = watchedColliderInsideGroup ? coin : fox });
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = watchedColliderInsideGroup ? fox : coin });
        scene.Children.Add(group);
        using SceneSimulationContext2D context = new(scene);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        context.Update();
        Assert.Empty(recorder.Take());

        group.X = 0;
        context.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());

        group.X = 100;
        context.Update();
        Assert.Equal(["coin exited fox"], recorder.Take());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventOrderIsDeterministicForSeveralPairs(bool moveSecondFoxFirst)
    {
        BoxCollider2D a = Box();
        BoxCollider2D b = Box();
        BoxCollider2D f1 = Box();
        BoxCollider2D f2 = Box();
        Sprite2D f1Sprite = new() { X = 50, Collider = f1 }; // starts on B
        Sprite2D f2Sprite = new() { X = 300, Collider = f2 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = a }); // A covers x 0..10
        scene.Children.Add(new Sprite2D { X = 50, Collider = b }); // B covers x 50..60
        scene.Children.Add(f1Sprite);
        scene.Children.Add(f2Sprite);
        using SceneSimulationContext2D context = new(scene);
        ContactRecorder recorder = new();
        recorder.Watch(a, "A");
        recorder.Watch(b, "B");
        recorder.Watch(f1, "F1");
        recorder.Watch(f2, "F2");
        context.Update();
        Assert.Equal(["B entered F1", "F1 entered B"], recorder.Take());

        // F1 moves from B onto A (x -6..4); F2 moves onto A (x 5..15). F1 and F2 stay 1 px apart.
        if (moveSecondFoxFirst)
        {
            f2Sprite.X = 5;
            f1Sprite.X = -6;
        }
        else
        {
            f1Sprite.X = -6;
            f2Sprite.X = 5;
        }
        context.Update();

        // Every Exited comes before every Entered; each group follows the receiver's
        // attachment order (A, B, F1, F2), then the other collider's.
        Assert.Equal(
            ["B exited F1", "F1 exited B", "A entered F1", "A entered F2", "F1 entered A", "F2 entered A"],
            recorder.Take());
    }

    [Fact]
    public void ASceneWithoutContactHandlersDoesNoExactTestsWhenAThousandCollidersMove()
    {
        Scene2D scene = new();
        List<Sprite2D> sprites = [];
        for (int index = 0; index < 1_000; index++)
        {
            // 40 columns and 25 rows, 8 px apart: every 10 x 10 box overlaps its neighbors.
            Sprite2D sprite = new() { X = index % 40 * 8, Y = index / 40 * 8, Collider = Box() };
            sprites.Add(sprite);
            scene.Children.Add(sprite);
        }
        using SceneSimulationContext2D context = new(scene);
        context.Update();
        long before = scene.CollisionWorld.GetDiagnosticsSnapshot().ExactTestCount;

        MoveAll(sprites, context);

        Assert.Equal(before, scene.CollisionWorld.GetDiagnosticsSnapshot().ExactTestCount);

        // The same moves cost exact tests once one collider has a handler, so the counter sees this work.
        sprites[0].Collider!.Entered += (_, _) => { };
        MoveAll(sprites, context);
        Assert.True(scene.CollisionWorld.GetDiagnosticsSnapshot().ExactTestCount > before);
    }

    [Fact]
    public void ColliderContactEventArgsRequiresOther()
    {
        Assert.Throws<ArgumentNullException>(() => new ColliderContactEventArgs(null!));
        BoxCollider2D other = Box();

        Assert.Same(other, new ColliderContactEventArgs(other).Other);
    }

    [Fact]
    public void HandlersAddedBeforeTheSimulationContextExistsReportTheOverlapAtTheFirstDelivery()
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = coin });
        scene.Children.Add(new Sprite2D { X = 5, Collider = fox });
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        Assert.Empty(recorder.Take());

        using SceneSimulationContext2D context = new(scene);
        context.Update();

        Assert.Equal(["coin entered fox"], recorder.Take());
        context.Update();
        Assert.Empty(recorder.Take());
    }

    [Theory]
    [InlineData(false)] // the fox starts just after the coin's right edge
    [InlineData(true)] // the fox ends just before the coin's left edge, in the broadphase cell on the other side
    public void AnEdgeTouchWithinEpsilonOnAGridCellBorderIsAContact(bool foxBeforeTheCoin)
    {
        // Broadphase cell borders are multiples of SparseCollisionGrid2D.CellSize. x = 0 is one, and
        // floats near 0 are finer than Epsilon, so a gap smaller than Epsilon can be written exactly.
        const float border = 0;
        Assert.Equal(0, border % SparseCollisionGrid2D.CellSize);
        float epsilon = CollisionNarrowPhase2D.Epsilon;
        float coinX = foxBeforeTheCoin ? border : border - 10; // the coin's near edge lies on the border
        float TouchingX() => foxBeforeTheCoin ? border - 10 - (epsilon / 2) : border + (epsilon / 2);
        float SeparatedX() => foxBeforeTheCoin ? border - 10 - (epsilon * 2) : border + (epsilon * 2);
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Sprite2D foxSprite = new() { X = 200, Collider = fox };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = coinX, Collider = coin });
        scene.Children.Add(foxSprite);
        using SceneSimulationContext2D context = new(scene);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        context.Update();
        Assert.Empty(recorder.Take());

        foxSprite.X = TouchingX();
        Assert.True(scene.CollisionWorld.Intersects(coin, fox)); // the rule the events reuse
        context.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());

        foxSprite.X = SeparatedX();
        Assert.False(scene.CollisionWorld.Intersects(coin, fox));
        context.Update();
        Assert.Equal(["coin exited fox"], recorder.Take());
    }

    [Fact]
    public void RemovingANestedGroupThatHoldsBothCollidersRaisesExited()
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Scene2D group = new();
        group.Children.Add(new Sprite2D { Collider = coin });
        group.Children.Add(new Sprite2D { X = 5, Collider = fox });
        Scene2D scene = new();
        scene.Children.Add(group);
        using SceneSimulationContext2D context = new(scene);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        context.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());

        Assert.True(scene.Children.Remove(group));

        context.Update();
        Assert.Equal(["coin exited fox"], recorder.Take());
        context.Update();
        Assert.Empty(recorder.Take());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MovingTheOtherSpriteToAnotherRootSceneRaisesExitedOnce(bool updateSourceFirst)
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Sprite2D foxSprite = new() { X = 5, Collider = fox };
        Scene2D a = new();
        a.Children.Add(new Sprite2D { Collider = coin });
        a.Children.Add(foxSprite);
        Scene2D b = new();
        using SceneSimulationContext2D contextA = new(a);
        using SceneSimulationContext2D contextB = new(b);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        contextA.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());

        Assert.True(a.Children.Remove(foxSprite));
        b.Children.Add(foxSprite); // same coordinates, but the coin is not in this scene

        UpdateBoth(contextA, contextB, updateSourceFirst);
        Assert.Equal(["coin exited fox"], recorder.Take());
        UpdateBoth(contextA, contextB, updateSourceFirst);
        Assert.Empty(recorder.Take());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MovingAGroupThatHoldsBothCollidersToAnotherRootSceneRaisesNoEvents(bool updateSourceFirst)
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Sprite2D foxSprite = new() { X = 5, Collider = fox };
        Scene2D group = new();
        group.Children.Add(new Sprite2D { Collider = coin });
        group.Children.Add(foxSprite);
        Scene2D a = new();
        a.Children.Add(group);
        Scene2D b = new();
        using SceneSimulationContext2D contextA = new(a);
        using SceneSimulationContext2D contextB = new(b);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        contextA.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());

        Assert.True(a.Children.Remove(group));
        b.Children.Add(group);

        UpdateBoth(contextA, contextB, updateSourceFirst);
        Assert.Empty(recorder.Take());

        foxSprite.X = 100;
        contextB.Update();
        Assert.Equal(["coin exited fox"], recorder.Take());
        contextA.Update();
        Assert.Empty(recorder.Take());
    }

    [Fact]
    public void AContactReportedBeforeTheSceneWasNestedIsKeptWithoutNewEvents()
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        BoxCollider2D fox = Box();
        Sprite2D foxSprite = new() { X = 5, Collider = fox };
        Scene2D g = new();
        g.Children.Add(new Sprite2D { Collider = coin });
        g.Children.Add(foxSprite);
        SceneSimulationContext2D contextG = new(g);
        ContactRecorder recorder = new();
        recorder.Watch(coin, "coin");
        recorder.Name(fox, "fox");
        contextG.Update();
        Assert.Equal(["coin entered fox"], recorder.Take());
        contextG.Dispose();
        Scene2D a = new();
        using SceneSimulationContext2D contextA = new(a);

        a.Children.Add(g);

        contextA.Update();
        Assert.Empty(recorder.Take());
        foxSprite.X = 100;
        contextA.Update();
        Assert.Equal(["coin exited fox"], recorder.Take());
    }

    [Fact]
    public async Task ASpriteMovedOntoAStaticTileReceivesEnteredWithTheTileCollider()
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
        Sprite2D foxSprite = new() { X = 200, Collider = fox };
        scene.Children.Add(foxSprite);
        List<Collider2D> entered = [];
        List<Collider2D> exited = [];
        fox.Entered += (_, e) => entered.Add(e.Other);
        fox.Exited += (_, e) => exited.Add(e.Other);
        context.Update();
        Assert.Empty(entered);

        foxSprite.X = 40; // tile cell (2, 0) covers x 32..48
        context.Update();

        Collider2D other = Assert.Single(entered);
        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Overlap(fox));
        Assert.Same(hit.Collider, other);
        Assert.IsType<TileStaticCollider2D>(other);
        Assert.Empty(exited);

        foxSprite.X = 200;
        context.Update();

        Assert.Same(other, Assert.Single(exited));
        Assert.Single(entered);
    }

    [Fact]
    public void AHandlerExceptionDoesNotStopTheOtherContactEventsAndIsRethrownAfterDelivery()
    {
        using CoinAndFox game = new(watchFox: true);
        InvalidOperationException failure = new("handler failure");
        game.Coin.Entered += (_, _) => throw failure;
        game.FoxSprite.X = 5;

        AggregateException thrown = Assert.Throws<AggregateException>(() => game.Context.Update());

        Assert.Same(failure, Assert.Single(thrown.InnerExceptions));
        Assert.Equal(["coin entered fox", "fox entered coin"], game.Recorder.Take());
        Assert.Empty(game.Update());
    }

    [Fact]
    public void UiHostDeliversContactsAfterInputDispatchAndBeforeProcessing()
    {
        List<string> order = [];
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        Sprite2D foxSprite = new() { X = 100, Collider = Box() };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = coin });
        scene.Children.Add(foxSprite);
        UIRoot root = new(100, 100);
        OrderProbe probe = new(order) { Focusable = true };
        root.VisualChildren.Add(probe);
        root.VisualChildren.Add(new RenderSurface2D { Scene = scene });
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.Zero);
        probe.InputHeld += (_, _) =>
        {
            order.Add("input");
            foxSprite.X = 5; // now on the coin
        };
        coin.Entered += (_, _) =>
        {
            order.Add("contact");
            probe.Invalidate(InvalidationFlags.Measure, "Contact delivery");
        };
        order.Clear();

        host.Update(KeyFrame(NoKeys, HeldD), new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(["input", "contact", "processing"], order);
    }

    [Fact]
    public void UiHostStopsDeliveringTheOldSceneAfterTheSurfaceSceneIsReplaced()
    {
        List<string> events = [];
        (Scene2D oldScene, Sprite2D oldFox) = CoinAndFoxScene("old", events);
        (Scene2D newScene, Sprite2D newFox) = CoinAndFoxScene("new", events);
        UIRoot root = new(100, 100);
        RenderSurface2D surface = new() { Scene = oldScene };
        root.VisualChildren.Add(surface);
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.Zero);
        oldFox.X = 5;
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));
        Assert.Equal(["old coin entered fox"], events);
        events.Clear();

        surface.Scene = newScene;
        oldFox.X = 100; // the old scene no longer has a simulation context
        newFox.X = 5;
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Equal(["new coin entered fox"], events);
        events.Clear();

        root.VisualChildren.Remove(surface);
        newFox.X = 100;
        host.Update(KeyFrame(NoKeys, NoKeys), new UiViewport(100, 100), TimeSpan.FromMilliseconds(16));

        Assert.Empty(events);
    }

    private static (Scene2D Scene, Sprite2D FoxSprite) CoinAndFoxScene(string name, List<string> events)
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        Sprite2D foxSprite = new() { X = 100, Collider = Box() };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = coin });
        scene.Children.Add(foxSprite);
        coin.Entered += (_, _) => events.Add($"{name} coin entered fox");
        coin.Exited += (_, _) => events.Add($"{name} coin exited fox");
        return (scene, foxSprite);
    }

    private static void UpdateBoth(SceneSimulationContext2D a, SceneSimulationContext2D b, bool aFirst)
    {
        if (aFirst)
        {
            a.Update();
            b.Update();
        }
        else
        {
            b.Update();
            a.Update();
        }
    }

    private static InputFrame KeyFrame(KeyboardSnapshot previous, KeyboardSnapshot current) =>
        new(PointerSnapshot.Empty, PointerSnapshot.Empty, previous, current, []);

    private static BoxCollider2D Box() => new() { Width = 10, Height = 10 };

    private static void MoveAll(List<Sprite2D> sprites, SceneSimulationContext2D context)
    {
        foreach (Sprite2D sprite in sprites)
        {
            sprite.X += 3;
        }

        context.Update();
    }

    // Records "processing" when UiHost measures it; the host measures after it delivers contacts.
    private sealed class OrderProbe(List<string> order) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            order.Add("processing");
            return new LayoutSize(10, 10);
        }
    }

    // A trigger coin at x 0..10 and a fox that starts at foxX, both 10 x 10 px.
    private sealed class CoinAndFox : IDisposable
    {
        public CoinAndFox(bool watchFox = false, float foxX = 100, bool watchCoin = true)
        {
            CoinSprite = new Sprite2D { Collider = Coin };
            FoxSprite = new Sprite2D { X = foxX, Collider = Fox };
            Scene.Children.Add(CoinSprite);
            Scene.Children.Add(FoxSprite);
            Context = new SceneSimulationContext2D(Scene);
            if (watchCoin) { Recorder.Watch(Coin, "coin"); } else { Recorder.Name(Coin, "coin"); }
            if (watchFox) { Recorder.Watch(Fox, "fox"); } else { Recorder.Name(Fox, "fox"); }
        }

        public BoxCollider2D Coin { get; } = new() { Width = 10, Height = 10, IsTrigger = true };

        public BoxCollider2D Fox { get; } = new() { Width = 10, Height = 10 };

        public Sprite2D CoinSprite { get; }

        public Sprite2D FoxSprite { get; }

        public Scene2D Scene { get; } = new();

        public SceneSimulationContext2D Context { get; }

        public ContactRecorder Recorder { get; } = new();

        public string[] Update()
        {
            Context.Update();
            return Recorder.Take();
        }

        public void Dispose() => Context.Dispose();
    }

    // Records "<receiver> entered <other>" and "<receiver> exited <other>"; the receiver is the event sender.
    private sealed class ContactRecorder
    {
        private readonly Dictionary<Collider2D, string> names = new(ReferenceEqualityComparer.Instance);
        private readonly List<string> events = [];

        public void Name(Collider2D collider, string name) => names[collider] = name;

        public void Watch(Collider2D collider, string name)
        {
            Name(collider, name);
            collider.Entered += OnEntered;
            collider.Exited += OnExited;
        }

        public void Unwatch(Collider2D collider)
        {
            collider.Entered -= OnEntered;
            collider.Exited -= OnExited;
        }

        public string[] Take()
        {
            string[] taken = [.. events];
            events.Clear();
            return taken;
        }

        private void OnEntered(object? sender, ColliderContactEventArgs e) =>
            events.Add($"{NameOf(sender)} entered {NameOf(e.Other)}");

        private void OnExited(object? sender, ColliderContactEventArgs e) =>
            events.Add($"{NameOf(sender)} exited {NameOf(e.Other)}");

        private string NameOf(object? value) =>
            value is Collider2D collider && names.TryGetValue(collider, out string? name) ? name : "unknown";
    }
}
