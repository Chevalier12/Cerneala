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

internal sealed partial class SdlGpuDrawingBackend :
    IDrawingBackend,
    IDrawingBackendFrameTimingSource,
    IDisposable
{
    private const int TextSubpixelPhaseCount = 8;
    private static readonly int[] QuadIndices = [0, 1, 2, 0, 2, 3];
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
            DrawCommandKind.RenderSurface3D,
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
    private readonly SdlGpuRenderSurface3DDiagnostics renderSurface3DDiagnostics = new();
    private readonly Cerberus batches;
    private readonly SdlGpuGeometryCache geometry = new();
    private readonly HashSet<object> retainedBrushTextureKeys = [];
    private readonly HashSet<object> activeBrushTextureKeys = [];
    private readonly List<object> unusedBrushTextureKeys = [];
    private readonly HashSet<SdlGpuRenderSurfaceStateCache> renderSurfaceStateCaches = [];
    private SdlGpuRenderSurface3DExecutor? surface3DExecutor;
    private readonly HashSet<PrismCacheOwnerToken> analyzedPrismOwners = [];
    private readonly HashSet<PrismCacheOwnerToken> pendingPrismOwnerInvalidations = [];
    private long textAtlasFrameToken;
    private TimeSpan textRequestCollectionTime;
    private TimeSpan textRasterizationTime;
    private TimeSpan textAtlasUploadTime;
    private TimeSpan cleanupTime;
    private int textRequestCount;
    private int activeCompositingLayerCount;
    private long rasterizedPixelCount;
    private bool frameActive;
    private bool disposed;

    public SdlGpuDrawingBackend(SdlGpuWindowGraphicsSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        resources = session.DrawingResources;
        CoordinateScale = session.CoordinateScale;
        prismExecutor = new SdlGpuPrismExecutor(session, this);
        batches = new Cerberus();
    }

    internal static IReadOnlySet<DrawCommandKind> HandledCommandKinds => CommandKinds;

    internal float CoordinateScale { get; set; }

    internal PrismExecutionDiagnostics PrismDiagnostics => prismExecutor.Diagnostics;

    DrawingBackendFrameTiming IDrawingBackendFrameTimingSource.LastFrameTiming =>
        LastFrameTiming;

    internal DrawingBackendFrameTiming LastFrameTiming { get; private set; }

    private SdlGpuImage? lastImageTextureSource;
    private SdlGpuCommandBufferToken lastImageTextureToken;
    private SdlGpuTextureResource? lastImageTexture;

    internal SdlGpuDrawingFrameCounters LastFrameCounters { get; private set; }

    internal SdlGpuPrismFrameCounters LastFramePrismCounters { get; private set; }

    internal SdlGpuRenderSurface3DDiagnostics RenderSurface3DDiagnostics =>
        renderSurface3DDiagnostics;

    internal SdlGpuRenderSurface3DFrameCounters LastFrameRenderSurface3DCounters =>
        renderSurface3DDiagnostics.FrameCounters;

    public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!frameActive || !session.IsFrameActive)
        {
            throw new InvalidOperationException(
                "SDL_GPU drawing requires an active window frame.");
        }
        frameContext.EnsureCurrent(commands);
        if (frameContext.PrismAnalysis.Scopes.IsDefaultOrEmpty)
        {
            // Owner disposal is frame-lifecycle work, including empty frames
            // which do not dispatch the Prism graph executor.
            prismExecutor.ProcessInvalidations(
                frameContext.PrismAnalysis, frameContext.PrismCacheInvalidations);
        }
        if (session.IsSuspended || commands.Count == 0)
        {
            return;
        }

        long preparationStarted = Stopwatch.GetTimestamp();
        DrawCommandStateAnalysis analysis = frameContext.StateAnalysis;
        SdlGpuRenderTarget target = session.WindowRenderTarget;
        RenderState state = RenderState.Create(target, CoordinateScale);
        CommandRangeState rangeState = new(target, state);
        batches.Begin(target);
        TimeSpan preparationTime = Stopwatch.GetElapsedTime(preparationStarted);
        long commandRenderingStarted = Stopwatch.GetTimestamp();
        try
        {
            if (!frameContext.PrismAnalysis.Scopes.IsDefaultOrEmpty)
            {
                prismExecutor.Execute(commands, frameContext);
                CountPrismExecution(prismExecutor.Diagnostics);
                return;
            }

            RenderRange(commands, 0, commands.Count, analysis, rangeState, batches);
            FlushBatches();
            EnsureCompositingScopesClosed(rangeState);
        }
        catch
        {
            DiscardBatches();
            throw;
        }
        finally
        {
            TimeSpan totalCommandTime = Stopwatch.GetElapsedTime(commandRenderingStarted);
            TimeSpan separatelyMeasured =
                textRequestCollectionTime + textRasterizationTime + textAtlasUploadTime + cleanupTime;
            TimeSpan commandRenderingTime = totalCommandTime > separatelyMeasured
                ? totalCommandTime - separatelyMeasured
                : TimeSpan.Zero;
            LastFrameTiming = new DrawingBackendFrameTiming(
                preparationTime,
                textRequestCollectionTime,
                textRasterizationTime,
                textAtlasUploadTime,
                commandRenderingTime,
                cleanupTime,
                textRequestCount,
                rasterizedPixelCount);
        }
    }

    internal void BeginFrame()
    {
        resources.ProcessImageInvalidations();
        ObjectDisposedException.ThrowIf(disposed, this);
        geometry.BeginFrame();
        textAtlasFrameToken = resources.BeginTextAtlasFrame();
        activeBrushTextureKeys.Clear();
        analyzedPrismOwners.Clear();
        pendingPrismOwnerInvalidations.Clear();
        BeginBrushCaptureFrame();
        textRequestCollectionTime = TimeSpan.Zero;
        textRasterizationTime = TimeSpan.Zero;
        textAtlasUploadTime = TimeSpan.Zero;
        cleanupTime = TimeSpan.Zero;
        textRequestCount = 0;
        activeCompositingLayerCount = 0;
        rasterizedPixelCount = 0;
        LastFrameTiming = default;
        LastFrameCounters = default;
        LastFramePrismCounters = default;
        renderSurface3DDiagnostics.BeginFrame();
        prismExecutor.Diagnostics.BeginFrame();
        frameActive = true;
    }

    private void CountBatch(CerberusFlushMetrics metrics)
    {
        if (metrics.DrawCallCount != 0)
        {
            LastFrameCounters = LastFrameCounters.AddFlush(metrics);
        }
    }

    private void FlushBatches()
    {
        FlushPendingTextAtlasUploads();
        long flushStarted = Stopwatch.GetTimestamp();
        try
        {
            CountBatch(batches.Flush(new CerberusExecutionContext(session, resources)));
        }
        finally
        {
            cleanupTime += Stopwatch.GetElapsedTime(flushStarted);
        }
    }

    private void DiscardBatches()
    {
        long discardStarted = Stopwatch.GetTimestamp();
        try
        {
            batches.Discard();
        }
        finally
        {
            cleanupTime += Stopwatch.GetElapsedTime(discardStarted);
        }
    }

    private void CountPrismExecution(PrismExecutionDiagnostics diagnostics)
    {
        LastFramePrismCounters = LastFramePrismCounters.Add(diagnostics);
    }

    internal void EndFrame()
    {
        frameActive = false;
        geometry.EndFrame();
        foreach (PrismCacheOwnerToken owner in pendingPrismOwnerInvalidations)
        {
            if (!analyzedPrismOwners.Contains(owner))
            {
                resources.PrismResources.Invalidate(PrismCacheInvalidation.ForOwner(owner));
            }
        }
        analyzedPrismOwners.Clear();
        pendingPrismOwnerInvalidations.Clear();
        CompleteBrushTextureFrame();
        CompleteBrushCaptureFrame();
        resources.EndTextAtlasFrame(textAtlasFrameToken);
        textAtlasFrameToken = 0;
    }

    internal void ProcessPrismInvalidations(
        PrismFrameAnalysis analysis,
        PrismCacheInvalidationQueue? queue)
    {
        // The root command list does not contain scopes recorded by nested
        // surfaces. Decide owner absence only after every surface has rendered;
        // live scopes reconcile their exact retained keys in their executor.
        foreach (PrismAnalyzedScope scope in analysis.Scopes)
        {
            analyzedPrismOwners.Add(scope.Scope.CacheOwnerToken);
        }
        while (queue?.TryDequeue(out PrismCacheInvalidation invalidation) == true)
        {
            if (invalidation.Kind == PrismCacheInvalidationKind.All)
            {
                resources.PrismResources.Invalidate(invalidation);
            }
            else
            {
                pendingPrismOwnerInvalidations.Add(invalidation.OwnerToken);
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        foreach (SdlGpuRenderSurfaceStateCache surfaceCache in renderSurfaceStateCaches.ToArray())
        {
            surfaceCache.Remove(this, session);
        }
        renderSurfaceStateCaches.Clear();
        geometry.Clear();
        surface3DExecutor?.Dispose();
        surface3DExecutor = null;
        prismExecutor.Dispose();
        resources.EndTextAtlasFrame(textAtlasFrameToken);
        textAtlasFrameToken = 0;
        foreach (object key in retainedBrushTextureKeys)
        {
            resources.ReleaseTexture(key);
        }
        retainedBrushTextureKeys.Clear();
        activeBrushTextureKeys.Clear();
        unusedBrushTextureKeys.Clear();
        DisposeBrushCaptures();
    }

    private void MarkBrushTextureUsed(object key)
    {
        // Device resources are shared. Each backend retains its last frame's
        // brush textures without evicting another window's live brush entries.
        if (retainedBrushTextureKeys.Add(key))
        {
            resources.RetainTexture(session, key);
        }
        activeBrushTextureKeys.Add(key);
    }

    private void CompleteBrushTextureFrame()
    {
        unusedBrushTextureKeys.Clear();
        foreach (object key in retainedBrushTextureKeys)
        {
            if (!activeBrushTextureKeys.Contains(key))
            {
                unusedBrushTextureKeys.Add(key);
            }
        }
        foreach (object key in unusedBrushTextureKeys)
        {
            retainedBrushTextureKeys.Remove(key);
            resources.ReleaseTexture(key);
        }
        unusedBrushTextureKeys.Clear();
    }

    private void RenderRange(
        DrawCommandList commands,
        int start,
        int end,
        DrawCommandStateAnalysis analysis,
        CommandRangeState rangeState,
        Cerberus batches,
        IReadOnlyDictionary<int, SdlGpuPrismPresentationSurface>? childSurfaces = null)
    {
        for (int index = start; index < end; index++)
        {
            // Read in place: scene frames replay thousands of large commands.
            ref readonly DrawCommand command = ref commands.ItemRef(index);
            RenderState state = rangeState.State;
            SdlGpuRenderTarget target = rangeState.Target;
            switch (command.Kind)
            {
                case DrawCommandKind.FillRectangle:
                    AddFillRectangle(command, state, batches);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                case DrawCommandKind.FillPath:
                case DrawCommandKind.FillEllipse:
                    AddPathFill(command, state, batches);
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
                    FlushBatches();
                    AddRenderSurface(command, state, target, batches);
                    break;
                case DrawCommandKind.RenderSurface3D:
                    FlushBatches();
                    (surface3DExecutor ??= new(this, session, resources, renderSurface3DDiagnostics))
                        .AddSurface(command, state, target, batches);
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
                    {
                        int matching = analysis.Entries[index].MatchingCommandIndex;
                        if (matching <= index || matching >= commands.Count)
                        {
                            throw new InvalidOperationException(
                                $"PushOpacity at command index {index} has no valid matching PopOpacity.");
                        }
                        FlushBatches();
                        BeginCompositingLayer(
                            rangeState,
                            command.Opacity,
                            DrawBlendMode.Normal,
                            DrawCommandKind.PushOpacity,
                            batches);
                        break;
                    }
                case DrawCommandKind.PopOpacity:
                    EndCompositingLayer(
                        rangeState,
                        DrawCommandKind.PushOpacity,
                        index,
                        batches);
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
                        if (matching <= index || matching >= commands.Count)
                        {
                            throw new InvalidOperationException(
                                $"PushLayer at command index {index} has no valid matching PopLayer.");
                        }
                        FlushBatches();
                        BeginCompositingLayer(
                            rangeState,
                            command.LayerOptions!.Opacity,
                            command.LayerOptions.BlendMode,
                            DrawCommandKind.PushLayer,
                            batches);
                        break;
                    }
                case DrawCommandKind.PopLayer:
                    EndCompositingLayer(
                        rangeState,
                        DrawCommandKind.PushLayer,
                        index,
                        batches);
                    break;
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
                        FlushBatches();
                        DrawPrismTextureCore(
                            child.Target.SampleTexture,
                            target,
                            child.Clip,
                            child.Destination,
                            state,
                            child.WorkingColorProfile);
                        // Presenting the child flushes (and ends) its batch. The
                        // enclosing range must resume its target for later siblings.
                        batches.Begin(target);
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
        Cerberus batches)
    {
        SdlGpuPaint paint = ResolvePaint(
            command.Brush,
            command.Rect,
            command.Color,
            command.BrushOpacity);
        Span<SdlGpuVertex> vertices = batches.Allocate(
            4,
            QuadIndices,
            CreateBatchKey(
                DrawPrimitiveTopology.TriangleList,
                paint.Texture,
                paint.Sampling,
                paint.AddressMode,
                state));
        DrawRect rect = command.Rect;
        float left = UiCoordinateMapper.LogicalToPhysicalPixel(rect.X, CoordinateScale) / CoordinateScale;
        float top = UiCoordinateMapper.LogicalToPhysicalPixel(rect.Y, CoordinateScale) / CoordinateScale;
        float right = UiCoordinateMapper.LogicalToPhysicalPixel(rect.Right, CoordinateScale) / CoordinateScale;
        float bottom = UiCoordinateMapper.LogicalToPhysicalPixel(rect.Bottom, CoordinateScale) / CoordinateScale;
        vertices[0] = CreatePaintedVertex(new DrawPoint(left, top), new DrawPoint(rect.X, rect.Y), paint, state);
        vertices[1] = CreatePaintedVertex(new DrawPoint(right, top), new DrawPoint(rect.Right, rect.Y), paint, state);
        vertices[2] = CreatePaintedVertex(new DrawPoint(right, bottom), new DrawPoint(rect.Right, rect.Bottom), paint, state);
        vertices[3] = CreatePaintedVertex(new DrawPoint(left, bottom), new DrawPoint(rect.X, rect.Bottom), paint, state);
    }

    private void AddPathFill(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuGeometry mesh = geometry.GetFill(command, CoordinateScale);
        if (mesh.IsEmpty)
        {
            return;
        }

        AddPaintedGeometry(
            mesh.Positions,
            mesh.BrushPoints,
            mesh.Indices,
            DrawPrimitiveTopology.TriangleList,
            command.Rect,
            command.Brush,
            command.BrushOpacity,
            command.Color,
            state,
            batches);
    }

    private void AddStroke(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuGeometry stroke = geometry.GetStroke(command, CoordinateScale);
        if (stroke.IsEmpty)
        {
            return;
        }

        AddPaintedGeometry(
            stroke.Positions,
            stroke.BrushPoints,
            stroke.Indices,
            DrawPrimitiveTopology.TriangleList,
            stroke.BrushBounds,
            command.Pen?.Brush ?? command.Brush,
            command.BrushOpacity,
            command.Color,
            state,
            batches);
    }

    private void AddImage(
        in DrawCommand command,
        RenderState state,
        Cerberus batches)
    {
        IDrawImage image = command.Image ??
            throw new InvalidOperationException("DrawImage requires an image.");
        DrawImageOptions options = command.ImageOptions ?? new DrawImageOptions();
        DrawRect source = DrawImageGeometry.ResolveSource(image, options);
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
        Color tint = DrawImageGeometry.EffectiveTint(options);
        SdlGpuTextureResource texture = GetImageTexture(image);
        bool imageDomain = IsPointClampImage(options.Sampling, options.AddressMode);
        CerberusBatchKey key = CreateBatchKey(
            DrawPrimitiveTopology.TriangleList,
            texture.Handle,
            options.Sampling,
            options.AddressMode,
            state,
            pointClampImageDomain: imageDomain);
        Span<SdlGpuVertex> vertices = imageDomain
            ? stackalloc SdlGpuVertex[4]
            : batches.Allocate(4, QuadIndices, key);
        DrawRect destination = command.Rect;
        // One pass resolves the source, origin and rotation for all corners.
        Span<DrawPoint> corners = stackalloc DrawPoint[4];
        DrawImageGeometry.WriteDestinationCorners(image, destination, options, corners);
        // All four corners share these values; preserve the same vertex math.
        Vector4 premultipliedTint = PremultiplyVertexColor(tint, state.Opacity);
        Matrix3x2 transform = state.Transform;
        float coordinateScale = CurrentScale.Value;
        vertices[0] = CreateVertex(
            corners[0],
            new DrawPoint(left, top),
            premultipliedTint,
            transform,
            coordinateScale);
        vertices[1] = CreateVertex(
            corners[1],
            new DrawPoint(right, top),
            premultipliedTint,
            transform,
            coordinateScale);
        vertices[2] = CreateVertex(
            corners[2],
            new DrawPoint(right, bottom),
            premultipliedTint,
            transform,
            coordinateScale);
        vertices[3] = CreateVertex(
            corners[3],
            new DrawPoint(left, bottom),
            premultipliedTint,
            transform,
            coordinateScale);
        if (imageDomain)
        {
            Span<SdlGpuImageDomainVertex> selected =
                batches.AllocateImageDomain(4, QuadIndices, key);
            for (int index = 0; index < 4; index++)
            {
                SdlGpuVertex vertex = vertices[index];
                selected[index] = new SdlGpuImageDomainVertex(
                    vertex.Position, vertex.TextureCoordinate, vertex.Color,
                    default, default);
            }
            WriteImageDomain(selected, nineSlice: false);
        }
    }

    private void AddCommandMesh(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
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
                command.Kind == DrawCommandKind.DrawNineSlice ||
                    command.Kind == DrawCommandKind.DrawSpriteBatch ||
                    (command.Kind == DrawCommandKind.DrawImageQuad &&
                        mesh.IsOptionsImageQuad),
                command.Kind == DrawCommandKind.DrawNineSlice,
                state,
                batches);
            return;
        }

        SdlGpuTextureResource white = GetWhiteTexture();
        Span<SdlGpuVertex> vertices = batches.Allocate(
            mesh.VertexArray.Length,
            mesh.IndexArray,
            CreateBatchKey(
                mesh.Topology,
                white.Handle,
                DrawSamplingMode.Point,
                DrawAddressMode.Clamp,
                state));
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
    }

    private void AddImageGeometry(
        DrawVertex2D[] sourceVertices,
        int[] sourceIndices,
        DrawPrimitiveTopology topology,
        IDrawImage image,
        DrawSamplingMode sampling,
        DrawAddressMode addressMode,
        bool imageProvenance,
        bool nineSlice,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuTextureResource texture = GetImageTexture(image);
        bool imageDomain = imageProvenance &&
            topology == DrawPrimitiveTopology.TriangleList &&
            IsPointClampImage(sampling, addressMode);
        CerberusBatchKey key = CreateBatchKey(
            topology,
            texture.Handle,
            sampling,
            addressMode,
            state,
            pointClampImageDomain: imageDomain);
        if (imageDomain)
        {
            Span<SdlGpuImageDomainVertex> selected =
                batches.AllocateImageDomain(sourceVertices.Length, sourceIndices, key);
            for (int index = 0; index < selected.Length; index++)
            {
                DrawVertex2D source = sourceVertices[index];
                SdlGpuVertex vertex = CreateVertex(
                    source.Position,
                    source.TextureCoordinate,
                    source.Color,
                    state.Transform,
                    state.Opacity);
                selected[index] = new SdlGpuImageDomainVertex(
                    vertex.Position, vertex.TextureCoordinate, vertex.Color,
                    default, default);
            }
            WriteImageDomain(selected, nineSlice);
            return;
        }

        Span<SdlGpuVertex> vertices = batches.Allocate(
            sourceVertices.Length,
            sourceIndices,
            key);
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
    }

    private static bool IsPointClampImage(
        DrawSamplingMode sampling,
        DrawAddressMode addressMode) =>
        sampling == DrawSamplingMode.Point && addressMode == DrawAddressMode.Clamp;

    private static void WriteImageDomain(
        Span<SdlGpuImageDomainVertex> vertices,
        bool nineSlice)
    {
        if (nineSlice)
        {
            WriteImageDomainQuad(vertices, 0, 3, 15, 12, 0, vertices.Length);
            return;
        }

        for (int first = 0; first < vertices.Length; first += 4)
        {
            WriteImageDomainQuad(
                vertices, first, first + 1, first + 2, first + 3, first, 4);
        }
    }

    private static void WriteImageDomainQuad(
        Span<SdlGpuImageDomainVertex> vertices,
        int q0,
        int q1,
        int q2,
        int q3,
        int first,
        int count)
    {
        if (!SdlGpuImageDomainGeometry.TryCreate(
            vertices[q0].Position,
            vertices[q1].Position,
            vertices[q2].Position,
            vertices[q3].Position,
            out SdlGpuImageDomainGeometry geometry))
        {
            // Both constituent triangles have zero area and emit no fragments.
            return;
        }

        for (int index = first; index < first + count; index++)
        {
            vertices[index] = vertices[index] with
            {
                FirstCorners = geometry.FirstCorners,
                LastCorners = geometry.LastCorners
            };
        }
    }

    private void AddPaintedGeometry(
        DrawPoint[] positions,
        DrawPoint[] brushPoints,
        int[] indices,
        DrawPrimitiveTopology topology,
        DrawRect bounds,
        IDrawBrush? brush,
        float commandOpacity,
        Color fallbackColor,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuPaint paint = ResolvePaint(
            brush,
            bounds,
            fallbackColor,
            commandOpacity);
        Span<SdlGpuVertex> vertices = batches.Allocate(
            positions.Length,
            indices,
            CreateBatchKey(
                topology,
                paint.Texture,
                paint.Sampling,
                paint.AddressMode,
                state));
        Matrix3x2 transform = state.Transform;
        float coordinateScale = CurrentScale.Value;
        Vector4 vertexColor = PremultiplyVertexColor(paint.Tint, state.Opacity);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = CreateVertex(
                positions[i],
                paint.MapTextureCoordinate(brushPoints[Math.Min(i, brushPoints.Length - 1)]),
                vertexColor,
                transform,
                coordinateScale);
        }
    }

    private static SdlGpuVertex CreatePaintedVertex(
        DrawPoint position,
        DrawPoint brushPoint,
        SdlGpuPaint paint,
        RenderState state) =>
        CreateVertex(
            position,
            paint.MapTextureCoordinate(brushPoint),
            paint.Tint,
            state.Transform,
            state.Opacity);

    private static void AddQuad(
        Cerberus batches,
        DrawRect destination,
        DrawRect textureCoordinates,
        Color tint,
        Matrix3x2 transform,
        float opacity,
        CerberusBatchKey key)
    {
        Span<SdlGpuVertex> vertices = batches.Allocate(4, QuadIndices, key);
        vertices[0] = CreateVertex(
            new DrawPoint(destination.X, destination.Y),
            new DrawPoint(textureCoordinates.X, textureCoordinates.Y),
            tint,
            transform,
            opacity);
        vertices[1] = CreateVertex(
            new DrawPoint(destination.Right, destination.Y),
            new DrawPoint(textureCoordinates.Right, textureCoordinates.Y),
            tint,
            transform,
            opacity);
        vertices[2] = CreateVertex(
            new DrawPoint(destination.Right, destination.Bottom),
            new DrawPoint(textureCoordinates.Right, textureCoordinates.Bottom),
            tint,
            transform,
            opacity);
        vertices[3] = CreateVertex(
            new DrawPoint(destination.X, destination.Bottom),
            new DrawPoint(textureCoordinates.X, textureCoordinates.Bottom),
            tint,
            transform,
            opacity);
    }

    private static CerberusBatchKey CreateBatchKey(
        DrawPrimitiveTopology topology,
        nint texture,
        DrawSamplingMode sampling,
        DrawAddressMode addressMode,
        RenderState state,
        SdlGpuColorWriteMask colorWriteMask = SdlGpuColorWriteMask.All,
        bool pointClampImageDomain = false) =>
        new(
            topology,
            texture,
            sampling,
            addressMode,
            state.Blend,
            state.StencilMode,
            state.StencilDepth,
            state.Scissor,
            colorWriteMask,
            PointClampImageDomain: pointClampImageDomain);

    private void PushRectangleClip(
        DrawCommand command,
        RenderState state,
        Cerberus batches)
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
        Cerberus batches)
    {
        SdlGpuGeometry mesh = geometry.GetFill(command, CoordinateScale);
        PushStencilClip(mesh.Positions, mesh.Indices, state, batches);
    }

    private void PushStencilClip(
        DrawPoint[] points,
        int[] indices,
        RenderState state,
        Cerberus batches)
    {
        SdlGpuTextureResource white = GetWhiteTexture();
        SdlGpuVertex[] vertices = CreateSolidVertices(
            points,
            Color.White,
            state.Transform,
            opacity: 1);
        CerberusBatch clip = new(
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

    private static void PopClip(RenderState state, Cerberus batches)
    {
        ClipEntry clip = state.Clips[^1];
        state.Clips.RemoveAt(state.Clips.Count - 1);
        if (clip.PreviousScissor is SdlRect)
        {
            state.Scissors.RemoveAt(state.Scissors.Count - 1);
            return;
        }

        state.StencilDepth--;
        CerberusBatch original = clip.StencilBatch ??
            throw new InvalidOperationException("Stencil clip state is incomplete.");
        batches.Add(original with
        {
            StencilMode = SdlGpuStencilMode.Decrement,
            StencilReference = checked((byte)(state.StencilDepth + 1)),
            Scissor = state.Scissor
        });
    }

    private void BeginCompositingLayer(
        CommandRangeState rangeState,
        float opacity,
        DrawBlendMode blendMode,
        DrawCommandKind pushKind,
        Cerberus batches)
    {
        RenderState parentState = rangeState.State;
        SdlGpuRenderTarget parentTarget = rangeState.Target;
        // Captures create new command ranges while their parent's layers are
        // still live. Pool depth follows the entire rendering stack, not one range.
        SdlGpuRenderTarget layer = resources.GetLayerTarget(
            activeCompositingLayerCount + 1,
            parentTarget.PixelWidth,
            parentTarget.PixelHeight,
            parentTarget.ColorFormat,
            SdlGpuSampleCount.One);
        session.BeginRenderTarget(layer, Color.Transparent, SdlGpuLoadOp.Clear);
        RenderState childState = RenderState.Create(layer, CoordinateScale);
        childState.Transforms[0] = parentState.Transform;
        batches.Begin(layer);
        rangeState.CompositingScopes.Add(new CompositingScope(
            parentTarget,
            parentState,
            layer,
            opacity,
            blendMode,
            pushKind));
        rangeState.Target = layer;
        rangeState.State = childState;
        activeCompositingLayerCount++;
    }

    private void EndCompositingLayer(
        CommandRangeState rangeState,
        DrawCommandKind expectedPushKind,
        int commandIndex,
        Cerberus batches)
    {
        if (rangeState.CompositingScopes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Unexpected {expectedPushKind switch
                {
                    DrawCommandKind.PushOpacity => nameof(DrawCommandKind.PopOpacity),
                    DrawCommandKind.PushLayer => nameof(DrawCommandKind.PopLayer),
                    _ => expectedPushKind.ToString()
                }} at command index {commandIndex}.");
        }

        CompositingScope scope = rangeState.CompositingScopes[^1];
        if (scope.PushKind != expectedPushKind)
        {
            throw new InvalidOperationException(
                $"Compositing scope opened by {scope.PushKind} cannot be closed by " +
                $"{expectedPushKind} at command index {commandIndex}.");
        }

        FlushBatches();
        rangeState.CompositingScopes.RemoveAt(rangeState.CompositingScopes.Count - 1);
        activeCompositingLayerCount--;
        rangeState.Target = scope.ParentTarget;
        rangeState.State = scope.ParentState;
        session.BeginRenderTarget(
            scope.ParentTarget,
            Color.Transparent,
            SdlGpuLoadOp.Load);
        batches.Begin(scope.ParentTarget);
        AddTargetComposite(
            scope.LayerTarget,
            scope.ParentTarget,
            scope.ParentState,
            scope.Opacity * scope.ParentState.Opacity,
            scope.BlendMode,
            batches);
    }

    private static void EnsureCompositingScopesClosed(CommandRangeState rangeState)
    {
        if (rangeState.CompositingScopes.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"The SDL_GPU command range ended with {rangeState.CompositingScopes.Count} " +
            "unclosed compositing scope(s).");
    }

    private void AddTargetComposite(
        SdlGpuRenderTarget source,
        SdlGpuRenderTarget destination,
        RenderState state,
        float opacity,
        DrawBlendMode blend,
        Cerberus batches)
    {
        float logicalWidth = destination.PixelWidth / CoordinateScale;
        float logicalHeight = destination.PixelHeight / CoordinateScale;
        Color tint = ApplyOpacity(Color.White, opacity);
        AddQuad(
            batches,
            new DrawRect(0, 0, logicalWidth, logicalHeight),
            new DrawRect(0, 0, 1, 1),
            tint,
            Matrix3x2.Identity,
            opacity: 1,
            new CerberusBatchKey(
                DrawPrimitiveTopology.TriangleList,
                source.SampleTexture,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                blend,
                state.StencilMode,
                state.StencilDepth,
                state.Scissor,
                SdlGpuColorWriteMask.All));
    }

    internal void RenderCommandRange(
        DrawCommandList commands,
        int start,
        int end,
        DrawCommandStateAnalysis analysis,
        SdlGpuRenderTarget target,
        IReadOnlyDictionary<int, SdlGpuPrismPresentationSurface>? childSurfaces,
        CommandRangeState? continuedState = null,
        Vector2 logicalOrigin = default,
        bool isolateCompositingState = false,
        (DrawRect Bounds, Matrix3x2 Transform)? captureClip = null)
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

        CommandRangeState rangeState;
        if (continuedState is not null)
        {
            if (continuedState.RootTarget != target)
            {
                throw new InvalidOperationException(
                    "A continued SDL_GPU command range cannot change its root render target.");
            }
            rangeState = continuedState;
        }
        else
        {
            RenderState state = RenderState.Create(target, CoordinateScale);
            DrawCommandStateEntry entry = analysis.Entries[start];
            state.Transforms[0] = Matrix3x2.Multiply(
                entry.Transform,
                Matrix3x2.CreateTranslation(-logicalOrigin));
            if (!isolateCompositingState)
            {
                state.Opacities[0] = entry.Opacity;
                state.Blends[0] = entry.BlendMode;
                if (entry.ClipBounds is DrawRect clip)
                {
                    state.Scissors[0] = IntersectScissor(
                        state.Scissors[0],
                        ToScissor(
                            new DrawRect(
                                clip.X - logicalOrigin.X,
                                clip.Y - logicalOrigin.Y,
                                clip.Width,
                                clip.Height),
                            CoordinateScale));
                }
            }
            rangeState = new CommandRangeState(target, state);
        }
        batches.Begin(rangeState.Target);
        if (captureClip is { } capture)
        {
            // Capture bounds constrain source pixels before effects run. Use the
            // existing rectangle/stencil path so rotated bounds are not widened
            // to an axis-aligned scissor. Command transforms stay unchanged.
            RenderState state = rangeState.State;
            Matrix3x2 commandTransform = state.Transforms[0];
            state.Transforms[0] = Matrix3x2.Multiply(
                capture.Transform, Matrix3x2.CreateTranslation(-logicalOrigin));
            PushRectangleClip(DrawCommand.PushClip(capture.Bounds), state, batches);
            state.Transforms[0] = commandTransform;
        }
        RenderRange(
            commands,
            start,
            end,
            analysis,
            rangeState,
            batches,
            childSurfaces);
        if (captureClip is not null)
        {
            PopClip(rangeState.State, batches);
        }
        FlushBatches();
        if (continuedState is null || end == commands.Count)
        {
            EnsureCompositingScopesClosed(rangeState);
        }
    }

    internal CommandRangeState CreateCommandRangeState(
        SdlGpuRenderTarget target) =>
        new(target, RenderState.Create(target, CoordinateScale));

    internal void DrawPrismTexture(
        nint texture,
        SdlGpuRenderTarget target,
        SdlRect? clip = null,
        DrawRect? destination = null,
        CommandRangeState? presentationState = null,
        Cerneala.Drawing.Prism.Catalog.PrismColorProfile? workingColorProfile = null)
    {
        if (presentationState is not null && presentationState.Target != target)
        {
            throw new InvalidOperationException(
                "A continued SDL_GPU Prism presentation cannot change render targets.");
        }
        DrawPrismTextureCore(
            texture,
            target,
            clip,
            destination,
            presentationState?.State ?? RenderState.Create(target, CoordinateScale),
            workingColorProfile);
    }

    private void DrawPrismTextureCore(
        nint texture,
        SdlGpuRenderTarget target,
        SdlRect? clip,
        DrawRect? destination,
        RenderState state,
        Cerneala.Drawing.Prism.Catalog.PrismColorProfile? workingColorProfile)
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
        DrawRect destinationRect = destination ?? new DrawRect(
            0,
            0,
            target.PixelWidth / CoordinateScale,
            target.PixelHeight / CoordinateScale);
        batches.Begin(target);
        AddQuad(
            batches,
            destinationRect,
            new DrawRect(0, 0, 1, 1),
            Color.White,
            Matrix3x2.Identity,
            state.Opacity,
            new CerberusBatchKey(
                DrawPrimitiveTopology.TriangleList,
                texture,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                state.Blend,
                state.StencilMode,
                state.StencilDepth,
                clip is SdlRect presentationClip
                    ? IntersectScissor(state.Scissor, presentationClip)
                    : state.Scissor,
                SdlGpuColorWriteMask.All,
                PrismWorkingColorProfile: workingColorProfile));
        FlushBatches();
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

        // Consecutive quads usually share one atlas. Its texture is already
        // resolved and pinned for the active command buffer.
        bool recording = session.TryGetActiveCommandBufferToken(out SdlGpuCommandBufferToken token);
        if (recording && ReferenceEquals(sdlImage, lastImageTextureSource) && token == lastImageTextureToken)
        {
            return lastImageTexture!;
        }

        SdlGpuTextureResource texture = resources.GetOrCreateTexture(
            session,
            sdlImage,
            sdlImage.Width,
            sdlImage.Height,
            sdlImage.RgbaPixels.Span);
        lastImageTextureSource = recording ? sdlImage : null;
        lastImageTextureToken = token;
        lastImageTexture = texture;
        return texture;
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
        float opacity) =>
        CreateVertex(position, textureCoordinate, PremultiplyVertexColor(color, opacity), transform, CurrentScale.Value);

    private static SdlGpuVertex CreateVertex(
        DrawPoint position,
        DrawPoint textureCoordinate,
        Vector4 premultipliedColor,
        Matrix3x2 transform,
        float coordinateScale)
    {
        Vector2 transformed = Vector2.Transform(
            new Vector2(position.X, position.Y),
            transform);
        return new SdlGpuVertex(
            transformed * coordinateScale,
            new Vector2(textureCoordinate.X, textureCoordinate.Y),
            premultipliedColor);
    }

    private static Vector4 PremultiplyVertexColor(Color color, float opacity)
    {
        Color effective = ApplyOpacity(color, opacity);
        float alpha = effective.A / 255f;
        return new Vector4(
            (effective.R / 255f) * alpha,
            (effective.G / 255f) * alpha,
            (effective.B / 255f) * alpha,
            alpha);
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

    internal sealed class RenderState
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
            state.Reset(target, coordinateScale);
            return state;
        }

        internal void Reset(SdlGpuRenderTarget target, float coordinateScale)
        {
            Transforms.Clear();
            Transforms.Add(Matrix3x2.Identity);
            Opacities.Clear();
            Opacities.Add(1);
            Blends.Clear();
            Blends.Add(DrawBlendMode.Normal);
            Scissors.Clear();
            Scissors.Add(new SdlRect(
                0,
                0,
                target.PixelWidth,
                target.PixelHeight));
            Clips.Clear();
            StencilDepth = 0;
            threadScale = coordinateScale;
        }
    }

    internal sealed class CommandRangeState
    {
        private readonly RenderState rootState;

        internal CommandRangeState(
            SdlGpuRenderTarget target,
            RenderState state)
        {
            RootTarget = target;
            Target = target;
            State = state;
            rootState = state;
        }

        internal SdlGpuRenderTarget RootTarget { get; private set; }

        internal SdlGpuRenderTarget Target { get; set; }

        internal RenderState State { get; set; }

        internal List<CompositingScope> CompositingScopes { get; } = [];

        internal void Reset(SdlGpuRenderTarget target, float coordinateScale)
        {
            // A failed range may still point at a compositing child. Restore
            // the owned root, not whichever child happened to execute last.
            RootTarget = target;
            Target = target;
            State = rootState;
            rootState.Reset(target, coordinateScale);
            CompositingScopes.Clear();
        }
    }

    internal sealed record CompositingScope(
        SdlGpuRenderTarget ParentTarget,
        RenderState ParentState,
        SdlGpuRenderTarget LayerTarget,
        float Opacity,
        DrawBlendMode BlendMode,
        DrawCommandKind PushKind);

    internal sealed record ClipEntry(
        SdlRect? PreviousScissor,
        CerberusBatch? StencilBatch)
    {
        public static ClipEntry ForScissor(SdlRect previous) =>
            new(previous, null);

        public static ClipEntry ForStencil(CerberusBatch batch) =>
            new(null, batch);
    }

}

internal readonly record struct SdlGpuPrismPresentationSurface(
    SdlGpuRenderTarget Target,
    SdlRect? Clip,
    Cerneala.Drawing.Prism.Catalog.PrismColorProfile WorkingColorProfile,
    DrawRect Destination);
