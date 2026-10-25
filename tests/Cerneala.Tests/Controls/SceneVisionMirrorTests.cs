using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// Contract: the cone of a Vision looks and sits where the sprite's image is drawn
// (docs/plans/2026-10-25-scene2d-vision.md, "D18 revizuit").
// 1. A flip mirrors the apex across the centre line of the sprite's local image rectangle, and Direction as before.
//    The image flips only by swapping texture coordinates inside an unchanged destination rectangle
//    (Drawing/DrawImageOptions.cs), so that rectangle is the mirror.
// 2. The flip is the one the user sees: Sprite2D.Flip composed (xor) with the Flip of the current animation frame.
//    Whenever that flip or the mirror centre changes, the cone re-evaluates its contacts at the next update.
//    A sprite without a Vision pays nothing for it.
// A 32 x 32 image at Origin (0, 0) fills x 0..32, y 0..32 from the sprite anchor, so its centre lines are x 16 and y 16.
public sealed class SceneVisionMirrorTests
{
    // ----- 1. The apex mirrors with the image -----

    [Theory]
    [InlineData(RenderSurface2DSpriteFlip.None, 28f, 10f)]
    [InlineData(RenderSurface2DSpriteFlip.Horizontal, 4f, 10f)] // 2 * 16 - 28
    [InlineData(RenderSurface2DSpriteFlip.Vertical, 28f, 22f)] // 2 * 16 - 10
    [InlineData(RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical, 4f, 22f)]
    public void TheApexMirrorsAcrossTheCentreLinesOfTheImage(RenderSurface2DSpriteFlip flip, float apexX, float apexY)
    {
        Sprite2D sprite = ImageBee(32, 32, new DrawPoint(0, 0));
        sprite.Flip = flip;

        Assert.Equal(new Vector2(apexX, apexY), ApexOf(sprite));
    }

    [Fact]
    public void AnImageCentredOnTheAnchorKeepsTheApexAtTheAnchorAndMirrorsAnyOffsetAroundIt()
    {
        // Origin (16, 16) puts the 32 x 32 image at x -16..16, y -16..16: the centre lines pass through the anchor.
        Sprite2D atAnchor = ImageBee(32, 32, new DrawPoint(16, 16), offsetX: 0, offsetY: 0);
        atAnchor.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;
        Assert.Equal(Vector2.Zero, ApexOf(atAnchor));

        Sprite2D offset = ImageBee(32, 32, new DrawPoint(16, 16), offsetX: 6, offsetY: -4);
        offset.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;
        Assert.Equal(new Vector2(-6, 4), ApexOf(offset));
    }

    [Fact]
    public void AnImageDrawnAtADeclaredSizeMirrorsAcrossTheCentreOfThatSize()
    {
        // The image is 32 x 32 but drawn 64 x 64: the rectangle is x 0..64, so the centre line is x 32.
        Sprite2D sprite = ImageBee(32, 32, new DrawPoint(0, 0));
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.Flip = RenderSurface2DSpriteFlip.Horizontal;

        Assert.Equal(new Vector2(36, 10), ApexOf(sprite)); // 2 * 32 - 28
    }

    [Fact]
    public void ADeclaredSizeWithoutAnImageIsTheRectangle()
    {
        // No image, but Width 20 and Height 10 at Origin (0, 0) declare the rectangle x 0..20, y 0..10.
        Sprite2D sprite = new() { Width = 20, Height = 10, Vision = Cone(offsetX: 28, offsetY: 10) };
        sprite.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;

        Assert.Equal(new Vector2(-8, 0), ApexOf(sprite)); // 2 * 10 - 28, 2 * 5 - 10
    }

    [Fact]
    public void ATrimmedFrameMirrorsAcrossTheCentreOfItsLogicalFrameNotOfItsVisiblePixels()
    {
        // The visible pixels are 10 x 12 at (8, 4) inside a 32 x 32 logical frame; the sprite anchors on the frame.
        SpriteAnimationFrame trimmed = new(
            new DrawRect(40, 0, 10, 12), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None,
            new DrawPoint(8, 4), new DrawSize(32, 32));
        Sprite2D sprite = new()
        {
            Image = new ImageReference(new FixedImage(64, 64)),
            Animations = new SpriteAnimationSet([new SpriteAnimationClip("Pose", [trimmed])]),
            AnimationState = "Pose",
            Vision = Cone(offsetX: 28, offsetY: 10)
        };
        sprite.Flip = RenderSurface2DSpriteFlip.Horizontal;

        Assert.Equal(new Vector2(4, 10), ApexOf(sprite)); // the frame is x 0..32: 2 * 16 - 28
    }

