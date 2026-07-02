namespace Cerneala.Drawing.Text;

public sealed class RasterizedText
{
    public RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult)
    {
        Width = width;
        Height = height;
        RgbaPixels = rgbaPixels ?? throw new ArgumentNullException(nameof(rgbaPixels));
        ShapeResult = shapeResult;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbaPixels { get; }

    public TextShapeResult ShapeResult { get; }
}
