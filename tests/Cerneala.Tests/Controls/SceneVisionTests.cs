using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Contract for Sprite2D.Vision and Scene2D.Vision (docs/plans/2026-10-25-scene2d-vision.md).
// A Vision is a sensor: it never blocks, is never picked, is not part of the input route, never sees its
// owner's own colliders, and only its owner learns about touches.
// A headless scene delivers its contacts in SceneSimulationContext2D.Update().
//
// Layout used by most tests. A bee is a Sprite2D at (X, Y) whose body is a 10 x 10 box behind the
// cone apex (x -12..-2, y -5..5 from the bee) and whose Vision is a cone of Radius 150, Angle 90,
// Direction 0 with its apex at the bee. A fox is a 10 x 10 box.
public sealed class SceneVisionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)] // IsTrigger on a Vision changes nothing
    public void AVisionOnASpriteRaisesOneEnteredAndOneExitedForTheBodyThatComesAndGoes(bool visionIsTrigger)
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(400, -5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        bee.Vision!.IsTrigger = visionIsTrigger;
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision);
        context.Update();
        Assert.Empty(log.Entered);

        fox.X = 60; // the fox centre is at (65, 0): 65 px in front of the apex
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));
        Assert.Empty(log.Exited);

        context.Update();
        Assert.Single(log.Entered);

        fox.X = 400;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Exited));
        Assert.Single(log.Entered);
        log.AssertEverySenderIsTheWatchedCollider();
    }

    [Fact]
    public void AVisionOnANestedGroupRaisesOneEnteredAndOneExitedForTheBodyThatComesAndGoes()
    {
        Scene2D scene = new();
        Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        dragon.Children.Add(new Sprite2D { X = -40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } });
        Sprite2D fox = Fox(600, 45);
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(dragon.Vision!);
        context.Update();
        Assert.Empty(log.Entered);

        fox.X = 160; // the fox centre is at (165, 50): 65 px in front of the group origin
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));

        fox.X = 600;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Exited));
        Assert.Single(log.Entered);
        log.AssertEverySenderIsTheWatchedCollider();
    }

    [Fact]
    public void ABodyBehindTheBeeButWithinTheRadiusRaisesNoEntered()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(-70, -5); // centre (-65, 0): 65 px behind the apex
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);

        context.Update();
        fox.X = -69;
        context.Update();

        Assert.Empty(log.Entered);
        Assert.Empty(log.Exited);
    }

    [Fact]
    public void TheBodyThatEntersAVisionReceivesNoEventForIt()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(400, -5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog visionLog = new(bee.Vision!);
        ContactLog foxLog = new(fox.Collider!);
        context.Update();

        fox.X = 60;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(visionLog.Entered)); // the cone did see the fox

        fox.X = 400;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(visionLog.Exited));

        Assert.Empty(foxLog.Entered);
        Assert.Empty(foxLog.Exited);
    }

    [Fact]
    public void AVisionFollowsItsOwnerWhenTheOwnerMoves()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(260, -5); // centre (265, 0): beyond the radius from the bee at x 0
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);
        context.Update();
        Assert.Empty(log.Entered);

        bee.X = 150; // the apex is now 115 px behind the fox
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Entered));
    }

    [Fact]
    public void AVisionTurnsWithTheRotationOfItsOwner()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(-70, -5); // centre (-65, 0): on the left of the bee
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);
        context.Update();
        Assert.Empty(log.Entered);

        bee.Rotation = MathF.PI;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Entered));
    }

    [Theory]
    [InlineData(0f, RenderSurface2DSpriteFlip.Horizontal, 180f)] // d becomes 180 - d
    [InlineData(90f, RenderSurface2DSpriteFlip.Vertical, -90f)] // d becomes -d
    [InlineData(0f, RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical, 180f)] // d becomes d + 180
    [InlineData(30f, RenderSurface2DSpriteFlip.Horizontal, 150f)]
    [InlineData(30f, RenderSurface2DSpriteFlip.Vertical, -30f)]
    [InlineData(30f, RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical, -150f)]
    public void TheFlipOfTheSpriteMirrorsTheDirectionOfItsVisionAtTheNextUpdate(
        float direction, RenderSurface2DSpriteFlip flip, float mirroredAngle)
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        bee.Vision!.Direction = direction;
        Vector2 centre = Vector2.Transform(new Vector2(65, 0), Matrix3x2.CreateRotation(mirroredAngle * MathF.PI / 180));
        Sprite2D fox = Fox(centre.X - 5, centre.Y - 5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision);
        context.Update();
        Assert.Empty(log.Entered); // not mirrored yet: the fox is outside the cone

        bee.Flip = flip;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Entered));
    }

    [Fact]
    public void FlipMirrorsTheApexAcrossTheCentreOfTheImageAndTheDirectionOfTheVision()
    {
        // A 32 x 32 image at Origin (0, 0) fills x 0..32, so its vertical centre line is x 16.
        // No body: only the cone, Radius 50, Direction 0, with its apex at (28, 10).
        // Flipped horizontally the apex is at x 4 and the cone looks left: a fox box at x -40..-30 (y 5..15) is
        // 34..44 px from the apex and is seen. From the unmirrored apex at x 28 it would be 58..68 px away.
        Scene2D scene = new();
        Sprite2D bee = new()
        {
            Image = new ImageReference(new FixedImage(32, 32)),
            Vision = new ConeCollider2D { Radius = 50, Angle = 90, Direction = 0, OffsetX = 28, OffsetY = 10 }
        };
        Sprite2D fox = Fox(-40, 5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);
        context.Update();
        Assert.Empty(log.Entered); // not flipped: the fox is behind the apex and out of reach

        bee.Flip = RenderSurface2DSpriteFlip.Horizontal;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Entered));
    }

    [Fact]
    public void AVisionOnAGroupFollowsTheXAndYOfTheGroup()
    {
        Scene2D scene = new();
        Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D fox = Fox(300, 45); // centre (305, 50): beyond the radius from the apex (100, 50)
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(dragon.Vision!);
        context.Update();
        Assert.Empty(log.Entered);

        dragon.X = 200; // the apex is now 105 px behind the fox
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));

        dragon.Y = 400;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Exited));
    }

    [Fact]
    public void AVisionOnAGroupTurnsAroundTheTransformOriginOfTheGroup()
    {
        // The apex is the group origin (0, 0). A half turn around (100, 0) moves it to (200, 0)
        // and makes the cone look left. A half turn around (0, 0) would keep it at (0, 0).
        Scene2D scene = new();
        Scene2D dragon = new()
        {
            TransformOrigin = new DrawPoint(100, 0),
            Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 }
        };
        Sprite2D fox = Fox(170, -5); // centre (175, 0): beyond the radius from (0, 0)
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(dragon.Vision!);
        context.Update();
        Assert.Empty(log.Entered);

        dragon.Rotation = MathF.PI;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Entered));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)] // IsTrigger on a Vision changes nothing
    public void MoveAndCollideThroughAVisionDoesNotBlockAndReportsNoTriggerHit(bool visionIsTrigger)
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        bee.Vision!.IsTrigger = visionIsTrigger;
        Sprite2D fox = Fox(60, 100); // below the cone: the cone covers |y| <= x
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        Vector2 displacement = new(0, -200); // straight up through the cone to y -100

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(fox.Collider!, displacement);

        Assert.Null(result.Collision);
        Assert.Equal(displacement, result.Travel);
        Assert.Equal(Vector2.Zero, result.Remainder);
        Assert.Empty(result.TriggerHits);

        // Control: a real body in the same path does block, so the move above was a real move.
        scene.Children.Add(new Sprite2D { X = 55, Y = -60, Collider = new BoxCollider2D { Width = 30, Height = 10 } });
        MoveCollisionResult2D blocked = scene.CollisionWorld.MoveAndCollide(fox.Collider!, displacement);
        Assert.NotNull(blocked.Collision);
        Assert.IsNotType<ConeCollider2D>(blocked.Collision.Collider);
    }

    [Fact]
    public void RaycastThroughAVisionDoesNotReturnIt()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D wall = new() { X = 95, Y = 150, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        scene.Children.Add(bee);
        scene.Children.Add(wall);

        // Straight down at x 100 through the whole cone (|y| <= 100 there), ending on the wall.
        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(100, -120), Vector2.UnitY, 300);

        Assert.Same(wall.Collider, Assert.Single(hits).Collider);
        Assert.DoesNotContain(hits, hit => ReferenceEquals(hit.Collider, bee.Vision));
    }

    [Fact]
    public void OverlapOfAnotherColliderDoesNotReturnAVision()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(60, -5); // inside the cone
        Sprite2D wall = new() { X = 62, Y = -5, Collider = new BoxCollider2D { Width = 20, Height = 20 } };
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        scene.Children.Add(wall);

        CollisionHit2D[] around = scene.CollisionWorld.Overlap(fox.Collider!);

        Assert.Same(wall.Collider, Assert.Single(around).Collider);
        Assert.DoesNotContain(around, hit => ReferenceEquals(hit.Collider, bee.Vision));
    }

    [Fact]
    public void AClickInsideTheVisionButOutsideTheBodyDoesNotHitTheBee()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = Bee(50, 100); // body x 38..48, y 95..105; the cone covers x 50..200 around y 100
        scene.Children.Add(bee);
        int beeClicks = 0;
        int visionClicks = 0;
        bee.AddHandler(InputEvents.MouseDownEvent, (_, _) => beeClicks++);
        bee.Vision!.AddHandler(InputEvents.MouseDownEvent, (_, _) => visionClicks++);
        UiHost host = CreateHost(root);
        RetainedServoInputDriver input = new(host);

        HitTestResult? inside = new HitTestService().HitTest(root, 110, 100);
        input.ClickAt(110, 100); // 60 px in front of the apex: inside the cone, outside the body

        Assert.NotSame(bee, inside?.Element);
        Assert.IsNotType<ConeCollider2D>(inside?.Element);
        Assert.Equal(0, beeClicks);
        Assert.Equal(0, visionClicks);

        // Control: a click on the body does hit the bee.
        HitTestResult? onBody = new HitTestService().HitTest(root, 43, 100);
        input.ClickAt(43, 100);

        Assert.Same(bee, onBody?.Element);
        Assert.Equal(1, beeClicks);
        Assert.Equal(0, visionClicks);
    }

    [Fact]
    public void AClickInsideTheVisionOfAGroupWithNoChildUnderThePointDoesNotHitTheGroupOrItsChildren()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        Scene2D dragon = new() { X = 50, Y = 100, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D head = new() { X = -40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // x 10..20, y 95..105
        dragon.Children.Add(head);
        scene.Children.Add(dragon);
        int dragonClicks = 0;
        int headClicks = 0;
        dragon.AddHandler(InputEvents.MouseDownEvent, (_, _) => dragonClicks++);
        head.AddHandler(InputEvents.MouseDownEvent, (_, _) => headClicks++);
        UiHost host = CreateHost(root);
        RetainedServoInputDriver input = new(host);

        HitTestResult? inside = new HitTestService().HitTest(root, 110, 100);
        input.ClickAt(110, 100); // 60 px in front of the group origin: inside the cone, no child there

        Assert.NotSame(dragon, inside?.Element);
        Assert.NotSame(head, inside?.Element);
        Assert.IsNotType<ConeCollider2D>(inside?.Element);
        Assert.Equal(0, dragonClicks);
        Assert.Equal(0, headClicks);

        // Control: a click on the child does hit it, and the group sees it bubble.
        HitTestResult? onHead = new HitTestService().HitTest(root, 15, 100);
        input.ClickAt(15, 100);

        Assert.Same(head, onHead?.Element);
        Assert.Equal(1, headClicks);
        Assert.Equal(1, dragonClicks);
    }

    // A Vision is not part of the input route of its owner. The input bounds of the owner, which Servo uses to
    // aim a click at its centre, are the bounds of the owner's own content and never include the cone in front of it.
    [Theory]
    [InlineData("Sprite2D", 38f, 95f)] // the bee body: x 38..48, y 95..105
    [InlineData("Scene2D", 10f, 95f)] // the head body of the dragon group: x 10..20, y 95..105
    public void AVisionDoesNotChangeTheInputBoundsOfItsOwner(string ownerKind, float bodyX, float bodyY)
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        SceneNode2D owner = CreateOwnerWithABodyAndAVision(ownerKind);
        scene.Children.Add(owner);

        LayoutRect withVision = InputCoordinateConverter.GetRootBounds(owner);
        SetVision(owner, null);
        LayoutRect withoutVision = InputCoordinateConverter.GetRootBounds(owner);

        Assert.Equal(new LayoutRect(bodyX, bodyY, 10, 10), withoutVision);
        Assert.Equal(withoutVision, withVision);
    }

    [Theory]
    [InlineData("Sprite2D")]
    [InlineData("Scene2D")]
    public async Task ServoClickOnTheOwnerOfAVisionHitsTheOwnerAndNotTheEmptyPlaceInFrontOfIt(string ownerKind)
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        SceneNode2D owner = CreateOwnerWithABodyAndAVision(ownerKind);
        scene.Children.Add(owner);
        ServoApi.SetId(owner, "owner");
        int clicks = 0;
        owner.AddHandler(InputEvents.MouseDownEvent, (_, _) => clicks++);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(500) });

        await servo.ClickAsync(ServoTarget.ById("owner"));

        Assert.Equal(1, clicks);
    }

    [Fact]
    public async Task AVisionIsNeitherInTheInputRouteNorInTheServoTreeWhileTheBodyIs()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = Bee(50, 100);
        Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        scene.Children.Add(bee);
        scene.Children.Add(dragon);
        ConeCollider2D cone = bee.Vision!;
        ConeCollider2D groupCone = dragon.Vision!;
        ServoApi.SetId(bee.Collider!, "body");
        ServoApi.SetId(cone, "cone");
        ServoApi.SetId(groupCone, "group-cone");
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);

        ElementInputRouteMap route = root.InputCache.EnsureCurrent(root);
        Assert.True(route.Contains(bee));
        Assert.True(route.Contains(bee.Collider!));
        Assert.False(route.Contains(cone));
        Assert.False(route.Contains(groupCone));
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("body")));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("cone")));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("group-cone")));

        // A cone that leaves its owner, and one that replaces it, stay out of the route.
        ConeCollider2D replacement = new() { Radius = 150 };
        ServoApi.SetId(replacement, "replacement");
        bee.Vision = replacement;
        dragon.Vision = null;
        bee.Vision = cone;
        dragon.Vision = groupCone;
        route = root.InputCache.EnsureCurrent(root);
        Assert.False(route.Contains(replacement));
        Assert.False(route.Contains(cone));
        Assert.False(route.Contains(groupCone));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("cone")));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("group-cone")));
        Assert.True(route.Contains(bee.Collider!));
    }

    [Fact]
    public void TwoOverlappingVisionsRaiseNoEventsForEachOther()
    {
        Scene2D scene = new();
        Sprite2D left = new() { Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D right = new() { X = 100, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 180 } };
        Sprite2D fox = Fox(400, 400);
        scene.Children.Add(left);
        scene.Children.Add(right);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog leftLog = new(left.Vision!);
        ContactLog rightLog = new(right.Vision!);

        context.Update();
        right.X = 90;
        context.Update();

        Assert.Empty(leftLog.Entered);
        Assert.Empty(rightLog.Entered);

        // Control: both cones do see a body that is in both of them, and only that body.
        fox.X = 45; // centre (50, 0): inside both cones
        fox.Y = -5;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(leftLog.Entered));
        Assert.Same(fox.Collider, Assert.Single(rightLog.Entered));
        Assert.Empty(leftLog.Exited);
        Assert.Empty(rightLog.Exited);
    }

    [Fact]
    public void OverlapOfAVisionReturnsExactlyTheBodiesInsideTheCone()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(60, -5); // in front
        Sprite2D wolf = Fox(-70, -5); // behind the bee, within the radius
        Sprite2D owl = Fox(400, -5); // in front but beyond the radius
        Sprite2D otherEyes = new() { X = 30, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        scene.Children.Add(wolf);
        scene.Children.Add(owl);
        scene.Children.Add(otherEyes);

        CollisionHit2D[] around = scene.CollisionWorld.Overlap(bee.Vision!);

        CollisionHit2D hit = Assert.Single(around);
        Assert.Same(fox.Collider, hit.Collider);
        Assert.Same(fox, hit.Entity);
        Assert.DoesNotContain(around, item => item.Collider is ConeCollider2D);
    }

    [Fact]
    public void NestedVisionsOfAGroupAndOfItsChildAreIndependentAndEachRaisesItsOwnEvents()
    {
        Scene2D scene = new();
        Scene2D dragon = new() { X = 100, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D head = new() { X = 30, Vision = new ConeCollider2D { Radius = 100, Angle = 90, Direction = 0 } };
        dragon.Children.Add(head);
        Sprite2D fox = Fox(600, -5);
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog dragonLog = new(dragon.Vision!);
        ContactLog headLog = new(head.Vision!);
        context.Update();

        fox.X = 170; // centre (175, 0): 75 px from the group apex (100, 0), 45 px from the head apex (130, 0)
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(dragonLog.Entered));
        Assert.Same(fox.Collider, Assert.Single(headLog.Entered));
        dragonLog.AssertEverySenderIsTheWatchedCollider();
        headLog.AssertEverySenderIsTheWatchedCollider();

        fox.X = 600;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(dragonLog.Exited));
        Assert.Same(fox.Collider, Assert.Single(headLog.Exited));
    }

    [Theory]
    [InlineData("Sprite2D", "Sprite2D")]
    [InlineData("Sprite2D", "Scene2D")]
    [InlineData("Scene2D", "Sprite2D")]
    [InlineData("Scene2D", "Scene2D")]
    public void TheSameConeCannotBeTheVisionOfTwoOwnersUntilTheFirstOneReleasesIt(string firstKind, string secondKind)
    {
        SceneNode2D first = CreateOwner(firstKind);
        SceneNode2D second = CreateOwner(secondKind);
        ConeCollider2D cone = new();
        SetVision(first, cone);

        Assert.Throws<InvalidOperationException>(() => SetVision(second, cone));
        Assert.Same(cone, GetVision(first));
        Assert.Null(GetVision(second));
        Assert.Same(first, cone.LogicalParent);

        SetVision(first, null);
        Assert.Null(GetVision(first));
        Assert.Null(cone.LogicalParent);

        SetVision(second, cone);
        Assert.Same(cone, GetVision(second));
        Assert.Null(GetVision(first));
        Assert.Same(second, cone.LogicalParent);
    }

    [Fact]
    public void ASpriteKeepsItsBodyAndItsVisionInSeparateSlotsAsLogicalChildren()
    {
        BoxCollider2D body = new() { Width = 10, Height = 10 };
        ConeCollider2D cone = new();
        Sprite2D bee = new() { Collider = body, Vision = cone };

        Assert.Same(body, bee.Collider);
        Assert.Same(cone, bee.Vision);
        Assert.Same(bee, body.LogicalParent);
        Assert.Same(bee, cone.LogicalParent);
        Assert.Equal(2, bee.LogicalChildren.Count);
        Assert.Contains(body, bee.LogicalChildren);
        Assert.Contains(cone, bee.LogicalChildren);
        Assert.Empty(bee.VisualChildren);
        Assert.Null(cone.VisualParent);

        bee.Vision = null;
        Assert.Same(body, bee.Collider);
        Assert.Same(body, Assert.Single(bee.LogicalChildren));
        Assert.Null(cone.LogicalParent);
    }

    [Fact]
    public void ASceneOwnsItsVisionAsALogicalChildAndNeverAsAChild()
    {
        Scene2D scene = new();
        ConeCollider2D cone = new();

        scene.Vision = cone;

        Assert.Same(cone, scene.Vision);
        Assert.Same(scene, cone.LogicalParent);
        Assert.Empty(scene.Children);
        Assert.Same(cone, Assert.Single(scene.LogicalChildren));
        Assert.Empty(scene.VisualChildren);
        Assert.Null(cone.VisualParent);

        Assert.Throws<InvalidOperationException>(() => scene.Children.Add(cone));
        Assert.Throws<InvalidOperationException>(() => scene.Children.Add(new ConeCollider2D()));
        Assert.Throws<InvalidOperationException>(() => scene.LogicalChildren.Add(new ConeCollider2D()));
        Assert.Throws<InvalidOperationException>(() => scene.LogicalChildren.Remove(cone));
        Assert.Same(cone, scene.Vision);
        Assert.Same(scene, cone.LogicalParent);
        Assert.Empty(scene.Children);
        Assert.Same(cone, Assert.Single(scene.LogicalChildren));
    }

    [Fact]
    public void ChildrenOfASceneWithAVisionAddInsertReplaceAndRemoveWithoutTouchingTheCone()
    {
        Scene2D scene = new() { Vision = new ConeCollider2D() };
        ConeCollider2D cone = scene.Vision!;
        Sprite2D a = new();
        Sprite2D b = new();
        Sprite2D c = new();

        scene.Children.Add(a);
        Assert.Equal<SceneNode2D>([a], scene.Children);

        scene.Children.Insert(0, b);
        Assert.Equal<SceneNode2D>([b, a], scene.Children);

        scene.Children[1] = c;
        Assert.Equal<SceneNode2D>([b, c], scene.Children);
        Assert.Null(a.LogicalParent);

        Assert.True(scene.Children.Remove(b));
        Assert.Equal<SceneNode2D>([c], scene.Children);

        scene.Children.Clear();
        Assert.Empty(scene.Children);

        Assert.Same(cone, scene.Vision);
        Assert.Same(scene, cone.LogicalParent);
        Assert.Contains(cone, scene.LogicalChildren);
        Assert.DoesNotContain(cone, scene.Children);
    }

    [Fact]
    public void ReplacingAVisionKeepsItAfterTheChildrenOfItsGroupAndNeverMovesTheBodyOfASprite()
    {
        Scene2D scene = new();
        ConeCollider2D first = new();
        ConeCollider2D second = new();
        Sprite2D a = new();
        Sprite2D b = new();
        scene.Children.Add(a);
        scene.Vision = first;
        scene.Children.Add(b);
        Assert.Equal<SceneNode2D>([a, b, first], scene.LogicalChildren.Cast<SceneNode2D>());

        scene.Vision = second;
        Assert.Equal<SceneNode2D>([a, b, second], scene.LogicalChildren.Cast<SceneNode2D>());
        Assert.Null(first.LogicalParent);
        scene.Children.Insert(1, new Sprite2D());
        Assert.Same(second, scene.LogicalChildren[^1]);
        Assert.Equal(3, scene.Children.Count);

        BoxCollider2D body = new();
        ConeCollider2D cone = new();
        Sprite2D bee = new() { Vision = cone };
        bee.Collider = body;
        Assert.Equal<SceneNode2D>([cone, body], bee.LogicalChildren.Cast<SceneNode2D>());
        bee.Flip = RenderSurface2DSpriteFlip.Horizontal; // a sprite outside every scene has no world to tell

        // The body slot refuses the sprite's own cone before it changes anything.
        Assert.Throws<InvalidOperationException>(() => bee.Collider = cone);
        Assert.Same(body, bee.Collider);
        Assert.Same(cone, bee.Vision);
        Assert.Equal<SceneNode2D>([cone, body], bee.LogicalChildren.Cast<SceneNode2D>());

        BoxCollider2D replacement = new();
        bee.Collider = replacement;
        Assert.Equal<SceneNode2D>([cone, replacement], bee.LogicalChildren.Cast<SceneNode2D>());
        Assert.Null(body.LogicalParent);
    }

    [Fact]
    public void RemovingTheVisionWhileABodyIsInsideRaisesOneExitedOnTheRemovedConeAtTheNextUpdate()
    {
        Scene2D scene = new();
        Sprite2D bee = Bee();
        Sprite2D fox = Fox(60, -5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ConeCollider2D cone = bee.Vision!;
        ContactLog log = new(cone);
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));
        Assert.Empty(log.Exited);

        bee.Vision = null;
        Assert.Empty(log.Exited); // nothing is raised from inside the setter
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(log.Exited));
        Assert.Single(log.Entered);
        log.AssertEverySenderIsTheWatchedCollider();
        context.Update();
        Assert.Single(log.Exited);
        Assert.Null(cone.LogicalParent);
    }

    // A Vision never sees its own owner (user decision, 2026-10-25). A sprite's Vision ignores that sprite's body.
    [Fact]
    public void AVisionNeverSeesItsOwnersBodyEvenWhenTheBodyOverlapsTheCone()
    {
        Scene2D scene = new();
        // The body (x 0..10, y 0..10) overlaps the cone (|y| <= x) right next to the apex at (0, 0).
        Sprite2D bee = new()
        {
            Collider = new BoxCollider2D { Width = 10, Height = 10 },
            Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 }
        };
        Sprite2D fox = Fox(400, -5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);
        context.Update();
        Assert.Empty(log.Entered);
        Assert.Empty(scene.CollisionWorld.Overlap(bee.Vision!));

        // Control: a body outside the owner is still seen.
        fox.X = 60;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));
        Assert.Same(fox.Collider, Assert.Single(scene.CollisionWorld.Overlap(bee.Vision!)).Collider);

        fox.X = 400;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Exited));
        Assert.DoesNotContain(log.Entered, other => ReferenceEquals(other, bee.Collider));
        Assert.DoesNotContain(log.Exited, other => ReferenceEquals(other, bee.Collider));
    }

    [Fact]
    public void AVisionSeesAnotherSpritesBodyAtTheSamePlaceWhereItIgnoresItsOwnersBody()
    {
        Scene2D scene = new();
        Sprite2D bee = new()
        {
            Collider = new BoxCollider2D { Width = 10, Height = 10 },
            Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 }
        };
        Sprite2D twin = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // exactly where the bee's body is
        scene.Children.Add(bee);
        scene.Children.Add(twin);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);

        context.Update();

        Assert.Same(twin.Collider, Assert.Single(log.Entered));
        Assert.Same(twin.Collider, Assert.Single(scene.CollisionWorld.Overlap(bee.Vision!)).Collider);
    }

    // A group's Vision ignores every collider in the group's subtree, also in nested groups.
    [Fact]
    public void AGroupVisionIgnoresEveryColliderInsideTheGroupSubtree()
    {
        Scene2D scene = new();
        Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D core = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // scene x 100..110: overlaps the apex
        Sprite2D scales = new() { X = 20, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // scene x 120..130: in front
        Scene2D tail = new() { X = 60 };
        Sprite2D tip = new() { Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // scene x 160..170: in front, in the sub-group
        tail.Children.Add(tip);
        dragon.Children.Add(core);
        dragon.Children.Add(scales);
        dragon.Children.Add(tail);
        Sprite2D fox = Fox(600, 45);
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(dragon.Vision!);
        context.Update();
        Assert.Empty(log.Entered);
        Assert.Empty(scene.CollisionWorld.Overlap(dragon.Vision!));

        // Control: a body outside the group is still seen (centre (205, 50): 105 px in front of the group origin).
        fox.X = 200;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Entered));
        Assert.Same(fox.Collider, Assert.Single(scene.CollisionWorld.Overlap(dragon.Vision!)).Collider);

        fox.X = 600;
        context.Update();
        Assert.Same(fox.Collider, Assert.Single(log.Exited));
        Assert.Single(log.Entered);
    }

    // A child sprite's Vision ignores only that sprite's body; it does see the other parts of its group.
    [Fact]
    public void AChildSpriteVisionIgnoresOnlyItsOwnBodyAndSeesTheSiblingPartsOfItsGroup()
    {
        Scene2D scene = new();
        Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D head = new()
        {
            Collider = new BoxCollider2D { Width = 10, Height = 10 }, // scene x 100..110: overlaps the head apex
            Vision = new ConeCollider2D { Radius = 100, Angle = 90, Direction = 0 }
        };
        Sprite2D wing = new() { X = 40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // scene x 140..150: in front of the head
        dragon.Children.Add(head);
        dragon.Children.Add(wing);
        Sprite2D fox = Fox(600, 45);
        scene.Children.Add(dragon);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        ContactLog headLog = new(head.Vision!);
        ContactLog dragonLog = new(dragon.Vision!);

        context.Update();

        AssertSameColliders(headLog.Entered, wing.Collider!); // the head sees its sibling, not itself
        AssertSameColliders(OverlapOf(scene, head.Vision!), wing.Collider!);
        Assert.Empty(dragonLog.Entered); // the group ignores the head and the wing: both are in its subtree
        Assert.Empty(OverlapOf(scene, dragon.Vision!));

        // Control: a body outside the group is seen by both (centre (175, 50): 75 px in front of both apexes).
        fox.X = 170;
        context.Update();

        AssertSameColliders(headLog.Entered, wing.Collider!, fox.Collider!);
        AssertSameColliders(OverlapOf(scene, head.Vision!), wing.Collider!, fox.Collider!);
        AssertSameColliders(dragonLog.Entered, fox.Collider!);
        AssertSameColliders(OverlapOf(scene, dragon.Vision!), fox.Collider!);

        fox.X = 600;
        context.Update();

        AssertSameColliders(headLog.Exited, fox.Collider!);
        AssertSameColliders(dragonLog.Exited, fox.Collider!);
        Assert.DoesNotContain(headLog.Entered, other => ReferenceEquals(other, head.Collider));
        Assert.DoesNotContain(dragonLog.Entered, other => ReferenceEquals(other, head.Collider) || ReferenceEquals(other, wing.Collider));
    }

    // Intersects follows the rule of the contact events: a body and a Vision touch when the shapes meet,
    // two Visions never touch, and a Vision never touches a collider inside its owner's subtree.
    [Fact]
    public void IntersectsTellsWhetherABodyTouchesAVisionButNeverForTwoVisionsOrTheOwnersSubtree()
    {
        Scene2D scene = new();
        Sprite2D bee = new()
        {
            Collider = new BoxCollider2D { Width = 10, Height = 10 }, // x 0..10, y 0..10: overlaps the cone next to its apex
            Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 }
        };
        Sprite2D twin = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // exactly where the bee's body is
        Sprite2D otherEyes = new() { X = 30, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D fox = Fox(60, -5); // in front of the apex
        Sprite2D wolf = Fox(-70, -5); // behind the apex
        Scene2D dragon = new() { X = 100, Y = 300, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
        Sprite2D core = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } }; // overlaps the group apex
        dragon.Children.Add(core);
        Sprite2D prey = Fox(200, 295); // centre (205, 300): 105 px in front of the group apex
        foreach (SceneNode2D node in new SceneNode2D[] { bee, twin, otherEyes, fox, wolf, dragon, prey })
        {
            scene.Children.Add(node);
        }

        CollisionWorld2D world = scene.CollisionWorld;

        Assert.True(world.Intersects(fox.Collider!, bee.Vision!));
        Assert.True(world.Intersects(bee.Vision!, fox.Collider!));
        Assert.False(world.Intersects(wolf.Collider!, bee.Vision!));
        Assert.True(world.Intersects(twin.Collider!, bee.Vision!)); // the same place, another sprite's body

        Assert.False(world.Intersects(bee.Collider!, bee.Vision!)); // the owner's own body
        Assert.False(world.Intersects(bee.Vision!, bee.Collider!));
        Assert.False(world.Intersects(bee.Vision!, otherEyes.Vision!)); // two cones that overlap
        Assert.False(world.Intersects(otherEyes.Vision!, bee.Vision!));

        Assert.True(world.Intersects(prey.Collider!, dragon.Vision!));
        Assert.False(world.Intersects(core.Collider!, dragon.Vision!)); // a collider inside the group
        Assert.False(world.Intersects(dragon.Vision!, core.Collider!));
    }

    [Fact]
    public void AVisionOnTheRootSceneSeesNothingBecauseEveryColliderIsInsideTheScene()
    {
        Scene2D scene = new() { Vision = new ConeCollider2D { Radius = 500, Angle = 360 } };
        Sprite2D fox = Fox(60, -5);
        Scene2D group = new();
        group.Children.Add(Fox(100, 0));
        scene.Children.Add(fox);
        scene.Children.Add(group);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(scene.Vision!);

        context.Update();
        fox.X = 80;
        context.Update();

        Assert.Equal(3, scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount); // both bodies and the cone are in the world
        Assert.Empty(log.Entered);
        Assert.Empty(log.Exited);
        Assert.Empty(scene.CollisionWorld.Overlap(scene.Vision!));
        Assert.False(scene.CollisionWorld.Intersects(scene.Vision!, fox.Collider!));
    }

    [Fact]
    public void AVisionUsesTheLayerAndTheMaskOfBothSidesLikeAnyOtherCollider()
    {
        Scene2D scene = new();
        Sprite2D bee = new() { Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0, CollisionMask = 2 } };
        Sprite2D fox = Fox(60, -5);
        Sprite2D rabbit = Fox(60, -25);
        rabbit.Collider!.CollisionLayer = 2;
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        scene.Children.Add(rabbit);
        using SceneSimulationContext2D context = new(scene);
        ContactLog log = new(bee.Vision!);

        context.Update();

        // The cone accepts layer 2 only: it sees the rabbit and not the fox (layer 1).
        Assert.Same(rabbit.Collider, Assert.Single(log.Entered));
        Assert.Same(rabbit.Collider, Assert.Single(scene.CollisionWorld.Overlap(bee.Vision!)).Collider);

        // The rabbit's own mask must accept the cone's layer too.
        rabbit.Collider.CollisionMask = 4;
        context.Update();

        Assert.Same(rabbit.Collider, Assert.Single(log.Exited));
        Assert.Empty(scene.CollisionWorld.Overlap(bee.Vision!));
    }

    // A sensor is not a moving shape: MoveAndCollide of a Vision is an error, enabled or not (user decision, 2026-10-25).
    [Theory]
    [InlineData("Sprite2D", true, true)]
    [InlineData("Sprite2D", false, true)]
    [InlineData("Scene2D", true, true)]
    [InlineData("Scene2D", false, true)]
    [InlineData("Sprite2D", true, false)] // the owner is not in the scene the world belongs to
    [InlineData("Sprite2D", false, false)]
    [InlineData("Scene2D", true, false)]
    [InlineData("Scene2D", false, false)]
    public void MoveAndCollideOfAVisionThrowsAnErrorThatNamesVision(string ownerKind, bool enabled, bool ownerInScene)
    {
        Scene2D scene = new();
        SceneNode2D owner = CreateOwner(ownerKind);
        if (ownerInScene) { scene.Children.Add(owner); }
        ConeCollider2D cone = new();
        SetVision(owner, cone);
        cone.Enabled = enabled;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => scene.CollisionWorld.MoveAndCollide(cone, new Vector2(10, 0)));

        Assert.Contains("Vision", error.Message);
    }

    // A Vision is a logical child of its group but it is not scene content: the group must record, order,
    // cull and offer input candidates exactly as it does without a Vision (docs/plans/2026-10-25-scene2d-vision.md, stage 2).
    [Theory]
    [InlineData(SceneOrderMode.Source)]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void AVisionOnAGroupDoesNotChangeWhichChildrenTheGroupAndItsParentRecordAndOrder(SceneOrderMode mode)
    {
        CullingRig plain = new(mode, withVision: false);
        CullingRig watched = new(mode, withVision: true);
        Assert.Equal(["near a", "near b"], plain.Record());
        Assert.Equal(["near a", "near b"], watched.Record());

        for (int frame = 0; frame < 3; frame++)
        {
            long plainGroupRecorded = plain.Group.RecordCounters.RecordedChildren;
            long plainGroupOrdered = plain.Group.RecordCounters.OrderedChildren;
            long plainRootRecorded = plain.Root.RecordCounters.RecordedChildren;
            long plainRootOrdered = plain.Root.RecordCounters.OrderedChildren;
            long watchedGroupRecorded = watched.Group.RecordCounters.RecordedChildren;
            long watchedGroupOrdered = watched.Group.RecordCounters.OrderedChildren;
            long watchedRootRecorded = watched.Root.RecordCounters.RecordedChildren;
            long watchedRootOrdered = watched.Root.RecordCounters.OrderedChildren;

            string[] plainNames = [.. plain.Record()];
            string[] watchedNames = [.. watched.Record()];

            Assert.Equal(plainNames, watchedNames); // the same images, in the same drawing order
            Assert.Equal(
                plain.Group.RecordCounters.RecordedChildren - plainGroupRecorded,
                watched.Group.RecordCounters.RecordedChildren - watchedGroupRecorded);
            Assert.Equal(
                plain.Group.RecordCounters.OrderedChildren - plainGroupOrdered,
                watched.Group.RecordCounters.OrderedChildren - watchedGroupOrdered);
            Assert.Equal(
                plain.Root.RecordCounters.RecordedChildren - plainRootRecorded,
                watched.Root.RecordCounters.RecordedChildren - watchedRootRecorded);
            Assert.Equal(
                plain.Root.RecordCounters.OrderedChildren - plainRootOrdered,
                watched.Root.RecordCounters.OrderedChildren - watchedRootOrdered);
        }
    }

    [Fact]
    public void ALargeVisionOnAGroupAddsNoVisibleBoundsAndNoInputCandidate()
    {
        CullingRig plain = new(SceneOrderMode.Source, withVision: false);
        CullingRig watched = new(SceneOrderMode.Source, withVision: true);

        Assert.Equal(plain.Group.GetVisibleLocalBounds(), watched.Group.GetVisibleLocalBounds());
        Assert.Equal(plain.Group.GetLocalBounds(), watched.Group.GetLocalBounds());
        Assert.Equal(plain.Root.GetVisibleLocalBounds(), watched.Root.GetVisibleLocalBounds());

        // One point on a visible child, one on an offscreen child and one where nothing is.
        foreach (DrawPoint point in new[] { new DrawPoint(15, 25), new DrawPoint(1005, 25), new DrawPoint(-3000, -3000) })
        {
            int[] expected = [.. plain.Group.GetInputCandidates(point, null).Select(node => plain.Group.Children.IndexOf(node))];
            IReadOnlyList<SceneNode2D> candidates = watched.Group.GetInputCandidates(point, null);

            Assert.DoesNotContain(candidates, node => node is Collider2D);
            Assert.Equal(expected, candidates.Select(node => watched.Group.Children.IndexOf(node)));
        }
    }

    [Fact]
    public void ChangesToAVisionAndToTheChildrenOfItsGroupKeepTheGroupCullingExact()
    {
        CullingRig plain = new(SceneOrderMode.Source, withVision: false);
        CullingRig watched = new(SceneOrderMode.Source, withVision: true);
        plain.Record();
        watched.Record();

        // Reshaping the Vision must not disturb the group's index, and a child that moves into view is still found.
        watched.Group.Vision!.Radius = 7;
        watched.Group.Vision.Direction = 200;
        watched.Group.Vision.IsTrigger = true;
        ((Sprite2D)watched.Group.Children[2]).X = 30;
        ((Sprite2D)plain.Group.Children[2]).X = 30;

        Assert.Equal(["far right", "near a", "near b"], plain.Record().OrderBy(name => name));
        Assert.Equal(["far right", "near a", "near b"], watched.Record().OrderBy(name => name));
        Assert.Equal(plain.Group.Children.Count, watched.Group.Children.Count);

        // Dropping the Vision and putting it back changes nothing either.
        ConeCollider2D cone = watched.Group.Vision;
        watched.Group.Vision = null;
        Assert.Equal(["far right", "near a", "near b"], watched.Record().OrderBy(name => name));
        watched.Group.Vision = cone;
        Assert.Equal(["far right", "near a", "near b"], watched.Record().OrderBy(name => name));
    }

    [Fact]
    public void TheSummaryExampleCompilesAndWorks()
    {
        Scene2D scene = new();
        Sprite2D bee = new() { Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = -12, OffsetY = -5 } };
        Sprite2D fox = Fox(400, -5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        HashSet<Collider2D> seen = [];

        bee.Vision = new ConeCollider2D
        {
            Radius = 150,      // sees 150 px
            Angle = 90,        // a 90 degree cone
            Direction = 0,     // looks to the right
        };
        bee.Vision.Entered += (_, e) => seen.Add(e.Other);
        bee.Vision.Exited += (_, e) => seen.Remove(e.Other);

        context.Update();
        CollisionHit2D[] around = scene.CollisionWorld.Overlap(bee.Vision);
        Assert.Empty(seen);
        Assert.Empty(around);

        fox.X = 60;
        context.Update();
        around = scene.CollisionWorld.Overlap(bee.Vision);
        Assert.Same(fox.Collider, Assert.Single(seen));
        Assert.Same(fox.Collider, Assert.Single(around).Collider);

        fox.X = 400;
        context.Update();
        Assert.Empty(seen);
        Assert.Empty(scene.CollisionWorld.Overlap(bee.Vision));
    }

    // A bee at (x, y): a body behind the apex and a cone that looks right. Its body never touches its cone.
    private static Sprite2D Bee(float x = 0, float y = 0) => new()
    {
        X = x,
        Y = y,
        Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = -12, OffsetY = -5 },
        Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 }
    };

    private static Sprite2D Fox(float x, float y) => new()
    {
        X = x,
        Y = y,
        Collider = new BoxCollider2D { Width = 10, Height = 10 }
    };

    private static IEnumerable<Collider2D> OverlapOf(Scene2D scene, Collider2D collider) =>
        scene.CollisionWorld.Overlap(collider).Select(hit => hit.Collider);

    // The same colliders, in any order, each once.
    private static void AssertSameColliders(IEnumerable<Collider2D> actual, params Collider2D[] expected)
    {
        Collider2D[] seen = [.. actual];
        Assert.Equal(expected.Length, seen.Length);
        foreach (Collider2D collider in expected)
        {
            Assert.Contains(collider, seen);
        }
    }

    private static SceneNode2D CreateOwner(string kind) => kind switch
    {
        "Sprite2D" => new Sprite2D(),
        "Scene2D" => new Scene2D(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // A bee at (50, 100) with a body (x 38..48, y 95..105) and a cone in front of it (x 50..200, y 25..175),
    // or a dragon group at (50, 100) whose only part is a head body (x 10..20, y 95..105) with the same cone.
    private static SceneNode2D CreateOwnerWithABodyAndAVision(string kind)
    {
        switch (kind)
        {
            case "Sprite2D":
                return Bee(50, 100);
            case "Scene2D":
                Scene2D dragon = new() { X = 50, Y = 100, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
                dragon.Children.Add(new Sprite2D { X = -40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } });
                return dragon;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void SetVision(SceneNode2D owner, ConeCollider2D? cone)
    {
        switch (owner)
        {
            case Sprite2D sprite: sprite.Vision = cone; break;
            case Scene2D scene: scene.Vision = cone; break;
            default: throw new ArgumentOutOfRangeException(nameof(owner));
        }
    }

    private static ConeCollider2D? GetVision(SceneNode2D owner) => owner switch
    {
        Sprite2D sprite => sprite.Vision,
        Scene2D scene => scene.Vision,
        _ => throw new ArgumentOutOfRangeException(nameof(owner))
    };

    private static UIRoot CreateRoot(out RenderSurface2D surface, out Scene2D scene)
    {
        UIRoot root = new(200, 200);
        surface = new RenderSurface2D { Width = 200, Height = 200 };
        surface.Measure(new MeasureContext(new LayoutSize(200, 200)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        scene = new Scene2D();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        return root;
    }

    private static UiHost CreateHost(UIRoot root)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(200, 200)
        });
        host.Update(
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }

    // A root scene holding a group of two sprites in a 100 x 100 view and two sprites far outside it
    // (plus a far group), with or without a large Vision on the group.
    private sealed class CullingRig
    {
        private readonly RenderSurface2D surface;

        public CullingRig(SceneOrderMode mode, bool withVision)
        {
            Group = new Scene2D { OrderMode = mode };
            Group.Children.Add(CullingSprite("near a", 10, 20));
            Group.Children.Add(CullingSprite("near b", 40, 20));
            Group.Children.Add(CullingSprite("far right", 1000, 20));
            Group.Children.Add(CullingSprite("far below", 20, 1000));
            if (withVision)
            {
                Group.Vision = new ConeCollider2D { Radius = 5000, Angle = 360 };
            }

            Scene2D farGroup = new();
            farGroup.Children.Add(CullingSprite("far group", 5000, 20));
            Root = new Scene2D { OrderMode = mode };
            Root.Children.Add(Group);
            Root.Children.Add(farGroup);
            surface = new RenderSurface2D { Scene = Root };
        }

        public Scene2D Group { get; }

        public Scene2D Root { get; }

        // The names of the images the root scene draws in the view, in drawing order.
        public IEnumerable<string> Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 100, 100));
            return [.. commands
                .Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => ((CullingImage)command.Image!).Name)];
        }

        private static Sprite2D CullingSprite(string name, float x, float y) => new()
        {
            Image = new ImageReference(new CullingImage(name)),
            X = x,
            Y = y,
            Width = 10,
            Height = 10
        };
    }

    private sealed class CullingImage(string name) : IDrawImage
    {
        public string Name { get; } = name;

        public int Width => 16;

        public int Height => 16;
    }

    // Records the collider that arrived or left and the sender of every event.
    private sealed class ContactLog
    {
        private readonly List<object?> senders = [];

        public ContactLog(Collider2D watched)
        {
            Watched = watched;
            watched.Entered += (sender, e) =>
            {
                senders.Add(sender);
                Entered.Add(e.Other);
            };
            watched.Exited += (sender, e) =>
            {
                senders.Add(sender);
                Exited.Add(e.Other);
            };
        }

        public Collider2D Watched { get; }

        public List<Collider2D> Entered { get; } = [];

        public List<Collider2D> Exited { get; } = [];

        public void AssertEverySenderIsTheWatchedCollider() => Assert.All(senders, sender => Assert.Same(Watched, sender));
    }
}
