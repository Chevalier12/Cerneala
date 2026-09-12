using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class Sprite2DAuthoringTests
{
    [Theory]
    [InlineData(0, 64, 32)]
    [InlineData(1, 20, 10)]
    [InlineData(2, 16, 12)]
    public void BoundsQueriesDoNotAllocateSourceOnlyDrawingOptions(int sourceMode, int width, int height)
    {
        Sprite2D sprite = new() { Image = new(new TestImage(64, 32)), X = 10, Y = 20 };
        if (sourceMode == 1)
        {
            sprite.SourceX = 4;
            sprite.SourceY = 2;
            sprite.SourceWidth = width;
            sprite.SourceHeight = height;
        }
        else if (sourceMode == 2)
        {
            sprite.Animations = new([new SpriteAnimationClip("Walk", [
                new(new DrawRect(0, 0, width, height), TimeSpan.FromMilliseconds(100)),
                new(new DrawRect(16, 0, 24, 20), TimeSpan.FromMilliseconds(100))])]);
            sprite.AnimationState = "Walk";
        }
        DrawRect expected = new(10, 20, width, height);
        for (int index = 0; index < 128; index++)
        {
            Assert.Equal(expected, SceneGeometry2D.TransformBounds(sprite.GetVisibleLocalBounds(), sprite.GetLocalTransform()).Bounds);
            Assert.Equal(expected, SceneGeometry2D.TransformBounds(sprite.GetHitTestLocalBounds(), sprite.GetLocalTransform()).Bounds);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 256; index++)
        {
            sprite.GetVisibleLocalBounds();
            sprite.GetHitTestLocalBounds();
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocatedBytes == 0,
            $"Visible/hit-test bounds allocated {allocatedBytes / 512} bytes/query after warmup.");
        Assert.Equal(expected, SceneGeometry2D.TransformBounds(sprite.GetVisibleLocalBounds(), sprite.GetLocalTransform()).Bounds);
        if (sourceMode == 2)
        {
            sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
            Assert.Equal(new DrawRect(10, 20, 24, 20), SceneGeometry2D.TransformBounds(sprite.GetHitTestLocalBounds(), sprite.GetLocalTransform()).Bounds);
        }
    }

    [Theory]
    [InlineData(64, float.NaN, "source")]
    [InlineData(65, float.NaN, "width")]
    [InlineData(63, 2, "options")]
    public void SourceBoundsValidationPreservesItsDiagnosticParameter(float x, float width, string parameter)
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage(64, 32)), SourceX = x, SourceWidth = width
        };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => sprite.GetHitTestLocalBounds());

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void ImageReferenceHasOneTypedSourceAndIdentityEqualityWithoutTakingOwnership()
    {
        TestImage image = new(64, 32);
        ImageReference direct = new(image);
        ImageReference resource = new(new ResourceId<ImageResource>("Atlas"));
        Assert.Same(image, direct.DirectImage);
        Assert.Null(direct.ResourceId);
        Assert.Null(resource.DirectImage);
        Assert.Equal(new ResourceId<ImageResource>("Atlas"), resource.ResourceId);
        Assert.Equal(direct, new ImageReference(image));
        Assert.Equal(direct.GetHashCode(), new ImageReference(image).GetHashCode());
        Assert.NotEqual(direct, new ImageReference(new TestImage(64, 32)));
        Assert.Equal(resource, new ImageReference(new ResourceId<ImageResource>("Atlas")));
        Assert.NotEqual(direct, resource);
        Assert.Throws<ArgumentNullException>(() => new ImageReference((IDrawImage)null!));
        Assert.Throws<ArgumentNullException>(() => new ImageReference(default(ResourceId<ImageResource>)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectAndResourceImagesUseNaturalRemainingCropForDrawingAndBounds(bool resource)
    {
        TestImage image = new(64, 32);
        Sprite2D sprite = new()
        {
            Image = resource ? new(new ResourceId<ImageResource>("Atlas")) : new(image),
            X = 100, Y = 80, SourceX = 12, SourceY = 7
        };
        RenderSurface2D surface = Surface(sprite);
        surface.Resources.SetResource(new ResourceId<ImageResource>("Atlas"), new ImageResource("atlas.png"));
        UIRoot root = new();
        root.SetImageLoader(new Loader(image));
        root.VisualChildren.Add(surface);

        DrawCommand draw = Draw(surface);
        Assert.Same(image, draw.Image);
        Assert.Equal(new DrawRect(12, 7, 52, 25), draw.ImageSource);
        Assert.Equal(new DrawRect(100, 80, 52, 25), draw.Rect);
        Assert.Equal(draw.Rect, SceneGeometry2D.TransformBounds(sprite.GetHitTestLocalBounds(), sprite.GetLocalTransform()).Bounds);
        Assert.True(float.IsNaN(sprite.Width));
        Assert.True(float.IsNaN(sprite.SourceWidth));
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    public void FullImageAndIndependentExplicitSizesCanReturnToAutomaticDimensions()
    {
        Sprite2D sprite = new() { Image = new(new TestImage(64, 32)) };
        RenderSurface2D surface = Surface(sprite);
        Assert.Equal(new DrawRect(0, 0, 64, 32), Draw(surface).Rect);
        sprite.Width = 100;
        sprite.SourceWidth = 20;
        sprite.SourceY = 2;
        Assert.Equal(new DrawRect(0, 0, 100, 30), Draw(surface).Rect);
        sprite.Width = float.NaN;
        Assert.Equal(new DrawRect(0, 0, 20, 30), Draw(surface).Rect);
        sprite.SourceWidth = float.NaN;
        Assert.Equal(new DrawRect(0, 0, 64, 30), Draw(surface).Rect);
        sprite.Height = 10;
        Assert.Equal(new DrawRect(0, 0, 64, 10), Draw(surface).Rect);
    }

    [Fact]
    public void ExplicitCropAndSizePreserveSourcePixelOriginAnchor()
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage(64, 32)), X = 100, Y = 80,
            Width = 40, Height = 30, SourceX = 10, SourceY = 4,
            SourceWidth = 20, SourceHeight = 10, Origin = new DrawPoint(5, 2)
        };
        DrawCommand draw = Draw(Surface(sprite));
        Assert.Equal(new DrawRect(100, 80, 40, 30), draw.Rect);
        Assert.Equal(new DrawRect(10, 4, 20, 10), draw.ImageSource);
        Assert.Equal(new DrawRect(90, 74, 40, 30), SceneGeometry2D.TransformBounds(sprite.GetHitTestLocalBounds(), sprite.GetLocalTransform()).Bounds);
    }

    [Fact]
    public void AnimationOwnsEffectiveCropAndAutomaticSizeWithoutChangingStaticValues()
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage(64, 32)), SourceX = 48, SourceWidth = 8, SourceHeight = 8,
            Animations = new([new SpriteAnimationClip("Walk", [
                new(new DrawRect(0, 0, 16, 12), TimeSpan.FromMilliseconds(100)),
                new(new DrawRect(16, 0, 24, 20), TimeSpan.FromMilliseconds(100))])]),
            AnimationState = "Walk"
        };
        RenderSurface2D surface = Surface(sprite);
        Assert.Equal(new DrawRect(0, 0, 16, 12), Draw(surface).Rect);
        sprite.AdvanceAnimation(TimeSpan.FromMilliseconds(100));
        Assert.Equal(new DrawRect(0, 0, 24, 20), Draw(surface).Rect);
        Assert.Equal(new DrawRect(0, 0, 24, 20), sprite.GetHitTestLocalBounds().Bounds);
        Assert.Equal(48, sprite.SourceX);
        Assert.Equal(8, sprite.SourceWidth);
        sprite.AnimationState = null;
        Assert.Equal(new DrawRect(0, 0, 8, 8), Draw(surface).Rect);
    }

    [Fact]
    public void EveryAuthoringPropertyInvalidatesTheOnDemandSurface()
    {
        Sprite2D sprite = new() { Image = new(new TestImage(64, 32)) };
        RenderSurface2D surface = Surface(sprite);
        surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        Action[] changes = [() => sprite.X = 1, () => sprite.Y = 2,
            () => sprite.Width = 30, () => sprite.Height = 20,
            () => sprite.SourceX = 3, () => sprite.SourceY = 4,
            () => sprite.SourceWidth = 16, () => sprite.SourceHeight = 12,
            () => sprite.Image = new(new TestImage(64, 32)), () => sprite.Image = null];
        foreach (Action change in changes)
        {
            long version = ((IRenderSurface2DFrameSource)surface).FrameVersion;
            change();
            Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > version);
        }
        Assert.DoesNotContain(Record(surface), c => c.Kind == DrawCommandKind.DrawImage);
    }

    [Fact]
    public void InvalidCoordinatesAndCropExtentsAreRejectedRatherThanClamped()
    {
        Sprite2D sprite = new() { Image = new(new TestImage(64, 32)) };
        Assert.ThrowsAny<ArgumentException>(() => sprite.X = float.NaN);
        Assert.ThrowsAny<ArgumentException>(() => sprite.Y = float.PositiveInfinity);
        Assert.ThrowsAny<ArgumentException>(() => sprite.SourceX = -1);
        Assert.ThrowsAny<ArgumentException>(() => sprite.SourceY = float.NaN);
        Assert.ThrowsAny<ArgumentException>(() => sprite.SourceWidth = 0);
        Assert.ThrowsAny<ArgumentException>(() => sprite.SourceHeight = float.PositiveInfinity);
        Assert.ThrowsAny<ArgumentException>(() => sprite.Width = -1);
        sprite.SourceX = 63;
        sprite.SourceWidth = 2;
        Assert.ThrowsAny<ArgumentException>(() => Record(Surface(sprite)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NaturalImageBoundsArePickedThroughRealServoInput(bool resource)
    {
        TestImage image = new(64, 32);
        Sprite2D sprite = new()
        {
            Image = resource ? new(new ResourceId<ImageResource>("Atlas")) : new(image),
            X = 10, Y = 20, SourceX = 48, SourceY = 20, Focusable = true
        };
        RenderSurface2D surface = Surface(sprite);
        surface.Width = 200; surface.Height = 200;
        surface.Resources.SetResource(new ResourceId<ImageResource>("Atlas"), new ImageResource("atlas.png"));
        UIRoot root = new(200, 200);
        root.SetImageLoader(new Loader(image));
        root.VisualChildren.Add(surface);
        ServoApi.SetId(sprite, "natural-sprite");
        int clicks = 0;
        sprite.MouseDown += (_, _) => clicks++;
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("natural-sprite");
        var found = await servo.FindAsync(target);
        Assert.Equal(16, found.Bounds.Width);
        Assert.Equal(12, found.Bounds.Height);
        await servo.ClickAsync(target);
        Assert.Equal(1, clicks);
        Assert.True(sprite.IsKeyboardFocused);
        root.VisualChildren.Remove(surface);
    }

    private static RenderSurface2D Surface(Sprite2D sprite)
    {
        Scene2D scene = new();
        scene.Children.Add(sprite);
        return new() { Scene = scene };
    }

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 200, 200));
        return commands;
    }

    private static DrawCommand Draw(RenderSurface2D surface) =>
        Assert.Single(Record(surface).Where(c => c.Kind == DrawCommandKind.DrawImage));

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width => width;
        public int Height => height;
    }

    private sealed class Loader(IDrawImage image) : IImageLoader
    {
        public IDrawImage Load(string path) => image;
    }
}
