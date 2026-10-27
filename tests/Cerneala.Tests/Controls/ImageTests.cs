using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ImageTests
{
    [Fact]
    public void ImageMeasuresIntrinsicSourceSize()
    {
        Image image = new()
        {
            Source = new FakeImage(32, 16),
            Stretch = DrawBrushStretch.None
        };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(32, 16), desired);
    }

    [Fact]
    public void ImageRendersDrawImageCommand()
    {
        UIRoot root = new();
        FakeImage source = new(32, 16);
        Image image = new() { Source = source };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        image.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawImage, commands[0].Kind);
        Assert.Same(source, commands[0].Image);
        Assert.Equal(new DrawRect(1, 4.5f, 30, 15), commands[0].Rect);
        Assert.Equal(Color.White, commands[0].Color);
    }

    [Fact]
    public void ImageRendersInsideBoundsWithoutDistortingAspectRatio()
    {
        UIRoot root = new();
        FakeImage source = new(512, 512);
        Image image = new() { Source = source };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        image.Arrange(new ArrangeContext(new LayoutRect(10, 20, 300, 100)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(new DrawRect(110, 20, 100, 100), commands[0].Rect);
    }

    [Fact]
    public void DefaultUniformMeasureScalesIntrinsicSizeDownToTheAvailableSize()
    {
        Image image = new() { Source = new FakeImage(200, 200) };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(new LayoutSize(100, 100), desired);
    }

    [Fact]
    public void StretchAndStretchDirectionHaveWpfDefaults()
    {
        Image image = new();

        Assert.Equal(DrawBrushStretch.Uniform, image.Stretch);
        Assert.Equal(StretchDirection.Both, image.StretchDirection);
    }

    [Fact]
    public void StretchChangeInvalidatesMeasureAndRender()
    {
        Image image = new() { Source = new FakeImage(32, 16) };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        image.Stretch = DrawBrushStretch.Fill;

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void StretchDirectionChangeInvalidatesMeasureAndRender()
    {
        Image image = new() { Source = new FakeImage(32, 16) };
        image.Measure(new MeasureContext(new LayoutSize(100, 100)));
        image.DirtyState.ClearAll();

        image.StretchDirection = StretchDirection.DownOnly;

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void UniformMeasureWithUnconstrainedHeightScalesByWidth()
    {
        Image image = new() { Source = new FakeImage(200, 200) };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, float.PositiveInfinity)));

        Assert.Equal(new LayoutSize(400, 400), desired);
    }

    [Fact]
    public void NoneMeasureKeepsIntrinsicSize()
    {
        Image image = new() { Source = new FakeImage(200, 200), Stretch = DrawBrushStretch.None };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(new LayoutSize(200, 200), desired);
    }

    [Fact]
    public void FillMeasureTakesTheAvailableSize()
    {
        Image image = new() { Source = new FakeImage(200, 200), Stretch = DrawBrushStretch.Fill };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(new LayoutSize(400, 100), desired);
    }

    [Fact]
    public void UniformToFillMeasureMayExceedTheAvailableSize()
    {
        Image image = new() { Source = new FakeImage(200, 200), Stretch = DrawBrushStretch.UniformToFill };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(new LayoutSize(400, 400), desired);
    }

    [Theory]
    [InlineData(DrawBrushStretch.None)]
    [InlineData(DrawBrushStretch.Fill)]
    [InlineData(DrawBrushStretch.Uniform)]
    [InlineData(DrawBrushStretch.UniformToFill)]
    public void MeasureIsZeroWithoutIntrinsicSizeForAnyStretch(DrawBrushStretch stretch)
    {
        Image image = new()
        {
            Source = new FakeImage(200, 200),
            Stretch = stretch,
            UseIntrinsicSize = false
        };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(LayoutSize.Zero, desired);
    }

    [Theory]
    [InlineData(DrawBrushStretch.None, 100, 0, 200, 200, true)]
    [InlineData(DrawBrushStretch.Fill, 0, 0, 400, 100, false)]
    [InlineData(DrawBrushStretch.Uniform, 150, 0, 100, 100, false)]
    [InlineData(DrawBrushStretch.UniformToFill, 0, 0, 400, 400, true)]
    public void StretchSizesTheDrawnImageInA400By100Slot(
        DrawBrushStretch stretch, float x, float y, float width, float height, bool clipped)
    {
        FakeImage source = new(200, 200);

        DrawCommandList commands = RenderImage(new Image { Source = source, Stretch = stretch }, new LayoutRect(0, 0, 400, 100));

        AssertDrawnImage(commands, source, new DrawRect(x, y, width, height), clipped, new DrawRect(0, 0, 400, 100));
    }

    [Theory]
    [InlineData(VerticalAlignment.Center, -150)]
    [InlineData(VerticalAlignment.Bottom, -300)]
    [InlineData(VerticalAlignment.Top, 0)]
    public void OverflowingContentIsPositionedByTheVerticalAlignmentOfTheImage(VerticalAlignment alignment, float expectedY)
    {
        FakeImage source = new(200, 200);
        // Explicit Width and Height make the arranged bounds exactly 400x100 whatever the alignment is.
        Image image = new()
        {
            Source = source,
            Stretch = DrawBrushStretch.UniformToFill,
            Width = 400,
            Height = 100,
            VerticalAlignment = alignment
        };

        DrawCommandList commands = RenderImage(image, new LayoutRect(0, 0, 400, 100));

        AssertDrawnImage(commands, source, new DrawRect(0, expectedY, 400, 400), clipped: true, new DrawRect(0, 0, 400, 100));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, 0)]
    [InlineData(HorizontalAlignment.Center, -150)]
    [InlineData(HorizontalAlignment.Right, -300)]
    public void OverflowingContentIsPositionedByTheHorizontalAlignmentOfTheImage(HorizontalAlignment alignment, float expectedX)
    {
        FakeImage source = new(200, 200);
        Image image = new()
        {
            Source = source,
            Stretch = DrawBrushStretch.UniformToFill,
            Width = 100,
            Height = 400,
            HorizontalAlignment = alignment
        };

        DrawCommandList commands = RenderImage(image, new LayoutRect(0, 0, 100, 400));

        AssertDrawnImage(commands, source, new DrawRect(expectedX, 0, 400, 400), clipped: true, new DrawRect(0, 0, 100, 400));
    }

    [Fact]
    public void DownOnlyDoesNotEnlargeASmallSource()
    {
        FakeImage source = new(50, 50);

        DrawCommandList commands = RenderImage(
            new Image { Source = source, StretchDirection = StretchDirection.DownOnly },
            new LayoutRect(0, 0, 400, 100));

        AssertDrawnImage(commands, source, new DrawRect(175, 25, 50, 50), clipped: false, new DrawRect(0, 0, 400, 100));
    }

    [Fact]
    public void UpOnlyDoesNotShrinkALargeSource()
    {
        FakeImage source = new(200, 200);

        DrawCommandList commands = RenderImage(
            new Image { Source = source, StretchDirection = StretchDirection.UpOnly },
            new LayoutRect(0, 0, 400, 100));

        AssertDrawnImage(commands, source, new DrawRect(100, 0, 200, 200), clipped: true, new DrawRect(0, 0, 400, 100));
    }

    [Fact]
    public void DownOnlyMeasureDoesNotEnlargeASmallSource()
    {
        Image image = new() { Source = new FakeImage(50, 50), StretchDirection = StretchDirection.DownOnly };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));

        Assert.Equal(new LayoutSize(50, 50), desired);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(30)]
    [InlineData(107)]
    public void UniformImageThatExactlyFitsItsSlotMeasuresAndDrawsExactlyThere(int slot)
    {
        LayoutSize expected = new(slot, slot);
        List<string> problems = [];
        Image rounded = new() { Source = new FakeImage(200, 200) };
        LayoutSize roundedSize = rounded.Measure(new MeasureContext(expected, LayoutRounding.Enabled));
        if (roundedSize != expected)
        {
            problems.Add($"measure with rounding {roundedSize.Width}x{roundedSize.Height}");
        }

        Image plain = new() { Source = new FakeImage(200, 200) };
        LayoutSize plainSize = plain.Measure(new MeasureContext(expected));
        if (plainSize != expected)
        {
            problems.Add($"measure without rounding {plainSize.Width:R}x{plainSize.Height:R}");
        }

        FakeImage source = new(200, 200);
        DrawCommandList commands = RenderImage(new Image { Source = source }, new LayoutRect(0, 0, slot, slot));
        bool exactDraw = commands.Count == 1 &&
            commands[0].Kind == DrawCommandKind.DrawImage &&
            commands[0].Rect == new DrawRect(0, 0, slot, slot);
        if (!exactDraw)
        {
            problems.Add("render: " + string.Join(", ", commands.Select(command => $"{command.Kind} {command.Rect}")));
        }

        Assert.True(problems.Count == 0, $"slot {slot}: {string.Join("; ", problems)}");
    }

    [Theory]
    [InlineData(DrawBrushStretch.Uniform, 200, 100, 60, 60, 60, 30)]
    [InlineData(DrawBrushStretch.Uniform, 100, 200, 60, 60, 30, 60)]
    [InlineData(DrawBrushStretch.Uniform, 200, 100, 60, float.PositiveInfinity, 60, 30)]
    [InlineData(DrawBrushStretch.Uniform, 100, 200, float.PositiveInfinity, 60, 30, 60)]
    [InlineData(DrawBrushStretch.Fill, 200, 200, 60, 30, 60, 30)]
    [InlineData(DrawBrushStretch.UniformToFill, 200, 200, 60, 30, 60, 60)]
    [InlineData(DrawBrushStretch.UniformToFill, 200, 100, 30, 60, 120, 60)]
    public void StretchedMeasureIsExactWhenTheScaledSizeIsAWholeNumber(
        DrawBrushStretch stretch, int sourceWidth, int sourceHeight,
        float availableWidth, float availableHeight, float expectedWidth, float expectedHeight)
    {
        LayoutSize available = new(availableWidth, availableHeight);
        LayoutSize expected = new(expectedWidth, expectedHeight);
        Image plain = new() { Source = new FakeImage(sourceWidth, sourceHeight), Stretch = stretch };
        Image rounded = new() { Source = new FakeImage(sourceWidth, sourceHeight), Stretch = stretch };

        LayoutSize plainSize = plain.Measure(new MeasureContext(available));
        LayoutSize roundedSize = rounded.Measure(new MeasureContext(available, LayoutRounding.Enabled));

        Assert.Equal((expected, expected), (plainSize, roundedSize));
    }

    private static DrawCommandList RenderImage(Image image, LayoutRect slot)
    {
        UIRoot root = new();
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        image.Arrange(new ArrangeContext(slot));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        return root.RetainedRenderer.Commit(root);
    }

    private static void AssertDrawnImage(
        DrawCommandList commands, FakeImage source, DrawRect expectedRect, bool clipped, DrawRect clipRect)
    {
        if (!clipped)
        {
            DrawCommand only = Assert.Single(commands);
            Assert.Equal(DrawCommandKind.DrawImage, only.Kind);
            Assert.Same(source, only.Image);
            Assert.Equal(expectedRect, only.Rect);
            return;
        }

        Assert.Equal(3, commands.Count);
        Assert.Equal(DrawCommandKind.PushClip, commands[0].Kind);
        Assert.Equal(clipRect, commands[0].Rect);
        Assert.Equal(DrawCommandKind.DrawImage, commands[1].Kind);
        Assert.Same(source, commands[1].Image);
        Assert.Equal(expectedRect, commands[1].Rect);
        Assert.Equal(DrawCommandKind.PopClip, commands[2].Kind);
    }

    [Fact]
    public void SourceChangeInvalidatesMeasureAndRender()
    {
        Image image = new();

        image.Source = new FakeImage(32, 16);

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void EqualSourceReplacementInvalidatesIntrinsicMeasurement()
    {
        Image image = new()
        {
            Source = new EqualImage(32, 16),
            Stretch = DrawBrushStretch.None
        };
        MeasureContext context = new(new LayoutSize(100, 100));
        image.Measure(context);

        image.Source = new EqualImage(64, 8);

        LayoutSize desired = image.Measure(context);
        Assert.Equal(new LayoutSize(64, 8), desired);
    }

    [Fact]
    public void ChangingUseIntrinsicSizeInvalidatesMeasurementAndRender()
    {
        Image image = new()
        {
            Source = new FakeImage(32, 16)
        };
        MeasureContext context = new(new LayoutSize(100, 100));
        image.Measure(context);
        image.DirtyState.ClearAll();

        image.UseIntrinsicSize = false;

        Assert.True(image.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(image.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(LayoutSize.Zero, image.Measure(context));
    }

    private sealed class FakeImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class EqualImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public override bool Equals(object? obj)
        {
            return obj is EqualImage;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
