using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.Drawing.Prism.Surfaces;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuPrismExecutor : IDisposable
{
    private const int PresentationSamplingOutset = 1;
    private const int ExecutionSurfaceTileSize = 16;
    private const long ShaderPackageVersion = 56;
    private static readonly PrismGraphCapabilities Capabilities =
        PrismGraphCapabilities.ControlCapture |
        PrismGraphCapabilities.FilterProcessing |
        PrismGraphCapabilities.StyleProcessing |
        PrismGraphCapabilities.MaskProcessing |
        PrismGraphCapabilities.GroupProcessing |
        PrismGraphCapabilities.GroupIsolation |
        PrismGraphCapabilities.Clipping |
        PrismGraphCapabilities.AdvancedBlending |
        PrismGraphCapabilities.ColorConversion |
        PrismGraphCapabilities.BackdropInput;

    private readonly SdlGpuWindowGraphicsSession session;
    private readonly SdlGpuDrawingBackend drawingBackend;
    private readonly SdlGpuPrismDeviceResources deviceResources;
    private readonly PrismGraphBuilder graphBuilder = new();
    private readonly PrismGraphOptimizer graphOptimizer = new();
    private readonly PrismExecutionDiagnostics diagnostics = new(detailedDiagnosticsEnabled: true);
    private readonly SdlGpuPrismUniforms uniforms = new();
    private readonly Dictionary<int, SdlGpuPrismSurfaceLease> surfaces = [];
    private readonly List<SdlGpuPrismSurfaceLease> frameLeases = [];
    private readonly HashSet<SdlGpuPrismSurfaceLease> promotedLeases = [];
    private readonly HashSet<PrismGraphNodeId> mipmappedNodes = [];
    private readonly Dictionary<StyleDistanceFieldKey, nint> styleDistanceFields = [];
    private readonly HashSet<PrismRetainedCacheKey> currentRetainedKeys = [];
    private readonly HashSet<PrismCacheOwnerToken> currentOwners = [];
    private readonly List<int> expiredSurfaceIndices = [];
    private readonly Dictionary<int, SdlGpuPrismPresentationSurface> childPresentationSurfaces = [];
    private readonly List<SdlGpuPrismSurfaceLease> presentationLeases = [];
    private readonly nint[] textures = new nint[15];
    private int executionOriginPixelX;
    private int executionOriginPixelY;
    private int executionPixelWidth;
    private int executionPixelHeight;
    private bool disposed;

    public SdlGpuPrismExecutor(
        SdlGpuWindowGraphicsSession session,
        SdlGpuDrawingBackend drawingBackend)
    {
        this.session = session;
        this.drawingBackend = drawingBackend;
        deviceResources = session.DrawingResources.PrismResources;
    }

    public PrismExecutionDiagnostics Diagnostics => diagnostics;

    public void Execute(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        Execute(commands, frameContext, session.WindowRenderTarget);
    }

    internal void Execute(
        DrawCommandList commands,
        in DrawingFrameContext frameContext,
        SdlGpuRenderTarget hostTarget)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(hostTarget);
        frameContext.EnsureCurrent(commands);
        PrismGraph sourceGraph = frameContext.BackdropLease is null
            ? graphBuilder.Build(frameContext.PrismAnalysis)
            : graphBuilder.Build(
                frameContext.PrismAnalysis,
                frameContext.BackdropLease.Metadata,
                frameContext.BackdropSourceToken);
        PrismGraphExecutionPlan plan = graphOptimizer.Optimize(sourceGraph);
        PrismGraph graph = plan.OptimizedGraph;
        ResolveExecutionExtent(plan, graph, hostTarget);
        ResolveMipmappedNodes(graph);
        ReconcileRetainedEntries(
            plan,
            graph,
            frameContext.PrismCacheInvalidations);
        long started = Stopwatch.GetTimestamp();
        long createdBefore = deviceResources.CreatedSurfaceCount;
        long reusedBefore = deviceResources.ReusedSurfaceCount;
        diagnostics.BeginExecution(
            frameContext.PrismAnalysis,
            plan,
            checked(plan.ExecutionOrder.Length + graph.Scopes.Length));

        SdlGpuDrawingBackend.CommandRangeState hostState =
            drawingBackend.CreateCommandRangeState(hostTarget);
        int hostCommandIndex = RenderHostPrelude(
            commands,
            graph,
            frameContext.StateAnalysis,
            hostTarget,
            hostState);
        try
        {
            for (int step = 0; step < plan.ExecutionOrder.Length; step++)
            {
                ReleaseExpired(plan, graph, step);
                PrismGraphNode node = graph.GetNode(plan.ExecutionOrder[step]);
                PrismRetainedCacheKey? cacheKey = CreateCacheKey(plan, node.Id);
                if (cacheKey is PrismRetainedCacheKey retainedKey &&
                    deviceResources.TryAcquireRetained(
                        retainedKey,
                        session.WindowIdentity,
                        out SdlGpuPrismSurfaceLease retainedLease))
                {
                    surfaces.Add(step, retainedLease);
                    frameLeases.Add(retainedLease);
                }
                else
                {
                    bool mipmapped = mipmappedNodes.Contains(node.Id);
                    SdlGpuPrismSurfaceLease lease = deviceResources.RentSurface(
                        session.WindowIdentity,
                        executionPixelWidth,
                        executionPixelHeight,
                        SdlGpuTextureFormat.R16G16B16A16Float,
                        mipmapped);
                    surfaces.Add(step, lease);
                    frameLeases.Add(lease);
                    RenderNode(
                        commands,
                        frameContext.StateAnalysis,
                        plan,
                        graph,
                        step,
                        node,
                        lease.Target,
                        frameContext.BackdropLease);
                    if (mipmapped)
                    {
                        session.GenerateMipmaps(lease.Target);
                    }
                    diagnostics.RecordGraphPass(node);
                    if (cacheKey is PrismRetainedCacheKey key && diagnostics.Count == 0)
                    {
                        deviceResources.Promote(key, lease);
                        promotedLeases.Add(lease);
                    }
                }

                PresentCompletedRoots(
                    commands,
                    frameContext.StateAnalysis,
                    plan,
                    graph,
                    step,
                    node,
                    hostTarget,
                    hostState,
                    ref hostCommandIndex);
                diagnostics.ObserveLiveSurfaces(surfaces.Count);
            }

            drawingBackend.RenderCommandRange(
                commands,
                hostCommandIndex,
                commands.Count,
                frameContext.StateAnalysis,
                hostTarget,
                childSurfaces: null,
                hostState);
        }
        catch (PrismSurfaceAllocationException exception)
        {
            diagnostics.Record(
                null,
                -1,
                PrismFallbackReason.SurfaceAllocationFailed,
                exception.Message);
            session.BeginRenderTarget(
                hostTarget,
                Color.Transparent,
                SdlGpuLoadOp.Load);
            drawingBackend.RenderCommandRange(
                commands,
                hostCommandIndex,
                commands.Count,
                frameContext.StateAnalysis,
                hostTarget,
                childSurfaces: null,
                hostState);
        }
        finally
        {
            foreach (SdlGpuPrismSurfaceLease lease in frameLeases)
            {
                lease.Dispose();
            }
            frameLeases.Clear();
            promotedLeases.Clear();
            surfaces.Clear();
            styleDistanceFields.Clear();
            mipmappedNodes.Clear();
            diagnostics.CompleteExecution(
                deviceResources.CreatedSurfaceCount - createdBefore,
                deviceResources.ReusedSurfaceCount - reusedBefore,
                0,
                deviceResources.TotalBytes,
                deviceResources.PeakBytes,
                Stopwatch.GetElapsedTime(started));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        foreach (SdlGpuPrismSurfaceLease lease in frameLeases)
        {
            lease.Dispose();
        }
        frameLeases.Clear();
        surfaces.Clear();
        styleDistanceFields.Clear();
        mipmappedNodes.Clear();
        currentRetainedKeys.Clear();
        currentOwners.Clear();
    }

    private int RenderHostPrelude(
        DrawCommandList commands,
        PrismGraph graph,
        DrawCommandStateAnalysis analysis,
        SdlGpuRenderTarget hostTarget,
        SdlGpuDrawingBackend.CommandRangeState hostState)
    {
        int firstRoot = commands.Count;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Depth == 0)
            {
                firstRoot = Math.Min(firstRoot, scope.BeginCommandIndex);
            }
        }
        drawingBackend.RenderCommandRange(
            commands,
            0,
            firstRoot,
            analysis,
            hostTarget,
            childSurfaces: null,
            hostState);
        return firstRoot;
    }

    private void RenderNode(
        DrawCommandList commands,
        DrawCommandStateAnalysis stateAnalysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int step,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        IBackdropFrameLease? backdropLease)
    {
        switch (node.Kind)
        {
            case PrismGraphNodeKind.ControlCapture:
                RenderControlCapture(commands, stateAnalysis, plan, graph, node, target);
                return;
            case PrismGraphNodeKind.BackdropInput:
                RenderBackdropInput(node, target, backdropLease);
                return;
            case PrismGraphNodeKind.Filter:
                RenderFilter(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Style:
                RenderStyle(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Mask:
                RenderMask(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Composite:
            case PrismGraphNodeKind.PassThroughComposite:
                RenderComposite(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ClipToBelow:
                RenderTwoInput(plan, graph, node, target, PrismGraphEdgeKind.Content,
                    PrismGraphEdgeKind.ClipBaseAlpha, 43);
                return;
            case PrismGraphNodeKind.BackdropCrop:
                RenderBackdropCrop(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ColorConversion:
                RenderColorConversion(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Fill:
            case PrismGraphNodeKind.Opacity:
                RenderSingleInput(plan, graph, node, target, 0,
                    Math.Clamp(node.Amount ?? 1, 0, 1));
                return;
            case PrismGraphNodeKind.Layer:
            case PrismGraphNodeKind.Group:
                RenderSingleInput(plan, graph, node, target, 0, 1);
                return;
            default:
                Clear(target, Color.Transparent);
                return;
        }
    }

    private void RenderControlCapture(
        DrawCommandList commands,
        DrawCommandStateAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        childPresentationSurfaces.Clear();
        presentationLeases.Clear();
        try
        {
            foreach (PrismGraphScope child in graph.Scopes)
            {
                if (child.ParentScopeIndex == scope.AnalysisScopeIndex &&
                    child.Output is PrismGraphNodeId output)
                {
                    int index = plan.GetExecutionIndex(output);
                    if (surfaces.TryGetValue(index, out SdlGpuPrismSurfaceLease? childLease))
                    {
                        PrismGraphNode childOutput = graph.GetNode(output);
                        SdlGpuPrismSurfaceLease presentationLease =
                            ConvertForPresentation(childLease.Target, child, childOutput);
                        presentationLeases.Add(presentationLease);
                        childPresentationSurfaces.Add(
                            child.BeginCommandIndex,
                            new SdlGpuPrismPresentationSurface(
                                presentationLease.Target,
                                ResolvePresentationClip(
                                    plan,
                                    child,
                                    childOutput,
                                    presentationLease.Target,
                                    executionOriginPixelX,
                                    executionOriginPixelY)));
                        diagnostics.RecordPresentation(
                            PrismExecutionPassKind.NestedPresent,
                            childOutput,
                            child.AnalysisScopeIndex);
                    }
                }
            }

            session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Clear);
            drawingBackend.RenderCommandRange(
                commands,
                scope.BeginCommandIndex + 1,
                scope.EndCommandIndex,
                analysis,
                target,
                childPresentationSurfaces,
                logicalOrigin: new Vector2(
                    executionOriginPixelX / drawingBackend.CoordinateScale,
                    executionOriginPixelY / drawingBackend.CoordinateScale));
        }
        finally
        {
            foreach (SdlGpuPrismSurfaceLease lease in presentationLeases)
            {
                lease.Dispose();
            }
            presentationLeases.Clear();
            childPresentationSurfaces.Clear();
        }
    }

    private void RenderBackdropInput(
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        IBackdropFrameLease? backdropLease)
    {
        if (backdropLease is not ISdlGpuBackdropFrameLease lease)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                backdropLease is null
                    ? PrismFallbackReason.MissingBackdrop
                    : PrismFallbackReason.UnsupportedCapability,
                "The active backdrop lease does not expose an SDL_GPU texture.");
            return;
        }
        try
        {
            nint texture = lease.Texture;
            BackdropFrameMetadata metadata = lease.Metadata;
            PrepareBaseUniforms(texture, texture, 0, 1);
            uniforms[1] = new Vector4(
                target.PixelWidth / (float)Math.Max(metadata.PixelWidth, 1),
                target.PixelHeight / (float)Math.Max(metadata.PixelHeight, 1),
                executionOriginPixelX / (float)Math.Max(metadata.PixelWidth, 1),
                executionOriginPixelY / (float)Math.Max(metadata.PixelHeight, 1));
            RenderPrepared(target, texture, texture);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.UnsupportedCapability, exception.Message);
        }
    }

    private void RenderBackdropCrop(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0 ||
            node.BackdropSourceBounds is not DrawRect sourceBounds ||
            sourceBounds.Width <= 0 ||
            sourceBounds.Height <= 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        BackdropFrameMetadata? metadata = FindBackdropMetadata(plan, graph, node.Id);
        if (metadata is not BackdropFrameMetadata backdropMetadata)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.MissingBackdrop,
                "The backdrop crop has no raster metadata.");
            return;
        }

        DrawRect backdropBounds = scope.Output is PrismGraphNodeId output
            ? plan.GetNodePlan(output).Bounds
            : scope.Bounds;
        SdlRect destination = ResolveBackdropDestination(
            backdropBounds,
            scope.PixelScale,
            target,
            executionOriginPixelX,
            executionOriginPixelY);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        Matrix3x2 transform = backdropMetadata.CoordinateTransform;
        float pixelScale = scope.PixelScale;
        nint source = GetSurface(sourceIndex).SampleTexture;
        PrepareBaseUniforms(source, source, 1, 1);
        uniforms[0] = new Vector4(
            1,
            1f / backdropMetadata.PixelWidth,
            1f / backdropMetadata.PixelHeight,
            executionOriginPixelX);
        uniforms[6] = new Vector4(
            1,
            (float)backdropMetadata.AlphaMode,
            0,
            0);
        uniforms[7] = new Vector4(
            transform.M11 / (pixelScale * backdropMetadata.PixelWidth),
            transform.M21 / (pixelScale * backdropMetadata.PixelWidth),
            ((executionOriginPixelX * transform.M11 +
                executionOriginPixelY * transform.M21) / pixelScale +
                transform.M31) / backdropMetadata.PixelWidth,
            0);
        uniforms[8] = new Vector4(
            transform.M12 / (pixelScale * backdropMetadata.PixelHeight),
            transform.M22 / (pixelScale * backdropMetadata.PixelHeight),
            ((executionOriginPixelX * transform.M12 +
                executionOriginPixelY * transform.M22) / pixelScale +
                transform.M32) / backdropMetadata.PixelHeight,
            0);
        RenderPrepared(target, source, source, destination);
    }

    private void RenderColorConversion(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        if (node.BackdropMetadata is null)
        {
            if (node.ColorProfile is PrismColorProfile profile && Enum.IsDefined(profile))
            {
                RenderSingleInput(
                    plan,
                    graph,
                    node,
                    target,
                    SdlGpuPrismKernelSelector.ForInputColorProfile(profile),
                    1);
                return;
            }

            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }

        PrismColorProfile sourceProfile = node.BackdropMetadata.Value.ColorProfile;
        if (node.ColorProfile is not PrismColorProfile targetProfile ||
            !Enum.IsDefined(sourceProfile) ||
            !Enum.IsDefined(targetProfile))
        {
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }

        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        nint source = GetSurface(sourceIndex).SampleTexture;
        PrepareBaseUniforms(source, source, 2, 1);
        uniforms[23] = new Vector4(
            (float)sourceProfile,
            (float)targetProfile,
            0,
            0);
        RenderPrepared(target, source, source);
    }

    private void RenderMask(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        if (node.MaskPass is null or PrismMaskPass.Extract)
        {
            PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
            if (node.Resource is not PrismResourceId id ||
                !TryResolveImage(scope, node, id, required: true, 0, out nint texture, out _))
            {
                Clear(target, Color.White);
                return;
            }
            PrepareBaseUniforms(texture, texture, 41, 1);
            uniforms[6] = new Vector4(
                1,
                (float)(node.MaskChannel ?? PrismMaskChannel.Alpha),
                (node.Feather ?? 0) > 0 ? 1 : node.Density ?? 1,
                node.Invert == true ? 1 : 0);
            ResolveScopeUvMapping(scope, out Vector3 rowX, out Vector3 rowY);
            uniforms[7] = new Vector4(rowX, 0);
            uniforms[8] = new Vector4(rowY, 0);
            RenderPrepared(target, texture, texture);
            return;
        }
        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            Clear(target, Color.White);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        PrismGraphScope owner = FindScope(graph, node.AnalysisScopeIndex);
        float scale = MathF.Max(
            new Vector2(owner.EffectiveTransform.M11, owner.EffectiveTransform.M12).Length(),
            new Vector2(owner.EffectiveTransform.M21, owner.EffectiveTransform.M22).Length());
        float radius = (node.Feather ?? 0) * scale * owner.PixelScale;
        PrepareBaseUniforms(source, source, 42, 1);
        uniforms[6] = new Vector4(1, 0,
            node.MaskPass == PrismMaskPass.FeatherVertical ? node.Density ?? 1 : 1, 0);
        uniforms[9] = node.MaskPass == PrismMaskPass.FeatherHorizontal
            ? new Vector4(radius / target.PixelWidth, 0, 0, 0)
            : new Vector4(0, radius / target.PixelHeight, 0, 0);
        RenderPrepared(target, source, source);
    }

    private void RenderStyle(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int contentIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        int sourceIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.StyleSource);
        if (contentIndex < 0 || sourceIndex < 0 || node.Style is not PrismStyleId style)
        {
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        PrismStylePlan stylePlan = PrismStylePlanner.Create(node, scope);
        nint content = GetSurface(contentIndex).SampleTexture;
        nint source = GetSurface(sourceIndex).SampleTexture;
        int backdropIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeBackground);
        nint backdrop = backdropIndex >= 0
            ? GetSurface(backdropIndex).SampleTexture
            : source;
        nint styleTexture = source;
        bool resourceAvailable = false;
        if (style == PrismStyleId.GradientOverlay)
        {
            PrismGradientMapResource gradient = PrismGradientOverlayStyle.DefaultGradient;
            long identity = 0;
            long version = 0;
            if (stylePlan.ResourceEnabled &&
                !scope.Resources.TryGetGradientMap(
                    stylePlan.Resource,
                    out gradient,
                    out identity,
                    out version))
            {
                diagnostics.Record(
                    node.Id,
                    node.AnalysisScopeIndex,
                    PrismFallbackReason.MissingResource,
                    $"Gradient resource '{stylePlan.Resource}' is not available.");
                RenderKernel(target, content, content, 0, 1, node, null);
                return;
            }

            styleTexture = deviceResources.GetGradientOverlayTexture(
                session,
                stylePlan.Resource,
                gradient,
                identity,
                version,
                (PrismGradientInterpolation)stylePlan.GradientMethod,
                scope.CompositionSettings.WorkingColorProfile);
            resourceAvailable = true;
        }
        else if (stylePlan.ResourceEnabled)
        {
            if (!TryResolveImage(
                    scope,
                    node,
                    stylePlan.Resource,
                    stylePlan.ResourceRequired,
                    source,
                    out nint resolved,
                    out resourceAvailable) &&
                stylePlan.ResourceRequired)
            {
                RenderKernel(target, content, content, 0, 1, node, null);
                return;
            }
            styleTexture = resolved;
        }
        PrismStyleSamplingGeometry geometry = PrismStylePlanner.ResolveSamplingGeometry(stylePlan, scope);
        ResolveScopeUvMapping(scope, out Vector3 rowX, out Vector3 rowY);
        bool alignGradientWithLayer =
            (stylePlan.Flags & PrismStyleFlags.AlignWithLayer) != 0;
        Vector2 gradientOffset = alignGradientWithLayer
            ? new Vector2(
                stylePlan.Offset.X / MathF.Max(scope.ControlBounds.Width, 1),
                stylePlan.Offset.Y / MathF.Max(scope.ControlBounds.Height, 1))
            : new Vector2(
                (stylePlan.Offset.X * scope.PixelScale) / Math.Max(target.PixelWidth, 1),
                (stylePlan.Offset.Y * scope.PixelScale) / Math.Max(target.PixelHeight, 1));
        float gradientAspect = alignGradientWithLayer
            ? scope.ControlBounds.Width / MathF.Max(scope.ControlBounds.Height, 1)
            : target.PixelWidth / (float)Math.Max(target.PixelHeight, 1);
        nint maskTexture = source;

        if (style == PrismStyleId.DropShadow)
        {
            using SdlGpuPrismSurfaceLease scratchA = deviceResources.RentSurface(
                session.WindowIdentity,
                target.PixelWidth,
                target.PixelHeight,
                SdlGpuTextureFormat.R16G16B16A16Float,
                mipmapped: false);
            using SdlGpuPrismSurfaceLease scratchB = deviceResources.RentSurface(
                session.WindowIdentity,
                target.PixelWidth,
                target.PixelHeight,
                SdlGpuTextureFormat.R16G16B16A16Float,
                mipmapped: false);
            maskTexture = PrepareDropShadowMask(
                source,
                scratchA.Target,
                scratchB.Target,
                geometry.Size,
                geometry.Spread,
                stylePlan.Technique);
        }
        else if (style is PrismStyleId.OuterGlow or
            PrismStyleId.BevelEmboss or
            PrismStyleId.Stroke)
        {
            maskTexture = GetOrPrepareStyleDistanceField(
                source,
                target,
                stylePlan,
                geometry,
                rowX,
                rowY,
                styleTexture,
                backdrop,
                resourceAvailable,
                backdropIndex >= 0,
                gradientAspect,
                gradientOffset);
            if (style == PrismStyleId.BevelEmboss)
            {
                using SdlGpuPrismSurfaceLease heightLease = deviceResources.RentSurface(
                    session.WindowIdentity,
                    target.PixelWidth,
                    target.PixelHeight,
                    SdlGpuTextureFormat.R32G32B32A32Float,
                    mipmapped: false);
                using SdlGpuPrismSurfaceLease lightingLease = deviceResources.RentSurface(
                    session.WindowIdentity,
                    target.PixelWidth,
                    target.PixelHeight,
                    SdlGpuTextureFormat.R32G32B32A32Float,
                    mipmapped: false);
                SdlGpuRenderTarget heightTarget = heightLease.Target;
                SdlGpuRenderTarget lightingTarget = lightingLease.Target;

                PrepareBaseUniforms(source, source, 87, 1);
                ConfigureStyle(
                    stylePlan,
                    gradientOverlay: false,
                    geometry,
                    rowX,
                    rowY,
                    styleTexture,
                    maskTexture,
                    source,
                    backdrop,
                    resourceAvailable,
                    backdropIndex >= 0,
                    gradientAspect,
                    gradientOffset);
                RenderPrepared(heightTarget, source, source);

                PrepareBaseUniforms(heightTarget.SampleTexture, source, 88, 1);
                ConfigureStyle(
                    stylePlan,
                    gradientOverlay: false,
                    geometry,
                    rowX,
                    rowY,
                    styleTexture,
                    heightTarget.SampleTexture,
                    source,
                    backdrop,
                    resourceAvailable,
                    backdropIndex >= 0,
                    gradientAspect,
                    gradientOffset);
                RenderPrepared(
                    lightingTarget,
                    heightTarget.SampleTexture,
                    source);
                maskTexture = lightingTarget.SampleTexture;
            }
        }

        PrepareBaseUniforms(content, source, 82, 1);
        ConfigureStyle(
            stylePlan,
            style == PrismStyleId.GradientOverlay,
            geometry,
            rowX,
            rowY,
            styleTexture,
            maskTexture,
            source,
            backdrop,
            resourceAvailable,
            backdropIndex >= 0,
            gradientAspect,
            gradientOffset);
        RenderPrepared(target, content, source);
    }

    private nint GetOrPrepareStyleDistanceField(
        nint source,
        SdlGpuRenderTarget target,
        PrismStylePlan stylePlan,
        PrismStyleSamplingGeometry geometry,
        Vector3 rowX,
        Vector3 rowY,
        nint styleTexture,
        nint backdrop,
        bool resourceAvailable,
        bool backdropAvailable,
        float gradientAspect,
        Vector2 gradientOffset)
    {
        StyleDistanceFieldKey key = new(
            source,
            target.PixelWidth,
            target.PixelHeight,
            stylePlan.Kind == (int)PrismStyleId.Stroke);
        if (styleDistanceFields.TryGetValue(key, out nint cached))
        {
            return cached;
        }

        SdlGpuPrismSurfaceLease scratchA = deviceResources.RentSurface(
            session.WindowIdentity,
            target.PixelWidth,
            target.PixelHeight,
            SdlGpuTextureFormat.R32G32B32A32Float,
            mipmapped: false);
        SdlGpuPrismSurfaceLease scratchB = deviceResources.RentSurface(
            session.WindowIdentity,
            target.PixelWidth,
            target.PixelHeight,
            SdlGpuTextureFormat.R32G32B32A32Float,
            mipmapped: false);
        frameLeases.Add(scratchA);
        frameLeases.Add(scratchB);
        nint prepared = PrepareStyleDistanceField(
            source,
            scratchA.Target,
            scratchB.Target,
            stylePlan,
            geometry,
            rowX,
            rowY,
            styleTexture,
            backdrop,
            resourceAvailable,
            backdropAvailable,
            gradientAspect,
            gradientOffset);
        styleDistanceFields.Add(key, prepared);
        return prepared;
    }

    private void ConfigureStyle(
        PrismStylePlan stylePlan,
        bool gradientOverlay,
        PrismStyleSamplingGeometry geometry,
        Vector3 rowX,
        Vector3 rowY,
        nint styleTexture,
        nint maskTexture,
        nint source,
        nint backdrop,
        bool resourceAvailable,
        bool backdropAvailable,
        float gradientAspect,
        Vector2 gradientOffset)
    {
        textures[4] = styleTexture;
        textures[5] = maskTexture;
        textures[8] = backdrop;
        textures[9] = deviceResources.GetGradientDitherTexture(session);
        textures[11] = maskTexture;
        textures[12] = source;
        uniforms[10] = stylePlan.PrimaryColor;
        uniforms[11] = stylePlan.SecondaryColor;
        uniforms[12] = new Vector4(
            geometry.Offset.X / Math.Max(executionPixelWidth, 1),
            geometry.Offset.Y / Math.Max(executionPixelHeight, 1),
            geometry.Size,
            geometry.Spread);
        uniforms[13] = new Vector4(
            stylePlan.Angle * MathF.PI / 180,
            stylePlan.Altitude * MathF.PI / 180,
            stylePlan.Depth,
            geometry.Soften);
        uniforms[14] = new Vector4(stylePlan.Opacity, stylePlan.SecondaryOpacity,
            stylePlan.Noise, stylePlan.Jitter);
        uniforms[15] = new Vector4(
            stylePlan.Scale,
            gradientOverlay
                ? gradientAspect
                : stylePlan.TextureDepth,
            gradientOverlay
                ? gradientOffset.X
                : stylePlan.Offset.X,
            gradientOverlay
                ? gradientOffset.Y
                : stylePlan.Offset.Y);
        uniforms[16] = new Vector4(
            stylePlan.Kind,
            SdlGpuPrismKernelSelector.ResolveBlendMode(stylePlan.BlendMode),
            SdlGpuPrismKernelSelector.ResolveBlendMode(stylePlan.SecondaryBlendMode),
            (int)stylePlan.PaintKind);
        uniforms[17] = new Vector4(stylePlan.Contour, stylePlan.DetailContour,
            stylePlan.Technique, stylePlan.Position);
        uniforms[18] = new Vector4(stylePlan.Origin, stylePlan.Direction,
            stylePlan.GradientMethod, stylePlan.GradientStyle);
        uniforms[19] = new Vector4(stylePlan.BevelStyle, (int)stylePlan.Flags,
            stylePlan.Range, 0);
        uniforms[20] = new Vector4(rowX, 0);
        uniforms[21] = new Vector4(rowY, 0);
        uniforms[22] = new Vector4(
            resourceAvailable ? 1 : 0,
            backdropAvailable ? 1 : 0,
            0,
            0);
    }

    private nint PrepareDropShadowMask(
        nint source,
        SdlGpuRenderTarget scratchA,
        SdlGpuRenderTarget scratchB,
        float size,
        float spread,
        float technique)
    {
        nint prepared = source;
        if (spread >= 0.5f)
        {
            RenderStyleMaskPass(scratchA, prepared, 83, MathF.Ceiling(spread), horizontal: true);
            RenderStyleMaskPass(scratchB, scratchA.SampleTexture, 83, MathF.Ceiling(spread), horizontal: false);
            prepared = scratchB.SampleTexture;
        }

        float techniqueScale = technique < 0.5f
            ? 1f
            : technique < 1.5f ? 0.65f : 0.8f;
        float radius = MathF.Max(MathF.Ceiling(size * techniqueScale * 1.5f), 1f);
        RenderStyleMaskPass(scratchA, prepared, 84, radius, horizontal: true);
        RenderStyleMaskPass(scratchB, scratchA.SampleTexture, 84, radius, horizontal: false);
        return scratchB.SampleTexture;
    }

    private nint PrepareStyleDistanceField(
        nint source,
        SdlGpuRenderTarget scratchA,
        SdlGpuRenderTarget scratchB,
        PrismStylePlan stylePlan,
        PrismStyleSamplingGeometry geometry,
        Vector3 rowX,
        Vector3 rowY,
        nint styleTexture,
        nint backdrop,
        bool resourceAvailable,
        bool backdropAvailable,
        float gradientAspect,
        Vector2 gradientOffset)
    {
        PrepareBaseUniforms(source, source, 85, 1);
        ConfigureStyle(
            stylePlan,
            gradientOverlay: false,
            geometry,
            rowX,
            rowY,
            styleTexture,
            source,
            source,
            backdrop,
            resourceAvailable,
            backdropAvailable,
            gradientAspect,
            gradientOffset);
        RenderPrepared(scratchA, source, source);

        SdlGpuRenderTarget read = scratchA;
        SdlGpuRenderTarget write = scratchB;
        int extent = Math.Max(scratchA.PixelWidth, scratchA.PixelHeight);
        int jump = 1;
        while (jump < extent)
        {
            jump <<= 1;
        }
        jump >>= 1;
        while (jump >= 1)
        {
            RenderStyleDistanceFloodPass(write, read.SampleTexture, jump);
            (read, write) = (write, read);
            jump >>= 1;
        }
        RenderStyleDistanceFloodPass(write, read.SampleTexture, 1);
        return write.SampleTexture;
    }

    private void RenderStyleDistanceFloodPass(
        SdlGpuRenderTarget target,
        nint source,
        int jump)
    {
        PrepareBaseUniforms(source, source, 86, 1);
        uniforms[6] = new Vector4(1, 0, 0, 0);
        uniforms[7] = new Vector4(1, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1, 0, 0);
        uniforms[9] = new Vector4(
            jump / (float)target.PixelWidth,
            jump / (float)target.PixelHeight,
            0,
            0);
        RenderPrepared(target, source, source);
    }

    private void RenderStyleMaskPass(
        SdlGpuRenderTarget target,
        nint source,
        int kernelId,
        float radius,
        bool horizontal)
    {
        PrepareBaseUniforms(source, source, kernelId, 1);
        textures[12] = source;
        uniforms[6] = new Vector4(1, 0, radius, 0);
        uniforms[7] = new Vector4(1, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1, 0, 0);
        uniforms[9] = horizontal
            ? new Vector4(1f / target.PixelWidth, 0, 0, 0)
            : new Vector4(0, 1f / target.PixelHeight, 0, 0);
        RenderPrepared(target, source, source);
    }

    private void RenderFilter(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int sourceIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        if (sourceIndex < 0 || node.Filter is not PrismFilterId filter)
        {
            Clear(target, Color.Transparent);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        int kernelId = SdlGpuPrismKernelSelector.ForNode(node);
        PrepareBaseUniforms(source, source, kernelId, Math.Clamp(node.Amount ?? 1, 0, 1));

        if (node.NeighborhoodPlan is PrismNeighborhoodPlan neighborhood)
        {
            PrismNeighborhoodPass pass = neighborhood.Passes[node.NeighborhoodPassIndex];
            ResolveFilterResource(scope, node, neighborhood.Resource,
                neighborhood.ResourceRequired, source, out nint resource, out bool available);
            textures[1] = resource;
            textures[6] = FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, source);
            uniforms[23] = new Vector4((int)neighborhood.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile, (int)pass.Kind,
                available ? 1 : 0);
            SetFilterOptions(neighborhood.Options0, neighborhood.Options1,
                neighborhood.Options2, neighborhood.Options3);
            uniforms[33] = new Vector4(
                pass.RadiusX,
                pass.RadiusY,
                pass.SampleCount,
                SdlGpuPrismKernelSelector.ResolveBlendMode(neighborhood.BlendMode));
        }
        else if (node.ResamplingPlan is PrismResamplingPlan resampling)
        {
            PrismResamplingPass pass = resampling.Passes[node.ResamplingPassIndex];
            ResolveFilterResource(scope, node, resampling.PrimaryResource,
                resampling.PrimaryResourceRequired, source, out nint primary, out bool available);
            ResolveFilterResource(scope, node, resampling.AuxiliaryResource,
                resampling.AuxiliaryResourceRequired, source, out nint auxiliary, out bool auxAvailable);
            textures[1] = primary;
            textures[6] = FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, auxiliary);
            uniforms[23] = new Vector4((int)resampling.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile, (int)pass.Kind,
                available ? 1 : 0);
            SetFilterOptions(resampling.Options0, resampling.Options1, resampling.Options2,
                resampling.Options3, resampling.Options4, resampling.Options5);
            uniforms[30] = new Vector4(auxAvailable ? 1 : 0, 0, 0, 0);
            uniforms[33] = new Vector4(
                0,
                0,
                0,
                SdlGpuPrismKernelSelector.ResolveBlendMode(resampling.BlendMode));
        }
        else if (node.CatalogFilterPlan is PrismCatalogFilterPlan catalog)
        {
            PrismCatalogFilterPass pass = catalog.Passes[node.CatalogFilterPassIndex];
            ResolveFilterResource(scope, node, catalog.PrimaryResource,
                catalog.PrimaryResourceRequired, source, out nint primary, out bool available);
            ResolveFilterResource(scope, node, catalog.AuxiliaryResource,
                catalog.AuxiliaryResourceRequired, source, out nint auxiliary, out bool auxAvailable);
            textures[1] = primary;
            bool usesWaveNoise = filter is
                PrismFilterId.Clouds or
                PrismFilterId.DifferenceClouds;
            bool usesSpatter = filter == PrismFilterId.Spatter;
            bool usesBlueNoisePoints = usesSpatter ||
                filter == PrismFilterId.SprayedStrokes;
            nint filterAuxiliary = PrismCatalogFilterPlanner.RequiresOriginalInput(filter, pass)
                ? FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, source)
                : usesWaveNoise
                    ? deviceResources.GetWaveNoiseTexture(session, catalog.WaveNoiseTable)
                    : usesBlueNoisePoints
                        ? deviceResources.GetSpatterPointTexture(session)
                        : auxiliary;
            textures[6] = filterAuxiliary;
            textures[10] = auxiliary;
            textures[13] = usesWaveNoise ? filterAuxiliary : textures[13];
            textures[14] = usesBlueNoisePoints ? filterAuxiliary : textures[14];
            uniforms[23] = new Vector4((int)catalog.Filter,
                (int)scope.CompositionSettings.WorkingColorProfile,
                (int)catalog.Primitive, (available ? 1 : 0) + (auxAvailable ? 2 : 0));
            SetFilterOptions(catalog.Options0, catalog.Options1, catalog.Options2,
                usesWaveNoise
                    ? PackSeed(catalog.WaveNoiseSeed)
                    : usesSpatter
                        ? PackSeed(catalog.SpatterSeed)
                        : catalog.Options3,
                catalog.Options4, catalog.Options5, catalog.Options6,
                catalog.Options7, catalog.Options8);
            uniforms[33] = new Vector4(
                usesWaveNoise ? catalog.WaveNoiseTable.Normalization : pass.RadiusX,
                pass.RadiusY,
                (int)pass.Kind + (pass.Iteration * 4),
                SdlGpuPrismKernelSelector.ResolveBlendMode(catalog.BlendMode));
        }
        else if (PrismAdjustmentPlanner.IsSupported(filter))
        {
            PrismAdjustmentPlan adjustment = PrismAdjustmentPlanner.Create(node, scope);
            ResolveFilterResource(scope, node, adjustment.Resource,
                adjustment.ResourceRequired, source, out nint resource, out bool available);
            if (filter == PrismFilterId.Threshold)
            {
                using SdlGpuPrismSurfaceLease threshold = RenderOtsuThreshold(
                    source,
                    adjustment,
                    scope.CompositionSettings.WorkingColorProfile);
                PrepareBaseUniforms(
                    source,
                    threshold.Target.SampleTexture,
                    kernelId,
                    Math.Clamp(node.Amount ?? 1, 0, 1));
                textures[1] = threshold.Target.SampleTexture;
                uniforms[23] = new Vector4(
                    (int)adjustment.Operation,
                    (int)scope.CompositionSettings.WorkingColorProfile,
                    SdlGpuPrismKernelSelector.ResolveBlendMode(adjustment.BlendMode),
                    0);
                SetFilterOptions(adjustment.Parameters0, adjustment.Parameters1,
                    adjustment.Parameters2, adjustment.Parameters3, adjustment.Parameters4,
                    adjustment.Parameters5, adjustment.Parameters6, adjustment.Parameters7,
                    adjustment.Parameters8, adjustment.Parameters9);
                RenderPrepared(target, source, textures[1]);
                return;
            }
            textures[1] = resource;
            uniforms[23] = new Vector4(
                (int)adjustment.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile,
                SdlGpuPrismKernelSelector.ResolveBlendMode(adjustment.BlendMode),
                0);
            SetFilterOptions(adjustment.Parameters0, adjustment.Parameters1,
                adjustment.Parameters2, adjustment.Parameters3, adjustment.Parameters4,
                adjustment.Parameters5, adjustment.Parameters6, adjustment.Parameters7,
                adjustment.Parameters8, adjustment.Parameters9);
            if (adjustment.ResourceRequired && !available)
            {
                RenderKernel(target, source, source, 0, 1, node, null);
                return;
            }
        }
        else
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingKernel, node.DiagnosticName);
            RenderKernel(target, source, source, 0, 1, node, null);
            return;
        }
        RenderPrepared(target, source, textures[1]);
    }

    private SdlGpuPrismSurfaceLease RenderOtsuThreshold(
        nint source,
        PrismAdjustmentPlan plan,
        PrismColorProfile workingProfile)
    {
        using SdlGpuPrismSurfaceLease cdf = deviceResources.RentSurface(
            session.WindowIdentity,
            PrismThresholdAnalysis.BinCount,
            1,
            SdlGpuTextureFormat.R32Float,
            mipmapped: false);
        SdlGpuPrismSurfaceLease threshold = deviceResources.RentSurface(
            session.WindowIdentity,
            1,
            1,
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            mipmapped: false);
        try
        {
            PrepareBaseUniforms(source, source, 4, 1);
            uniforms[23] = new Vector4(0, (int)workingProfile, 0, 0);
            RenderPrepared(cdf.Target, source, source);

            PrepareBaseUniforms(cdf.Target.SampleTexture, cdf.Target.SampleTexture, 6, 1);
            uniforms[23] = new Vector4(plan.Parameters0.X, 0, 0, 0);
            RenderPrepared(
                threshold.Target,
                cdf.Target.SampleTexture,
                cdf.Target.SampleTexture);
            return threshold;
        }
        catch
        {
            threshold.Dispose();
            throw;
        }
    }

    private void RenderComposite(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int maskIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.MaskAlpha);
        if (maskIndex >= 0)
        {
            RenderTwoInput(plan, graph, node, target, PrismGraphEdgeKind.Content,
                PrismGraphEdgeKind.MaskAlpha, 40);
            return;
        }
        int foreground = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.CompositeForeground);
        if (foreground < 0)
        {
            foreground = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        }
        if (foreground < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }
        int background = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.CompositeBackground);
        nint source = GetSurface(foreground).SampleTexture;
        nint secondary = background >= 0 ? GetSurface(background).SampleTexture : source;
        PrepareBaseUniforms(source, secondary, SdlGpuPrismKernelSelector.ForNode(node), 1);
        Vector4 maskControl = uniforms[6];
        maskControl.X = background >= 0 ? 1 : 0;
        uniforms[6] = maskControl;
        if (node.LayerSettings is PrismGraphLayerSettings settings)
        {
            uniforms[2] = ResolveBlendChannels(settings.BlendChannels);
            uniforms[3] = new Vector4((int)settings.Knockout, 0,
                (int)settings.BlendIfChannel,
                PrismBlendMath.NormalizeDissolveSeed(
                    settings.DissolveSeed,
                    node.DefinitionNodeId?.Value ?? 0));
            uniforms[4] = ResolveBlendRange(settings.ThisLayerRange);
            uniforms[5] = ResolveBlendRange(settings.UnderlyingRange);
        }
        int knockoutBackdrop = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.KnockoutBackdrop);
        int knockoutShape = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.KnockoutShape);
        if (knockoutBackdrop >= 0)
        {
            textures[2] = GetSurface(knockoutBackdrop).SampleTexture;
            Vector4 blendControl = uniforms[3];
            blendControl.Y = 1;
            uniforms[3] = blendControl;
        }
        if (knockoutShape >= 0)
        {
            textures[3] = GetSurface(knockoutShape).SampleTexture;
        }
        RenderPrepared(target, source, secondary);
    }

    private void RenderSingleInput(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        int kernelId,
        float opacity)
    {
        int input = FindAnyInputIndex(plan, graph, node.Id);
        if (input < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }
        nint source = GetSurface(input).SampleTexture;
        RenderKernel(target, source, source, kernelId, opacity, node, null);
    }

    private void RenderTwoInput(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        PrismGraphEdgeKind sourceKind,
        PrismGraphEdgeKind secondaryKind,
        int kernelId)
    {
        int sourceIndex = FindInputIndex(plan, graph, node.Id, sourceKind);
        int secondaryIndex = FindInputIndex(plan, graph, node.Id, secondaryKind);
        if (sourceIndex < 0 || secondaryIndex < 0)
        {
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        nint secondary = GetSurface(secondaryIndex).SampleTexture;
        RenderKernel(target, source, secondary, kernelId, 1, node, null);
    }

    private void RenderKernel(
        SdlGpuRenderTarget target,
        nint source,
        nint secondary,
        int kernelId,
        float opacity,
        PrismGraphNode? node,
        Action<SdlGpuPrismUniforms>? configure)
    {
        try
        {
            PrepareBaseUniforms(source, secondary, kernelId, opacity);
            configure?.Invoke(uniforms);
            RenderPrepared(target, source, secondary);
        }
        catch (Exception exception)
        {
            if (node is null)
            {
                throw;
            }
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.ShaderUnavailable, exception.Message);
            if (kernelId == 0)
            {
                throw;
            }
            PrepareBaseUniforms(source, source, 0, opacity);
            RenderPrepared(target, source, source);
        }
    }

    private void PrepareBaseUniforms(nint source, nint secondary, int kernelId, float opacity)
    {
        uniforms.Reset();
        Array.Fill(textures, deviceResources.GetWhiteTexture(session));
        textures[0] = source != 0 ? source : textures[0];
        textures[1] = secondary != 0 ? secondary : textures[0];
        uniforms[0] = new Vector4(opacity,
            1f / Math.Max(executionPixelWidth, 1),
            1f / Math.Max(executionPixelHeight, 1),
            executionOriginPixelX);
        uniforms[1] = new Vector4(1, 1, 0, 0);
        uniforms[34] = new Vector4(
            executionPixelWidth,
            executionPixelHeight,
            kernelId,
            executionOriginPixelY);
    }

    private void RenderPrepared(
        SdlGpuRenderTarget target,
        nint source,
        nint secondary,
        SdlRect? destination = null)
    {
        textures[0] = source;
        textures[1] = secondary;
        if (textures.Contains(target.SampleTexture))
        {
            throw new InvalidOperationException(
                "An SDL_GPU Prism render pass cannot sample from its active color target.");
        }
        session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Clear);
        SdlRect destinationRect = destination ?? new SdlRect(
            0,
            0,
            target.PixelWidth,
            target.PixelHeight);
        ISdlApi api = session.Api;
        nint pass = session.ActiveRenderPass;
        api.BindGpuGraphicsPipeline(pass, deviceResources.GetPipeline(target.ColorFormat));
        Span<float> viewport =
        [
            target.PixelWidth,
            target.PixelHeight,
            0,
            0,
            destinationRect.X,
            destinationRect.Y,
            destinationRect.Width,
            destinationRect.Height
        ];
        api.PushGpuVertexUniformData(session.ActiveCommandBuffer, 0,
            MemoryMarshal.AsBytes(viewport));
        api.PushGpuFragmentUniformData(session.ActiveCommandBuffer, 0, uniforms.Pack());
        for (int slot = 0; slot < textures.Length; slot++)
        {
            nint sampler = GetSamplerForSlot(slot);
            api.BindGpuFragmentSampler(pass, checked((uint)slot),
                new SdlGpuTextureSamplerBinding(textures[slot], sampler));
        }
        api.SetGpuScissor(pass, destinationRect);
        api.SetGpuStencilReference(pass, 0);
        api.DrawGpuPrimitives(pass, 3, 0);
    }

    private nint GetSamplerForSlot(int slot) => slot switch
    {
        4 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Linear,
            DrawAddressMode.Wrap),
        7 or 9 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Point,
            DrawAddressMode.Wrap),
        10 or 11 or 13 or 14 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Point,
            DrawAddressMode.Clamp),
        _ => session.DrawingResources.GetSampler(
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp)
    };

    private void Clear(SdlGpuRenderTarget target, Color color) =>
        session.BeginRenderTarget(target, color, SdlGpuLoadOp.Clear);

    private void PresentCompletedRoots(
        DrawCommandList commands,
        DrawCommandStateAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int step,
        PrismGraphNode node,
        SdlGpuRenderTarget hostTarget,
        SdlGpuDrawingBackend.CommandRangeState hostState,
        ref int hostCommandIndex)
    {
        int previousBeginCommandIndex = -1;
        while (TryFindNextCompletedRoot(
            graph,
            node.Id,
            previousBeginCommandIndex,
            out PrismGraphScope scope))
        {
            using SdlGpuPrismSurfaceLease presentationLease =
                ConvertForPresentation(surfaces[step].Target, scope, node);
            SdlRect? presentationClip = ResolvePresentationClip(
                plan,
                scope,
                node,
                hostTarget);
            session.BeginRenderTarget(hostTarget, Color.Transparent, SdlGpuLoadOp.Load);
            drawingBackend.DrawPrismTexture(
                presentationLease.Target.SampleTexture,
                hostTarget,
                presentationClip,
                new DrawRect(
                    executionOriginPixelX / drawingBackend.CoordinateScale,
                    executionOriginPixelY / drawingBackend.CoordinateScale,
                    presentationLease.Target.PixelWidth / drawingBackend.CoordinateScale,
                    presentationLease.Target.PixelHeight / drawingBackend.CoordinateScale));
            diagnostics.RecordPresentation(
                PrismExecutionPassKind.RootPresent,
                node,
                scope.AnalysisScopeIndex);
            hostCommandIndex = scope.EndCommandIndex + 1;
            int nextRoot = commands.Count;
            foreach (PrismGraphScope candidate in graph.Scopes)
            {
                if (candidate.Depth == 0 &&
                    candidate.BeginCommandIndex > scope.BeginCommandIndex)
                {
                    nextRoot = Math.Min(nextRoot, candidate.BeginCommandIndex);
                }
            }
            drawingBackend.RenderCommandRange(
                commands,
                hostCommandIndex,
                nextRoot,
                analysis,
                hostTarget,
                childSurfaces: null,
                hostState);
            hostCommandIndex = nextRoot;
            previousBeginCommandIndex = scope.BeginCommandIndex;
        }
    }

    private static bool TryFindNextCompletedRoot(
        PrismGraph graph,
        PrismGraphNodeId output,
        int previousBeginCommandIndex,
        out PrismGraphScope result)
    {
        result = default;
        bool found = false;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Depth != 0 ||
                scope.Output != output ||
                scope.BeginCommandIndex <= previousBeginCommandIndex ||
                found && scope.BeginCommandIndex >= result.BeginCommandIndex)
            {
                continue;
            }
            result = scope;
            found = true;
        }
        return found;
    }

    private SdlGpuPrismSurfaceLease ConvertForPresentation(
        SdlGpuRenderTarget source,
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        SdlGpuPrismSurfaceLease lease = deviceResources.RentSurface(
            session.WindowIdentity,
            source.PixelWidth,
            source.PixelHeight,
            source.ColorFormat,
            mipmapped: false);
        try
        {
            RenderKernel(
                lease.Target,
                source.SampleTexture,
                source.SampleTexture,
                SdlGpuPrismKernelSelector.ForPresentation(
                    scope.CompositionSettings.WorkingColorProfile),
                1,
                node,
                null);
            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private PrismRetainedCacheKey? CreateCacheKey(
        PrismGraphExecutionPlan plan,
        PrismGraphNodeId nodeId)
    {
        PrismRetainedRasterContext context = new(
            executionPixelWidth,
            executionPixelHeight,
            PrismColorProfile.Srgb,
            ToBackdropFormat(session.Diagnostics.TextureFormat),
            PrismSampling.Linear,
            Capabilities,
            ShaderPackageVersion);
        return PrismRetainedCacheKey.TryCreate(plan, nodeId, context, out PrismRetainedCacheKey key)
            ? key
            : null;
    }

    private void ReconcileRetainedEntries(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismCacheInvalidationQueue? queue)
    {
        currentOwners.Clear();
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            currentOwners.Add(scope.CacheOwnerToken);
        }

        while (queue?.TryDequeue(out PrismCacheInvalidation invalidation) == true)
        {
            if (invalidation.Kind == PrismCacheInvalidationKind.All ||
                !currentOwners.Contains(invalidation.OwnerToken))
            {
                deviceResources.Invalidate(invalidation);
            }
        }

        currentRetainedKeys.Clear();
        foreach (PrismGraphNodeId nodeId in plan.ExecutionOrder)
        {
            if (CreateCacheKey(plan, nodeId) is PrismRetainedCacheKey key)
            {
                currentRetainedKeys.Add(key);
            }
        }

        foreach (PrismCacheOwnerToken owner in currentOwners)
        {
            deviceResources.InvalidateStaleOwnerEntries(
                owner,
                currentRetainedKeys);
        }
    }

    private void ReleaseExpired(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int step)
    {
        expiredSurfaceIndices.Clear();
        foreach ((int index, SdlGpuPrismSurfaceLease _) in surfaces)
        {
            if (plan.SurfaceLifetimes[index].LastStep < step &&
                !IsPendingNestedPresentation(plan, graph, index, step))
            {
                expiredSurfaceIndices.Add(index);
            }
        }
        foreach (int index in expiredSurfaceIndices)
        {
            SdlGpuPrismSurfaceLease lease = surfaces[index];
            lease.Dispose();
            frameLeases.Remove(lease);
            surfaces.Remove(index);
        }
    }

    private static bool IsPendingNestedPresentation(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int executionIndex,
        int step)
    {
        PrismGraphNodeId nodeId = plan.ExecutionOrder[executionIndex];
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Output != nodeId ||
                scope.ParentScopeIndex is not int parentScopeIndex)
            {
                continue;
            }

            foreach (PrismGraphNode parentCapture in graph.Nodes)
            {
                if (parentCapture.AnalysisScopeIndex == parentScopeIndex &&
                    parentCapture.Kind == PrismGraphNodeKind.ControlCapture)
                {
                    return plan.GetExecutionIndex(parentCapture.Id) >= step;
                }
            }
            throw new InvalidOperationException(
                $"Prism scope '{parentScopeIndex}' has no control-capture node.");
        }

        return false;
    }

    private readonly record struct StyleDistanceFieldKey(
        nint Source,
        int PixelWidth,
        int PixelHeight,
        bool DirectionalCoverage);

    private bool TryResolveImage(
        PrismGraphScope scope,
        PrismGraphNode node,
        PrismResourceId id,
        bool required,
        nint fallback,
        out nint texture,
        out bool available)
    {
        if (id.Value > 0 && scope.Resources.TryGetImage(id, out IDrawImage image))
        {
            if (image is SdlGpuImage sdlImage)
            {
                texture = session.DrawingResources.GetOrCreateTexture(
                    session,
                    sdlImage,
                    sdlImage.Width,
                    sdlImage.Height,
                    sdlImage.RgbaPixels.Span).Handle;
                available = true;
                return true;
            }
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.UnsupportedCapability,
                "The Prism image resource is not owned by SDL_GPU.");
            texture = fallback;
            available = false;
            return false;
        }
        texture = fallback;
        available = false;
        if (required)
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingResource,
                $"Prism resource '{id}' is not available.");
            return false;
        }
        return true;
    }

    private bool ResolveFilterResource(
        PrismGraphScope scope,
        PrismGraphNode node,
        PrismResourceId id,
        bool required,
        nint source,
        out nint texture,
        out bool available) =>
        TryResolveImage(scope, node, id, required, source, out texture, out available);

    private nint FindOptionalInput(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        PrismGraphEdgeKind kind,
        nint fallback)
    {
        int index = FindInputIndex(plan, graph, node.Id, kind);
        return index >= 0 ? GetSurface(index).SampleTexture : fallback;
    }

    private SdlGpuRenderTarget GetSurface(int executionIndex) =>
        surfaces.TryGetValue(executionIndex, out SdlGpuPrismSurfaceLease? lease)
            ? lease.Target
            : throw new InvalidOperationException(
                $"SDL_GPU Prism execution surface {executionIndex} is no longer live.");

    private static PrismGraphScope FindScope(PrismGraph graph, int index)
    {
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.AnalysisScopeIndex == index)
            {
                return scope;
            }
        }
        throw new KeyNotFoundException($"Prism graph scope '{index}' does not exist.");
    }

    private static int FindInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target,
        PrismGraphEdgeKind kind)
    {
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            if (edge.Target == target && edge.Kind == kind)
            {
                return plan.GetExecutionIndex(edge.Source);
            }
        }
        return -1;
    }

    private static int FindAnyInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target)
    {
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            if (edge.Target == target)
            {
                return plan.GetExecutionIndex(edge.Source);
            }
        }
        return -1;
    }

    private static BackdropFrameMetadata? FindBackdropMetadata(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId cropNodeId)
    {
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            if (edge.Source != cropNodeId)
            {
                continue;
            }

            PrismGraphNode target = graph.GetNode(edge.Target);
            if (target.Kind == PrismGraphNodeKind.ColorConversion &&
                plan.GetExecutionIndex(target.Id) >= 0)
            {
                return target.BackdropMetadata;
            }
        }

        return null;
    }

    private void SetFilterOptions(params Vector4[] options)
    {
        for (int index = 0; index < Math.Min(options.Length, 10); index++)
        {
            uniforms[24 + index] = options[index];
        }
    }

    private static Vector4 PackSeed(uint seed) =>
        new(seed & 0xffffu, seed >> 16, 0, 0);

    private bool ResolveScopeUvMapping(
        PrismGraphScope scope,
        out Vector3 rowX,
        out Vector3 rowY)
    {
        DrawRect bounds = scope.ControlBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 ||
            !Matrix3x2.Invert(scope.EffectiveTransform, out Matrix3x2 inverse))
        {
            rowX = Vector3.Zero;
            rowY = Vector3.Zero;
            return false;
        }
        rowX = new Vector3(
            inverse.M11 / (scope.PixelScale * bounds.Width),
            inverse.M21 / (scope.PixelScale * bounds.Width),
            ((executionOriginPixelX * inverse.M11 +
                executionOriginPixelY * inverse.M21) / scope.PixelScale +
                inverse.M31 - bounds.X) / bounds.Width);
        rowY = new Vector3(
            inverse.M12 / (scope.PixelScale * bounds.Height),
            inverse.M22 / (scope.PixelScale * bounds.Height),
            ((executionOriginPixelX * inverse.M12 +
                executionOriginPixelY * inverse.M22) / scope.PixelScale +
                inverse.M32 - bounds.Y) / bounds.Height);
        return true;
    }

    private static Vector4 ResolveBlendChannels(PrismBlendChannels channels) => new(
        (channels & PrismBlendChannels.Red) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Green) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Blue) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Alpha) != 0 ? 1 : 0);

    private static Vector4 ResolveBlendRange(PrismBlendRange range) =>
        new(range.BlackStart, range.BlackEnd, range.WhiteStart, range.WhiteEnd);

    private static SdlRect ResolveBackdropDestination(
        DrawRect bounds,
        float pixelScale,
        SdlGpuRenderTarget target,
        int originPixelX,
        int originPixelY)
    {
        int left = (int)Math.Clamp(
            MathF.Floor(bounds.X * pixelScale) - originPixelX,
            0,
            target.PixelWidth);
        int top = (int)Math.Clamp(
            MathF.Floor(bounds.Y * pixelScale) - originPixelY,
            0,
            target.PixelHeight);
        int right = (int)Math.Clamp(
            MathF.Ceiling(bounds.Right * pixelScale) - originPixelX,
            0,
            target.PixelWidth);
        int bottom = (int)Math.Clamp(
            MathF.Ceiling(bounds.Bottom * pixelScale) - originPixelY,
            0,
            target.PixelHeight);
        return new SdlRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private static SdlRect? ResolvePresentationClip(
        PrismGraphExecutionPlan plan,
        PrismGraphScope scope,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        int originPixelX = 0,
        int originPixelY = 0)
    {
        PrismGraphNodePlan nodePlan = plan.GetNodePlan(node.Id);
        if (nodePlan.BoundsStatus == PrismGraphBoundsStatus.Unknown)
        {
            return null;
        }

        DrawRect bounds = UnionBounds(nodePlan.Bounds, scope.ControlBounds);
        float pixelScale = scope.PixelScale;
        int left = (int)Math.Clamp(
            MathF.Floor(bounds.X * pixelScale) - PresentationSamplingOutset - originPixelX,
            0,
            target.PixelWidth);
        int top = (int)Math.Clamp(
            MathF.Floor(bounds.Y * pixelScale) - PresentationSamplingOutset - originPixelY,
            0,
            target.PixelHeight);
        int right = (int)Math.Clamp(
            MathF.Ceiling(bounds.Right * pixelScale) + PresentationSamplingOutset - originPixelX,
            0,
            target.PixelWidth);
        int bottom = (int)Math.Clamp(
            MathF.Ceiling(bounds.Bottom * pixelScale) + PresentationSamplingOutset - originPixelY,
            0,
            target.PixelHeight);
        return new SdlRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private void ResolveExecutionExtent(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        SdlGpuRenderTarget hostTarget)
    {
        if (graph.Nodes.Any(static node =>
                node.Filter is PrismFilterId filter &&
                (PrismNeighborhoodPlanner.RequiresStableHostCoordinates(filter) ||
                    PrismResamplingPlanner.RequiresStableHostCoordinates(filter) ||
                    PrismCatalogFilterPlanner.RequiresStableHostCoordinates(filter))))
        {
            executionOriginPixelX = 0;
            executionOriginPixelY = 0;
            executionPixelWidth = hostTarget.PixelWidth;
            executionPixelHeight = hostTarget.PixelHeight;
            return;
        }

        int left = hostTarget.PixelWidth - 1;
        int top = hostTarget.PixelHeight - 1;
        int right = hostTarget.PixelWidth;
        int bottom = hostTarget.PixelHeight;
        bool hasKnownOutput = false;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Output is not PrismGraphNodeId output)
            {
                continue;
            }

            PrismGraphNodePlan outputPlan = plan.GetNodePlan(output);
            if (outputPlan.BoundsStatus == PrismGraphBoundsStatus.Unknown)
            {
                executionOriginPixelX = 0;
                executionOriginPixelY = 0;
                executionPixelWidth = hostTarget.PixelWidth;
                executionPixelHeight = hostTarget.PixelHeight;
                return;
            }

            DrawRect bounds = UnionBounds(outputPlan.Bounds, scope.ControlBounds);
            int scopeLeft =
                (int)MathF.Floor(bounds.X * scope.PixelScale) -
                PresentationSamplingOutset;
            int scopeTop =
                (int)MathF.Floor(bounds.Y * scope.PixelScale) -
                PresentationSamplingOutset;
            int scopeRight =
                (int)MathF.Ceiling(bounds.Right * scope.PixelScale) +
                PresentationSamplingOutset;
            int scopeBottom =
                (int)MathF.Ceiling(bounds.Bottom * scope.PixelScale) +
                PresentationSamplingOutset;
            if (!hasKnownOutput)
            {
                left = scopeLeft;
                top = scopeTop;
                right = scopeRight;
                bottom = scopeBottom;
            }
            else
            {
                left = Math.Min(left, scopeLeft);
                top = Math.Min(top, scopeTop);
                right = Math.Max(right, scopeRight);
                bottom = Math.Max(bottom, scopeBottom);
            }
            hasKnownOutput = true;
        }

        if (!hasKnownOutput)
        {
            executionOriginPixelX = 0;
            executionOriginPixelY = 0;
            executionPixelWidth = hostTarget.PixelWidth;
            executionPixelHeight = hostTarget.PixelHeight;
            return;
        }

        int clampedLeft = Math.Clamp(left, 0, hostTarget.PixelWidth - 1);
        int clampedTop = Math.Clamp(top, 0, hostTarget.PixelHeight - 1);
        int clampedRight = Math.Clamp(
            right,
            clampedLeft + 1,
            hostTarget.PixelWidth);
        int clampedBottom = Math.Clamp(
            bottom,
            clampedTop + 1,
            hostTarget.PixelHeight);
        executionOriginPixelX = AlignDown(clampedLeft, ExecutionSurfaceTileSize);
        executionOriginPixelY = AlignDown(clampedTop, ExecutionSurfaceTileSize);
        int alignedRight = AlignUp(
            clampedRight,
            ExecutionSurfaceTileSize,
            hostTarget.PixelWidth);
        int alignedBottom = AlignUp(
            clampedBottom,
            ExecutionSurfaceTileSize,
            hostTarget.PixelHeight);
        executionPixelWidth = alignedRight - executionOriginPixelX;
        executionPixelHeight = alignedBottom - executionOriginPixelY;
    }

    private void ResolveMipmappedNodes(PrismGraph graph)
    {
        mipmappedNodes.Clear();
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            PrismGraphNode consumer = graph.GetNode(edge.Target);
            if (consumer.ResamplingPlan is not null ||
                consumer.CatalogFilterPlan is not null)
            {
                mipmappedNodes.Add(edge.Source);
            }
        }
    }

    private static int AlignDown(int value, int alignment) =>
        value - (value % alignment);

    private static int AlignUp(int value, int alignment, int maximum)
    {
        int remainder = value % alignment;
        return remainder == 0
            ? value
            : (int)Math.Min(
                (long)value + alignment - remainder,
                maximum);
    }

    private static DrawRect UnionBounds(DrawRect first, DrawRect second)
    {
        if (first.Width <= 0 || first.Height <= 0)
        {
            return second;
        }
        if (second.Width <= 0 || second.Height <= 0)
        {
            return first;
        }

        float left = MathF.Min(first.X, second.X);
        float top = MathF.Min(first.Y, second.Y);
        float right = MathF.Max(first.Right, second.Right);
        float bottom = MathF.Max(first.Bottom, second.Bottom);
        return new DrawRect(left, top, right - left, bottom - top);
    }

    private static BackdropPixelFormat ToBackdropFormat(SdlGpuTextureFormat format) => format switch
    {
        SdlGpuTextureFormat.B8G8R8A8Unorm or SdlGpuTextureFormat.B8G8R8A8UnormSrgb =>
            BackdropPixelFormat.Bgra8Unorm,
        _ => BackdropPixelFormat.Rgba8Unorm
    };
}
