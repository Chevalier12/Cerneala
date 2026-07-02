namespace Cerneala.Drawing.Text;

public sealed class RasterizedText
{
    private readonly byte[] _rgbaPixels;

    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(rgbaPixels);

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
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbaPixels => (byte[])_rgbaPixels.Clone();

    public TextShapeResult ShapeResult { get; }
}
