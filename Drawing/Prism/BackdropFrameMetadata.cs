using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Drawing.Prism;

public readonly record struct BackdropFrameMetadata
{
    public BackdropFrameMetadata(
        int PixelWidth,
        int PixelHeight,
        float PixelScale,
        PrismColorProfile ColorProfile,
        BackdropPixelFormat PixelFormat,
        BackdropAlphaMode AlphaMode,
        Matrix3x2 CoordinateTransform,
        long ContentVersion)
    {
        if (PixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelWidth),
                "Backdrop pixel width must be greater than zero.");
        }
        if (PixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelHeight),
                "Backdrop pixel height must be greater than zero.");
        }
        if (!float.IsFinite(PixelScale) || PixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PixelScale),
                "Backdrop pixel scale must be finite and greater than zero.");
        }
        if (!Enum.IsDefined(ColorProfile))
        {
            throw new ArgumentOutOfRangeException(nameof(ColorProfile));
        }
        if (!Enum.IsDefined(PixelFormat))
        {
            throw new ArgumentOutOfRangeException(nameof(PixelFormat));
        }
        if (!Enum.IsDefined(AlphaMode))
        {
            throw new ArgumentOutOfRangeException(nameof(AlphaMode));
        }
        if (!IsFinite(CoordinateTransform))
        {
            throw new ArgumentOutOfRangeException(
                nameof(CoordinateTransform),
                "Backdrop coordinate transform must contain only finite values.");
        }
        if (ContentVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ContentVersion),
                "Backdrop content version cannot be negative.");
        }

        this.PixelWidth = PixelWidth;
        this.PixelHeight = PixelHeight;
        this.PixelScale = PixelScale;
        this.ColorProfile = ColorProfile;
        this.PixelFormat = PixelFormat;
        this.AlphaMode = AlphaMode;
        this.CoordinateTransform = CoordinateTransform;
        this.ContentVersion = ContentVersion;
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public float PixelScale { get; }

    public PrismColorProfile ColorProfile { get; }

    public BackdropPixelFormat PixelFormat { get; }

    public BackdropAlphaMode AlphaMode { get; }

    public Matrix3x2 CoordinateTransform { get; }

    public long ContentVersion { get; }

    private static bool IsFinite(Matrix3x2 matrix)
    {
        return float.IsFinite(matrix.M11) &&
            float.IsFinite(matrix.M12) &&
            float.IsFinite(matrix.M21) &&
            float.IsFinite(matrix.M22) &&
            float.IsFinite(matrix.M31) &&
            float.IsFinite(matrix.M32);
    }
}
