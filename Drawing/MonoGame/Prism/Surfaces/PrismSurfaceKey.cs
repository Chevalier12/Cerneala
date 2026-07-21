using Cerneala.Drawing.Prism.Catalog;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal readonly record struct PrismSurfaceKey
{
    public PrismSurfaceKey(
        int width,
        int height,
        SurfaceFormat format,
        int multiSampleCount,
        PrismColorProfile colorProfile)
    {
        Validate(width, height, format, multiSampleCount, colorProfile);

        Width = width;
        Height = height;
        Format = format;
        MultiSampleCount = multiSampleCount;
        ColorProfile = colorProfile;
    }

    public int Width { get; }

    public int Height { get; }

    public SurfaceFormat Format { get; }

    public int MultiSampleCount { get; }

    public PrismColorProfile ColorProfile { get; }

    internal void Validate()
    {
        Validate(Width, Height, Format, MultiSampleCount, ColorProfile);
    }

    internal long CalculateByteSize()
    {
        Validate();
        GetStorageLayout(
            Format,
            out int blockWidth,
            out int blockHeight,
            out int bytesPerBlock);
        long blocksWide =
            ((long)Width + blockWidth - 1) / blockWidth;
        long blocksHigh =
            ((long)Height + blockHeight - 1) / blockHeight;
        long sampleCount = Math.Max(1, MultiSampleCount);
        return checked(
            blocksWide *
            blocksHigh *
            bytesPerBlock *
            sampleCount);
    }

    private static void GetStorageLayout(
        SurfaceFormat format,
        out int blockWidth,
        out int blockHeight,
        out int bytesPerBlock)
    {
        blockWidth = format switch
        {
            SurfaceFormat.RgbPvrtc2Bpp or
            SurfaceFormat.RgbaPvrtc2Bpp => 8,
            SurfaceFormat.Dxt1 or
            SurfaceFormat.Dxt3 or
            SurfaceFormat.Dxt5 or
            SurfaceFormat.Dxt1SRgb or
            SurfaceFormat.Dxt3SRgb or
            SurfaceFormat.Dxt5SRgb or
            SurfaceFormat.RgbPvrtc4Bpp or
            SurfaceFormat.RgbaPvrtc4Bpp or
            SurfaceFormat.RgbEtc1 or
            SurfaceFormat.Dxt1a or
            SurfaceFormat.RgbaAtcExplicitAlpha or
            SurfaceFormat.RgbaAtcInterpolatedAlpha or
            SurfaceFormat.Rgb8Etc2 or
            SurfaceFormat.Srgb8Etc2 or
            SurfaceFormat.Rgb8A1Etc2 or
            SurfaceFormat.Srgb8A1Etc2 or
            SurfaceFormat.Rgba8Etc2 or
            SurfaceFormat.SRgb8A8Etc2 or
            SurfaceFormat.Astc4X4Rgba => 4,
            _ => 1
        };
        blockHeight = blockWidth == 1 ? 1 : 4;
        bytesPerBlock = format switch
        {
            SurfaceFormat.Alpha8 => 1,
            SurfaceFormat.Bgr565 or
            SurfaceFormat.Bgra5551 or
            SurfaceFormat.Bgra4444 or
            SurfaceFormat.NormalizedByte2 or
            SurfaceFormat.HalfSingle => 2,
            SurfaceFormat.Color or
            SurfaceFormat.NormalizedByte4 or
            SurfaceFormat.Rgba1010102 or
            SurfaceFormat.Rg32 or
            SurfaceFormat.Single or
            SurfaceFormat.HalfVector2 or
            SurfaceFormat.Bgr32 or
            SurfaceFormat.Bgra32 or
            SurfaceFormat.ColorSRgb or
            SurfaceFormat.Bgr32SRgb or
            SurfaceFormat.Bgra32SRgb => 4,
            SurfaceFormat.Dxt1 or
            SurfaceFormat.Dxt1SRgb or
            SurfaceFormat.RgbPvrtc2Bpp or
            SurfaceFormat.RgbPvrtc4Bpp or
            SurfaceFormat.RgbaPvrtc2Bpp or
            SurfaceFormat.RgbaPvrtc4Bpp or
            SurfaceFormat.RgbEtc1 or
            SurfaceFormat.Dxt1a or
            SurfaceFormat.Rgb8Etc2 or
            SurfaceFormat.Srgb8Etc2 or
            SurfaceFormat.Rgb8A1Etc2 or
            SurfaceFormat.Srgb8A1Etc2 or
            SurfaceFormat.Rgba64 or
            SurfaceFormat.Vector2 or
            SurfaceFormat.HalfVector4 or
            SurfaceFormat.HdrBlendable => 8,
            SurfaceFormat.Dxt3 or
            SurfaceFormat.Dxt5 or
            SurfaceFormat.Dxt3SRgb or
            SurfaceFormat.Dxt5SRgb or
            SurfaceFormat.RgbaAtcExplicitAlpha or
            SurfaceFormat.RgbaAtcInterpolatedAlpha or
            SurfaceFormat.Rgba8Etc2 or
            SurfaceFormat.SRgb8A8Etc2 or
            SurfaceFormat.Astc4X4Rgba or
            SurfaceFormat.Vector4 => 16,
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unknown MonoGame surface storage layout.")
        };
    }

    private static void Validate(
        int width,
        int height,
        SurfaceFormat format,
        int multiSampleCount,
        PrismColorProfile colorProfile)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegative(multiSampleCount);

        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unknown MonoGame surface format.");
        }

        if (!Enum.IsDefined(colorProfile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(colorProfile),
                colorProfile,
                "Unknown Prism color profile.");
        }
    }
}
