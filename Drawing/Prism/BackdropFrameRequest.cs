using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism;

public readonly record struct BackdropFrameRequest
{
    public BackdropFrameRequest(
        int PixelWidth,
        int PixelHeight,
        float PixelScale)
        : this(
            PixelWidth,
            PixelHeight,
            PixelScale,
            backdropRequirement: null)
    {
    }

    internal BackdropFrameRequest(
        int PixelWidth,
        int PixelHeight,
        float PixelScale,
        PrismBackdropRequirement? backdropRequirement)
    {
        if (PixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelWidth),
                "Requested backdrop pixel width must be greater than zero.");
        }
        if (PixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelHeight),
                "Requested backdrop pixel height must be greater than zero.");
        }
        if (!float.IsFinite(PixelScale) || PixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelScale),
                "Requested backdrop pixel scale must be finite and greater than zero.");
        }

        this.PixelWidth = PixelWidth;
        this.PixelHeight = PixelHeight;
        this.PixelScale = PixelScale;
        BackdropRequirement = backdropRequirement;
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public float PixelScale { get; }

    internal PrismBackdropRequirement? BackdropRequirement { get; }
}
