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
