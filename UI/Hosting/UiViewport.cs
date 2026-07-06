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
}