    [Theory]
    [InlineData("NoImageNoSize")]
    [InlineData("OriginWithoutAKnownSourceSize")]
    public void WithoutAKnownRectangleTheAxisIsTheAnchorOfTheSprite(string kind)
    {
        // Origin (8, 8) needs the source size to place the rectangle, and there is no image: the rectangle is unknown.
        Sprite2D sprite = kind == "NoImageNoSize"
            ? new Sprite2D { Vision = Cone(offsetX: 28, offsetY: 10) }
            : new Sprite2D { Width = 32, Height = 32, Origin = new DrawPoint(8, 8), Vision = Cone(offsetX: 28, offsetY: 10) };
        sprite.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;

        Assert.Equal(new Vector2(-28, -10), ApexOf(sprite));
    }

    [Fact]
    public void AVisionOnAGroupDoesNotMirror()
    {
        Scene2D group = new() { Vision = Cone(offsetX: 28, offsetY: 10) };
        Scene2D root = new();
        root.Children.Add(group);

        Assert.True(group.Vision!.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        Assert.Equal(new Vector2(28, 10), geometry.ShapeToSceneTransform.Translation);
    }

    [Fact]
    public void AVerticalFlipSeesAboveTheMirroredApexAndNotAboveTheOriginalOne()
    {
        // Cone: Radius 50, Direction 90 (down), apex (10, 28). A fox box at x 5..15, y -40..-30 is above the sprite.
        // Flipped vertically the apex is (10, 4) and the cone looks up: the fox is 34..44 px away.
        // A mirrored direction with an unmoved apex would look up from (10, 28): 58..68 px away, out of reach.
        Scene2D scene = new();
        Sprite2D bee = ImageBee(32, 32, new DrawPoint(0, 0), offsetX: 10, offsetY: 28, direction: 90);
        Sprite2D fox = Fox(5, -40);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        List<Collider2D> entered = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);
        context.Update();
        Assert.Empty(entered);

        bee.Flip = RenderSurface2DSpriteFlip.Vertical;
        context.Update();

        Assert.Same(fox.Collider, Assert.Single(entered));
    }

    // ----- 2. The cone follows the flip the user sees -----

    [Fact]
    public void AFrameThatFlipsTheImageMakesTheConeLookLeftWhileTheSpriteFlipIsNone()
    {
        // Cone apex (28, 10), Direction 0. The only clip is one frame that flips horizontally; Sprite2D.Flip stays None.
        // The image is drawn mirrored, so the cone looks left from x 4. The fox box x -36..-26 is 30..40 px away.
        Scene2D scene = new();
        Sprite2D bee = FrameBee(SingleFrameClip("Left", RenderSurface2DSpriteFlip.Horizontal), "Left");
        Sprite2D fox = Fox(-36, 5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        using SceneSimulationContext2D context = new(scene);
        List<Collider2D> entered = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);

        context.Update();

        Assert.Equal(RenderSurface2DSpriteFlip.None, bee.Flip);
        Assert.Same(fox.Collider, Assert.Single(entered));
    }

    [Fact]
    public void TheSpriteFlipAndTheFrameFlipComposeLikeTheImage()
    {
        // Horizontal on the sprite and Horizontal on the frame cancel out: the image is drawn unmirrored,
        // so the cone looks right from x 28 and does not see the fox on the left.
        Scene2D scene = new();
        Sprite2D bee = FrameBee(SingleFrameClip("Mirror", RenderSurface2DSpriteFlip.Horizontal), "Mirror");
        bee.Flip = RenderSurface2DSpriteFlip.Horizontal;
        Sprite2D left = Fox(-36, 5);
        Sprite2D right = Fox(50, 5); // box x 50..60 is 22..32 px in front of the apex at x 28
        scene.Children.Add(bee);
        scene.Children.Add(left);
        scene.Children.Add(right);
        using SceneSimulationContext2D context = new(scene);
        List<Collider2D> entered = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);

        context.Update();

