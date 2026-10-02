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
    private void AddText(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
    {
        if (command.TextRun is null)
        {
            return;
        }

        IDrawBrush? brush = command.Brush;
        float commandOpacity = command.BrushOpacity;
        AddTextRun(
            command.TextRun,
            command.Position,
            brush,
            command.Color,
            commandOpacity,
            state,
            batches);
    }

    private void AddTextLayout(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
    {
        if (command.TextLayout is null)
        {
            return;
        }

        foreach (DrawTextLayoutLine line in command.TextLayout.Lines)
        {
            foreach (DrawTextLayoutRun run in line.Runs)
            {
                AddTextRun(
                    run.TextRun,
                    new DrawPoint(
                        command.Position.X + run.Position.X,
                        command.Position.Y + run.Position.Y),
                    run.Brush,
                    Color.White,
                    run.Opacity * command.BrushOpacity,
                    state,
                    batches);
            }
        }
    }

    private void AddTextRun(
        DrawTextRun textRun,
        DrawPoint baseline,
        IDrawBrush? brush,
        Color fallbackColor,
        float commandOpacity,
        RenderState state,
        Cerberus batches)
    {
        long requestCollectionStarted = Stopwatch.GetTimestamp();
        DrawPoint phase = CanonicalPhase(baseline, CoordinateScale);
        DrawBrushDescriptor descriptor = brush?.CreateDescriptor() ??
            new SolidDrawBrushDescriptor(fallbackColor, 1);
        SdlGpuTextRasterKey rasterKey = new(
            textRun.Font is SkiaFont skiaFont ? skiaFont.Typeface : textRun.Font,
            textRun.Text,
            textRun.Size,
            CoordinateScale,
            phase);
        SdlGpuTextLayerTextureKey redKey = new(
            rasterKey,
            SdlGpuColorWriteMask.Red);
        SdlGpuTextLayerTextureKey greenKey = new(
            rasterKey,
            SdlGpuColorWriteMask.Green);
        SdlGpuTextLayerTextureKey blueKey = new(
            rasterKey,
            SdlGpuColorWriteMask.Blue);
        SolidDrawBrushDescriptor? cachedSolid =
            descriptor as SolidDrawBrushDescriptor;
        SdlGpuTextAtlasEntries cachedEntries = default;
        bool atlasHit = cachedSolid is not null &&
            resources.TryGetTextAtlasEntries(
                session,
                redKey,
                greenKey,
                blueKey,
                textAtlasFrameToken,
                out cachedEntries);
        object? brushTextureKey = cachedSolid is not null ? null :
            descriptor is TileDrawBrushDescriptor ? new TextCoverageKey(rasterKey) :
            new SdlGpuTextBrushTextureKey(rasterKey, (object?)brush ?? descriptor);
        SdlGpuTextureResource? cachedBrushTexture = brushTextureKey is null
            ? null : resources.FindTexture(session, brushTextureKey);
        textRequestCollectionTime += Stopwatch.GetElapsedTime(requestCollectionStarted);
        if (atlasHit)
        {
            AddTextAtlasLayers(
                cachedEntries,
                CreateTextDestination(
                    baseline,
                    cachedEntries[0].OriginOffset,
                    cachedEntries[0].Width,
                    cachedEntries[0].Height),
                ApplyOpacity(
                    cachedSolid!.Color,
                    cachedSolid.Opacity * commandOpacity),
                state,
                batches);
            return;
        }

        if (cachedBrushTexture is not null)
        {
            AddBrushTextTexture(cachedBrushTexture, brushTextureKey!, rasterKey,
                baseline, brush, descriptor, commandOpacity, state, batches);
            return;
        }

        if (cachedSolid is not null &&
            resources.FindTexture(session, redKey) is { } redTexture &&
            resources.FindTexture(session, greenKey) is { } greenTexture &&
            resources.FindTexture(session, blueKey) is { } blueTexture)
        {
            DrawRect destination = CreateTextDestination(
                baseline, redTexture.OriginOffset, redTexture.Width, redTexture.Height);
            Color tint = ApplyOpacity(cachedSolid.Color, cachedSolid.Opacity * commandOpacity);
            AddTextLayer(redTexture, redKey, destination, tint, SdlGpuColorWriteMask.Red, state, batches);
            AddTextLayer(greenTexture, greenKey, destination, tint, SdlGpuColorWriteMask.Green, state, batches);
            AddTextLayer(blueTexture, blueKey, destination, tint, SdlGpuColorWriteMask.Blue, state, batches);
            return;
        }

        textRequestCount++;
        long rasterizationStarted = Stopwatch.GetTimestamp();
        RasterizedText[] layers;
        try
        {
            layers = textRasterizer.RasterizeSubpixelAtPhase(
                textRun,
                Color.White,
                CoordinateScale,
                phase);
        }
        finally
        {
            textRasterizationTime += Stopwatch.GetElapsedTime(rasterizationStarted);
        }
        foreach (RasterizedText layer in layers)
        {
            rasterizedPixelCount = checked(
                rasterizedPixelCount + ((long)layer.Width * layer.Height));
        }
        try
        {
            RasterizedText first = layers[0];
            DrawRect destination = CreateTextDestination(
                baseline,
                first.OriginOffset,
                first.Width,
                first.Height);
            if (descriptor is SolidDrawBrushDescriptor solid)
            {
                Color tint = ApplyOpacity(
                    solid.Color,
                    solid.Opacity * commandOpacity);
                SdlGpuTextAtlasEntries? atlasEntries =
                    resources.GetOrCreateTextAtlasEntries(
                        session,
                        redKey,
                        greenKey,
                        blueKey,
                        layers,
                        textAtlasFrameToken);
                if (atlasEntries is not null)
                {
                    AddTextAtlasLayers(
                        atlasEntries.Value,
                        destination,
                        tint,
                        state,
                        batches);
                    return;
                }

                AddTextLayer(
                    layers[0],
                    redKey,
                    destination,
                    tint,
                    SdlGpuColorWriteMask.Red,
                    state,
                    batches);
                AddTextLayer(
                    layers[1],
                    greenKey,
                    destination,
                    tint,
                    SdlGpuColorWriteMask.Green,
                    state,
                    batches);
                AddTextLayer(
                    layers[2],
                    blueKey,
                    destination,
                    tint,
                    SdlGpuColorWriteMask.Blue,
                    state,
                    batches);
                return;
            }

            byte[] pixels = descriptor is TileDrawBrushDescriptor
                ? CreateTextCoveragePixels(layers)
                : ColorizeTextLayers(layers, descriptor, CoordinateScale);
            SdlGpuTextureResource texture = resources.GetOrCreateTexture(
                session,
                brushTextureKey!,
                first.Width,
                first.Height,
                pixels,
                first.OriginOffset);
            AddBrushTextTexture(texture, brushTextureKey!, rasterKey,
                baseline, brush, descriptor, commandOpacity, state, batches);
        }
        finally
        {
            foreach (RasterizedText layer in layers)
            {
                layer.ReturnPixelBuffer();
            }
        }
    }

    private void AddBrushTextTexture(
        SdlGpuTextureResource texture,
        object textureKey,
        SdlGpuTextRasterKey rasterKey,
        DrawPoint baseline,
        IDrawBrush? brush,
        DrawBrushDescriptor descriptor,
        float commandOpacity,
        RenderState state,
        Cerberus batches)
    {
        MarkBrushTextureUsed(textureKey);
        DrawRect destination = CreateTextDestination(
            baseline, texture.OriginOffset, texture.Width, texture.Height);
        nint handle = texture.Handle;
        Color tint = ApplyOpacity(Color.White, commandOpacity);
        DrawSamplingMode sampling = DrawSamplingMode.Linear;
        DrawAddressMode addressMode = DrawAddressMode.Clamp;
        if (descriptor is TileDrawBrushDescriptor tile)
        {
            SdlGpuPaint paint = ResolveTileBrushPaint(brush!, tile,
                new DrawRect(0, 0, destination.Width, destination.Height),
                commandOpacity, rasterKey, texture);
            handle = paint.Texture;
            tint = paint.Tint;
            sampling = paint.Sampling;
            addressMode = paint.AddressMode;
        }
        AddQuad(batches, destination, new DrawRect(0, 0, 1, 1), tint,
            state.Transform, state.Opacity,
            CreateBatchKey(DrawPrimitiveTopology.TriangleList, handle,
                sampling, addressMode, state));
    }

    private void FlushPendingTextAtlasUploads()
    {
        if (!resources.HasPendingTextAtlasUploads)
        {
            return;
        }

        long uploadStarted = Stopwatch.GetTimestamp();
        try
        {
            resources.FlushTextAtlasUploads(session);
        }
        finally
        {
            textAtlasUploadTime += Stopwatch.GetElapsedTime(uploadStarted);
        }
    }

    private DrawRect CreateTextDestination(
        DrawPoint baseline,
        DrawPoint originOffset,
        int width,
        int height)
    {
        float left = MathF.Round(
            (baseline.X * CoordinateScale) + originOffset.X) /
            CoordinateScale;
        float top = MathF.Round(
            (baseline.Y * CoordinateScale) + originOffset.Y) /
            CoordinateScale;
        return new DrawRect(
            left,
            top,
            width / CoordinateScale,
            height / CoordinateScale);
    }

    private void AddTextAtlasLayers(
        SdlGpuTextAtlasEntries entries,
        DrawRect destination,
        Color tint,
        RenderState state,
        Cerberus batches)
    {
        for (int i = 0; i < 3; i++)
        {
            SdlGpuTextAtlasEntry entry = entries[i];
            SdlGpuColorWriteMask channel = i switch
            {
                0 => SdlGpuColorWriteMask.Red,
                1 => SdlGpuColorWriteMask.Green,
                _ => SdlGpuColorWriteMask.Blue
            };
            AddQuad(
                batches,
                destination,
                entry.TextureCoordinates,
                tint,
                state.Transform,
                state.Opacity,
                CreateBatchKey(
                    DrawPrimitiveTopology.TriangleList,
                    entry.Texture.Handle,
                    DrawSamplingMode.Linear,
                    DrawAddressMode.Clamp,
                    state,
                    channel));
        }
    }

    private void AddTextLayer(
        RasterizedText layer,
        object textureKey,
        DrawRect destination,
        Color tint,
        SdlGpuColorWriteMask colorWriteMask,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuTextureResource texture = resources.GetOrCreateTexture(
            session,
            textureKey,
            layer.Width,
            layer.Height,
            layer.PixelSpan,
            layer.OriginOffset);
        AddTextLayer(texture, textureKey, destination, tint, colorWriteMask, state, batches);
    }

    private void AddTextLayer(
        SdlGpuTextureResource texture,
        object textureKey,
        DrawRect destination,
        Color tint,
        SdlGpuColorWriteMask colorWriteMask,
        RenderState state,
        Cerberus batches)
    {
        // Standalone coverage follows the same shared-device, per-backend
        // leases as brush text; it must not live until device disposal.
        MarkBrushTextureUsed(textureKey);
        AddQuad(
            batches,
            destination,
            new DrawRect(0, 0, 1, 1),
            tint,
            state.Transform,
            state.Opacity,
            CreateBatchKey(
                DrawPrimitiveTopology.TriangleList,
                texture.Handle,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                state,
                colorWriteMask));
    }

    private static byte[] ColorizeTextLayers(
        RasterizedText[] layers,
        DrawBrushDescriptor descriptor,
        float coordinateScale)
    {
        RasterizedText first = layers[0];
        ReadOnlySpan<byte> red = first.PixelSpan;
        ReadOnlySpan<byte> green = layers[1].PixelSpan;
        ReadOnlySpan<byte> blue = layers[2].PixelSpan;
        byte[] output = new byte[first.PixelLength];
        for (int offset = 0; offset < output.Length; offset += 4)
        {
            int pixel = offset / 4;
            int x = pixel % first.Width;
            int y = pixel / first.Width;
            int coverage = Math.Max(red[offset], Math.Max(green[offset + 1], blue[offset + 2]));
            DrawPoint point = new(
                (x + 0.5f) / coordinateScale,
                (y + 0.5f) / coordinateScale);
            Color sampled = SampleBrush(descriptor, point);
            byte alpha = MultiplyByte(sampled.A, (byte)coverage);
            output[offset] = MultiplyByte(sampled.R, alpha);
            output[offset + 1] = MultiplyByte(sampled.G, alpha);
            output[offset + 2] = MultiplyByte(sampled.B, alpha);
            output[offset + 3] = alpha;
        }
        return output;
    }

    private static DrawPoint CanonicalPhase(DrawPoint point, float scale)
        => new(
            CanonicalizePixelPhase(point.X * scale),
            CanonicalizePixelPhase(point.Y * scale));

    private static float CanonicalizePixelPhase(float physicalPosition)
    {
        float phase = physicalPosition - MathF.Floor(physicalPosition);
        int bucket = (int)MathF.Floor(
            (phase * TextSubpixelPhaseCount) + 0.5f);
        bucket %= TextSubpixelPhaseCount;
        return bucket / (float)TextSubpixelPhaseCount;
    }

    private readonly record struct SdlGpuTextBrushTextureKey(
        SdlGpuTextRasterKey Raster,
        object Brush);
}
