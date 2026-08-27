using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Paths;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuDrawingBackend : IDrawingBackend, IDisposable
{
    private const int TextSubpixelPhaseCount = 8;
    private static readonly object WhiteTextureKey = new();
    private static readonly IReadOnlySet<DrawCommandKind> CommandKinds =
        new HashSet<DrawCommandKind>
        {
            DrawCommandKind.FillRectangle,
            DrawCommandKind.DrawRectangle,
            DrawCommandKind.FillRoundedRectangle,
            DrawCommandKind.DrawRoundedRectangle,
            DrawCommandKind.FillEllipse,
            DrawCommandKind.DrawEllipse,
            DrawCommandKind.DrawLine,
            DrawCommandKind.FillPath,
            DrawCommandKind.DrawText,
            DrawCommandKind.DrawImage,
            DrawCommandKind.DrawImageQuad,
            DrawCommandKind.DrawNineSlice,
            DrawCommandKind.DrawMesh,
            DrawCommandKind.DrawPointBatch,
            DrawCommandKind.DrawLineBatch,
            DrawCommandKind.DrawSpriteBatch,
            DrawCommandKind.RenderSurface2D,
            DrawCommandKind.PushClip,
            DrawCommandKind.PopClip,
            DrawCommandKind.BeginPrism,
            DrawCommandKind.EndPrism,
            DrawCommandKind.DrawPath,
            DrawCommandKind.DrawTextLayout,
            DrawCommandKind.PushTransform,
            DrawCommandKind.PopTransform,
            DrawCommandKind.PushPathClip,
            DrawCommandKind.PushOpacity,
            DrawCommandKind.PopOpacity,
            DrawCommandKind.PushBlend,
            DrawCommandKind.PopBlend,
            DrawCommandKind.PushLayer,
            DrawCommandKind.PopLayer
        };

    private readonly SdlGpuWindowGraphicsSession session;
    private readonly SdlGpuDrawingResources resources;
    private readonly SkiaTextRasterizer textRasterizer = new();
    private readonly SdlGpuPrismExecutor prismExecutor;
    private readonly HashSet<SdlGpuImage> subscribedImages =
        new(ReferenceEqualityComparer.Instance);
    private long textAtlasFrameToken;
    private bool frameActive;
    private bool disposed;

    public SdlGpuDrawingBackend(SdlGpuWindowGraphicsSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        resources = session.DrawingResources;
        CoordinateScale = session.CoordinateScale;
        prismExecutor = new SdlGpuPrismExecutor(session, this);
    }

    internal static IReadOnlySet<DrawCommandKind> HandledCommandKinds => CommandKinds;

    internal float CoordinateScale { get; set; }

    internal PrismExecutionDiagnostics PrismDiagnostics => prismExecutor.Diagnostics;

    public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!frameActive || !session.IsFrameActive)
        {
            throw new InvalidOperationException(
                "SDL_GPU drawing requires an active window frame.");
        }
        if (session.IsSuspended || commands.Count == 0)
        {
            return;
        }

        frameContext.EnsureCurrent(commands);
        if (!frameContext.PrismAnalysis.Scopes.IsDefaultOrEmpty)
        {
            prismExecutor.Execute(commands, frameContext);
            return;
        }

        DrawCommandStateAnalysis analysis = frameContext.StateAnalysis;
        SdlGpuRenderTarget target = session.WindowRenderTarget;
        RenderState state = RenderState.Create(target, CoordinateScale);
        BatchCollector batches = new(this, target);
        RenderRange(commands, 0, commands.Count, analysis, state, target, batches, 0);
        batches.Flush();
    }

    internal void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        textAtlasFrameToken = resources.BeginTextAtlasFrame();
        frameActive = true;
    }

    internal void EndFrame()
    {
        frameActive = false;
        resources.EndTextAtlasFrame(textAtlasFrameToken);
        textAtlasFrameToken = 0;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        prismExecutor.Dispose();
        resources.EndTextAtlasFrame(textAtlasFrameToken);
        textAtlasFrameToken = 0;
        foreach (SdlGpuImage image in subscribedImages)
        {
            image.ContentChanged -= OnImageContentChanged;
        }
        subscribedImages.Clear();
    }

    private void RenderRange(
        DrawCommandList commands,
        int start,
        int end,
        DrawCommandStateAnalysis analysis,
        RenderState state,
        SdlGpuRenderTarget target,
        BatchCollector batches,
        int layerDepth,
        IReadOnlyDictionary<int, SdlGpuPrismPresentationSurface>? childSurfaces = null)
    {
        for (int index = start; index < end; index++)
        {
            DrawCommand command = commands[index];
            switch (command.Kind)
            {
                case DrawCommandKind.FillRectangle:
                    AddFillRectangle(command, state, batches);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                case DrawCommandKind.FillPath:
                    AddPathFill(command, state, batches);
                    break;
                case DrawCommandKind.FillEllipse:
                    AddEllipseFill(command, state, batches);
                    break;
                case DrawCommandKind.DrawRectangle:
                case DrawCommandKind.DrawRoundedRectangle:
                case DrawCommandKind.DrawEllipse:
                case DrawCommandKind.DrawLine:
                case DrawCommandKind.DrawPath:
                    AddStroke(command, state, batches);
                    break;
                case DrawCommandKind.DrawImage:
                    AddImage(command, state, batches);
                    break;
                case DrawCommandKind.DrawImageQuad:
                case DrawCommandKind.DrawNineSlice:
                case DrawCommandKind.DrawMesh:
                case DrawCommandKind.DrawPointBatch:
                case DrawCommandKind.DrawLineBatch:
                case DrawCommandKind.DrawSpriteBatch:
                    AddCommandMesh(command, state, batches);
                    break;
                case DrawCommandKind.DrawText:
                    AddText(command, state, batches);
                    break;
                case DrawCommandKind.DrawTextLayout:
                    AddTextLayout(command, state, batches);
                    break;
                case DrawCommandKind.RenderSurface2D:
                    batches.Flush();
                    AddRenderSurface(command, state, target, batches);
                    break;
                case DrawCommandKind.PushTransform:
                    state.Transforms.Add(Matrix3x2.Multiply(
                        command.Transform,
                        state.Transforms[^1]));
                    break;
                case DrawCommandKind.PopTransform:
                    state.Transforms.RemoveAt(state.Transforms.Count - 1);
                    break;
                case DrawCommandKind.PushOpacity:
                    state.Opacities.Add(state.Opacities[^1] * command.Opacity);
                    break;
                case DrawCommandKind.PopOpacity:
                    state.Opacities.RemoveAt(state.Opacities.Count - 1);
                    break;
                case DrawCommandKind.PushBlend:
                    state.Blends.Add(command.BlendMode);
                    break;
                case DrawCommandKind.PopBlend:
                    state.Blends.RemoveAt(state.Blends.Count - 1);
                    break;
                case DrawCommandKind.PushClip:
                    PushRectangleClip(command, state, batches);
                    break;
                case DrawCommandKind.PushPathClip:
                    PushPathClip(command, state, batches);
                    break;
                case DrawCommandKind.PopClip:
                    PopClip(state, batches);
                    break;
                case DrawCommandKind.PushLayer:
                {
                    int matching = analysis.Entries[index].MatchingCommandIndex;
                    if (matching <= index || matching >= end)
                    {
                        throw new InvalidOperationException(
                            $"PushLayer at command index {index} has no valid matching PopLayer.");
                    }
                    batches.Flush();
                    RenderLayer(
                        commands,
                        index + 1,
                        matching,
                        analysis,
                        state,
                        target,
                        command.LayerOptions!,
                        layerDepth + 1,
                        batches,
                        childSurfaces);
                    index = matching;
                    break;
                }
                case DrawCommandKind.PopLayer:
                    throw new InvalidOperationException(
                        $"Unexpected PopLayer at command index {index}.");
                case DrawCommandKind.BeginPrism:
                    if (childSurfaces is not null &&
                        childSurfaces.TryGetValue(index, out SdlGpuPrismPresentationSurface child))
                    {
                        int matching = analysis.Entries[index].MatchingCommandIndex;
                        if (matching <= index || matching >= end)
                        {
                            throw new InvalidOperationException(
                                $"BeginPrism at command index {index} has no valid matching EndPrism.");
                        }
                        batches.Flush();
                        DrawPrismTexture(
                            child.Target.SampleTexture,
                            target,
                            child.Clip);
                        index = matching;
                    }
                    break;
                case DrawCommandKind.EndPrism:
                    break;
                default:
                    throw new NotSupportedException(
                        $"SDL_GPU does not handle draw command '{command.Kind}'.");
            }
        }
    }

    private void AddFillRectangle(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        DrawPoint[] points = RectanglePoints(command.Rect);
        int[] indices = [0, 1, 2, 0, 2, 3];
        AddPaintedGeometry(
            points,
            points,
            indices,
            DrawPrimitiveTopology.TriangleList,
            command.Rect,
            command.Brush,
            command.BrushOpacity,
            command.Color,
            state,
            batches);
    }

    private void AddPathFill(
        DrawCommand command,
        RenderState state,
        BatchCollector batches,
        DrawRect? destinationOverride = null)
    {
        DrawPath path = command.Path ??
            throw new InvalidOperationException($"{command.Kind} requires path geometry.");
        DrawRect destination = destinationOverride ?? command.Rect;
        DrawTriangleMesh mesh = DrawPathMeshBuilder.Build(
            path,
            command.SourceRect,
            destination.Width * CoordinateScale,
            destination.Height * CoordinateScale,
            destination.X * CoordinateScale,
            destination.Y * CoordinateScale,
            command.FillRule);
        if (mesh.IsEmpty)
        {
            return;
        }

        DrawPoint[] logical = new DrawPoint[mesh.Vertices.Length];
        for (int i = 0; i < logical.Length; i++)
        {
            logical[i] = new DrawPoint(
                mesh.Vertices[i].X / CoordinateScale,
                mesh.Vertices[i].Y / CoordinateScale);
        }
        AddPaintedGeometry(
            logical,
            logical,
            mesh.Indices,
            DrawPrimitiveTopology.TriangleList,
            command.Rect,
            command.Brush,
            command.BrushOpacity,
            command.Color,
            state,
            batches);
    }

    private void AddEllipseFill(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        AddPathFill(
            command,
            state,
            batches,
            DrawEllipseCoverage.AdjustBounds(
                command.Rect,
                CoordinateScale));
    }

    private void AddStroke(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        DrawStrokeRenderMesh stroke = DrawStrokeMeshBuilder.Build(
            command,
            command.Pen?.Thickness ?? command.Thickness,
            command.Pen?.Style ?? DrawStrokeStyle.Default,
            CoordinateScale);
        if (stroke.Mesh.IsEmpty)
        {
            return;
        }

        DrawPoint[] logical = new DrawPoint[stroke.Mesh.Vertices.Length];
        for (int i = 0; i < logical.Length; i++)
        {
            logical[i] = new DrawPoint(
                (stroke.Mesh.Vertices[i].X + stroke.Left) / CoordinateScale,
                (stroke.Mesh.Vertices[i].Y + stroke.Top) / CoordinateScale);
        }
        DrawRect bounds = command.Kind == DrawCommandKind.DrawLine
            ? BoundsOf(stroke.BrushPoints)
            : command.Rect;
        AddPaintedGeometry(
            logical,
            stroke.BrushPoints,
            stroke.Mesh.Indices,
            DrawPrimitiveTopology.TriangleList,
            bounds,
            command.Pen?.Brush ?? command.Brush,
            command.BrushOpacity,
            command.Color,
            state,
            batches);
    }

    private void AddImage(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        IDrawImage image = command.Image ??
            throw new InvalidOperationException("DrawImage requires an image.");
        DrawImageOptions options = command.ImageOptions ?? new DrawImageOptions();
        DrawPoint[] points = DrawImageGeometry.GetDestinationCorners(
            image,
            command.Rect,
            options);
        DrawPoint[] textureCoordinates = DrawImageGeometry.GetTextureCoordinates(
            image,
            options);
        Color tint = DrawImageGeometry.EffectiveTint(options);
        DrawVertex2D[] vertices = new DrawVertex2D[4];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new DrawVertex2D(points[i], tint, textureCoordinates[i]);
        }
        AddImageGeometry(
            vertices,
            [0, 1, 2, 0, 2, 3],
            DrawPrimitiveTopology.TriangleList,
            image,
            options.Sampling,
            options.AddressMode,
            state,
            batches);
    }

    private void AddCommandMesh(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        DrawMesh2D mesh = command.Mesh ??
            throw new InvalidOperationException($"{command.Kind} requires a mesh.");
        DrawImageOptions options = command.ImageOptions ?? new DrawImageOptions();
        if (mesh.Image is IDrawImage image)
        {
            AddImageGeometry(
                mesh.VertexArray,
                mesh.IndexArray,
                mesh.Topology,
                image,
                options.Sampling,
                options.AddressMode,
                state,
                batches);
            return;
        }

        SdlGpuTextureResource white = GetWhiteTexture();
        SdlGpuVertex[] vertices = new SdlGpuVertex[mesh.VertexArray.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            DrawVertex2D source = mesh.VertexArray[i];
            vertices[i] = CreateVertex(
                source.Position,
                source.TextureCoordinate,
                source.Color,
                state.Transform,
                state.Opacity);
        }
        batches.Add(new CpuDrawBatch(
            vertices,
            mesh.IndexArray,
            mesh.Topology,
            white.Handle,
            DrawSamplingMode.Point,
            DrawAddressMode.Clamp,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor));
    }

    private void AddImageGeometry(
        IReadOnlyList<DrawVertex2D> sourceVertices,
        IReadOnlyList<int> sourceIndices,
        DrawPrimitiveTopology topology,
        IDrawImage image,
        DrawSamplingMode sampling,
        DrawAddressMode addressMode,
        RenderState state,
        BatchCollector batches)
    {
        SdlGpuTextureResource texture = GetImageTexture(image);
        SdlGpuVertex[] vertices = new SdlGpuVertex[sourceVertices.Count];
        for (int i = 0; i < vertices.Length; i++)
        {
            DrawVertex2D source = sourceVertices[i];
            vertices[i] = CreateVertex(
                source.Position,
                source.TextureCoordinate,
                source.Color,
                state.Transform,
                state.Opacity);
        }
        batches.Add(new CpuDrawBatch(
            vertices,
            sourceIndices.ToArray(),
            topology,
            texture.Handle,
            sampling,
            addressMode,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor));
    }

    private void AddPaintedGeometry(
        IReadOnlyList<DrawPoint> positions,
        IReadOnlyList<DrawPoint> brushPoints,
        IReadOnlyList<int> indices,
        DrawPrimitiveTopology topology,
        DrawRect bounds,
        IDrawBrush? brush,
        float commandOpacity,
        Color fallbackColor,
        RenderState state,
        BatchCollector batches)
    {
        SdlGpuPaint paint = ResolvePaint(
            brush,
            bounds,
            fallbackColor,
            commandOpacity);
        SdlGpuVertex[] vertices = new SdlGpuVertex[positions.Count];
        for (int i = 0; i < vertices.Length; i++)
        {
            DrawPoint textureCoordinate = paint.MapTextureCoordinate(
                brushPoints[Math.Min(i, brushPoints.Count - 1)]);
            vertices[i] = CreateVertex(
                positions[i],
                textureCoordinate,
                paint.Tint,
                state.Transform,
                state.Opacity);
        }
        batches.Add(new CpuDrawBatch(
            vertices,
            indices.ToArray(),
            topology,
            paint.Texture.Handle,
            paint.Sampling,
            paint.AddressMode,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor));
    }

    private void AddText(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
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
        BatchCollector batches)
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
        BatchCollector batches)
    {
        DrawPoint phase = CanonicalPhase(baseline, CoordinateScale);
        DrawBrushDescriptor descriptor = brush?.CreateDescriptor() ??
            new SolidDrawBrushDescriptor(fallbackColor, 1);
        SdlGpuTextRasterKey rasterKey = new(
            RuntimeHelpers.GetHashCode(textRun.Font),
            textRun.Text,
            textRun.Size,
            CoordinateScale,
            phase);
        object[] layerKeys =
        [
            new SdlGpuTextLayerTextureKey(rasterKey, SdlGpuColorWriteMask.Red),
            new SdlGpuTextLayerTextureKey(rasterKey, SdlGpuColorWriteMask.Green),
            new SdlGpuTextLayerTextureKey(rasterKey, SdlGpuColorWriteMask.Blue)
        ];
        if (descriptor is SolidDrawBrushDescriptor cachedSolid &&
            resources.TryGetTextAtlasEntries(
                layerKeys,
                textAtlasFrameToken,
                out SdlGpuTextAtlasEntry[] cachedEntries))
        {
            AddTextAtlasLayers(
                cachedEntries,
                CreateTextDestination(
                    baseline,
                    cachedEntries[0].OriginOffset,
                    cachedEntries[0].Width,
                    cachedEntries[0].Height),
                ApplyOpacity(
                    cachedSolid.Color,
                    cachedSolid.Opacity * commandOpacity),
                state,
                batches);
            return;
        }

        RasterizedText[] layers = textRasterizer.RasterizeSubpixelAtPhase(
            textRun,
            Color.White,
            CoordinateScale,
            phase);
        try
        {
            RasterizedText first = layers[0];
            DrawRect destination = CreateTextDestination(
                baseline,
                first.OriginOffset,
                first.Width,
                first.Height);
            DrawPoint[] positions = RectanglePoints(destination);
            DrawPoint[] uv =
            [
                new DrawPoint(0, 0),
                new DrawPoint(1, 0),
                new DrawPoint(1, 1),
                new DrawPoint(0, 1)
            ];
            if (descriptor is SolidDrawBrushDescriptor solid)
            {
                Color tint = ApplyOpacity(
                    solid.Color,
                    solid.Opacity * commandOpacity);
                SdlGpuTextAtlasEntry[]? atlasEntries =
                    resources.GetOrCreateTextAtlasEntries(
                        session,
                        layerKeys,
                        layers,
                        textAtlasFrameToken);
                if (atlasEntries is not null)
                {
                    AddTextAtlasLayers(
                        atlasEntries,
                        destination,
                        tint,
                        state,
                        batches);
                    return;
                }

                AddTextLayer(
                    layers[0],
                    layerKeys[0],
                    positions,
                    uv,
                    tint,
                    SdlGpuColorWriteMask.Red,
                    state,
                    batches);
                AddTextLayer(
                    layers[1],
                    layerKeys[1],
                    positions,
                    uv,
                    tint,
                    SdlGpuColorWriteMask.Green,
                    state,
                    batches);
                AddTextLayer(
                    layers[2],
                    layerKeys[2],
                    positions,
                    uv,
                    tint,
                    SdlGpuColorWriteMask.Blue,
                    state,
                    batches);
                return;
            }

            SdlGpuTextBrushTextureKey brushKey = new(
                rasterKey,
                (object?)brush ?? descriptor,
                baseline);
            byte[] pixels = ColorizeTextLayers(
                layers,
                descriptor,
                baseline,
                first.OriginOffset,
                CoordinateScale);
            SdlGpuTextureResource texture = resources.GetOrCreateTexture(
                session,
                brushKey,
                first.Width,
                first.Height,
                pixels);
            SdlGpuVertex[] vertices = CreateTextVertices(
                positions,
                uv,
                ApplyOpacity(Color.White, commandOpacity),
                state);
            batches.Add(new CpuDrawBatch(
                vertices,
                [0, 1, 2, 0, 2, 3],
                DrawPrimitiveTopology.TriangleList,
                texture.Handle,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                state.Blend,
                state.StencilMode,
                state.StencilDepth,
                state.Scissor));
        }
        finally
        {
            foreach (RasterizedText layer in layers)
            {
                layer.ReturnPixelBuffer();
            }
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
        IReadOnlyList<SdlGpuTextAtlasEntry> entries,
        DrawRect destination,
        Color tint,
        RenderState state,
        BatchCollector batches)
    {
        DrawPoint[] positions = RectanglePoints(destination);
        SdlGpuColorWriteMask[] channels =
        [
            SdlGpuColorWriteMask.Red,
            SdlGpuColorWriteMask.Green,
            SdlGpuColorWriteMask.Blue
        ];
        for (int i = 0; i < entries.Count; i++)
        {
            SdlGpuTextAtlasEntry entry = entries[i];
            batches.Add(new CpuDrawBatch(
                CreateTextVertices(
                    positions,
                    RectanglePoints(entry.TextureCoordinates),
                    tint,
                    state),
                [0, 1, 2, 0, 2, 3],
                DrawPrimitiveTopology.TriangleList,
                entry.Texture.Handle,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                state.Blend,
                state.StencilMode,
                state.StencilDepth,
                state.Scissor,
                channels[i]));
        }
    }

    private void AddTextLayer(
        RasterizedText layer,
        object textureKey,
        IReadOnlyList<DrawPoint> positions,
        IReadOnlyList<DrawPoint> textureCoordinates,
        Color tint,
        SdlGpuColorWriteMask colorWriteMask,
        RenderState state,
        BatchCollector batches)
    {
        SdlGpuTextureResource texture = resources.GetOrCreateTexture(
            session,
            textureKey,
            layer.Width,
            layer.Height,
            layer.PixelSpan);
        batches.Add(new CpuDrawBatch(
            CreateTextVertices(positions, textureCoordinates, tint, state),
            [0, 1, 2, 0, 2, 3],
            DrawPrimitiveTopology.TriangleList,
            texture.Handle,
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor,
            colorWriteMask));
    }

    private static SdlGpuVertex[] CreateTextVertices(
        IReadOnlyList<DrawPoint> positions,
        IReadOnlyList<DrawPoint> textureCoordinates,
        Color tint,
        RenderState state)
    {
        SdlGpuVertex[] vertices = new SdlGpuVertex[positions.Count];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = CreateVertex(
                positions[i],
                textureCoordinates[i],
                tint,
                state.Transform,
                state.Opacity);
        }
        return vertices;
    }

    private void PushRectangleClip(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        Matrix3x2 transform = state.Transform;
        if (MathF.Abs(transform.M12) <= 0.00001f &&
            MathF.Abs(transform.M21) <= 0.00001f)
        {
            DrawRect world = DrawCommandStateAnalyzer.TransformBounds(
                command.Rect,
                transform);
            SdlRect next = IntersectScissor(
                state.Scissor,
                ToScissor(world, CoordinateScale));
            state.Clips.Add(ClipEntry.ForScissor(state.Scissor));
            state.Scissors.Add(next);
            return;
        }

        DrawPoint[] points = RectanglePoints(command.Rect);
        PushStencilClip(points, [0, 1, 2, 0, 2, 3], state, batches);
    }

    private void PushPathClip(
        DrawCommand command,
        RenderState state,
        BatchCollector batches)
    {
        DrawPath path = command.Path ??
            throw new InvalidOperationException("PushPathClip requires a path.");
        DrawTriangleMesh mesh = DrawPathMeshBuilder.Build(
            path,
            command.SourceRect,
            command.Rect.Width * CoordinateScale,
            command.Rect.Height * CoordinateScale,
            command.Rect.X * CoordinateScale,
            command.Rect.Y * CoordinateScale,
            command.FillRule);
        DrawPoint[] logical = new DrawPoint[mesh.Vertices.Length];
        for (int i = 0; i < logical.Length; i++)
        {
            logical[i] = new DrawPoint(
                mesh.Vertices[i].X / CoordinateScale,
                mesh.Vertices[i].Y / CoordinateScale);
        }
        PushStencilClip(logical, mesh.Indices, state, batches);
    }

    private void PushStencilClip(
        DrawPoint[] points,
        int[] indices,
        RenderState state,
        BatchCollector batches)
    {
        SdlGpuTextureResource white = GetWhiteTexture();
        SdlGpuVertex[] vertices = CreateSolidVertices(
            points,
            Color.White,
            state.Transform,
            opacity: 1);
        CpuDrawBatch clip = new(
            vertices,
            indices,
            DrawPrimitiveTopology.TriangleList,
            white.Handle,
            DrawSamplingMode.Point,
            DrawAddressMode.Clamp,
            DrawBlendMode.Normal,
            SdlGpuStencilMode.Increment,
            state.StencilDepth,
            state.Scissor);
        batches.Add(clip);
        state.Clips.Add(ClipEntry.ForStencil(clip));
        state.StencilDepth++;
    }

    private static void PopClip(RenderState state, BatchCollector batches)
    {
        +
        state.Clips.RemoveAt(state.Clips.Count - 1);
        if (clip.PreviousScissor is SdlRect)
        {
            state.Scissors.RemoveAt(state.Scissors.Count - 1);
            return;
        }

        state.StencilDepth--;
        CpuDrawBatch original = clip.StencilBatch ??
            throw new InvalidOperationException("Stencil clip state is incomplete.");
        batches.Add(original with
        {
            StencilMode = SdlGpuStencilMode.Decrement,
            StencilReference = checked((byte)(state.StencilDepth + 1)),
            Scissor = state.Scissor
        });
    }

    private void RenderLayer(
        DrawCommandList commands,
        int start,
        int end,
        DrawCommandStateAnalysis analysis,
        RenderState parentState,
        SdlGpuRenderTarget parentTarget,
        DrawLayerOptions options,
        int layerDepth,
        BatchCollector parentBatches,
        IReadOnlyDictionary<int, SdlGpuPrismPresentationSurface>? childSurfaces = null)
    {
        SdlGpuRenderTarget layer = resources.GetLayerTarget(
            layerDepth,
            parentTarget.PixelWidth,
            parentTarget.PixelHeight,
            parentTarget.ColorFormat,
            SdlGpuSampleCount.One);
        session.BeginRenderTarget(layer, Color.Transparent, SdlGpuLoadOp.Clear);
        RenderState childState = RenderState.Create(layer, CoordinateScale);
        childState.Transforms[0] = parentState.Transform;
        BatchCollector childBatches = new(this, layer);
        RenderRange(
            commands,
            start,
            end,
            analysis,
            childState,
            layer,
            childBatches,
            layerDepth,
            childSurfaces);
        childBatches.Flush();
        session.BeginRenderTarget(parentTarget, Color.Transparent, SdlGpuLoadOp.Load);

        AddTargetComposite(
            layer,
            parentTarget,
            parentState,
            options.Opacity * parentState.Opacity,
            options.BlendMode,
            parentBatches);
    }

    private void AddRenderSurface(
        DrawCommand command,
        RenderState state,
        SdlGpuRenderTarget parentTarget,
        BatchCollector parentBatches)
    {
        IRenderSurface2DFrameSource source = command.RenderSurface as IRenderSurface2DFrameSource ??
            throw new InvalidOperationException(
                "RenderSurface2D requires a frame-producing source.");
        int width = Math.Max(1, checked((int)MathF.Ceiling(
            command.Rect.Width * CoordinateScale)));
        int height = Math.Max(1, checked((int)MathF.Ceiling(
            command.Rect.Height * CoordinateScale)));
        SdlGpuRenderSurfaceState? surface =
            source.GetBackendState(resources) as SdlGpuRenderSurfaceState;
        if (surface is null || surface.PixelWidth != width || surface.PixelHeight != height)
        {
            surface?.Dispose();
            surface = new SdlGpuRenderSurfaceState(
                resources,
                resources.CreateRenderTarget(
                    width,
                    height,
                    parentTarget.ColorFormat,
                    parentTarget.SampleCount));
            source.SetBackendState(resources, surface);
        }

        if (surface.FrameVersion != source.FrameVersion)
        {
            surface.Commands.Clear();
            source.RecordFrame(
                surface.Commands,
                new DrawRect(
                    0,
                    0,
                    width / CoordinateScale,
                    height / CoordinateScale));
            DrawCommandStateAnalysis surfaceAnalysis =
                new DrawCommandStateAnalyzer().Analyze(surface.Commands);
            session.BeginRenderTarget(
                surface.Target,
                source.ClearColor,
                SdlGpuLoadOp.Clear);
            RenderState surfaceState = RenderState.Create(
                surface.Target,
                CoordinateScale);
            BatchCollector surfaceBatches = new(this, surface.Target);
            RenderRange(
                surface.Commands,
                0,
                surface.Commands.Count,
                surfaceAnalysis,
                surfaceState,
                surface.Target,
                surfaceBatches,
                layerDepth: 0);
            surfaceBatches.Flush();
            surface.FrameVersion = source.FrameVersion;
            session.BeginRenderTarget(
                parentTarget,
                Color.Transparent,
                SdlGpuLoadOp.Load);
        }

        DrawPoint[] points = RectanglePoints(command.Rect);
        DrawPoint[] uv =
        [
            new DrawPoint(0, 0),
            new DrawPoint(1, 0),
            new DrawPoint(1, 1),
            new DrawPoint(0, 1)
        ];
        SdlGpuVertex[] vertices = new SdlGpuVertex[4];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = CreateVertex(
                points[i],
                uv[i],
                command.Color,
                state.Transform,
                state.Opacity);
        }
        parentBatches.Add(new CpuDrawBatch(
            vertices,
            [0, 1, 2, 0, 2, 3],
            DrawPrimitiveTopology.TriangleList,
            surface.Target.SampleTexture,
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor));
    }

    private void AddTargetComposite(
        SdlGpuRenderTarget source,
        SdlGpuRenderTarget destination,
        RenderState state,
        float opacity,
        DrawBlendMode blend,
        BatchCollector batches)
    {
        float logicalWidth = destination.PixelWidth / CoordinateScale;
        float logicalHeight = destination.PixelHeight / CoordinateScale;
        DrawPoint[] positions = RectanglePoints(
            new DrawRect(0, 0, logicalWidth, logicalHeight));
        DrawPoint[] uv =
        [
            new DrawPoint(0, 0),
            new DrawPoint(1, 0),
            new DrawPoint(1, 1),
            new DrawPoint(0, 1)
        ];
        SdlGpuVertex[] vertices = new SdlGpuVertex[4];
        Color tint = ApplyOpacity(Color.White, opacity);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = CreateVertex(
                positions[i],
                uv[i],
                tint,
                Matrix3x2.Identity,
                opacity: 1);
        }
        batches.Add(new CpuDrawBatch(
            vertices,
            [0, 1, 2, 0, 2, 3],
            DrawPrimitiveTopology.TriangleList,
            source.SampleTexture,
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp,
            blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor));
    }

    internal void RenderCommandRange(
        DrawCommandList commands,
        int start,
        int end,
        DrawCommandStateAnalysis analysis,
        SdlGpuRenderTarget target,
        IReadOnlyDictionary<int, SdlGpuPrismPresentationSurface>? childSurfaces)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(analysis);
        if (start < 0 || end < start || end > commands.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        if (start == end)
        {
            return;
        }

        RenderState state = RenderState.Create(target, CoordinateScale);
        DrawCommandStateEntry entry = analysis.Entries[start];
        state.Transforms[0] = entry.Transform;
        state.Opacities[0] = entry.Opacity;
        state.Blends[0] = entry.BlendMode;
        if (entry.ClipBounds is DrawRect clip)
        {
            state.Scissors[0] = IntersectScissor(
                state.Scissors[0],
                ToScissor(clip, CoordinateScale));
        }
        BatchCollector batches = new(this, target);
        RenderRange(
            commands,
            start,
            end,
            analysis,
            state,
            target,
            batches,
            layerDepth: 0,
            childSurfaces);
        batches.Flush();
    }

    internal void DrawPrismTexture(
        nint texture,
        SdlGpuRenderTarget target,
        SdlRect? clip = null)
    {
        if (texture == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(texture));
        }
        if (clip is SdlRect empty &&
            (empty.Width <= 0 || empty.Height <= 0))
        {
            return;
        }
        session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Load);
        RenderState state = RenderState.Create(target, CoordinateScale);
        float logicalWidth = target.PixelWidth / CoordinateScale;
        float logicalHeight = target.PixelHeight / CoordinateScale;
        DrawPoint[] positions = RectanglePoints(
            new DrawRect(0, 0, logicalWidth, logicalHeight));
        DrawPoint[] uv =
        [
            new DrawPoint(0, 0),
            new DrawPoint(1, 0),
            new DrawPoint(1, 1),
            new DrawPoint(0, 1)
        ];
        SdlGpuVertex[] vertices = new SdlGpuVertex[4];
        for (int index = 0; index < vertices.Length; index++)
        {
            vertices[index] = CreateVertex(
                positions[index], uv[index], Color.White, Matrix3x2.Identity, 1);
        }
        BatchCollector batches = new(this, target);
        batches.Add(new CpuDrawBatch(
            vertices,
            [0, 1, 2, 0, 2, 3],
            DrawPrimitiveTopology.TriangleList,
            texture,
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp,
            DrawBlendMode.Normal,
            SdlGpuStencilMode.Disabled,
            0,
            clip ?? state.Scissor));
        batches.Flush();
    }

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
                SdlGpuBrushTextureKey key = new(
                    brush,
                    bounds,
                    width,
                    height);
                byte[] pixels = RasterizeBrush(descriptor, bounds, width, height);
                SdlGpuTextureResource texture = resources.GetOrCreateTexture(
                    session,
                    key,
                    width,
                    height,
                    pixels);
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
                return SdlGpuPaint.ImageBrush(
                    texture,
                    bounds,
                    imageBrush,
                    ApplyOpacity(
                        Color.White,
                        imageBrush.Opacity * commandOpacity));
            }
            default:
                throw new NotSupportedException(
                    $"SDL_GPU does not support brush descriptor '{descriptor.GetType().Name}' in Stage 5.");
        }
    }

    private SdlGpuTextureResource GetWhiteTexture() =>
        resources.GetOrCreateTexture(
            session,
            WhiteTextureKey,
            1,
            1,
            [byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue]);

    private SdlGpuTextureResource GetImageTexture(IDrawImage image)
    {
        if (image is not SdlGpuImage sdlImage)
        {
            throw new InvalidOperationException(
                "SDL_GPU image drawing requires an image created by SdlGpuImageLoader.");
        }
        if (subscribedImages.Add(sdlImage))
        {
            sdlImage.ContentChanged += OnImageContentChanged;
        }
        return resources.GetOrCreateTexture(
            session,
            sdlImage,
            sdlImage.Width,
            sdlImage.Height,
            sdlImage.RgbaPixels.Span);
    }

    private void OnImageContentChanged(object? sender, EventArgs args)
    {
        if (sender is SdlGpuImage image)
        {
            resources.InvalidateTexture(image);
            image.ContentChanged -= OnImageContentChanged;
            subscribedImages.Remove(image);
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

    private static byte[] ColorizeTextLayers(
        RasterizedText[] layers,
        DrawBrushDescriptor descriptor,
        DrawPoint baseline,
        DrawPoint originOffset,
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
                baseline.X + ((originOffset.X + x) / coordinateScale),
                baseline.Y + ((originOffset.Y + y) / coordinateScale));
            Color sampled = SampleBrush(descriptor, point);
            byte alpha = MultiplyByte(sampled.A, (byte)coverage);
            output[offset] = MultiplyByte(sampled.R, alpha);
            output[offset + 1] = MultiplyByte(sampled.G, alpha);
            output[offset + 2] = MultiplyByte(sampled.B, alpha);
            output[offset + 3] = alpha;
        }
        return output;
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

    private static SdlGpuVertex[] CreateSolidVertices(
        IReadOnlyList<DrawPoint> points,
        Color color,
        Matrix3x2 transform,
        float opacity)
    {
        SdlGpuVertex[] vertices = new SdlGpuVertex[points.Count];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = CreateVertex(
                points[i],
                new DrawPoint(0.5f, 0.5f),
                color,
                transform,
                opacity);
        }
        return vertices;
    }

    private static SdlGpuVertex CreateVertex(
        DrawPoint position,
        DrawPoint textureCoordinate,
        Color color,
        Matrix3x2 transform,
        float opacity)
    {
        Vector2 transformed = Vector2.Transform(
            new Vector2(position.X, position.Y),
            transform);
        Color effective = ApplyOpacity(color, opacity);
        float alpha = effective.A / 255f;
        return new SdlGpuVertex(
            transformed * CurrentScale.Value,
            new Vector2(textureCoordinate.X, textureCoordinate.Y),
            new Vector4(
                (effective.R / 255f) * alpha,
                (effective.G / 255f) * alpha,
                (effective.B / 255f) * alpha,
                alpha));
    }

    [ThreadStatic]
    private static float threadScale;

    private static class CurrentScale
    {
        public static float Value => threadScale > 0 ? threadScale : 1;
    }

    private static DrawPoint[] RectanglePoints(DrawRect rect) =>
    [
        new DrawPoint(rect.X, rect.Y),
        new DrawPoint(rect.Right, rect.Y),
        new DrawPoint(rect.Right, rect.Bottom),
        new DrawPoint(rect.X, rect.Bottom)
    ];

    private static DrawRect BoundsOf(IReadOnlyList<DrawPoint> points)
    {
        if (points.Count == 0)
        {
            return default;
        }
        float left = points[0].X;
        float top = points[0].Y;
        float right = left;
        float bottom = top;
        for (int i = 1; i < points.Count; i++)
        {
            left = MathF.Min(left, points[i].X);
            top = MathF.Min(top, points[i].Y);
            right = MathF.Max(right, points[i].X);
            bottom = MathF.Max(bottom, points[i].Y);
        }
        return new DrawRect(left, top, right - left, bottom - top);
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

    private static SdlRect ToScissor(DrawRect rect, float scale)
    {
        int left = (int)MathF.Floor(rect.X * scale);
        int top = (int)MathF.Floor(rect.Y * scale);
        int right = (int)MathF.Ceiling(rect.Right * scale);
        int bottom = (int)MathF.Ceiling(rect.Bottom * scale);
        return new SdlRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private static SdlRect IntersectScissor(SdlRect left, SdlRect right)
    {
        int x = Math.Max(left.X, right.X);
        int y = Math.Max(left.Y, right.Y);
        int edgeX = Math.Min(left.X + left.Width, right.X + right.Width);
        int edgeY = Math.Min(left.Y + left.Height, right.Y + right.Height);
        return new SdlRect(
            x,
            y,
            Math.Max(0, edgeX - x),
            Math.Max(0, edgeY - y));
    }

    private static Color ApplyOpacity(Color color, float opacity) =>
        new(
            color.R,
            color.G,
            color.B,
            (byte)Math.Clamp(
                (int)MathF.Round(color.A * Math.Clamp(opacity, 0, 1)),
                0,
                255));

    private static byte MultiplyByte(byte left, byte right) =>
        (byte)(((left * right) + 127) / 255);

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

    private sealed class BatchCollector(
        SdlGpuDrawingBackend owner,
        SdlGpuRenderTarget target)
    {
        private readonly List<CpuDrawBatch> batches = [];

        public void Add(CpuDrawBatch batch)
        {
            if (batch.Indices.Length == 0 || batch.Vertices.Length == 0)
            {
                return;
            }
            if (batches.Count > 0 && batches[^1].CanMerge(batch))
            {
                batches[^1] = batches[^1].Merge(batch);
                return;
            }
            batches.Add(batch);
        }

        public void Flush()
        {
            if (batches.Count == 0)
            {
                return;
            }
            int vertexCount = batches.Sum(static batch => batch.Vertices.Length);
            int indexCount = batches.Sum(static batch => batch.Indices.Length);
            SdlGpuVertex[] vertices = new SdlGpuVertex[vertexCount];
            int[] indices = new int[indexCount];
            int vertexOffset = 0;
            int indexOffset = 0;
            List<(CpuDrawBatch Batch, int VertexOffset, int IndexOffset)> draws = [];
            foreach (CpuDrawBatch batch in batches)
            {
                batch.Vertices.CopyTo(vertices, vertexOffset);
                batch.Indices.CopyTo(indices, indexOffset);
                draws.Add((batch, vertexOffset, indexOffset));
                vertexOffset += batch.Vertices.Length;
                indexOffset += batch.Indices.Length;
            }

            threadScale = owner.CoordinateScale;
            SdlGpuGeometryBinding geometry = owner.resources.UploadGeometry(
                owner.session,
                vertices,
                indices);
            ISdlApi api = owner.session.Api;
            nint renderPass = owner.session.ActiveRenderPass;
            api.BindGpuVertexBuffer(
                renderPass,
                0,
                new SdlGpuBufferBinding(geometry.VertexBuffer));
            api.BindGpuIndexBuffer(
                renderPass,
                new SdlGpuBufferBinding(geometry.IndexBuffer));
            float[] viewport = [target.PixelWidth, target.PixelHeight, 0, 0];
            api.PushGpuVertexUniformData(
                owner.session.ActiveCommandBuffer,
                0,
                MemoryMarshal.AsBytes(viewport.AsSpan()));
            foreach ((CpuDrawBatch batch, int batchVertexOffset, int batchIndexOffset) in draws)
            {
                nint pipeline = owner.resources.GetPipeline(
                    target.ColorFormat,
                    target.SampleCount,
                    batch.Topology,
                    batch.BlendMode,
                    batch.StencilMode,
                    batch.ColorWriteMask);
                nint sampler = owner.resources.GetSampler(
                    batch.Sampling,
                    batch.AddressMode);
                api.BindGpuGraphicsPipeline(renderPass, pipeline);
                api.BindGpuFragmentSampler(
                    renderPass,
                    0,
                    new SdlGpuTextureSamplerBinding(batch.Texture, sampler));
                api.SetGpuScissor(renderPass, batch.Scissor);
                api.SetGpuStencilReference(renderPass, batch.StencilReference);
                api.DrawGpuIndexedPrimitives(
                    renderPass,
                    checked((uint)batch.Indices.Length),
                    checked((uint)batchIndexOffset),
                    batchVertexOffset);
            }
            batches.Clear();
        }
    }

    private sealed class RenderState
    {
        public List<Matrix3x2> Transforms { get; } = [Matrix3x2.Identity];
        public List<float> Opacities { get; } = [1];
        public List<DrawBlendMode> Blends { get; } = [DrawBlendMode.Normal];
        public List<SdlRect> Scissors { get; } = [];
        public List<ClipEntry> Clips { get; } = [];
        public byte StencilDepth { get; set; }

        public Matrix3x2 Transform => Transforms[^1];
        public float Opacity => Opacities[^1];
        public DrawBlendMode Blend => Blends[^1];
        public SdlRect Scissor => Scissors[^1];
        public SdlGpuStencilMode StencilMode =>
            StencilDepth == 0
                ? SdlGpuStencilMode.Disabled
                : SdlGpuStencilMode.Test;

        public static RenderState Create(
            SdlGpuRenderTarget target,
            float coordinateScale)
        {
            RenderState state = new();
            state.Scissors.Add(new SdlRect(
                0,
                0,
                target.PixelWidth,
                target.PixelHeight));
            threadScale = coordinateScale;
            return state;
        }
    }

    private sealed record ClipEntry(
        SdlRect? PreviousScissor,
        CpuDrawBatch? StencilBatch)
    {
        public static ClipEntry ForScissor(SdlRect previous) =>
            new(previous, null);

        public static ClipEntry ForStencil(CpuDrawBatch batch) =>
            new(null, batch);
    }

    private sealed record CpuDrawBatch(
        SdlGpuVertex[] Vertices,
        int[] Indices,
        DrawPrimitiveTopology Topology,
        nint Texture,
        DrawSamplingMode Sampling,
        DrawAddressMode AddressMode,
        DrawBlendMode BlendMode,
        SdlGpuStencilMode StencilMode,
        byte StencilReference,
        SdlRect Scissor,
        SdlGpuColorWriteMask ColorWriteMask = SdlGpuColorWriteMask.All)
    {
        public bool CanMerge(CpuDrawBatch other) =>
            Topology == DrawPrimitiveTopology.TriangleList &&
            other.Topology == Topology &&
            Texture == other.Texture &&
            Sampling == other.Sampling &&
            AddressMode == other.AddressMode &&
            BlendMode == other.BlendMode &&
            StencilMode == other.StencilMode &&
            StencilReference == other.StencilReference &&
            Scissor == other.Scissor &&
            ColorWriteMask == other.ColorWriteMask;

        public CpuDrawBatch Merge(CpuDrawBatch other)
        {
            SdlGpuVertex[] vertices = new SdlGpuVertex[
                Vertices.Length + other.Vertices.Length];
            Vertices.CopyTo(vertices, 0);
            other.Vertices.CopyTo(vertices, Vertices.Length);
            int[] indices = new int[Indices.Length + other.Indices.Length];
            Indices.CopyTo(indices, 0);
            for (int i = 0; i < other.Indices.Length; i++)
            {
                indices[Indices.Length + i] = other.Indices[i] + Vertices.Length;
            }
            return this with { Vertices = vertices, Indices = indices };
        }
    }

    private sealed record SdlGpuPaint(
        SdlGpuTextureResource Texture,
        Color Tint,
        DrawSamplingMode Sampling,
        DrawAddressMode AddressMode,
        Func<DrawPoint, DrawPoint> MapTextureCoordinate)
    {
        public static SdlGpuPaint Solid(
            SdlGpuTextureResource texture,
            Color tint) =>
            new(
                texture,
                tint,
                DrawSamplingMode.Point,
                DrawAddressMode.Clamp,
                static _ => new DrawPoint(0.5f, 0.5f));

        public static SdlGpuPaint BoundsMapped(
            SdlGpuTextureResource texture,
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
                texture,
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

    private sealed record SdlGpuRenderSurfaceState(
        SdlGpuDrawingResources Resources,
        SdlGpuRenderTarget Target) : IRenderSurface2DBackendState
    {
        private bool disposed;

        public int PixelWidth => Target.PixelWidth;
        public int PixelHeight => Target.PixelHeight;
        public DrawCommandList Commands { get; } = new();
        public long FrameVersion { get; set; } = long.MinValue;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Resources.RetireRenderTarget(Target);
        }
    }

    private readonly record struct SdlGpuBrushTextureKey(
        IDrawBrush Brush,
        DrawRect Bounds,
        int Width,
        int Height);

    private readonly record struct SdlGpuTextRasterKey(
        int FontIdentity,
        string Text,
        float Size,
        float CoordinateScale,
        DrawPoint PixelPhase);

    private readonly record struct SdlGpuTextLayerTextureKey(
        SdlGpuTextRasterKey Raster,
        SdlGpuColorWriteMask Channel);

    private readonly record struct SdlGpuTextBrushTextureKey(
        SdlGpuTextRasterKey Raster,
        object Brush,
        DrawPoint Position);
}

internal readonly record struct SdlGpuPrismPresentationSurface(
    SdlGpuRenderTarget Target,
    SdlRect? Clip);
