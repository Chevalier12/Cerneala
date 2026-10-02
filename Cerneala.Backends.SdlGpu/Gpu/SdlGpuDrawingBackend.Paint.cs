using System.Diagnostics;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Paths;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    private SdlGpuPaint ResolvePaint(
        IDrawBrush? brush,
        DrawRect bounds,
        Color fallbackColor,
        float commandOpacity)
    {
        if (brush is null)
        {
            return SdlGpuPaint.Solid(
                GetWhiteTexture(),
                ApplyOpacity(fallbackColor, commandOpacity));
        }

        DrawBrushDescriptor descriptor = brush.CreateDescriptor();
        switch (descriptor)
        {
            case SolidDrawBrushDescriptor solid:
                return SdlGpuPaint.Solid(
                    GetWhiteTexture(),
                    ApplyOpacity(
                        solid.Color,
                        solid.Opacity * commandOpacity));
            case LinearGradientDrawBrushDescriptor:
            case RadialGradientDrawBrushDescriptor:
                {
                    int width = Math.Max(1, checked((int)MathF.Ceiling(
                        bounds.Width * CoordinateScale)));
                    int height = Math.Max(1, checked((int)MathF.Ceiling(
                        bounds.Height * CoordinateScale)));
                    if (descriptor is LinearGradientDrawBrushDescriptor linear)
                    {
                        // Keep every sample on the varying axis. The other axis
                        // is constant, so one texel has identical clamped sampling.
                        if (linear.StartPoint.X == linear.EndPoint.X) width = 1;
                        if (linear.StartPoint.Y == linear.EndPoint.Y) height = 1;
                    }
                    object key = new SdlGpuBrushTextureKey(
                        brush,
                        bounds,
                        width,
                        height);
                    SdlGpuTextureResource? texture = resources.FindTexture(session, key);
                    if (texture is null)
                    {
                        // A valid retained texture already owns these pixels.
                        // Do not rasterize a replacement before checking for it.
                        byte[] pixels = RasterizeBrush(descriptor, bounds, width, height);
                        texture = resources.GetOrCreateTexture(
                            session,
                            key,
                            width,
                            height,
                            pixels,
                            recycleStorage: true);
                    }
                    MarkBrushTextureUsed(key);
                    return SdlGpuPaint.BoundsMapped(
                        texture,
                        bounds,
                        ApplyOpacity(Color.White, commandOpacity));
                }
            case ImageDrawBrushDescriptor imageBrush:
                {
                    if (imageBrush.Image is null)
                    {
                        if (!string.IsNullOrWhiteSpace(imageBrush.SourceIdentity))
                        {
                            throw new InvalidOperationException(
                                $"ImageBrush source '{imageBrush.SourceIdentity}' was not resolved to an SDL_GPU image.");
                        }
                        return SdlGpuPaint.Solid(
                            GetWhiteTexture(),
                            Color.Transparent);
                    }
                    SdlGpuTextureResource texture = GetImageTexture(imageBrush.Image);
                    if (imageBrush.Stretch != DrawBrushStretch.Fill ||
                        imageBrush.TileMode != DrawTileMode.None || imageBrush.Viewport is not null)
                    {
                        return ResolveTileBrushPaint(brush, imageBrush, bounds, commandOpacity);
                    }
                    return SdlGpuPaint.ImageBrush(
                        texture,
                        bounds,
                        imageBrush,
                        ApplyOpacity(
                            Color.White,
                            imageBrush.Opacity * commandOpacity));
                }
            case DrawingDrawBrushDescriptor drawing:
                return ResolveTileBrushPaint(brush, drawing, bounds, commandOpacity);
            case VisualDrawBrushDescriptor visual:
                return ResolveTileBrushPaint(brush, visual, bounds, commandOpacity);
            default:
                throw new NotSupportedException(
                    $"SDL_GPU does not support brush descriptor '{descriptor.GetType().Name}'.");
        }
    }

    private static byte[] RasterizeBrush(
        DrawBrushDescriptor descriptor,
        DrawRect bounds,
        int width,
        int height)
    {
        byte[] pixels = new byte[checked(width * height * 4)];
        float logicalWidth = MathF.Max(bounds.Width, float.Epsilon);
        float logicalHeight = MathF.Max(bounds.Height, float.Epsilon);
        for (int y = 0; y < height; y++)
        {
            float localY = ((y + 0.5f) / height) * logicalHeight;
            for (int x = 0; x < width; x++)
            {
                float localX = ((x + 0.5f) / width) * logicalWidth;
                Color color = SampleBrush(
                    descriptor,
                    new DrawPoint(localX, localY));
                WritePremultipliedColor(pixels, ((y * width) + x) * 4, color);
            }
        }
        return pixels;
    }

    private static Color SampleBrush(
        DrawBrushDescriptor descriptor,
        DrawPoint point) => descriptor switch
        {
            SolidDrawBrushDescriptor solid => ApplyOpacity(
                solid.Color,
                solid.Opacity),
            LinearGradientDrawBrushDescriptor linear => ApplyOpacity(
                SampleLinear(linear, point),
                linear.Opacity),
            RadialGradientDrawBrushDescriptor radial => ApplyOpacity(
                SampleRadial(radial, point),
                radial.Opacity),
            _ => Color.Transparent
        };

    private static Color SampleLinear(
        LinearGradientDrawBrushDescriptor gradient,
        DrawPoint point)
    {
        float dx = gradient.EndPoint.X - gradient.StartPoint.X;
        float dy = gradient.EndPoint.Y - gradient.StartPoint.Y;
        float lengthSquared = (dx * dx) + (dy * dy);
        float offset = lengthSquared <= float.Epsilon
            ? 1
            : (((point.X - gradient.StartPoint.X) * dx) +
                ((point.Y - gradient.StartPoint.Y) * dy)) / lengthSquared;
        return InterpolateStops(gradient.Stops, offset);
    }

    private static Color SampleRadial(
        RadialGradientDrawBrushDescriptor gradient,
        DrawPoint point)
    {
        float dx = (point.X - gradient.Center.X) / gradient.RadiusX;
        float dy = (point.Y - gradient.Center.Y) / gradient.RadiusY;
        return InterpolateStops(
            gradient.Stops,
            MathF.Sqrt((dx * dx) + (dy * dy)));
    }

    private static Color InterpolateStops(
        IReadOnlyList<DrawGradientStop> stops,
        float offset)
    {
        if (stops.Count == 1 || offset <= stops[0].Offset)
        {
            return stops[0].Color;
        }
        for (int i = 1; i < stops.Count; i++)
        {
            DrawGradientStop next = stops[i];
            if (offset > next.Offset)
            {
                continue;
            }
            DrawGradientStop previous = stops[i - 1];
            float range = next.Offset - previous.Offset;
            float amount = range <= float.Epsilon
                ? 1
                : Math.Clamp((offset - previous.Offset) / range, 0, 1);
            return new Color(
                Lerp(previous.Color.R, next.Color.R, amount),
                Lerp(previous.Color.G, next.Color.G, amount),
                Lerp(previous.Color.B, next.Color.B, amount),
                Lerp(previous.Color.A, next.Color.A, amount));
        }
        return stops[^1].Color;
    }

    private static byte Lerp(byte first, byte second, float amount) =>
        (byte)Math.Clamp(
            (int)MathF.Round(first + ((second - first) * amount)),
            0,
            255);

    private static void WritePremultipliedColor(
        byte[] pixels,
        int offset,
        Color color)
    {
        pixels[offset] = MultiplyByte(color.R, color.A);
        pixels[offset + 1] = MultiplyByte(color.G, color.A);
        pixels[offset + 2] = MultiplyByte(color.B, color.A);
        pixels[offset + 3] = color.A;
    }

    private readonly record struct SdlGpuPaint(
        nint Texture,
        Color Tint,
        DrawSamplingMode Sampling,
        DrawAddressMode AddressMode,
        Func<DrawPoint, DrawPoint> MapTextureCoordinate)
    {
        public static SdlGpuPaint Solid(
            SdlGpuTextureResource texture,
            Color tint) =>
            new(
                texture.Handle,
                tint,
                DrawSamplingMode.Point,
                DrawAddressMode.Clamp,
                static _ => new DrawPoint(0.5f, 0.5f));

        public static SdlGpuPaint BoundsMapped(
            SdlGpuTextureResource texture,
            DrawRect bounds,
            Color tint) => BoundsMapped(texture.Handle, bounds, tint);

        public static SdlGpuPaint BoundsMapped(
            nint texture,
            DrawRect bounds,
            Color tint) =>
            new(
                texture,
                tint,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                point => new DrawPoint(
                    bounds.Width <= float.Epsilon
                        ? 0
                        : (point.X - bounds.X) / bounds.Width,
                    bounds.Height <= float.Epsilon
                        ? 0
                        : (point.Y - bounds.Y) / bounds.Height));

        public static SdlGpuPaint ImageBrush(
            SdlGpuTextureResource texture,
            DrawRect bounds,
            ImageDrawBrushDescriptor descriptor,
            Color tint)
        {
            DrawRect viewport = descriptor.Viewport ?? bounds;
            DrawRect viewbox = descriptor.Viewbox ??
                new DrawRect(0, 0, texture.Width, texture.Height);
            return new SdlGpuPaint(
                texture.Handle,
                tint,
                DrawSamplingMode.Linear,
                descriptor.TileMode == DrawTileMode.None
                    ? DrawAddressMode.Clamp
                    : DrawAddressMode.Wrap,
                point =>
                {
                    float u = viewport.Width <= float.Epsilon
                        ? 0
                        : (point.X - viewport.X) / viewport.Width;
                    float v = viewport.Height <= float.Epsilon
                        ? 0
                        : (point.Y - viewport.Y) / viewport.Height;
                    return new DrawPoint(
                        (viewbox.X + (u * viewbox.Width)) / texture.Width,
                        (viewbox.Y + (v * viewbox.Height)) / texture.Height);
                });
        }
    }

    private readonly record struct SdlGpuBrushTextureKey(
        IDrawBrush Brush,
        DrawRect Bounds,
        int Width,
        int Height);

}