        Assert.Same(right.Collider, Assert.Single(entered));
    }

    [Fact]
    public void SwitchingTheAnimationStateToAnUnflippedClipMirrorsTheConeBackAtTheNextUpdate()
    {
        SpriteAnimationSet clips = new([
            new SpriteAnimationClip("Left", [Frame(0, RenderSurface2DSpriteFlip.Horizontal)]),
            new SpriteAnimationClip("Right", [Frame(0, RenderSurface2DSpriteFlip.None)])]);
        Scene2D scene = new();
        Sprite2D bee = FrameBee(clips, "Left");
        Sprite2D left = Fox(-36, 5);
        Sprite2D right = Fox(50, 5);
        scene.Children.Add(bee);
        scene.Children.Add(left);
        scene.Children.Add(right);
        using SceneSimulationContext2D context = new(scene);
        List<Collider2D> entered = [];
        List<Collider2D> exited = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);
        bee.Vision.Exited += (_, e) => exited.Add(e.Other);
        context.Update();
        Assert.Same(left.Collider, Assert.Single(entered));

        bee.AnimationState = "Right";
        Assert.Single(entered); // nothing is raised from inside the setter
        context.Update();

        Assert.Same(left.Collider, Assert.Single(exited));
        Assert.Equal([left.Collider, right.Collider], entered);
    }

    [Theory]
    [InlineData(1.0)] // a rate of one follows the surface clock (AdvanceScheduledAnimation)
    [InlineData(2.0)] // any other rate advances on every frame (AdvanceAnimation)
    public void AFrameAdvanceToAFrameWithAnotherFlipMirrorsTheConeInTheSameUpdate(double rate)
    {
        // Walk: frame 0 unflipped, frame 1 flipped horizontally, 100 ms each, looping.
        // The apex is at scene x 128 (unflipped) or 104 (flipped); the fox box x 64..74 is behind the first and
        // 30..40 px in front of the second.
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = HostedBee(rate, WalkFlippingOnTheSecondFrame());
        Sprite2D fox = Fox(64, 105);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        List<Collider2D> entered = [];
        List<Collider2D> exited = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);
        bee.Vision.Exited += (_, e) => exited.Add(e.Other);
        UiHost host = VisionHostRig.CreateHost(root);
        Assert.Empty(entered);
        TimeSpan step = TimeSpan.FromMilliseconds(100 / rate);

        VisionHostRig.Update(host, step);

        Assert.Same(fox.Collider, Assert.Single(entered));
        Assert.Empty(exited);

        VisionHostRig.Update(host, step); // back to frame 0

        Assert.Same(fox.Collider, Assert.Single(exited));
        Assert.Single(entered);
    }

    [Fact]
    public void AFrameAdvanceNotifiesTheVisionOnlyWhenTheComposedFlipChanges()
    {
        // A and B differ only in their source cell, C and D flip horizontally: A to B and C to D keep the flip.
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = HostedBee(1, new SpriteAnimationSet([new SpriteAnimationClip("Walk", [
            Frame(0, RenderSurface2DSpriteFlip.None),
            Frame(32, RenderSurface2DSpriteFlip.None),
            Frame(0, RenderSurface2DSpriteFlip.Horizontal),
            Frame(32, RenderSurface2DSpriteFlip.Horizontal)])]));
        scene.Children.Add(bee);
        UiHost host = VisionHostRig.CreateHost(root);
        ConeCollider2D cone = bee.Vision!;
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;
        TimeSpan step = TimeSpan.FromMilliseconds(100);

        int[] expected = [0, 1, 1, 2]; // A to B, B to C, C to D, D to A
        for (int index = 0; index < expected.Length; index++)
        {
            VisionHostRig.Update(host, step);
            Assert.Equal(expected[index], CountGeometryMutations(mutations, cone));
        }

        Assert.All(mutations, mutation => Assert.Same(cone, mutation.Node));
    }

    [Fact]
    public void AFrameAdvanceOfASpriteWithoutAVisionMutatesNothing()
    {
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = HostedBee(1, WalkFlippingOnTheSecondFrame());
        bee.Vision = null;
        bee.Collider = new BoxCollider2D { Width = 10, Height = 10 };
        scene.Children.Add(bee);
        UiHost host = VisionHostRig.CreateHost(root);
        long before = scene.CollisionMutationVersion;

        for (int index = 0; index < 4; index++)
        {
            VisionHostRig.Update(host, TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(before, scene.CollisionMutationVersion);
    }

    [Fact]
    public void RestartingTheAnimationOntoAFrameWithAnotherFlipNotifiesTheVision()
    {
        UIRoot root = VisionHostRig.CreateRoot(out _, out Scene2D scene);
        Sprite2D bee = HostedBee(1, WalkFlippingOnTheSecondFrame());
        scene.Children.Add(bee);
        UiHost host = VisionHostRig.CreateHost(root);
        VisionHostRig.Update(host, TimeSpan.FromMilliseconds(100)); // frame 1, flipped
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;

        bee.RestartAnimation(); // frame 0, unflipped

        Assert.Equal(1, CountGeometryMutations(mutations, bee.Vision!));
    }

    public static TheoryData<string> PropertiesThatMoveTheMirrorCentre() => new()
    {
        "Width", "Height", "Origin", "Image", "SourceX", "SourceY", "SourceWidth", "SourceHeight", "Flip"
    };

    [Theory]
    [MemberData(nameof(PropertiesThatMoveTheMirrorCentre))]
    public void ChangingAPropertyThatMovesTheMirrorCentreNotifiesTheVisionOnce(string property)
    {
        // Flipped on both axes, so both centre lines matter. The image is 32 x 32 at Origin (0, 0).
        Scene2D scene = new();
        Sprite2D bee = ImageBee(32, 32, new DrawPoint(0, 0));
        bee.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;
        scene.Children.Add(bee);
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;

        switch (property)
        {
            case "Width": bee.Width = 64; break;
            case "Height": bee.Height = 64; break;
            case "Origin": bee.Origin = new DrawPoint(8, 8); break;
            case "Image": bee.Image = new ImageReference(new FixedImage(64, 64)); break;
            case "SourceX": bee.SourceX = 8; break; // the source width becomes 24
            case "SourceY": bee.SourceY = 8; break;
            case "SourceWidth": bee.SourceWidth = 16; break;
            case "SourceHeight": bee.SourceHeight = 16; break;
            case "Flip": bee.Flip = RenderSurface2DSpriteFlip.Horizontal; break;
            default: throw new ArgumentOutOfRangeException(nameof(property));
        }

        Assert.Equal(1, CountGeometryMutations(mutations, bee.Vision!));
    }

    [Theory]
    [InlineData("Tint")]
    [InlineData("LayerDepth")]
    [InlineData("Opacity")]
    [InlineData("HeightWhenOnlyHorizontal")] // the horizontal mirror reads the vertical centre line never
    [InlineData("WidthWhenNotFlipped")]
    public void ChangingAPropertyThatLeavesTheMirrorUnchangedNotifiesNothing(string property)
    {
        Scene2D scene = new();
        Sprite2D bee = ImageBee(32, 32, new DrawPoint(0, 0));
        bee.Flip = property switch
        {
            "HeightWhenOnlyHorizontal" => RenderSurface2DSpriteFlip.Horizontal,
            "WidthWhenNotFlipped" => RenderSurface2DSpriteFlip.None,
            _ => RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical
        };
        scene.Children.Add(bee);
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;

        switch (property)
        {
            case "Tint": bee.Tint = Color.Red; break;
            case "LayerDepth": bee.LayerDepth = 0.5f; break;
            case "Opacity": bee.Opacity = 0.5f; break;
            case "HeightWhenOnlyHorizontal": bee.Height = 64; break;
            case "WidthWhenNotFlipped": bee.Width = 64; break;
            default: throw new ArgumentOutOfRangeException(nameof(property));
        }

        Assert.Equal(0, CountGeometryMutations(mutations, bee.Vision!));
    }

    [Fact]
    public void ASpriteWithoutAVisionMutatesNothingWhenItsImageOrFrameChanges()
    {
        Scene2D scene = new();
        Sprite2D bee = ImageBee(32, 32, new DrawPoint(0, 0));
        bee.Vision = null;
        bee.Collider = new BoxCollider2D { Width = 10, Height = 10 };
        bee.Flip = RenderSurface2DSpriteFlip.Horizontal;
        scene.Children.Add(bee);
        long before = scene.CollisionMutationVersion;

        bee.Width = 64;
        bee.Origin = new DrawPoint(8, 8);
        bee.Image = new ImageReference(new FixedImage(64, 64));
        bee.Animations = SingleFrameClip("Left", RenderSurface2DSpriteFlip.Horizontal);
        bee.AnimationState = "Left";

        Assert.Equal(before, scene.CollisionMutationVersion);
    }

    // A sprite that SceneItems2D culls is not visited for its frame changes (CanParkAnimation). The cone of a
    // Vision still lives in the collision world, so such a sprite is never parked.
    [Fact]
    public void ACulledSpriteWithAVisionKeepsFollowingItsFrameFlips()
    {
        UIRoot root = VisionHostRig.CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        Sprite2D bee = HostedBee(1, WalkFlippingOnTheSecondFrame());
        bee.X = 1000; // far outside the 200 x 200 view
        bee.Width = 32; // a declared size and Origin (0, 0): a frame change cannot change the bounds, so parking is allowed
        bee.Height = 32;
        scene.Children.Add(new SceneItems2D { ItemsSource = new[] { bee } });
        UiHost host = VisionHostRig.CreateHost(root);
        Record(surface);
        Assert.False(bee.IsAnimationParked);
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;

        VisionHostRig.Update(host, TimeSpan.FromMilliseconds(100)); // frame 1, flipped
        Record(surface);

        Assert.Equal(1, CountGeometryMutations(mutations, bee.Vision!));
    }

    [Fact]
    public void AssigningAVisionToAParkedSpriteUnparksIt()
    {
        UIRoot root = VisionHostRig.CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        Sprite2D bee = HostedBee(1, WalkFlippingOnTheSecondFrame());
        bee.Vision = null;
        bee.X = 1000;
        bee.Width = 32;
        bee.Height = 32;
        scene.Children.Add(new SceneItems2D { ItemsSource = new[] { bee } });
        UiHost host = VisionHostRig.CreateHost(root);
        Record(surface);
        Assert.True(bee.IsAnimationParked); // control: without a Vision the culled sprite parks

        bee.Vision = Cone(offsetX: 28, offsetY: 10);
        Record(surface);

        Assert.False(bee.IsAnimationParked);
        List<SceneCollisionMutation2D> mutations = [];
        scene.CollisionMutation += mutations.Add;
        VisionHostRig.Update(host, TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, CountGeometryMutations(mutations, bee.Vision));
    }

    [Fact]
    public void ReadingTheMirroredShapeAndOffsetOfAFlippedConeAllocatesNothingOnceWarm()
    {
        Scene2D scene = new();
        Sprite2D bee = ImageBee(32, 32, new DrawPoint(0, 0));
        bee.Flip = RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical;
        scene.Children.Add(bee);
        ConeCollider2D cone = bee.Vision!;
        for (int index = 0; index < 1000; index++)
        {
            _ = cone.GetShapeOffset();
            _ = cone.GetLocalShape();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            _ = cone.GetShapeOffset();
            _ = cone.GetLocalShape();
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    // Known limit. A resource image has no callback to the sprite when it finishes loading, and the place where the
    // sprite learns of it (presentation check) also runs while a frame is recorded, which must not change collision
    // state. So the cone keeps the mirror it had until the sprite next changes in the collision world.
    [Fact]
    public void AResourceImageThatFinishesLoadingMovesTheApexOnlyAtTheNextChangeOfTheSprite()
    {
        ResourceId<ImageResource> id = new("BeeAtlas");
        ResourceStore resources = new();
        UIRoot root = VisionHostRig.CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        root.SetResourceProvider(resources);
        root.SetImageLoader(new FixedImageLoader(32, 32));
        // Flipped horizontally with OffsetX 28. While the image is missing the apex mirrors across the anchor, to x -28,
        // and the cone looks left. The fox box x -2..8 is behind that apex. Once the 32 by 32 image is known the apex
        // is at x 4 and the fox, whose centre is at x 3, is in front of it.
        Sprite2D bee = new()
        {
            Image = new ImageReference(id),
            Flip = RenderSurface2DSpriteFlip.Horizontal,
            Vision = Cone(offsetX: 28, offsetY: 10)
        };
        Sprite2D fox = Fox(-2, 5);
        scene.Children.Add(bee);
        scene.Children.Add(fox);
        List<Collider2D> entered = [];
        bee.Vision!.Entered += (_, e) => entered.Add(e.Other);
        UiHost host = VisionHostRig.CreateHost(root);
        Assert.Empty(entered);
        long before = scene.CollisionMutationVersion;

        resources.SetResource(id, new ImageResource("bee.png"));
        VisionHostRig.Update(host, TimeSpan.FromMilliseconds(16));
        Record(surface);
        VisionHostRig.Update(host, TimeSpan.FromMilliseconds(16));

        Assert.True(bee.Vision.TryGetSceneGeometry(out ColliderGeometry2D loaded));
        Assert.Equal(new Vector2(4, 10), loaded.ShapeToSceneTransform.Translation); // read now, the cone is at x 4
        Assert.Equal(before, scene.CollisionMutationVersion); // but nothing told the collision world
        Assert.Empty(entered); // which still has the apex at x -28

        bee.X = 1; // any change of the sprite makes the world read the cone again
        VisionHostRig.Update(host, TimeSpan.Zero);

        Assert.Same(fox.Collider, Assert.Single(entered));
    }

    private sealed class FixedImageLoader(int width, int height) : IAsyncImageLoader
    {
        private readonly FixedImage image = new(width, height);

        public IDrawImage Load(string path) => image;

        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDrawImage>(image);
    }

    private static int CountGeometryMutations(IEnumerable<SceneCollisionMutation2D> mutations, ConeCollider2D cone) =>
        mutations.Count(mutation => ReferenceEquals(mutation.Node, cone) && mutation.Kind == SceneCollisionMutationKind.Geometry);

    private static void Record(RenderSurface2D surface) =>
        ((IRenderSurface2DFrameSource)surface).RecordFrame(new DrawCommandList(), new DrawRect(0, 0, 200, 200));

    // The scene position of the apex of the cone of a sprite that sits at the scene origin.
    private static Vector2 ApexOf(Sprite2D sprite)
    {
        Scene2D scene = new();
        scene.Children.Add(sprite);
        Assert.True(sprite.Vision!.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        return geometry.ShapeToSceneTransform.Translation;
    }

    private static ConeCollider2D Cone(float offsetX, float offsetY, float direction = 0) =>
        new() { Radius = 50, Angle = 90, Direction = direction, OffsetX = offsetX, OffsetY = offsetY };

    // A sprite at the scene origin that draws an image of the given size and has a cone.
    private static Sprite2D ImageBee(
        int imageWidth, int imageHeight, DrawPoint origin,
        float offsetX = 28, float offsetY = 10, float direction = 0) => new()
    {
        Image = new ImageReference(new FixedImage(imageWidth, imageHeight)),
        Origin = origin,
        Vision = Cone(offsetX, offsetY, direction)
    };

    // A sprite at the scene origin that draws a 32 x 32 frame of a 64 x 32 image and has the cone of the other tests.
    private static Sprite2D FrameBee(SpriteAnimationSet clips, string state) => new()
    {
        Image = new ImageReference(new FixedImage(64, 32)),
        Animations = clips,
        AnimationState = state,
        Vision = Cone(offsetX: 28, offsetY: 10)
    };

    private static Sprite2D HostedBee(double rate, SpriteAnimationSet clips)
    {
        Sprite2D bee = FrameBee(clips, "Walk");
        bee.X = 100;
        bee.Y = 100;
        bee.AnimationPlaybackRate = rate;
        return bee;
    }

    private static Sprite2D Fox(float x, float y) => new()
    {
        X = x,
        Y = y,
        Collider = new BoxCollider2D { Width = 10, Height = 10 }
    };

    private static SpriteAnimationFrame Frame(float x, RenderSurface2DSpriteFlip flip) =>
        new(new DrawRect(x, 0, 32, 32), TimeSpan.FromMilliseconds(100), flip);

    private static SpriteAnimationSet SingleFrameClip(string name, RenderSurface2DSpriteFlip flip) =>
        new([new SpriteAnimationClip(name, [Frame(0, flip)])]);

    private static SpriteAnimationSet WalkFlippingOnTheSecondFrame() => new([new SpriteAnimationClip("Walk", [
        Frame(0, RenderSurface2DSpriteFlip.None),
        Frame(32, RenderSurface2DSpriteFlip.Horizontal)])]);
}
