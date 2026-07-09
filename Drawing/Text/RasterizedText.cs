using Cerneala.Drawing;

namespace Cerneala.Drawing.Text;

public sealed class RasterizedText
{
    private readonly byte[] _rgbaPixels;

    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult)
        : this(width, height, rgbaPixels, shapeResult, default)
    {
    }

    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult, DrawPoint originOffset)
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

        if (rgbaPixels.Length != expectedPixelLength)
        {
            throw new ArgumentException("RGBA pixel buffer length must equal width * height * 4.", nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        _rgbaPixels = (byte[])rgbaPixels.Clone();
        ShapeResult = shapeResult;
        OriginOffset = originOffset;
    }

    public int Width { get; }

    public int Height { get; }

    public DrawPoint OriginOffset { get; }

    public byte[] RgbaPixels => (byte[])_rgbaPixels.Clone();

    public TextShapeResult ShapeResult { get; }
}
