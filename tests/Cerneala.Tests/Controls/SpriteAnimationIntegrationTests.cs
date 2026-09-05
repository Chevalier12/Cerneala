using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SpriteAnimationIntegrationTests
{
    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void StateChangeInvalidatesSurfaceExactlyOnce()
    {
        Sprite2D sprite = Animated(Set(Clip("Idle", true, Frame(0, 100)), Clip("Walk", true, Frame(16, 100))), "Idle");
        RenderSurface2D surface = Surface(sprite);
        IRenderSurface2DFrameSource source = surface;
        long before = source.FrameVersion;

        sprite.AnimationState = "Walk";

        Assert.Equal(before + 1, source.FrameVersion);
        sprite.AnimationState = "Walk";
        Assert.Equal(before + 1, source.FrameVersion);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void AspectSelectsClipWhileMotionAndPrismPreserveFrameOwnership()
    {
        Sprite2D sprite = new()
        {
            Source = new TestImage(),
            Destination = new DrawRect(0, 0, 16, 16),
            Animations = Set(Clip("Walk", true, Frame(0, 100), Frame(16, 100))),
            Aspect = new ElementAspect([
                new ElementAspectValue(Sprite2D.AnimationStateProperty, "Walk"),
                new ElementAspectValue(Sprite2D.TintProperty, Color.Red)])
        };
        RenderSurface2D surface = Surface(sprite);
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        using IDisposable prism = GeneratedMarkup.AttachPrism(sprite,
            () => new PrismInstance(PrismTestData.Composition("Animated", PrismTestData.Layer(1, "Content"))));
        using MotionHandle motion = sprite.Motion().Animate(UIElement.OpacityProperty).To(0.25f)
            .With(Cerneala.UI.Motion.Specs.Motion.Tween<float>(TimeSpan.FromMilliseconds(200)));
        root.ProcessFrame(); // Establish Motion's initial sample before advancing its clock.
        Assert.Equal("Walk", sprite.AnimationState);
        sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        DrawCommandList commands = Record(surface);
        Assert.Equal([DrawCommandKind.BeginPrism, DrawCommandKind.DrawImage, DrawCommandKind.EndPrism],
            commands.Select(command => command.Kind));
        Assert.Equal(new DrawRect(16, 0, 16, 16), commands[1].ImageSource);
        Assert.Equal(Color.Red, sprite.Tint);
        Assert.InRange(sprite.Opacity, 0.25f, 0.99f);
        motion.Cancel(MotionCancelBehavior.Revert);
        root.ProcessFrame(); // Commit the reverted Motion sample through the property store.
        Assert.Equal(1f, sprite.Opacity);
        Assert.Equal(new DrawRect(16, 0, 16, 16), Record(surface).Single(IsImage).ImageSource);
        AnimatablePropertyRegistry registry = new();
        Assert.True(registry.TryGet(Sprite2D.AnimationPlaybackRateProperty, out _));
        Assert.True(registry.TryGet(TileInstance2D.AnimationPlaybackRateProperty, out _));
        Assert.False(registry.TryGet(Sprite2D.AnimationsProperty, out _));
        Assert.False(registry.TryGet(Sprite2D.AnimationStateProperty, out _));
        Assert.False(registry.TryGet(Sprite2D.SourceRectProperty, out _));
        Assert.False(registry.TryGet(Sprite2D.FlipProperty, out _));
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void SpriteUsesCurrentFrameAndXorFlipWithoutMutatingStaticFallback()
    {
        SpriteAnimationSet animations = Set(Clip("Walk", true, Frame(0, 100), Frame(16, 100, RenderSurface2DSpriteFlip.Horizontal)));
        Sprite2D sprite = new()
        {
            Source = new TestImage(),
            SourceRect = new DrawRect(48, 0, 16, 16),
            Destination = new DrawRect(4, 5, 16, 16),
            Flip = RenderSurface2DSpriteFlip.Horizontal,
            Animations = animations,
            AnimationState = "Walk"
        };

        Assert.True(sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100)));
        DrawCommand draw = Assert.Single(Record(Surface(sprite)).Where(IsImage));

        Assert.Equal(new DrawRect(16, 0, 16, 16), draw.ImageSource);
        Assert.Equal(DrawImageFlip.None, draw.ImageFlip);
        Assert.Equal(new DrawRect(48, 0, 16, 16), sprite.SourceRect);
        Assert.Equal(RenderSurface2DSpriteFlip.Horizontal, sprite.Flip);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void SharedDefinitionsKeepIndependentProgressAndRestartResumePolicy()
    {
        SpriteAnimationSet animations = Set(
            Clip("Idle", true, Frame(0, 100), Frame(16, 100)),
            Clip("Walk", true, Frame(0, 100, y: 16), Frame(16, 100, y: 16)));
        Sprite2D first = Animated(animations, "Idle");
        Sprite2D second = Animated(animations, "Idle");
        second.IsAnimationPaused = true;

        Assert.True(first.AdvanceAnimation(TimeSpan.FromMilliseconds(100)));
        Assert.False(second.AdvanceAnimation(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(new DrawRect(16, 0, 16, 16), Draw(first).ImageSource);
        Assert.Equal(new DrawRect(0, 0, 16, 16), Draw(second).ImageSource);
        Assert.Same(first.Animations, second.Animations);

        first.AnimationStateChangeMode = SpriteAnimationStateChangeMode.Resume;
        first.AnimationState = "Walk";
        first.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
        first.AnimationState = "Idle";
        Assert.Equal(new DrawRect(16, 0, 16, 16), Draw(first).ImageSource);

        first.AnimationStateChangeMode = SpriteAnimationStateChangeMode.Restart;
        first.AnimationState = "Walk";
        Assert.Equal(new DrawRect(0, 16, 16, 16), Draw(first).ImageSource);
        first.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
        first.RestartAnimation();
        Assert.Equal(new DrawRect(0, 16, 16, 16), Draw(first).ImageSource);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void AtlasAndDataContextChangesPreserveProgressButDefinitionReplacementResets()
    {
        SpriteAnimationSet original = Set(Clip("Walk", true, Frame(0, 100), Frame(16, 100)));
        Sprite2D sprite = Animated(original, "Walk");
        sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100));

        sprite.Source = new TestImage();
        sprite.DataContext = new object();
        Assert.Equal(new DrawRect(16, 0, 16, 16), Draw(sprite).ImageSource);

        sprite.Animations = Set(Clip("Walk", true, Frame(32, 100), Frame(48, 100)));
        Assert.Equal(new DrawRect(32, 0, 16, 16), Draw(sprite).ImageSource);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void PromotedAnimatedPrismTileKeepsOrdinaryTilesBatched()
    {
        ResourceId<ImageResource> atlasId = new("Atlas");
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [new TileSet2D("World", atlasId, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                [new TileLayer2DModel("Ground", [new TileChunk2D(new TileCoordinate2D(0, 0), 3, 1, [new TileCell2D(1), new TileCell2D(1), new TileCell2D(1)])])],
                new TileMapBounds2D(0, 0, 3, 1))
        };
        TileInstance2D promoted = map.Promote(new TileCellKey2D("Ground", 1, 0));
        promoted.Animations = Set(Clip("Walk", true, Frame(0, 100), Frame(16, 100, RenderSurface2DSpriteFlip.Horizontal)));
        promoted.AnimationState = "Walk";
        promoted.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
        RenderSurface2D surface = Surface(map);
        surface.Resources.SetResource(atlasId, new ImageResource("atlas.png"));
        UIRoot root = new();
        root.SetImageLoader(new TestImageLoader());
        root.VisualChildren.Add(surface);
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            promoted,
            () => new PrismInstance(PrismTestData.Composition("Tile", PrismTestData.Layer(1, "Content"))));

        DrawCommandList commands = Record(surface);
        DrawCommand promotedDraw = Assert.Single(commands.Where(IsImage));
        Assert.Equal(new DrawRect(16, 0, 16, 16), promotedDraw.ImageSource);
        Assert.Equal(DrawImageFlip.Horizontal, promotedDraw.ImageFlip);
        Assert.Contains(commands, static command => command.Kind == DrawCommandKind.DrawSpriteBatch);
        Assert.Equal(
            [DrawCommandKind.DrawSpriteBatch, DrawCommandKind.BeginPrism, DrawCommandKind.DrawImage, DrawCommandKind.EndPrism, DrawCommandKind.DrawSpriteBatch],
            commands.Where(static command => command.Kind is DrawCommandKind.DrawSpriteBatch or DrawCommandKind.BeginPrism or DrawCommandKind.DrawImage or DrawCommandKind.EndPrism)
                .Select(static command => command.Kind));
        Assert.Equal(1, map.GetDiagnosticsSnapshot().PromotedInstancesVisible);
        Assert.True(promoted.AdvanceAnimation(TimeSpan.FromMilliseconds(100)));
        Record(surface);
        Assert.Equal(0, map.GetDiagnosticsSnapshot().BatchesRebuilt);
        Assert.True(map.GetDiagnosticsSnapshot().BatchesReused > 0);
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void CurrentFrameChangesOnlyVisualSourceAndNotSpriteBoundsOrPickingBounds()
    {
        Sprite2D sprite = Animated(
            Set(Clip("Walk", true, Frame(0, 100), new SpriteAnimationFrame(new DrawRect(32, 32, 8, 24), TimeSpan.FromMilliseconds(100)))),
            "Walk");
        sprite.Destination = new DrawRect(10, 20, 30, 40);
        SceneBounds2D before = sprite.GetHitTestLocalBounds();

        sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100));

        Assert.Equal(before, sprite.GetHitTestLocalBounds());
        Assert.Equal(new DrawRect(10, 20, 30, 40), sprite.GetHitTestLocalBounds().Bounds);
        Assert.Equal(new DrawRect(32, 32, 8, 24), Draw(sprite).ImageSource);
    }

    private static Sprite2D Animated(SpriteAnimationSet animations, string state) => new()
    {
        Source = new TestImage(),
        Destination = new DrawRect(0, 0, 16, 16),
        Animations = animations,
        AnimationState = state
    };

    private static SpriteAnimationSet Set(params SpriteAnimationClip[] clips) => new(clips);

    private static SpriteAnimationClip Clip(string name, bool loop, params SpriteAnimationFrame[] frames) => new(name, frames, loop);

    private static SpriteAnimationFrame Frame(
        float x,
        int milliseconds,
        RenderSurface2DSpriteFlip flip = RenderSurface2DSpriteFlip.None,
        float y = 0) =>
        new(new DrawRect(x, y, 16, 16), TimeSpan.FromMilliseconds(milliseconds), flip);

    private static RenderSurface2D Surface(SceneNode2D node)
    {
        Scene2D scene = new();
        scene.Children.Add(node);
        return new RenderSurface2D { Scene = scene };
    }

    private static DrawCommand Draw(Sprite2D sprite) =>
        Assert.Single(Record(sprite.Surface ?? Surface(sprite)).Where(IsImage));

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 128, 128));
        return commands;
    }

    private static bool IsImage(DrawCommand command) => command.Kind == DrawCommandKind.DrawImage;

    private sealed class TestImage : IDrawImage
    {
        public int Width => 64;

        public int Height => 64;
    }

    private sealed class TestImageLoader : IImageLoader
    {
        private readonly TestImage image = new();

        public IDrawImage Load(string path) => image;
    }
}
