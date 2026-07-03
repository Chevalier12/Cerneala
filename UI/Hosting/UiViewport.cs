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
}
