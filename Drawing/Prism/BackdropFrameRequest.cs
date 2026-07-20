using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.Prism;

public readonly record struct BackdropFrameRequest
{
    public BackdropFrameRequest(
        int PixelWidth,
        int PixelHeight,
        float PixelScale,
        PrismBackdropRequirement BackdropRequirement)
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
        this.BackdropRequirement = BackdropRequirement ??
            throw new ArgumentNullException(nameof(BackdropRequirement));
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public float PixelScale { get; }

    public PrismBackdropRequirement BackdropRequirement { get; }
}
