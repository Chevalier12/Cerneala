using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    private readonly Dictionary<BrushCaptureKey, BrushCapture> brushCaptures = [];
    private readonly HashSet<object> activeBrushCaptures = new(ReferenceEqualityComparer.Instance);
    private readonly List<BrushCaptureKey> unusedBrushCaptures = [];

    private SdlGpuPaint ResolveTileBrushPaint(
        IDrawBrush brush,
        TileDrawBrushDescriptor descriptor,
        DrawRect bounds,
        float commandOpacity,
        SdlGpuTextRasterKey? textKey = null,
        SdlGpuTextureResource? textCoverage = null)
    {
        object identity = descriptor is VisualDrawBrushDescriptor visual ? visual.VisualIdentity : brush;
        if (!activeBrushCaptures.Add(identity))
        {
            throw new InvalidOperationException("Brush rendering cycle detected.");
        }

        try
        {
            SdlGpuRenderTarget parent = batches.Target;
            float parentScale = CoordinateScale;
            float parentThreadScale = threadScale;
            int width = Math.Max(1, checked((int)MathF.Ceiling(bounds.Width * parentScale)));
            int height = Math.Max(1, checked((int)MathF.Ceiling(bounds.Height * parentScale)));
            BrushCaptureKey key = new(brush, bounds, width, height, parent.ColorFormat, textKey);
            if (!brushCaptures.TryGetValue(key, out BrushCapture? capture))
            {
                capture = new BrushCapture(new SdlGpuRenderSurfaceState(resources,
                    resources.CreateRenderTarget(width, height, parent.ColorFormat, SdlGpuSampleCount.One),
                    new SdlGpuPrismExecutor(session, this)));
                brushCaptures.Add(key, capture);
            }
            capture.Used = true;
            SdlGpuRenderSurfaceState surface = capture.Surface;
            RecordTileBrush(surface.Commands, descriptor, bounds, parentScale);

            // The parent batch must finish before changing its render target.
            // Captures share the ordinary command, Prism and retained-raster path.
            FlushBatches();
            CoordinateScale = 1;
            threadScale = 1;
            try
            {
                bool rendered = RenderRecordedSurfaceFrame(surface, Color.Transparent, batches,
                    requireFullReplay: textCoverage is not null);
                if (rendered && textCoverage is not null)
                {
                    ApplyTextCoverage(surface.Target, textCoverage);
                }
            }
            catch
            {
                surface.RetainedEntries = null;
                DiscardBatches();
                throw;
            }
            finally
            {
                CoordinateScale = parentScale;
                threadScale = parentThreadScale;
                session.BeginRenderTarget(parent, Color.Transparent, SdlGpuLoadOp.Load);
                batches.Begin(parent);
            }

            return SdlGpuPaint.BoundsMapped(surface.Target.SampleTexture, bounds,
                ApplyOpacity(Color.White, descriptor.Opacity * commandOpacity));
        }
        finally
        {
            activeBrushCaptures.Remove(identity);
        }
    }

    private static void RecordTileBrush(
        DrawCommandList output,
        TileDrawBrushDescriptor descriptor,
        DrawRect bounds,
        float scale)
    {
        output.Clear();
        IDrawImage? image = null;
        IReadOnlyList<DrawCommand>? content = null;
        DrawRect source;
        switch (descriptor)
        {
            case ImageDrawBrushDescriptor imageBrush:
                image = imageBrush.Image;
                if (image is null)
                {
                    if (!string.IsNullOrWhiteSpace(imageBrush.SourceIdentity))
                    {
                        throw new InvalidOperationException(
                            $"ImageBrush source '{imageBrush.SourceIdentity}' was not resolved to an SDL_GPU image.");
                    }
                    return;
                }
                if (image is not SdlGpuImage sdlImage)
                {
                    throw new InvalidOperationException("SDL_GPU image drawing requires an image created by SdlGpuImageLoader.");
                }
                // Validate even when the completed brush raster can be reused.
                _ = sdlImage.RgbaPixels;
                source = descriptor.Viewbox ?? new DrawRect(0, 0, image.Width, image.Height);
                float left = Math.Clamp(source.X, 0, image.Width);
                float top = Math.Clamp(source.Y, 0, image.Height);
                source = new DrawRect(left, top,
                    Math.Clamp(source.Right, left, image.Width) - left,
                    Math.Clamp(source.Bottom, top, image.Height) - top);
                if (source.Width == 0 || source.Height == 0) { return; }
                break;
            case DrawingDrawBrushDescriptor drawing:
                content = drawing.Commands;
                source = descriptor.Viewbox ?? drawing.ContentBounds;
                break;
            case VisualDrawBrushDescriptor visual:
                content = visual.Commands;
                source = descriptor.Viewbox ?? visual.ContentBounds;
                break;
            default:
                throw new NotSupportedException($"Unsupported tile brush '{descriptor.GetType().Name}'.");
        }

        output.Add(DrawCommand.PushTransform(
            Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y) * Matrix3x2.CreateScale(scale)));
        DrawRect viewport = descriptor.Viewport ?? bounds;
        if (descriptor.TileMode == DrawTileMode.None)
        {
            AddTile(viewport, 0, 0);
        }
        else
        {
            int columns = checked((int)MathF.Ceiling(bounds.Width / viewport.Width));
            int rows = checked((int)MathF.Ceiling(bounds.Height / viewport.Height));
            for (int column = 0; column < columns; column++)
            {
                float x = bounds.X + column * viewport.Width;
                for (int row = 0; row < rows; row++)
                {
                    float y = bounds.Y + row * viewport.Height;
                    AddTile(new DrawRect(x, y,
                        MathF.Min(viewport.Width, bounds.Right - x),
                        MathF.Min(viewport.Height, bounds.Bottom - y)), column, row);
                }
            }
        }
        output.Add(DrawCommand.PopTransform());

        void AddTile(DrawRect tile, int column, int row)
        {
            DrawRect fitted = FitBrushTile(tile, source.Width, source.Height, descriptor);
            bool flipX = column % 2 != 0 && descriptor.TileMode is DrawTileMode.FlipX or DrawTileMode.FlipXY;
            bool flipY = row % 2 != 0 && descriptor.TileMode is DrawTileMode.FlipY or DrawTileMode.FlipXY;
            output.Add(DrawCommand.PushClip(tile));
            if (image is not null)
            {
                DrawImageFlip flip = (flipX ? DrawImageFlip.Horizontal : DrawImageFlip.None) |
                    (flipY ? DrawImageFlip.Vertical : DrawImageFlip.None);
                output.Add(DrawCommand.DrawImage(image, fitted, new DrawImageOptions(source: source, flip: flip)));
            }
            else
            {
                Matrix3x2 transform = Matrix3x2.CreateTranslation(-source.X, -source.Y) *
                    Matrix3x2.CreateScale(
                        fitted.Width / source.Width * (flipX ? -1 : 1),
                        fitted.Height / source.Height * (flipY ? -1 : 1)) *
                    Matrix3x2.CreateTranslation(flipX ? fitted.Right : fitted.X, flipY ? fitted.Bottom : fitted.Y);
                output.Add(DrawCommand.PushTransform(transform));
                foreach (DrawCommand command in content!) { output.Add(command); }
                output.Add(DrawCommand.PopTransform());
            }
            output.Add(DrawCommand.PopClip());
        }
    }

    private static DrawRect FitBrushTile(
        DrawRect tile, float sourceWidth, float sourceHeight, TileDrawBrushDescriptor descriptor)
    {
        if (descriptor.Stretch == DrawBrushStretch.Fill) { return tile; }
        float scale = descriptor.Stretch switch
        {
            DrawBrushStretch.Uniform => MathF.Min(tile.Width / sourceWidth, tile.Height / sourceHeight),
            DrawBrushStretch.UniformToFill => MathF.Max(tile.Width / sourceWidth, tile.Height / sourceHeight),
            _ => 1
        };
        float width = sourceWidth * scale;
        float height = sourceHeight * scale;
        float x = descriptor.AlignmentX switch
        {
            DrawBrushAlignmentX.Left => tile.X,
            DrawBrushAlignmentX.Right => tile.Right - width,
            _ => tile.X + (tile.Width - width) / 2
        };
        float y = descriptor.AlignmentY switch
        {
            DrawBrushAlignmentY.Top => tile.Y,
            DrawBrushAlignmentY.Bottom => tile.Bottom - height,
            _ => tile.Y + (tile.Height - height) / 2
        };
        return new DrawRect(x, y, width, height);
    }

    private static byte[] CreateTextCoveragePixels(RasterizedText[] layers)
    {
        RasterizedText first = layers[0];
        byte[] pixels = new byte[first.PixelLength];
        ReadOnlySpan<byte> red = first.PixelSpan;
        ReadOnlySpan<byte> green = layers[1].PixelSpan;
        ReadOnlySpan<byte> blue = layers[2].PixelSpan;
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            byte coverage = Math.Max(red[offset], Math.Max(green[offset + 1], blue[offset + 2]));
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = coverage;
        }
        return pixels;
    }

    private void ApplyTextCoverage(SdlGpuRenderTarget target, SdlGpuTextureResource texture)
    {
        session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Load);
        batches.Begin(target);
        AddQuad(batches, new DrawRect(0, 0, target.PixelWidth, target.PixelHeight),
            new DrawRect(0, 0, 1, 1), Color.White, Matrix3x2.Identity, 1,
            new CerberusBatchKey(DrawPrimitiveTopology.TriangleList, texture.Handle,
                DrawSamplingMode.Point, DrawAddressMode.Clamp, DrawBlendMode.Normal,
                SdlGpuStencilMode.Disabled, 0, new SdlRect(0, 0, target.PixelWidth, target.PixelHeight),
                SdlGpuColorWriteMask.All, AlphaMask: true));
        FlushBatches();
    }

    private void BeginBrushCaptureFrame()
    {
        foreach (BrushCapture capture in brushCaptures.Values) { capture.Used = false; }
    }

    private void CompleteBrushCaptureFrame()
    {
        unusedBrushCaptures.Clear();
        foreach ((BrushCaptureKey key, BrushCapture capture) in brushCaptures)
        {
            if (!capture.Used) { unusedBrushCaptures.Add(key); }
        }
        foreach (BrushCaptureKey key in unusedBrushCaptures)
        {
            brushCaptures[key].Surface.Dispose();
            brushCaptures.Remove(key);
        }
        unusedBrushCaptures.Clear();
    }

    private void DisposeBrushCaptures()
    {
        foreach (BrushCapture capture in brushCaptures.Values) { capture.Surface.Dispose(); }
        brushCaptures.Clear();
        unusedBrushCaptures.Clear();
        activeBrushCaptures.Clear();
    }

    private sealed class BrushCapture(SdlGpuRenderSurfaceState surface)
    {
        internal SdlGpuRenderSurfaceState Surface { get; } = surface;
        internal bool Used { get; set; }
    }

    private readonly record struct BrushCaptureKey(
        IDrawBrush Brush, DrawRect Bounds, int Width, int Height,
        SdlGpuTextureFormat Format, SdlGpuTextRasterKey? Text);

    private readonly record struct TextCoverageKey(SdlGpuTextRasterKey Raster);
}
