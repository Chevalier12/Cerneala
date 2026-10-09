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

internal sealed partial class SdlGpuPrismExecutor : IDisposable
{
    private const int PresentationSamplingOutset = 1;
    private const int ExecutionSurfaceTileSize = 16;
    private const long ShaderPackageVersion = 59;
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
    private readonly HashSet<PrismGraphNodeId> mipmappedNodes = [];
    private readonly HashSet<PrismRetainedCacheKey> currentRetainedKeys = [];
    private readonly HashSet<PrismCacheOwnerToken> currentOwners = [];
    private readonly List<int> expiredSurfaceIndices = [];
    private readonly Dictionary<int, SdlGpuPrismPresentationSurface> childPresentationSurfaces = [];
    private readonly Dictionary<int, PrismRasterExtent> scopeExecutionExtents = [];
    private readonly Dictionary<int, PrismRasterExtent> inputReferenceExtents = [];
    private readonly HashSet<int> hostCoordinateScopes = [];
    private PrismRasterExtent referenceExtent;
    private PrismRasterExtent currentReferenceExtent;
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
        bool strictSurfaceAllocation = false;
        foreach (PrismAnalyzedScope scope in frameContext.PrismAnalysis.Scopes)
        {
            strictSurfaceAllocation |= scope.Scope.StrictSurfaceAllocation;
        }
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
        ResolveScopeExecutionExtents(plan, graph, hostTarget, frameContext.PrismAnalysis);
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
                currentReferenceExtent = GetReferenceExtent(node.AnalysisScopeIndex);
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
                        deviceResources.Promote(session, key, lease);
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
        catch (PrismSurfaceAllocationException exception) when (!strictSurfaceAllocation)
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
                deviceResources.TryAcquireRetained(
                    session,
                    key,
                    session.WindowIdentity,
                    out SdlGpuPrismSurfaceLease lease))
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
            GetReferenceExtent(plan.OptimizedGraph.GetNode(nodeId).AnalysisScopeIndex));
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

    private static BackdropPixelFormat ToBackdropFormat(SdlGpuTextureFormat format) => format switch
    {
        SdlGpuTextureFormat.B8G8R8A8Unorm or SdlGpuTextureFormat.B8G8R8A8UnormSrgb =>
            BackdropPixelFormat.Bgra8Unorm,
        _ => BackdropPixelFormat.Rgba8Unorm
    };
}
