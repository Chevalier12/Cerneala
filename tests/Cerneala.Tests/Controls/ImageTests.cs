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
            Source = new FakeImage(32, 16)
        };

        LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(32, 16), desired);
    }

    [Fact]
    public void ImageRendersDrawImageCommand()
    {
        UIRoot root = new();
        FakeImage source = new(32, 16);
        Image image = new()
        {
            Source = source,
            Foreground = DrawColor.White
        };
        root.VisualChildren.Add(image);
        image.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 20)));

        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawImage, commands[0].Kind);
        Assert.Same(source, commands[0].Image);
        Assert.Equal(new DrawRect(1, 2, 30, 20), commands[0].Rect);
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
            Source = new EqualImage(32, 16)
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
