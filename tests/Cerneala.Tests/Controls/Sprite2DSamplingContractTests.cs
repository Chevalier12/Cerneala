using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class Sprite2DSamplingContractTests
{
    [Fact]
    public void SamplingDefaultsToLinearAndEachSpriteRecordsItsOwnSelection()
    {
        TestImage atlas = new();
        Sprite2D linear = new()
        {
            Image = new(atlas), X = 4, Y = 6,
            SourceX = 8, SourceY = 12, SourceWidth = 16, SourceHeight = 16
        };
        Sprite2D point = new()
        {
            Image = new(atlas), X = 28, Y = 6,
            SourceX = 8, SourceY = 12, SourceWidth = 16, SourceHeight = 16,
            Sampling = DrawSamplingMode.Point
        };
        RenderSurface2D surface = Surface(linear, point);

        Assert.Equal(DrawSamplingMode.Linear, linear.Sampling);
        Assert.Equal(DrawSamplingMode.Linear, Sprite2D.SamplingProperty.Metadata.DefaultValue);
        Assert.Equal(UiPropertyOptions.AffectsRender, Sprite2D.SamplingProperty.Metadata.Options);
        Assert.Equal(DrawSamplingMode.Point, point.Sampling);
        DrawCommand[] draws = Record(surface).Where(IsImage).OrderBy(command => command.Rect.X).ToArray();
        Assert.Equal(2, draws.Length);
        Assert.All(draws, draw =>
        {
            Assert.Same(atlas, draw.Image);
            Assert.Equal(new DrawRect(8, 12, 16, 16), draw.ImageSource);
            Assert.Equal(DrawAddressMode.Clamp, draw.ImageOptions!.AddressMode);
        });
        Assert.Equal(DrawSamplingMode.Linear, draws[0].ImageOptions!.Sampling);
        Assert.Equal(DrawSamplingMode.Point, draws[1].ImageOptions!.Sampling);
    }

    [Fact]
    public void SamplingChangeInvalidatesOnlyRenderingAndRejectsUnsupportedValues()
    {
        Sprite2D sprite = new() { Image = new(new TestImage()), X = 10, Y = 20 };
        RenderSurface2D surface = Surface(sprite);
        surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        IRenderSurface2DFrameSource frameSource = surface;
        SceneBounds2D bounds = sprite.GetHitTestLocalBounds();
        System.Numerics.Matrix3x2 transform = sprite.GetLocalTransform();
        long version = frameSource.FrameVersion;

        sprite.Sampling = DrawSamplingMode.Point;

        Assert.Equal(version + 1, frameSource.FrameVersion);
        Assert.Equal(DrawSamplingMode.Point, Assert.Single(Record(surface), IsImage).ImageOptions!.Sampling);
        Assert.Equal(bounds, sprite.GetHitTestLocalBounds());
        Assert.Equal(transform, sprite.GetLocalTransform());
        sprite.Sampling = DrawSamplingMode.Point;
        Assert.Equal(version + 1, frameSource.FrameVersion);
        sprite.Sampling = DrawSamplingMode.Linear;
        Assert.Equal(version + 2, frameSource.FrameVersion);
        Assert.Equal(DrawSamplingMode.Linear, Assert.Single(Record(surface), IsImage).ImageOptions!.Sampling);
        Assert.Throws<ArgumentException>(() => sprite.Sampling = (DrawSamplingMode)99);
        Assert.Equal(DrawSamplingMode.Linear, sprite.Sampling);
        Assert.Equal(version + 2, frameSource.FrameVersion);
    }

    [Fact]
    public void PointSamplingPreservesAnimatedCropComposedFlipAndOtherDrawOptions()
    {
        TestImage atlas = new();
        Sprite2D sprite = new()
        {
            Image = new(atlas), X = 24, Y = 32, Width = 32, Height = 48,
            SourceX = 48, SourceWidth = 8, SourceHeight = 8,
            Tint = Color.Red, Rotation = 0.25f, Origin = new DrawPoint(2, 3),
            Flip = RenderSurface2DSpriteFlip.Horizontal, LayerDepth = 0.4f,
            Sampling = DrawSamplingMode.Point,
            Animations = new([new SpriteAnimationClip("Walk", [
                new(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
                new(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.Horizontal)])]),
            AnimationState = "Walk"
        };
        RenderSurface2D surface = Surface(sprite);

        DrawCommand first = Assert.Single(Record(surface), IsImage);
        AssertDraw(first, new DrawRect(0, 0, 16, 16), DrawImageFlip.Horizontal);
        Assert.True(sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100)));
        DrawCommand second = Assert.Single(Record(surface), IsImage);
        AssertDraw(second, new DrawRect(16, 0, 16, 16), DrawImageFlip.None);
        Assert.Equal(48, sprite.SourceX);
        Assert.Equal(RenderSurface2DSpriteFlip.Horizontal, sprite.Flip);
        Assert.Equal(DrawSamplingMode.Point, sprite.Sampling);

        void AssertDraw(DrawCommand draw, DrawRect source, DrawImageFlip flip)
        {
            Assert.Same(atlas, draw.Image);
            Assert.Equal(new DrawRect(24, 32, 32, 48), draw.Rect);
            Assert.Equal(source, draw.ImageSource);
            Assert.Equal(Color.Red, draw.Color);
            Assert.Equal(0.25f, draw.ImageRotation);
            Assert.Equal(new DrawPoint(2, 3), draw.ImageOrigin);
            Assert.Equal(flip, draw.ImageFlip);
            Assert.Equal(0.4f, draw.LayerDepth);
            Assert.Equal(DrawSamplingMode.Point, draw.ImageOptions!.Sampling);
            Assert.Equal(DrawAddressMode.Clamp, draw.ImageOptions.AddressMode);
        }
    }

    [Theory]
    [InlineData(RenderSurface2DSpriteFlip.None, DrawImageFlip.None)]
    [InlineData(RenderSurface2DSpriteFlip.Horizontal, DrawImageFlip.Horizontal)]
    [InlineData(RenderSurface2DSpriteFlip.Vertical, DrawImageFlip.Vertical)]
    [InlineData(RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical,
        DrawImageFlip.Horizontal | DrawImageFlip.Vertical)]
    public void PointSamplingMapsEveryValidSpriteFlipToTheSameDrawFlip(
        RenderSurface2DSpriteFlip spriteFlip, DrawImageFlip expected)
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage()),
            Sampling = DrawSamplingMode.Point,
            Flip = spriteFlip
        };

        DrawCommand draw = Assert.Single(Record(Surface(sprite)), IsImage);

        Assert.Equal(expected, draw.ImageFlip);
        Assert.Equal(DrawSamplingMode.Point, draw.ImageOptions!.Sampling);
    }

    private static RenderSurface2D Surface(params Sprite2D[] sprites)
    {
        Scene2D scene = new();
        foreach (Sprite2D sprite in sprites) { scene.Children.Add(sprite); }
        return new() { Scene = scene };
    }

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
}
