namespace Cerneala.UI.Hosting;

public readonly record struct UiViewport
{
    public UiViewport(float width, float height, float scale = 1)
    {
        if (!float.IsFinite(width) || width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Viewport width must be finite and cannot be negative.");
        }

        if (!float.IsFinite(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Viewport height must be finite and cannot be negative.");
        }

        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Viewport scale must be finite and greater than zero.");
        }

        Width = width;
        Height = height;
        Scale = scale;
    }

    public float Width { get; }

    public float Height { get; }

    public float Scale { get; }

    public static UiViewport FromPhysicalPixels(int pixelWidth, int pixelHeight, float scale)
    {
        if (pixelWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Viewport pixel width cannot be negative.");
        }

        if (pixelHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Viewport pixel height cannot be negative.");
        }

        UiCoordinateMapper.ValidateScale(scale);
        return new UiViewport(
            UiCoordinateMapper.PhysicalToLogical(pixelWidth, scale),
            UiCoordinateMapper.PhysicalToLogical(pixelHeight, scale),
            scale);
    }

    internal bool TryGetPixelSize(out int pixelWidth, out int pixelHeight)
    {
        double width = ToPixelExtent(Width, Scale);
        double height = ToPixelExtent(Height, Scale);
        if (width <= 0 ||
            height <= 0 ||
            width > int.MaxValue ||
            height > int.MaxValue)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return false;
        }

        pixelWidth = (int)width;
        pixelHeight = (int)height;
        return true;
    }

    // The smallest pixel count whose logical extent covers the given one, so a viewport made by FromPhysicalPixels
    // converts back to exactly its pixels. Rounding the product up is not enough: 898 / 1.75f is stored as
    // 513.142883f, whose product with 1.75 is 898.00005 and rounds up to 899.
    private static double ToPixelExtent(float logical, float scale)
    {
        double pixels = Math.Ceiling((double)logical * scale);
        while (pixels > 0 &&
            pixels <= int.MaxValue &&
            UiCoordinateMapper.PhysicalToLogical((float)(pixels - 1), scale) >= logical)
        {
            pixels--;
        }

        return pixels;
    }
}
