namespace Cerneala.Drawing;

public enum DrawSamplingMode
{
    Point,
    Linear
}

public enum DrawAddressMode
{
    Clamp,
    Wrap
}

public sealed record DrawImageOptions
{
    public DrawImageOptions(
        DrawRect? source = null,
        Color? tint = null,
        float opacity = 1,
        float rotation = 0,
        DrawPoint origin = default,
        DrawImageFlip flip = DrawImageFlip.None,
        float layerDepth = 0,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp)
    {
        if (source is DrawRect sourceRect &&
            (sourceRect.Width <= 0 || sourceRect.Height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
        if (!float.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }
        if ((flip & ~(DrawImageFlip.Horizontal | DrawImageFlip.Vertical)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flip));
        }
        if (!float.IsFinite(layerDepth) || layerDepth < 0 || layerDepth > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(layerDepth));
        }
        if (!Enum.IsDefined(sampling))
        {
            throw new ArgumentOutOfRangeException(nameof(sampling));
        }
        if (!Enum.IsDefined(addressMode))
        {
            throw new ArgumentOutOfRangeException(nameof(addressMode));
        }

        Source = source;
        Tint = tint ?? Color.White;
        Opacity = opacity;
        Rotation = rotation;
        Origin = origin;
        Flip = flip;
        LayerDepth = layerDepth;
        Sampling = sampling;
        AddressMode = addressMode;
    }

    public DrawRect? Source { get; }

    public Color Tint { get; }

    public float Opacity { get; }

    public float Rotation { get; }

    public DrawPoint Origin { get; }

    public DrawImageFlip Flip { get; }

    public float LayerDepth { get; }

    public DrawSamplingMode Sampling { get; }

    public DrawAddressMode AddressMode { get; }
}

public readonly record struct DrawInsets
{
    public DrawInsets(float uniform)
        : this(uniform, uniform, uniform, uniform)
    {
    }

    public DrawInsets(float left, float top, float right, float bottom)
    {
        ThrowIfInvalid(left, nameof(left));
        ThrowIfInvalid(top, nameof(top));
        ThrowIfInvalid(right, nameof(right));
        ThrowIfInvalid(bottom, nameof(bottom));

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public float Left { get; }

    public float Top { get; }

    public float Right { get; }

    public float Bottom { get; }

    private static void ThrowIfInvalid(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal static class DrawImageGeometry
{
    public static void ValidateImage(IDrawImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width <= 0 || image.Height <= 0)
        {
            throw new ArgumentException(
                "The image must have positive dimensions.",
                nameof(image));
        }
    }

    public static DrawRect ResolveSource(
        IDrawImage image,
        DrawImageOptions options)
    {
        ValidateImage(image);
        ArgumentNullException.ThrowIfNull(options);
        DrawRect source = options.Source ??
            new DrawRect(0, 0, image.Width, image.Height);
        if (source.X < 0 ||
            source.Y < 0 ||
            source.Right > image.Width ||
            source.Bottom > image.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The source rectangle must stay within the image.");
        }

        return source;
    }

    public static DrawPoint[] GetDestinationCorners(
        IDrawImage image,
        DrawRect destination,
        DrawImageOptions options)
    {
        DrawPoint[] corners =
        [
            TransformDestinationPoint(image, destination, options, 0, 0),
            TransformDestinationPoint(image, destination, options, destination.Width, 0),
            TransformDestinationPoint(image, destination, options, destination.Width, destination.Height),
            TransformDestinationPoint(image, destination, options, 0, destination.Height)
        ];
        return corners;
    }

    public static DrawPoint TransformDestinationPoint(
        IDrawImage image,
        DrawRect destination,
        DrawImageOptions options,
        float x,
        float y)
    {
        DrawRect source = ResolveSource(image, options);
        float localX = x - (options.Origin.X * destination.Width / source.Width);
        float localY = y - (options.Origin.Y * destination.Height / source.Height);
        float cosine = MathF.Cos(options.Rotation);
        float sine = MathF.Sin(options.Rotation);
        return new DrawPoint(
            destination.X + (localX * cosine) - (localY * sine),
            destination.Y + (localX * sine) + (localY * cosine));
    }

    public static DrawPoint[] GetTextureCoordinates(
        IDrawImage image,
        DrawImageOptions options)
    {
        DrawRect source = ResolveSource(image, options);
        float left = source.X / image.Width;
        float top = source.Y / image.Height;
        float right = source.Right / image.Width;
        float bottom = source.Bottom / image.Height;
        if ((options.Flip & DrawImageFlip.Horizontal) != 0)
        {
            (left, right) = (right, left);
        }
        if ((options.Flip & DrawImageFlip.Vertical) != 0)
        {
            (top, bottom) = (bottom, top);
        }

        return
        [
            new DrawPoint(left, top),
            new DrawPoint(right, top),
            new DrawPoint(right, bottom),
            new DrawPoint(left, bottom)
        ];
    }

    public static Color EffectiveTint(DrawImageOptions options) =>
        ApplyOpacity(options.Tint, options.Opacity);

    public static Color ApplyOpacity(Color color, float opacity) =>
        opacity >= 1
            ? color
            : new Color(
                color.R,
                color.G,
                color.B,
                (byte)Math.Clamp(
                    (int)MathF.Round(color.A * opacity),
                    0,
                    255));
}
