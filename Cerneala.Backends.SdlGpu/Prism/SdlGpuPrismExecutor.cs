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
    private const long ShaderPackageVersion = 58;
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
    private readonly PrismRasterPlanner rasterPlanner = new();
    private PrismRasterExecutionPlan rasterPlan = null!;
    private readonly PrismExecutionDiagnostics diagnostics = new(detailedDiagnosticsEnabled: true);
    private readonly SdlGpuPrismUniforms uniforms = new();
    private readonly Dictionary<int, SdlGpuPrismSurfaceLease> surfaces = [];
    private readonly Dictionary<int, SdlGpuPrismSurfaceLease> retainedHits = [];
    private bool[] requiredNodes = [];
    private bool[] fallbackDependentNodes = [];
    private readonly List<SdlGpuPrismSurfaceLease> frameLeases = [];
    private readonly HashSet<SdlGpuPrismSurfaceLease> promotedLeases = [];
    private readonly HashSet<PrismGraphNodeId> mipmappedNodes = [];
    private readonly HashSet<PrismRetainedCacheKey> currentRetainedKeys = [];
    private readonly HashSet<PrismCacheOwnerToken> currentOwners = [];
    private readonly List<int> expiredSurfaceIndices = [];
    private readonly Dictionary<int, SdlGpuPrismPresentationSurface> childPresentationSurfaces = [];
    private readonly Dictionary<int, PrismRasterExtent> scopeExecutionExtents = [];
    private readonly HashSet<int> hostCoordinateScopes = [];
    private PrismRasterExtent referenceExtent;
    private readonly nint[] textures = new nint[15];
    private SdlGpuDrawingBackend.CommandRangeState? hostCommandRangeState;
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
        ProcessInvalidations(frameContext.PrismAnalysis, frameContext.PrismCacheInvalidations);
        PrismGraph sourceGraph = frameContext.BackdropLease is null
            ? graphBuilder.Build(frameContext.PrismAnalysis)
            : graphBuilder.Build(
                frameContext.PrismAnalysis,
                frameContext.BackdropLease.Metadata,
                frameContext.BackdropSourceToken);
        PrismGraphExecutionPlan plan = graphOptimizer.Optimize(sourceGraph);
        PrismGraph graph = plan.OptimizedGraph;
        ResolveExecutionExtent(plan, graph, hostTarget);
        referenceExtent = new(executionOriginPixelX, executionOriginPixelY,
            executionPixelWidth, executionPixelHeight);
        ResolveScopeExecutionExtents(plan, graph, hostTarget);
        rasterPlan = rasterPlanner.Prepare(plan, referenceExtent.Width, referenceExtent.Height,
            scopeExecutionExtents);
        plan = rasterPlan.GraphPlan;
        graph = plan.OptimizedGraph;
        ResolveMipmappedNodes(graph);
        ReconcileRetainedEntries(plan, graph);
        long started = Stopwatch.GetTimestamp();
        long createdBefore = deviceResources.CreatedSurfaceCount;
        long reusedBefore = deviceResources.ReusedSurfaceCount;
        diagnostics.BeginExecution(
            frameContext.PrismAnalysis,
            plan,
            checked(plan.ExecutionOrder.Length + graph.Scopes.Length));

        if (hostCommandRangeState is null)
        {
            hostCommandRangeState = drawingBackend.CreateCommandRangeState(hostTarget);
        }
        else
        {
            hostCommandRangeState.Reset(hostTarget, drawingBackend.CoordinateScale);
        }
        SdlGpuDrawingBackend.CommandRangeState hostState = hostCommandRangeState;
        int hostCommandIndex = 0;
        try
        {
            AcquireRequiredRetainedHits(plan);
            hostCommandIndex = RenderHostPrelude(
                commands,
                graph,
                frameContext.StateAnalysis,
                hostTarget,
                hostState);
            for (int step = 0; step < plan.ExecutionOrder.Length; step++)
            {
                ReleaseExpired(plan, step);
                if (!requiredNodes[step])
                {
                    continue;
                }
                PrismGraphNode node = graph.GetNode(plan.ExecutionOrder[step]);
                SetExecutionExtent(scopeExecutionExtents[node.AnalysisScopeIndex]);
                PrismRetainedCacheKey? cacheKey = CreateCacheKey(plan, node.Id);
                if (retainedHits.Remove(step, out SdlGpuPrismSurfaceLease retainedLease))
                {
                    surfaces.Add(step, retainedLease);
                }
                else
                {
                    bool mipmapped = mipmappedNodes.Contains(node.Id);
                    PrismRasterSurface surface = rasterPlan.AuxiliaryPasses.TryGetValue(
                        node.Id, out PrismRasterPass auxiliary)
                        ? auxiliary.Surface
                        : new(executionPixelWidth, executionPixelHeight, PrismRasterSurfaceFormat.Rgba16Float);
                    SdlGpuPrismSurfaceLease lease = deviceResources.RentSurface(
                        session.WindowIdentity,
                        surface.Width,
                        surface.Height,
                        ResolveSurfaceFormat(surface.Format),
                        mipmapped);
                    surfaces.Add(step, lease);
                    frameLeases.Add(lease);
                    ObserveTransientSurfaces();
                    int fallbacksBeforeNode = diagnostics.Count;
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
                    // A fallback taints its output and all consumers, including
                    // parent captures of nested children. Independent branches
                    // must remain retainable regardless of execution order.
                    bool fallbackDependent = diagnostics.Count != fallbacksBeforeNode;
                    foreach (int input in plan.CacheInputExecutionIndices[step])
                    {
                        fallbackDependent |= fallbackDependentNodes[input];
                    }
                    fallbackDependentNodes[step] = fallbackDependent;
                    if (cacheKey is PrismRetainedCacheKey key && !fallbackDependent)
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
                ObserveTransientSurfaces();
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
                hostState.Target,
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
            retainedHits.Clear();
            promotedLeases.Clear();
            surfaces.Clear();
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
        retainedHits.Clear();
        surfaces.Clear();
        mipmappedNodes.Clear();
        currentRetainedKeys.Clear();
        currentOwners.Clear();
        scopeExecutionExtents.Clear();
        hostCoordinateScopes.Clear();
        hostCommandRangeState = null;
    }

    private void ObserveTransientSurfaces()
    {
        int transientCount = 0;
        foreach (SdlGpuPrismSurfaceLease lease in frameLeases)
        {
            if (!lease.IsRetained)
            {
                transientCount++;
            }
        }
        // Match the shared execution diagnostic's transient-pool contract.
        // Retained pins do not become newly live transient surfaces on a hit.
        diagnostics.ObserveLiveSurfaces(transientCount);
    }

    private void AcquireRequiredRetainedHits(PrismGraphExecutionPlan plan)
    {
        int count = plan.ExecutionOrder.Length;
        if (requiredNodes.Length < count)
        {
            Array.Resize(ref requiredNodes, count);
            Array.Resize(ref fallbackDependentNodes, count);
        }
        Array.Clear(requiredNodes, 0, count);
        Array.Clear(fallbackDependentNodes, 0, count);
        foreach (int output in plan.RootOutputExecutionIndices)
        {
            requiredNodes[output] = true;
        }

        // The shared plan includes both explicit inputs and nested capture outputs.
        // A pinned cached result terminates traversal of precisely its covered inputs.
        for (int step = count - 1; step >= 0; step--)
        {
            if (!requiredNodes[step])
            {
                continue;
            }
            if (CreateCacheKey(plan, plan.ExecutionOrder[step]) is PrismRetainedCacheKey key &&
                deviceResources.TryAcquireRetained(key, session.WindowIdentity, out SdlGpuPrismSurfaceLease lease))
            {
                retainedHits.Add(step, lease);
                frameLeases.Add(lease);
                continue;
            }
            foreach (int input in plan.CacheInputExecutionIndices[step])
            {
                requiredNodes[input] = true;
            }
        }
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
            case PrismGraphNodeKind.RasterAuxiliary:
                RenderRasterAuxiliary(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ControlCapture:
                RenderControlCapture(commands, stateAnalysis, plan, graph, node, target);
                return;
            case PrismGraphNodeKind.BackdropInput:
                RenderBackdropInput(graph, node, target, backdropLease);
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

    private static SdlGpuTextureFormat ResolveSurfaceFormat(PrismRasterSurfaceFormat format) =>
        format switch
        {
            PrismRasterSurfaceFormat.Rgba16Float => SdlGpuTextureFormat.R16G16B16A16Float,
            PrismRasterSurfaceFormat.Rgba32Float => SdlGpuTextureFormat.R32G32B32A32Float,
            PrismRasterSurfaceFormat.R32Float => SdlGpuTextureFormat.R32Float,
            PrismRasterSurfaceFormat.Rgba8Unorm => SdlGpuTextureFormat.R8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private void RenderRasterAuxiliary(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        PrismRasterPass pass = rasterPlan.AuxiliaryPasses[node.Id];
        nint source = GetSurface(FindInputIndex(plan, graph, node.Id,
            PrismGraphEdgeKind.Content)).SampleTexture;
        switch (pass.Kind)
        {
            case PrismRasterPassKind.ThresholdCdf:
                PrepareBaseUniforms(source, source, 4, 1);
                uniforms[23] = new Vector4(0,
                    (int)FindScope(graph, node.AnalysisScopeIndex).CompositionSettings.WorkingColorProfile, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.ThresholdSelection:
                PrepareBaseUniforms(source, source, 6, 1);
                uniforms[23] = new Vector4(pass.RadiusOrJump, 0, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.ShadowSpread:
            case PrismRasterPassKind.ShadowBlur:
                RenderStyleMaskPass(target, source,
                    pass.Kind == PrismRasterPassKind.ShadowSpread ? 83 : 84,
                    pass.RadiusOrJump, pass.Horizontal);
                return;
            case PrismRasterPassKind.DistanceSeed:
                PrepareBaseUniforms(source, source, 85, 1);
                textures[12] = source;
                uniforms[16] = new Vector4(rasterPlan.Styles[pass.Owner].Kind, 0, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.DistanceFlood:
                RenderStyleDistanceFloodPass(target, source, checked((int)pass.RadiusOrJump));
                return;
            case PrismRasterPassKind.BevelHeight:
            case PrismRasterPassKind.BevelLighting:
                RenderStyle(plan, graph, node, target, pass);
                return;
            default:
                throw new InvalidOperationException($"Unsupported shared raster pass '{pass.Kind}'.");
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
        try
        {
            foreach (PrismGraphScope child in graph.Scopes)
            {
                if (child.ParentScopeIndex == scope.AnalysisScopeIndex &&
                    child.Output is PrismGraphNodeId output)
                {
                    int index = plan.GetExecutionIndex(output);
                    if (surfaces.TryGetValue(index, out SdlGpuPrismSurfaceLease childLease))
                    {
                        PrismGraphNode childOutput = graph.GetNode(output);
                        PrismRasterExtent childExtent = scopeExecutionExtents[child.AnalysisScopeIndex];
                        childPresentationSurfaces.Add(
                            child.BeginCommandIndex,
                            new SdlGpuPrismPresentationSurface(
                                childLease.Target,
                                ResolvePresentationClip(
                                    plan,
                                    child,
                                    childOutput,
                                    target,
                                    executionOriginPixelX,
                                    executionOriginPixelY),
                                child.CompositionSettings.WorkingColorProfile,
                                new DrawRect(
                                    (childExtent.X - executionOriginPixelX) / drawingBackend.CoordinateScale,
                                    (childExtent.Y - executionOriginPixelY) / drawingBackend.CoordinateScale,
                                    childLease.Target.PixelWidth / drawingBackend.CoordinateScale,
                                    childLease.Target.PixelHeight / drawingBackend.CoordinateScale)));
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
                    executionOriginPixelY / drawingBackend.CoordinateScale),
                isolateCompositingState: true,
                captureClip: (scope.ControlBounds, scope.EffectiveTransform));
        }
        finally
        {
            childPresentationSurfaces.Clear();
        }
    }

    private void RenderBackdropInput(
        PrismGraph graph,
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
            Matrix3x2 transform = metadata.CoordinateTransform;
            float pixelScale = FindScope(graph, node.AnalysisScopeIndex).PixelScale;
            // Map the provider raster exactly once into the bounded execution space.
            // Later graph crops must address this snapshot, not the provider's dimensions.
            PrepareBaseUniforms(texture, texture, 1, 1);
            uniforms[0] = new Vector4(1, 1f / metadata.PixelWidth, 1f / metadata.PixelHeight, executionOriginPixelX);
            uniforms[6] = new Vector4(1, (float)metadata.AlphaMode, 0, 0);
            uniforms[7] = new Vector4(
                transform.M11 / (pixelScale * metadata.PixelWidth),
                transform.M21 / (pixelScale * metadata.PixelWidth),
                ((executionOriginPixelX * transform.M11 + executionOriginPixelY * transform.M21) / pixelScale +
                    transform.M31) / metadata.PixelWidth, 0);
            uniforms[8] = new Vector4(
                transform.M12 / (pixelScale * metadata.PixelHeight),
                transform.M22 / (pixelScale * metadata.PixelHeight),
                ((executionOriginPixelX * transform.M12 + executionOriginPixelY * transform.M22) / pixelScale +
                    transform.M32) / metadata.PixelHeight, 0);
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
        if (metadata is null)
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

        SdlGpuRenderTarget snapshot = GetSurface(sourceIndex);
        nint source = snapshot.SampleTexture;
        PrepareBaseUniforms(source, source, 1, 1);
        uniforms[0] = new Vector4(
            1,
            1f / snapshot.PixelWidth,
            1f / snapshot.PixelHeight,
            executionOriginPixelX);
        uniforms[6] = new Vector4(
            1,
            (float)BackdropAlphaMode.Premultiplied,
            0,
            0);
        uniforms[7] = new Vector4(1f / snapshot.PixelWidth, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1f / snapshot.PixelHeight, 0, 0);
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
        SdlGpuRenderTarget target,
        PrismRasterPass? auxiliary = null)
    {
        PrismGraphNode owner = auxiliary is PrismRasterPass pass ? graph.GetNode(pass.Owner) : node;
        int contentIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        int sourceIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.StyleSource);
        if (contentIndex < 0 || sourceIndex < 0 || owner.Style is not PrismStyleId style)
        {
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        PrismStylePlan stylePlan = rasterPlan.Styles[owner.Id];
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
                (stylePlan.Offset.X * scope.PixelScale) / referenceExtent.Width,
                (stylePlan.Offset.Y * scope.PixelScale) / referenceExtent.Height);
        float gradientAspect = alignGradientWithLayer
            ? scope.ControlBounds.Width / MathF.Max(scope.ControlBounds.Height, 1)
            : referenceExtent.Width / (float)referenceExtent.Height;
        nint maskTexture = auxiliary is not null
            ? content
            : FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.PreparedInput, source);
        int kernel = auxiliary?.Kind switch
        {
            PrismRasterPassKind.BevelHeight => 87,
            PrismRasterPassKind.BevelLighting => 88,
            null => 82,
            _ => throw new InvalidOperationException("Only bevel auxiliary passes use the style bindings.")
        };
        nint rasterSource = auxiliary?.Kind == PrismRasterPassKind.BevelHeight ? source : content;
        PrepareBaseUniforms(rasterSource, source, kernel, 1);
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
        RenderPrepared(target, rasterSource, source);
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
            PrismAdjustmentPlan adjustment = rasterPlan.Adjustments[node.Id];
            ResolveFilterResource(scope, node, adjustment.Resource,
                adjustment.ResourceRequired, source, out nint resource, out bool available);
            if (filter == PrismFilterId.Threshold)
            {
                resource = GetSurface(FindInputIndex(plan, graph, node.Id,
                    PrismGraphEdgeKind.PreparedInput)).SampleTexture;
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
            if (filter == PrismFilterId.ColorLookup && available &&
                scope.Resources.TryGetImage(adjustment.Resource, out IDrawImage lookup))
            {
                int level = (int)Math.Round(Math.Cbrt(lookup.Width));
                if (lookup.Width != lookup.Height || level < 2 ||
                    (long)level * level * level != lookup.Width)
                {
                    diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                        PrismFallbackReason.UnsupportedCapability,
                        "ColorLookup requires a square Hald LUT whose side is level cubed (level >= 2).");
                    RenderKernel(target, source, source, 0, 1, node, null);
                    return;
                }
                Vector4 header = uniforms[23];
                header.W = level * level;
                uniforms[23] = header;
                Vector4 control = uniforms[34];
                control.X = lookup.Width;
                control.Y = lookup.Height;
                uniforms[34] = control;
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
        uniforms[59] = new Vector4(referenceExtent.X, referenceExtent.Y,
            referenceExtent.Width, referenceExtent.Height);
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
        0 when uniforms[34].Z == 8 &&
            uniforms[23].X == (int)PrismResamplingOperation.Transform =>
            session.DrawingResources.GetSampler(
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                anisotropic: true),
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
            SdlGpuRenderTarget source = surfaces[step].Target;
            SdlGpuRenderTarget presentationTarget = hostState.Target;
            SdlRect? presentationClip = ResolvePresentationClip(
                plan,
                scope,
                node,
                presentationTarget);
            drawingBackend.DrawPrismTexture(
                source.SampleTexture,
                presentationTarget,
                presentationClip,
                new DrawRect(
                    executionOriginPixelX / drawingBackend.CoordinateScale,
                    executionOriginPixelY / drawingBackend.CoordinateScale,
                    source.PixelWidth / drawingBackend.CoordinateScale,
                    source.PixelHeight / drawingBackend.CoordinateScale),
                presentationState: hostState,
                workingColorProfile: scope.CompositionSettings.WorkingColorProfile);
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

    private PrismRetainedCacheKey? CreateCacheKey(
        PrismGraphExecutionPlan plan,
        PrismGraphNodeId nodeId)
    {
        PrismRasterExtent extent = scopeExecutionExtents[
            plan.OptimizedGraph.GetNode(nodeId).AnalysisScopeIndex];
        PrismRetainedRasterContext context = new(
            extent.Width,
            extent.Height,
            PrismColorProfile.Srgb,
            ToBackdropFormat(session.Diagnostics.TextureFormat),
            PrismSampling.Linear,
            Capabilities,
            ShaderPackageVersion,
            extent.X,
            extent.Y,
            referenceExtent);
        return PrismRetainedCacheKey.TryCreate(plan, nodeId, context, out PrismRetainedCacheKey key)
            ? key
            : null;
    }

    internal void ProcessInvalidations(
        PrismFrameAnalysis analysis,
        PrismCacheInvalidationQueue? queue)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        drawingBackend.ProcessPrismInvalidations(analysis, queue);
    }

    private void ReconcileRetainedEntries(
        PrismGraphExecutionPlan plan,
        PrismGraph graph)
    {
        currentOwners.Clear();
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            currentOwners.Add(scope.CacheOwnerToken);
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
        int step)
    {
        expiredSurfaceIndices.Clear();
        foreach ((int index, SdlGpuPrismSurfaceLease _) in surfaces)
        {
            if (plan.SurfaceLifetimes[index].LastStep < step)
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
        out bool available)
    {
        if (node.Filter != PrismFilterId.Curves)
        {
            return TryResolveImage(scope, node, id, required, source, out texture, out available);
        }
        if (id.Value > 0 && scope.Resources.TryGetCurves(id,
            out PrismCurvesResource resource, out long identity, out long version))
        {
            texture = deviceResources.GetCurvesTexture(session, id, resource, identity, version);
            available = true;
            Vector4 control = uniforms[34];
            control.X = PrismCurveLut.SampleCount;
            control.Y = 1;
            uniforms[34] = control;
            return true;
        }
        texture = source;
        available = false;
        if (required)
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingResource,
                $"Prism curves resource '{id}' is not available.");
        }
        return !required;
    }

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
        surfaces.TryGetValue(executionIndex, out SdlGpuPrismSurfaceLease lease)
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

    private SdlRect? ResolvePresentationClip(
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

        // Both bounds must be in host coordinates; ControlBounds is local.
        DrawRect bounds = UnionBounds(nodePlan.Bounds, scope.Bounds);
        // RenderSurface2D commands already use surface pixels. The owner's
        // effect DPI must not scale their host-space positions a second time.
        float pixelScale = drawingBackend.CoordinateScale;
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
        if (graph.Nodes.Any(RequiresHostCoordinates))
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

            DrawRect bounds = UnionBounds(outputPlan.Bounds, scope.Bounds);
            float pixelScale = drawingBackend.CoordinateScale;
            int scopeLeft =
                (int)MathF.Floor(bounds.X * pixelScale) -
                PresentationSamplingOutset;
            int scopeTop =
                (int)MathF.Floor(bounds.Y * pixelScale) -
                PresentationSamplingOutset;
            int scopeRight =
                (int)MathF.Ceiling(bounds.Right * pixelScale) +
                PresentationSamplingOutset;
            int scopeBottom =
                (int)MathF.Ceiling(bounds.Bottom * pixelScale) +
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

        SetExecutionExtent(AlignExecutionExtent(left, top, right, bottom, hostTarget));
    }

    private void ResolveScopeExecutionExtents(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        SdlGpuRenderTarget hostTarget)
    {
        scopeExecutionExtents.Clear();
        hostCoordinateScopes.Clear();
        foreach (PrismGraphNode node in graph.Nodes)
        {
            if (RequiresHostCoordinates(node))
            {
                hostCoordinateScopes.Add(node.AnalysisScopeIndex);
            }
        }
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            PrismRasterExtent extent = new(0, 0, hostTarget.PixelWidth, hostTarget.PixelHeight);
            if (!hostCoordinateScopes.Contains(scope.AnalysisScopeIndex) &&
                scope.Output is PrismGraphNodeId output)
            {
                PrismGraphNodePlan outputPlan = plan.GetNodePlan(output);
                if (outputPlan.BoundsStatus != PrismGraphBoundsStatus.Unknown)
                {
                    DrawRect bounds = UnionBounds(outputPlan.Bounds, scope.Bounds);
                    float scale = drawingBackend.CoordinateScale;
                    extent = AlignExecutionExtent(
                        (int)MathF.Floor(bounds.X * scale) - PresentationSamplingOutset,
                        (int)MathF.Floor(bounds.Y * scale) - PresentationSamplingOutset,
                        (int)MathF.Ceiling(bounds.Right * scale) + PresentationSamplingOutset,
                        (int)MathF.Ceiling(bounds.Bottom * scale) + PresentationSamplingOutset,
                        hostTarget);
                }
            }
            scopeExecutionExtents.Add(scope.AnalysisScopeIndex, extent);
        }
    }

    private static bool RequiresHostCoordinates(PrismGraphNode node) =>
        node.Filter is PrismFilterId filter &&
        (PrismNeighborhoodPlanner.RequiresStableHostCoordinates(filter) ||
            PrismResamplingPlanner.RequiresStableHostCoordinates(filter) ||
            PrismCatalogFilterPlanner.RequiresStableHostCoordinates(filter));

    private void SetExecutionExtent(PrismRasterExtent extent)
    {
        executionOriginPixelX = extent.X;
        executionOriginPixelY = extent.Y;
        executionPixelWidth = extent.Width;
        executionPixelHeight = extent.Height;
    }

    private static PrismRasterExtent AlignExecutionExtent(
        int left, int top, int right, int bottom, SdlGpuRenderTarget hostTarget)
    {
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
        int originX = AlignDown(clampedLeft, ExecutionSurfaceTileSize);
        int originY = AlignDown(clampedTop, ExecutionSurfaceTileSize);
        int alignedRight = AlignUp(
            clampedRight,
            ExecutionSurfaceTileSize,
            hostTarget.PixelWidth);
        int alignedBottom = AlignUp(
            clampedBottom,
            ExecutionSurfaceTileSize,
            hostTarget.PixelHeight);
        return new(originX, originY, alignedRight - originX, alignedBottom - originY);
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
