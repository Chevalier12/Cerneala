using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Media;

public sealed class ImageSourceTests
{
    [Fact]
    public void BitmapImageExposesIdentityAndIntrinsicSize()
    {
        FakeImage image = new();
        BitmapImage source = new("asset://logo", new LayoutSize(32, 16), image);

        Assert.Equal("asset://logo", source.Identity);
        Assert.Equal(new LayoutSize(32, 16), source.IntrinsicSize);
        Assert.Same(image, source.ResolveDrawImage());
    }

    [Fact]
    public void RenderTargetImageRequiresDrawImage()
    {
        FakeImage image = new();
        RenderTargetImage source = new("render-target://main", new LayoutSize(64, 32), image);

        Assert.Same(image, source.ResolveDrawImage());
    }

    [Fact]
    public void BitmapImageEqualityUsesDrawImageReferenceIdentity()
    {
        BitmapImage first = new("asset://logo", new LayoutSize(32, 16), new EqualImage(32, 16));
        BitmapImage second = new("asset://logo", new LayoutSize(32, 16), new EqualImage(32, 16));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RenderTargetImageEqualityUsesDrawImageReferenceIdentity()
    {
        RenderTargetImage first = new("render-target://main", new LayoutSize(64, 32), new EqualImage(64, 32));
        RenderTargetImage second = new("render-target://main", new LayoutSize(64, 32), new EqualImage(64, 32));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ImageSourcesWithSameMetadataAndDrawImageReferenceAreEqual()
    {
        FakeImage image = new();
        BitmapImage first = new("asset://logo", new LayoutSize(32, 16), image);
        BitmapImage second = new("asset://logo", new LayoutSize(32, 16), image);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ImageSourceRejectsInvalidIdentity()
    {
        Assert.Throws<ArgumentException>(() => new BitmapImage("", new LayoutSize(1, 1)));
    }

    [Fact]
    public void ImageSourceRejectsNullIdentity()
    {
        Assert.Throws<ArgumentException>(() => new BitmapImage(null!, new LayoutSize(1, 1)));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(float.NaN, 1)]
    [InlineData(1, float.PositiveInfinity)]
    public void ImageSourceRejectsInvalidIntrinsicSize(float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitmapImage("asset://bad", new LayoutSize(width, height)));
    }

    [Fact]
    public void BitmapImageCanRepresentUnresolvedDrawImage()
    {
        BitmapImage source = new("asset://pending", new LayoutSize(32, 16));

        Assert.Null(source.ResolveDrawImage());
    }

    [Fact]
    public void RenderTargetImageRejectsNullDrawImage()
    {
        Assert.Throws<ArgumentNullException>(() => new RenderTargetImage("render-target://bad", new LayoutSize(1, 1), null!));
    }

    private sealed class FakeImage : IDrawImage
    {
        public int Width => 32;

        public int Height => 16;
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
