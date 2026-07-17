using System.Buffers;
using Cerneala.Drawing;

namespace Cerneala.Drawing.Text;

public sealed class RasterizedText
{
    private readonly byte[] _rgbaPixels;
    private readonly bool returnPixelBufferToPool;
    private readonly int pixelOffset;
    private readonly int pixelLength;
    private int pixelBufferReturned;

    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult)
        : this(width, height, rgbaPixels, shapeResult, default)
    {
    }

    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult, DrawPoint originOffset)
        : this(width, height, rgbaPixels, shapeResult, originOffset, takeOwnership: false)
    {
    }

    private RasterizedText(
        int width,
        int height,
        byte[] rgbaPixels,
        TextShapeResult shapeResult,
        DrawPoint originOffset,
        bool takeOwnership,
        int pixelOffset = 0,
        bool returnPixelBufferToPool = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (!float.IsFinite(originOffset.X) || !float.IsFinite(originOffset.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(originOffset), "Origin offset must be finite.");
        }

        long expectedPixelLength = (long)width * height * 4;

        if (expectedPixelLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "RGBA pixel buffer dimensions are too large.");
        }

        if (pixelOffset < 0 ||
            pixelOffset > rgbaPixels.Length ||
            expectedPixelLength > rgbaPixels.Length - pixelOffset)
        {
            throw new ArgumentException("RGBA pixel slice must contain width * height * 4 bytes.", nameof(rgbaPixels));
        }

        if (!takeOwnership && (pixelOffset != 0 || rgbaPixels.Length != expectedPixelLength))
        {
            throw new ArgumentException("Public RGBA pixel buffers must equal width * height * 4.", nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        _rgbaPixels = takeOwnership ? rgbaPixels : (byte[])rgbaPixels.Clone();
        this.returnPixelBufferToPool = returnPixelBufferToPool;
        this.pixelOffset = takeOwnership ? pixelOffset : 0;
        pixelLength = checked((int)expectedPixelLength);
        ShapeResult = shapeResult;
        OriginOffset = originOffset;
    }

    public int Width { get; }

    public int Height { get; }

    public DrawPoint OriginOffset { get; }

    public byte[] RgbaPixels => PixelSpan.ToArray();

    public TextShapeResult ShapeResult { get; }

    internal ReadOnlySpan<byte> PixelSpan => _rgbaPixels.AsSpan(pixelOffset, pixelLength);

    internal byte[] PixelBuffer => _rgbaPixels;

    internal int PixelOffset => pixelOffset;

    internal int PixelLength => pixelLength;

    internal static RasterizedText FromOwnedPixels(
        int width,
        int height,
        byte[] rgbaPixels,
        TextShapeResult shapeResult,
        DrawPoint originOffset = default)
    {
        return new RasterizedText(
            width,
            height,
            rgbaPixels,
            shapeResult,
            originOffset,
            takeOwnership: true);
    }

    internal static RasterizedText FromOwnedPixelSlice(
        int width,
        int height,
        byte[] rgbaPixels,
        int pixelOffset,
        TextShapeResult shapeResult,
        DrawPoint originOffset = default)
    {
        return new RasterizedText(
            width,
            height,
            rgbaPixels,
            shapeResult,
            originOffset,
            takeOwnership: true,
            pixelOffset);
    }

    internal static RasterizedText FromPooledPixelSlice(
        int width,
        int height,
        byte[] rgbaPixels,
        int pixelOffset,
        TextShapeResult shapeResult,
        DrawPoint originOffset,
        bool returnPixelBufferToPool)
    {
        return new RasterizedText(
            width,
            height,
            rgbaPixels,
            shapeResult,
            originOffset,
            takeOwnership: true,
            pixelOffset,
            returnPixelBufferToPool);
    }

    internal void ReturnPixelBuffer()
    {
        if (returnPixelBufferToPool &&
            Interlocked.Exchange(ref pixelBufferReturned, 1) == 0)
        {
            ArrayPool<byte>.Shared.Return(_rgbaPixels);
        }
    }
}
