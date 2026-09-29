using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Xunit.Abstractions;

namespace Cerneala.Tests.Drawing;

public sealed class ImageMetadataSnapshotTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IdentityRetainsEveryImageOptionWithoutReadingTheLiveImage(bool explicitSource)
    {
        MutableImage image = new();
        DrawImageOptions options = new(
            source: explicitSource ? new DrawRect(2, 3, 8, 9) : null,
            tint: new Color(5, 17, 31, 127), opacity: 0.25f, rotation: 0.8f,
            origin: new DrawPoint(3, 5), flip: DrawImageFlip.Horizontal | DrawImageFlip.Vertical,
            layerDepth: 0.25f, sampling: DrawSamplingMode.Point, addressMode: DrawAddressMode.Wrap);
        DrawCommand command = DrawCommand.DrawImage(image, new DrawRect(-7, 11, 37, 29), options);
        DrawCommandMetadata metadata = DrawCommandMetadata.Create(command);
        image.ThrowOnDimensionRead = true;

        Assert.Equal(command, metadata.RetainedIdentity);
        Assert.Same(image, metadata.RetainedIdentity.Image);
        Assert.Same(options, metadata.RetainedIdentity.ImageOptions);
        Assert.Same(image, Assert.Single(metadata.Resources));
    }

    [Fact]
    public void NewKeyDoesNotMutateThePriorIdentityOrResourceSnapshot()
    {
        MutableImage image = new();
        object owner = new();
        DrawCommand command = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16), Color.White);
        DrawCommandMetadata previous = DrawCommandMetadata.Create(command, key: new(owner, 1));
        DrawCommandMetadata current = DrawCommandMetadata.Create(command, previous, new(owner, 2));

        Assert.NotSame(previous, current);
        Assert.Equal(new RetainedCommandKey(owner, 1), previous.RetainedKey);
        Assert.Equal(new RetainedCommandKey(owner, 2), current.RetainedKey);
        Assert.Equal(command, previous.RetainedIdentity);
        Assert.Equal(command, current.RetainedIdentity);
        Assert.Same(previous.Resources, current.Resources);
    }

    [Fact]
    public void ResizedImageRevalidatesBoundsWithoutChangingTheOlderSnapshot()
    {
        MutableImage image = new();
        object owner = new();
        DrawCommand command = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16),
            new DrawImageOptions(origin: new DrawPoint(8, 0)));
        DrawCommandMetadata previous = DrawCommandMetadata.Create(command, key: new(owner, 1));
        image.PixelWidth = 32;
        DrawCommandMetadata current = DrawCommandMetadata.Create(command, previous, new(owner, 1));

        Assert.NotSame(previous, current);
        Assert.Equal(new DrawRect(-8, 0, 16, 16), previous.Bounds);
        Assert.Equal(new DrawRect(-4, 0, 16, 16), current.Bounds);
        image.ThrowOnDimensionRead = true;
        Assert.Equal(command, previous.RetainedIdentity);
        Assert.Equal(command, current.RetainedIdentity);
    }

    [Fact]
    public void ImageKindWithAnInternalMeshKeepsItsCompleteIdentity()
    {
        MutableImage image = new();
        DrawMesh2D mesh = new(
            [new DrawVertex2D(new(0, 0), Color.Red),
             new DrawVertex2D(new(8, 0), Color.Green),
             new DrawVertex2D(new(0, 8), Color.Blue)],
            [0, 1, 2], image: image);
        DrawCommand ordinary = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16), Color.White);
        DrawCommand extended = DrawCommand.WithMesh(ordinary, mesh);
        DrawCommandMetadata metadata = DrawCommandMetadata.Create(extended);

        Assert.Equal(extended, metadata.RetainedIdentity);
        Assert.Same(mesh, metadata.RetainedIdentity.Mesh);
        Assert.NotEqual(ordinary, metadata.RetainedIdentity);
    }

    [Fact]
    public void SignedZeroKeepsBitwiseAndValueIdentityComparisonDistinct()
    {
        MutableImage image = new();
        DrawImageOptions options = new();
        float negativeZero = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
        DrawCommand positive = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16), options);
        DrawCommand negative = DrawCommand.DrawImage(image, new DrawRect(negativeZero, 0, 16, 16), options);
        DrawCommandMetadata metadata = DrawCommandMetadata.Create(positive);

        Assert.True(metadata.MatchesBits(positive));
        Assert.False(metadata.MatchesBits(negative));
        Assert.Equal(positive, negative);
        Assert.True(metadata.HasSameIdentity(DrawCommandMetadata.Create(negative)));
    }

    [Fact]
    public void ValueEquivalentOptionsDoNotBecomeBitwiseIdenticalReferences()
    {
        MutableImage image = new();
        DrawCommand first = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16), new DrawImageOptions());
        DrawCommand second = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16), new DrawImageOptions());
        DrawCommandMetadata metadata = DrawCommandMetadata.Create(first);

        Assert.NotSame(first.ImageOptions, second.ImageOptions);
        Assert.False(metadata.MatchesBits(second));
        Assert.Equal(first, second);
        Assert.True(metadata.HasSameIdentity(DrawCommandMetadata.Create(second)));
    }

    [Fact]
    public void ChangedImageSnapshotAllocatesLessThanTheWholeGeneralCommandPayload()
    {
        MutableImage image = new() { PixelWidth = 32 };
        object owner = new();
        DrawCommand first = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16),
            new DrawImageOptions(source: new DrawRect(0, 0, 16, 16)));
        DrawCommand second = DrawCommand.DrawImage(image, new DrawRect(0, 0, 16, 16),
            new DrawImageOptions(source: new DrawRect(16, 0, 16, 16)));
        DrawCommandMetadata current = DrawCommandMetadata.Create(first, key: new(owner, 0));
        long version = 0;
        for (int i = 0; i < 64; i++)
        {
            current = DrawCommandMetadata.Create(i % 2 == 0 ? second : first, current, new(owner, ++version));
        }
        const int iterations = 256;
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            current = DrawCommandMetadata.Create(i % 2 == 0 ? second : first, current, new(owner, ++version));
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;

        output.WriteLine($"Changed image snapshots: {allocated} bytes / {iterations} = {allocated / (double)iterations:R} bytes per command.");
        Assert.Equal(first, current.RetainedIdentity);
        // This is an internal storage regression guard, not the native FPS gate:
        // a plain image snapshot must not retain the entire general command union.
        Assert.True(allocated < (long)iterations * Unsafe.SizeOf<DrawCommand>(),
            $"Changed image snapshots allocated {allocated} bytes for {iterations} commands; full command size is {Unsafe.SizeOf<DrawCommand>()}.");
    }

    private sealed class MutableImage : IDrawImage
    {
        internal int PixelWidth { get; set; } = 16;
        internal bool ThrowOnDimensionRead { get; set; }
        public int Width => ThrowOnDimensionRead ? throw new InvalidOperationException("Live image dimension read") : PixelWidth;
        public int Height => ThrowOnDimensionRead ? throw new InvalidOperationException("Live image dimension read") : 16;
    }
}
