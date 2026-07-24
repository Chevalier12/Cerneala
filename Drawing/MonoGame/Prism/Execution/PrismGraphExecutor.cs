using System.Diagnostics;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGraphExecutor : IDisposable
{
    private static readonly Vector2 FullUvScale = Vector2.One;
    private static readonly Vector2 ZeroUvOffset = Vector2.Zero;

    private readonly GraphicsDevice graphicsDevice;
    private readonly PrismSurfacePool surfacePool;
    private readonly PrismRetainedSurfaceCache retainedSurfaceCache;
    private readonly PrismKernelRegistry kernels;
    private readonly PrismExecutionDiagnostics diagnostics;
    private readonly bool retainedCacheEnabled;
    private readonly bool developmentDiagnosticsEnabled;
    private readonly long[] missCounts =
        new long[(int)PrismCacheMissReason.Disabled + 1];
    private readonly Dictionary<
        PrismCacheOwnerToken,
        PrismRetainedCacheKey> ownerFinalKeys = [];
    private PrismSurfaceKey[] surfaceKeys = [];
    private bool[] requiredNodes = [];
    private bool[] requiredTransientSurfaces = [];
    private bool[] cacheResultValid = [];
    private bool[] retainedKeyAvailable = [];
    private PrismRetainedCacheKey[] retainedKeys = [];
    private PrismRetainedSurfaceLease[] retainedLeases = [];
    private int[] requiredTraversal = [];
    private int[] promotionHeads = [];
    private int[] promotionNext = [];
    private bool[] bypassedScopes = [];
    private int[] captureSteps = [];
    private int[] captureCommandIndices = [];
    private bool[] initializedCaptures = [];
    private PrismRetainedRasterContext lastRasterContext;
    private PrismCacheMissReason pendingMissReason =
        PrismCacheMissReason.NotFound;
    private PrismCacheMissReason lastMissReason;
    private PrismDependencyChange lastDependencyChange;
    private long finalHitCount;
    private long intermediateHitCount;
    private long missCount;
    private long savedCaptureCount;
    private long savedPassCount;
    private bool hasLastRasterContext;
    private bool disposed;

    public PrismGraphExecutor(
        GraphicsDevice graphicsDevice,
        PrismExecutionDiagnostics? diagnostics = null)
        : this(
            graphicsDevice,
            diagnostics,
            new PrismRendererOptions
            {
                EnableDevelopmentDiagnostics =
                    diagnostics?.DetailedDiagnosticsEnabled == true
            },
            retainedCacheEnabled: true)
    {
    }

    internal PrismGraphExecutor(
        GraphicsDevice graphicsDevice,
        PrismExecutionDiagnostics? diagnostics,
        PrismRendererOptions options,
        bool retainedCacheEnabled)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(options);
        ObjectDisposedException.ThrowIf(
            graphicsDevice.IsDisposed,
            graphicsDevice);
        options.Validate();

        this.graphicsDevice = graphicsDevice;
        this.diagnostics =
            diagnostics ?? new PrismExecutionDiagnostics(
                options.EnableDevelopmentDiagnostics);
        this.retainedCacheEnabled = retainedCacheEnabled;
        developmentDiagnosticsEnabled =
            options.EnableDevelopmentDiagnostics ||
            this.diagnostics.DetailedDiagnosticsEnabled;
        surfacePool = new PrismSurfacePool(
            graphicsDevice,
            new PrismSurfaceBudget(
                options.SurfaceHardByteLimit,
                options.RetainedCacheSoftByteLimit,
                options.RetainedCacheEntryLimit));
        retainedSurfaceCache =
            new PrismRetainedSurfaceCache(surfacePool);
        try
        {
            kernels = new PrismKernelRegistry(graphicsDevice);
        }
        catch
        {
            try
            {
                retainedSurfaceCache.Dispose();
            }
            finally
            {
                surfacePool.Dispose();
            }
            throw;
        }
    }

    public PrismExecutionDiagnostics Diagnostics => diagnostics;

    public PrismSurfacePool SurfacePool => surfacePool;

    public PrismRetainedSurfaceCache RetainedSurfaceCache =>
        retainedSurfaceCache;

    public PrismKernelRegistry Kernels => kernels;

    public PrismRendererDiagnostics RendererDiagnostics =>
        new(
            retainedCacheEnabled,
            finalHitCount,
            intermediateHitCount,
            missCount,
            lastMissReason,
            retainedSurfaceCache.LookupCount,
            retainedSurfaceCache.PromotionCount,
            retainedSurfaceCache.RejectedPromotionCount,
            retainedSurfaceCache.EvictionCount,
            retainedSurfaceCache.LastEvictionReason,
            retainedSurfaceCache.EntryCount,
            retainedSurfaceCache.PinnedEntryCount,
            surfacePool.TransientByteCount,
            retainedSurfaceCache.RetainedByteCount,
            surfacePool.TotalByteCount,
            surfacePool.PeakTotalByteCount,
            savedCaptureCount,
            savedPassCount,
            lastDependencyChange,
            GetMissCount(PrismCacheMissReason.NotFound),
            GetMissCount(PrismCacheMissReason.NotCacheable),
            GetMissCount(PrismCacheMissReason.DependencyChanged),
            GetMissCount(PrismCacheMissReason.Invalidated),
            GetMissCount(PrismCacheMissReason.Disabled),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.Capacity),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.Invalidation),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.TransientPressure),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.Replacement),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.InvalidSurface),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.DeviceReset),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.Disposal),
            retainedSurfaceCache.GetEvictionCount(
                PrismCacheEvictionReason.ExplicitRemoval));

    public void Execute(
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        IPrismCommandRenderer renderer,
        Viewport hostViewport,
        IBackdropFrameLease? backdropLease)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(renderer);
        analysis.EnsureCurrent(commands);
        if (!ReferenceEquals(
            graphicsDevice,
            renderer.GraphicsDevice))
        {
            throw new ArgumentException(
                "The Prism renderer must use the executor GraphicsDevice.",
                nameof(renderer));
        }
        if (hostViewport.Width <= 0 || hostViewport.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hostViewport),
                "A Prism execution viewport must have positive dimensions.");
        }

        PrismGraph graph = plan.OptimizedGraph;
        long submitStarted = Stopwatch.GetTimestamp();
        long createdBefore = surfacePool.CreatedSurfaceCount;
        long reusedBefore = surfacePool.ReusedSurfaceCount;
        diagnostics.BeginExecution(
            analysis,
            plan,
            checked(plan.ExecutionOrder.Length + graph.Scopes.Length));
        try
        {
            if (plan.ExecutionOrder.IsEmpty)
            {
                RenderRawRange(renderer, commands, 0, commands.Count);
                return;
            }

            PrepareFrameBuffers(
                analysis,
                plan,
                graph,
                hostViewport);
            int hostCommandIndex = 0;
            try
            {
                AcquireRetainedHits(plan);
                hostCommandIndex =
                    RenderHostPrelude(renderer, commands, graph);

                try
                {
                    using PrismSurfaceFrame frame =
                        surfacePool.BeginFrame(plan);
                    for (int step = 0;
                        step < plan.ExecutionOrder.Length;
                        step++)
                    {
                        frame.AdvanceToStep(
                            step,
                            surfaceKeys.AsSpan(0, plan.ExecutionOrder.Length),
                            requiredTransientSurfaces.AsSpan(0, plan.ExecutionOrder.Length));
                        diagnostics.ObserveLiveSurfaces(
                            surfacePool.ActiveLeaseCount);
                        PrismGraphNode node =
                            graph.GetNode(plan.ExecutionOrder[step]);
                        int fallbackCountBefore = diagnostics.Count;
                        if (requiredTransientSurfaces[step])
                        {
                            RenderNode(
                                renderer,
                                commands,
                                plan,
                                graph,
                                frame,
                                step,
                                node,
                                backdropLease);
                            diagnostics.RecordGraphPass(node);
                        }
                        if (requiredNodes[step])
                        {
                            PresentCompletedNestedScopes(
                                renderer,
                                commands,
                                plan,
                                graph,
                                frame,
                                step,
                                node);
                            PresentCompletedRootScopes(
                                renderer,
                                commands,
                                plan,
                                graph,
                                frame,
                                step,
                                node,
                                hostViewport,
                                ref hostCommandIndex);
                            cacheResultValid[step] =
                                cacheResultValid[step] &&
                                diagnostics.Count ==
                                    fallbackCountBefore &&
                                AreCacheInputsValid(plan, step);
                        }

                        PromoteCompletedResults(
                            plan,
                            frame,
                            step);
                    }
                }
                catch (PrismSurfaceAllocationException exception)
                {
                    diagnostics.Record(
                        null,
                        -1,
                        PrismFallbackReason.SurfaceAllocationFailed,
                        exception.Message);
                    renderer.EndBatch();
                    renderer.RestoreHostTarget();
                    renderer.BeginCommandBatch();
                    RenderRawRange(
                        renderer,
                        commands,
                        hostCommandIndex,
                        commands.Count);
                    return;
                }
            }
            finally
            {
                ReleaseRetainedLeases();
            }

            renderer.EndBatch();
            renderer.RestoreHostTarget();
            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                commands.Count);
        }
        catch
        {
            InvalidateGraphOwners(graph);
            throw;
        }
        finally
        {
            diagnostics.CompleteExecution(
                surfacePool.CreatedSurfaceCount - createdBefore,
                surfacePool.ReusedSurfaceCount - reusedBefore,
                surfacePool.ActiveLeaseCount,
                surfacePool.TotalByteCount,
                surfacePool.PeakTotalByteCount,
                Stopwatch.GetElapsedTime(submitStarted));
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        surfacePool.Reset();
        ownerFinalKeys.Clear();
        hasLastRasterContext = false;
        pendingMissReason = PrismCacheMissReason.Invalidated;
    }

    public void Invalidate(
        PrismCacheInvalidation invalidation)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        switch (invalidation.Kind)
        {
            case PrismCacheInvalidationKind.Owner:
                InvalidateOwner(
                    invalidation.OwnerToken,
                    PrismCacheMissReason.Invalidated);
                break;
            case PrismCacheInvalidationKind.All:
                InvalidateAll();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(invalidation),
                    invalidation.Kind,
                    "Unknown Prism cache invalidation kind.");
        }
    }

    public void InvalidateAll()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        retainedSurfaceCache.Clear(
            PrismCacheEvictionReason.Invalidation);
        ownerFinalKeys.Clear();
        hasLastRasterContext = false;
        pendingMissReason = PrismCacheMissReason.Invalidated;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            kernels.Dispose();
        }
        finally
        {
            try
            {
                retainedSurfaceCache.Dispose();
            }
            finally
            {
                surfacePool.Dispose();
                disposed = true;
            }
        }
    }

    private void PrepareFrameBuffers(
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        Viewport hostViewport)
    {
        int nodeCount = plan.ExecutionOrder.Length;
        if (surfaceKeys.Length < nodeCount)
        {
            Array.Resize(
                ref surfaceKeys,
                nodeCount);
        }
        EnsureCacheBuffers(nodeCount);
        Array.Clear(requiredNodes, 0, nodeCount);
        Array.Clear(requiredTransientSurfaces, 0, nodeCount);
        Array.Clear(cacheResultValid, 0, nodeCount);
        Array.Clear(retainedKeyAvailable, 0, nodeCount);
        Array.Fill(promotionHeads, -1, 0, nodeCount);
        Array.Fill(promotionNext, -1, 0, nodeCount);

        int requiredScopeSlots = 0;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            requiredScopeSlots = Math.Max(
                requiredScopeSlots,
                graph.Scopes[index].AnalysisScopeIndex + 1);
        }
        if (bypassedScopes.Length < requiredScopeSlots)
        {
            Array.Resize(
                ref bypassedScopes,
                requiredScopeSlots);
        }
        if (captureSteps.Length < requiredScopeSlots)
        {
            Array.Resize(ref captureSteps, requiredScopeSlots);
            Array.Resize(
                ref captureCommandIndices,
                requiredScopeSlots);
            Array.Resize(
                ref initializedCaptures,
                requiredScopeSlots);
        }
        Array.Clear(bypassedScopes);
        Array.Clear(initializedCaptures);
        Array.Fill(
            captureSteps,
            -1,
            0,
            requiredScopeSlots);
        if (developmentDiagnosticsEnabled)
        {
            lastDependencyChange =
                PrismDependencyChange.None;
        }

        PrismRetainedRasterContext rasterContext = new(
            hostViewport.Width,
            hostViewport.Height,
            PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba16Float,
            PrismSampling.Linear,
            analysis.RequiredCapabilities,
            PrismKernelRegistry.ShaderPackageVersion);
        EnsureRasterContext(rasterContext);
        for (int index = 0; index < nodeCount; index++)
        {
            PrismGraphNode node =
                graph.GetNode(plan.ExecutionOrder[index]);
            PrismGraphScope scope =
                FindScope(graph, node.AnalysisScopeIndex);
            if (node.Kind == PrismGraphNodeKind.ControlCapture)
            {
                captureSteps[node.AnalysisScopeIndex] = index;
                captureCommandIndices[node.AnalysisScopeIndex] =
                    scope.BeginCommandIndex + 1;
            }
            PrismColorProfile profile =
                node.ColorProfile ??
                scope.CompositionSettings.WorkingColorProfile;
            surfaceKeys[index] = new PrismSurfaceKey(
                hostViewport.Width,
                hostViewport.Height,
                SurfaceFormat.HalfVector4,
                0,
                profile);
            if (!retainedCacheEnabled)
            {
                continue;
            }
            if (!PrismRetainedCacheKey.TryCreate(
                    plan,
                    node.Id,
                    rasterContext,
                    out retainedKeys[index]))
            {
                continue;
            }

            retainedKeyAvailable[index] = true;
            int promotionStep =
                plan.SurfaceLifetimes[index].LastStep;
            promotionNext[index] =
                promotionHeads[promotionStep];
            promotionHeads[promotionStep] = index;
        }
        if (retainedCacheEnabled)
        {
            InvalidateChangedOwners(plan, graph);
        }
    }

    internal void EnsureRasterContext(
        PrismRetainedRasterContext rasterContext)
    {
        if (hasLastRasterContext &&
            lastRasterContext != rasterContext)
        {
            if (developmentDiagnosticsEnabled)
            {
                lastDependencyChange =
                    DiffRasterContext(
                        lastRasterContext,
                        rasterContext);
            }
            retainedSurfaceCache.Clear(
                PrismCacheEvictionReason.Invalidation);
            ownerFinalKeys.Clear();
            pendingMissReason =
                PrismCacheMissReason.DependencyChanged;
        }

        lastRasterContext = rasterContext;
        hasLastRasterContext = true;
    }

    private void InvalidateChangedOwners(
        PrismGraphExecutionPlan plan,
        PrismGraph graph)
    {
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Output is not PrismGraphNodeId output)
            {
                InvalidateOwner(
                    scope.CacheOwnerToken,
                    PrismCacheMissReason.DependencyChanged);
                continue;
            }

            int outputIndex =
                plan.GetExecutionIndex(output);
            if (!retainedKeyAvailable[outputIndex] ||
                plan.NodePlans[outputIndex].CacheCandidateKind !=
                    PrismRetainedCacheCandidateKind.Final)
            {
                InvalidateOwner(
                    scope.CacheOwnerToken,
                    PrismCacheMissReason.DependencyChanged);
                continue;
            }

            PrismRetainedCacheKey current =
                retainedKeys[outputIndex];
            if (ownerFinalKeys.TryGetValue(
                    scope.CacheOwnerToken,
                out PrismRetainedCacheKey previous) &&
                previous != current)
            {
                if (developmentDiagnosticsEnabled)
                {
                    lastDependencyChange |=
                        DiffRetainedKey(previous, current);
                }
                retainedSurfaceCache.RemoveOwner(
                    scope.CacheOwnerToken);
                pendingMissReason =
                    PrismCacheMissReason.DependencyChanged;
            }
            ownerFinalKeys[scope.CacheOwnerToken] =
                current;
        }
    }

    private void InvalidateGraphOwners(
        PrismGraph graph)
    {
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            InvalidateOwner(
                scope.CacheOwnerToken,
                PrismCacheMissReason.Invalidated);
        }
    }

    private void InvalidateOwner(
        PrismCacheOwnerToken ownerToken,
        PrismCacheMissReason missReason)
    {
        retainedSurfaceCache.RemoveOwner(ownerToken);
        ownerFinalKeys.Remove(ownerToken);
        pendingMissReason = missReason;
    }

    private void EnsureCacheBuffers(int nodeCount)
    {
        if (requiredNodes.Length < nodeCount)
        {
            Array.Resize(ref requiredNodes, nodeCount);
            Array.Resize(
                ref requiredTransientSurfaces,
                nodeCount);
            Array.Resize(ref cacheResultValid, nodeCount);
            Array.Resize(ref retainedKeyAvailable, nodeCount);
            Array.Resize(ref retainedKeys, nodeCount);
            Array.Resize(ref requiredTraversal, nodeCount);
            Array.Resize(ref promotionHeads, nodeCount);
            Array.Resize(ref promotionNext, nodeCount);
        }
        if (retainedLeases.Length >= nodeCount)
        {
            return;
        }

        int previousLength = retainedLeases.Length;
        Array.Resize(ref retainedLeases, nodeCount);
        for (int index = previousLength;
            index < retainedLeases.Length;
            index++)
        {
            retainedLeases[index] =
                new PrismRetainedSurfaceLease();
        }
    }

    private void AcquireRetainedHits(
        PrismGraphExecutionPlan plan)
    {
        RecalculateRequiredNodes(plan);
        int baselinePassCount =
            CountRequiredNodes(
                plan.ExecutionOrder.Length,
                requiredNodes);
        int baselineCaptureCount =
            CountRequiredCaptures(plan, requiredNodes);
        PrismCacheMissReason frameMissReason =
            pendingMissReason;
        pendingMissReason =
            PrismCacheMissReason.NotFound;
        AcquireRetainedHits(
            plan,
            finalCandidates: true,
            frameMissReason);
        AcquireRetainedHits(
            plan,
            finalCandidates: false,
            frameMissReason);

        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            requiredTransientSurfaces[index] =
                requiredNodes[index] &&
                !retainedLeases[index].IsActive;
            if (requiredTransientSurfaces[index])
            {
                cacheResultValid[index] = true;
            }
        }

        int requiredPassCount =
            CountRequiredNodes(
                plan.ExecutionOrder.Length,
                requiredTransientSurfaces);
        int requiredCaptureCount =
            CountRequiredCaptures(
                plan,
                requiredTransientSurfaces);
        savedPassCount = checked(
            savedPassCount +
            baselinePassCount -
            requiredPassCount);
        savedCaptureCount = checked(
            savedCaptureCount +
            baselineCaptureCount -
            requiredCaptureCount);
    }

    private void AcquireRetainedHits(
        PrismGraphExecutionPlan plan,
        bool finalCandidates,
        PrismCacheMissReason frameMissReason)
    {
        for (int index = plan.ExecutionOrder.Length - 1;
            index >= 0;
            index--)
        {
            PrismRetainedCacheCandidateKind kind =
                plan.NodePlans[index].CacheCandidateKind;
            bool isCandidate = finalCandidates
                ? kind == PrismRetainedCacheCandidateKind.Final ||
                    IsRootOutput(plan, index)
                : kind is
                    PrismRetainedCacheCandidateKind.Capture or
                    PrismRetainedCacheCandidateKind.Intermediate;
            if (!isCandidate ||
                !requiredNodes[index])
            {
                continue;
            }
            if (!retainedCacheEnabled)
            {
                RecordMiss(PrismCacheMissReason.Disabled);
                continue;
            }
            if (!retainedKeyAvailable[index])
            {
                RecordMiss(PrismCacheMissReason.NotCacheable);
                continue;
            }
            if (!retainedSurfaceCache.TryAcquire(
                retainedKeys[index],
                retainedLeases[index]))
            {
                RecordMiss(frameMissReason);
                continue;
            }

            cacheResultValid[index] = true;
            if (kind ==
                PrismRetainedCacheCandidateKind.Final)
            {
                finalHitCount++;
            }
            else
            {
                intermediateHitCount++;
            }
            RecalculateRequiredNodes(plan);
            ReleaseUnusedRetainedLeases(
                plan.ExecutionOrder.Length);
        }
    }

    private void RecalculateRequiredNodes(
        PrismGraphExecutionPlan plan)
    {
        int nodeCount = plan.ExecutionOrder.Length;
        Array.Clear(requiredNodes, 0, nodeCount);
        int pendingCount = 0;
        foreach (int rootIndex in
            plan.RootOutputExecutionIndices)
        {
            if (requiredNodes[rootIndex])
            {
                continue;
            }

            requiredNodes[rootIndex] = true;
            requiredTraversal[pendingCount++] = rootIndex;
        }

        while (pendingCount > 0)
        {
            int index =
                requiredTraversal[--pendingCount];
            if (retainedLeases[index].IsActive)
            {
                continue;
            }

            foreach (int inputIndex in
                plan.CacheInputExecutionIndices[index])
            {
                if (requiredNodes[inputIndex])
                {
                    continue;
                }

                requiredNodes[inputIndex] = true;
                requiredTraversal[pendingCount++] =
                    inputIndex;
            }
        }
    }

    private void ReleaseUnusedRetainedLeases(
        int nodeCount)
    {
        for (int index = 0; index < nodeCount; index++)
        {
            if (retainedLeases[index].IsActive &&
                !requiredNodes[index])
            {
                retainedLeases[index].Dispose();
                cacheResultValid[index] = false;
            }
        }
    }

    private bool AreCacheInputsValid(
        PrismGraphExecutionPlan plan,
        int executionIndex)
    {
        foreach (int inputIndex in
            plan.CacheInputExecutionIndices[executionIndex])
        {
            if (!cacheResultValid[inputIndex])
            {
                return false;
            }
        }

        return true;
    }

    private void PromoteCompletedResults(
        PrismGraphExecutionPlan plan,
        PrismSurfaceFrame frame,
        int step)
    {
        if (!retainedCacheEnabled)
        {
            return;
        }

        for (int index = promotionHeads[step];
            index >= 0;
            index = promotionNext[index])
        {
            if (requiredTransientSurfaces[index] &&
                cacheResultValid[index])
            {
                retainedSurfaceCache.TryPromote(
                    retainedKeys[index],
                    frame,
                    index);
            }
        }
    }

    private void RecordMiss(PrismCacheMissReason reason)
    {
        if (reason == PrismCacheMissReason.None ||
            !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "A retained cache miss requires a concrete reason.");
        }

        missCount++;
        missCounts[(int)reason]++;
        lastMissReason = reason;
    }

    private long GetMissCount(
        PrismCacheMissReason reason) =>
        missCounts[(int)reason];

    private static int CountRequiredNodes(
        int nodeCount,
        bool[] required)
    {
        int count = 0;
        for (int index = 0; index < nodeCount; index++)
        {
            if (required[index])
            {
                count++;
            }
        }
        return count;
    }

    private static int CountRequiredCaptures(
        PrismGraphExecutionPlan plan,
        bool[] required)
    {
        int count = 0;
        PrismGraph graph = plan.OptimizedGraph;
        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            if (required[index] &&
                graph.GetNode(plan.ExecutionOrder[index]).Kind ==
                    PrismGraphNodeKind.ControlCapture)
            {
                count++;
            }
        }
        return count;
    }

    private static bool IsRootOutput(
        PrismGraphExecutionPlan plan,
        int executionIndex)
    {
        foreach (int rootIndex in
            plan.RootOutputExecutionIndices)
        {
            if (rootIndex == executionIndex)
            {
                return true;
            }
        }
        return false;
    }

    private static PrismDependencyChange DiffRetainedKey(
        in PrismRetainedCacheKey previous,
        in PrismRetainedCacheKey current)
    {
        PrismDependencyChange changes =
            PrismDependencyChange.None;
        if (previous.DependencyStamp.CacheOwnerToken !=
                current.DependencyStamp.CacheOwnerToken ||
            previous.StableNodeId.ScopeOwnerToken !=
                current.StableNodeId.ScopeOwnerToken)
        {
            changes |= PrismDependencyChange.Owner;
        }
        if (previous.CandidateKind != current.CandidateKind ||
            previous.StableNodeId.DefinitionNodeId !=
                current.StableNodeId.DefinitionNodeId ||
            previous.StableNodeId.Kind !=
                current.StableNodeId.Kind ||
            previous.StableNodeId.Ordinal !=
                current.StableNodeId.Ordinal ||
            previous.DependencyStamp.StructuralVersion !=
                current.DependencyStamp.StructuralVersion ||
            previous.StructuralFingerprint !=
                current.StructuralFingerprint)
        {
            changes |= PrismDependencyChange.Structure;
        }
        if (previous.DependencyStamp.ValueVersion !=
                current.DependencyStamp.ValueVersion ||
            previous.DependencyStamp.VisualContentVersion !=
                current.DependencyStamp.VisualContentVersion ||
            previous.DependencyStamp.DescendantVersion !=
                current.DependencyStamp.DescendantVersion ||
            previous.ValueFingerprint != current.ValueFingerprint)
        {
            changes |= PrismDependencyChange.Values;
        }
        if (previous.DependencyFingerprint !=
            current.DependencyFingerprint)
        {
            changes |= PrismDependencyChange.Resources;
        }
        if (previous.RasterBounds != current.RasterBounds)
        {
            changes |= PrismDependencyChange.RasterBounds;
        }
        if (previous.SurfaceWidth != current.SurfaceWidth ||
            previous.SurfaceHeight != current.SurfaceHeight)
        {
            changes |= PrismDependencyChange.SurfaceSize;
        }
        if (previous.LowerUiVersion != current.LowerUiVersion)
        {
            changes |= PrismDependencyChange.LowerUi;
        }
        if (previous.PixelScaleBits != current.PixelScaleBits)
        {
            changes |= PrismDependencyChange.PixelScale;
        }
        if (previous.EffectiveTransform !=
            current.EffectiveTransform)
        {
            changes |= PrismDependencyChange.Transform;
        }
        if (previous.WorkingColorProfile !=
            current.WorkingColorProfile)
        {
            changes |=
                PrismDependencyChange.WorkingColorProfile;
        }
        if (previous.OutputColorProfile !=
            current.OutputColorProfile)
        {
            changes |=
                PrismDependencyChange.OutputColorProfile;
        }
        if (previous.SurfaceFormat != current.SurfaceFormat)
        {
            changes |= PrismDependencyChange.SurfaceFormat;
        }
        if (previous.Sampling != current.Sampling)
        {
            changes |= PrismDependencyChange.Sampling;
        }
        if (previous.CapabilitySet != current.CapabilitySet)
        {
            changes |= PrismDependencyChange.Capabilities;
        }
        if (previous.ShaderPackageVersion !=
            current.ShaderPackageVersion)
        {
            changes |= PrismDependencyChange.ShaderPackage;
        }
        return changes;
    }

    private static PrismDependencyChange DiffRasterContext(
        in PrismRetainedRasterContext previous,
        in PrismRetainedRasterContext current)
    {
        PrismDependencyChange changes =
            PrismDependencyChange.None;
        if (previous.SurfaceWidth != current.SurfaceWidth ||
            previous.SurfaceHeight != current.SurfaceHeight)
        {
            changes |= PrismDependencyChange.SurfaceSize;
        }
        if (previous.OutputColorProfile !=
            current.OutputColorProfile)
        {
            changes |=
                PrismDependencyChange.OutputColorProfile;
        }
        if (previous.SurfaceFormat != current.SurfaceFormat)
        {
            changes |= PrismDependencyChange.SurfaceFormat;
        }
        if (previous.Sampling != current.Sampling)
        {
            changes |= PrismDependencyChange.Sampling;
        }
        if (previous.CapabilitySet != current.CapabilitySet)
        {
            changes |= PrismDependencyChange.Capabilities;
        }
        if (previous.ShaderPackageVersion !=
            current.ShaderPackageVersion)
        {
            changes |= PrismDependencyChange.ShaderPackage;
        }
        return changes;
    }

    private void ReleaseRetainedLeases()
    {
        for (int index = 0;
            index < retainedLeases.Length;
            index++)
        {
            retainedLeases[index]?.Dispose();
        }
    }

    private int RenderHostPrelude(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraph graph)
    {
        int firstRootBegin = commands.Count;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth == 0)
            {
                firstRootBegin = Math.Min(
                    firstRootBegin,
                    scope.BeginCommandIndex);
            }
        }

        RenderRawRange(
            renderer,
            commands,
            0,
            firstRootBegin);
        return firstRootBegin;
    }

    private void RenderNode(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node,
        IBackdropFrameLease? backdropLease)
    {
        RenderTarget2D target = frame.GetSurface(step);
        switch (node.Kind)
        {
            case PrismGraphNodeKind.ControlCapture:
                CaptureControl(
                    renderer,
                    commands,
                    graph,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.BackdropInput:
                RenderBackdropInput(
                    renderer,
                    target,
                    node,
                    backdropLease);
                break;
            case PrismGraphNodeKind.BackdropCrop:
                RenderBackdropCrop(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.ColorConversion:
                RenderColorConversion(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Layer:
            case PrismGraphNodeKind.Group:
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                break;
            case PrismGraphNodeKind.Filter:
                RenderFilter(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Style:
                RenderStyle(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Mask:
                RenderMask(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Fill:
            case PrismGraphNodeKind.Opacity:
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    Math.Clamp(node.Amount ?? 1f, 0f, 1f));
                break;
            case PrismGraphNodeKind.ClipToBelow:
                RenderTwoInputKernel(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    PrismGraphEdgeKind.Content,
                    PrismGraphEdgeKind.ClipBaseAlpha,
                    kernels.ClipAlpha,
                    1f);
                break;
            case PrismGraphNodeKind.Composite:
            case PrismGraphNodeKind.PassThroughComposite:
                RenderComposite(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported Prism graph node kind '{node.Kind}'.");
        }
    }

    private void CaptureControl(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraph graph,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        int scopeIndex = scope.AnalysisScopeIndex;

        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        if (!initializedCaptures[scopeIndex])
        {
            graphicsDevice.Clear(
                Microsoft.Xna.Framework.Color.Transparent);
            initializedCaptures[scopeIndex] = true;
        }

        renderer.BeginCommandBatch();
        RenderRawRange(
            renderer,
            commands,
            captureCommandIndices[scopeIndex],
            scope.EndCommandIndex);
        captureCommandIndices[scopeIndex] =
            scope.EndCommandIndex;
        renderer.EndBatch();
    }

    private void RenderBackdropInput(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        PrismGraphNode node,
        IBackdropFrameLease? backdropLease)
    {
        if (backdropLease is null)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            RecordFallback(
                node,
                PrismFallbackReason.MissingBackdrop,
                "The frame did not provide a backdrop lease.");
            return;
        }
        if (backdropLease is not IMonoGameBackdropFrameLease monoGameLease)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            RecordFallback(
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The backdrop lease does not expose a MonoGame texture.");
            return;
        }

        Texture2D texture;
        BackdropFrameMetadata metadata;
        try
        {
            texture = monoGameLease.Texture;
            metadata = backdropLease.Metadata;
        }
        catch (Exception exception) when (
            exception is ObjectDisposedException or
                InvalidOperationException)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            RecordFallback(
                node,
                PrismFallbackReason.UnsupportedCapability,
                exception.Message);
            return;
        }

        if (!MonoGameBackdropFrameValidation.TryValidate(
            texture,
            graphicsDevice,
            in metadata,
            out string diagnostic))
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            RecordFallback(
                node,
                PrismFallbackReason.UnsupportedCapability,
                diagnostic);
            return;
        }

        RenderKernel(
            renderer,
            target,
            texture,
            texture,
            kernels.Copy,
            1f);
    }

    private void RenderBackdropCrop(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        int sourceIndex =
            FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0 ||
            node.BackdropSourceBounds is not DrawRect sourceBounds ||
            sourceBounds.Width <= 0 ||
            sourceBounds.Height <= 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        Texture2D source =
            GetExecutionSurface(frame, sourceIndex);
        BackdropFrameMetadata? metadata = FindBackdropMetadata(
            plan,
            graph,
            node.Id);
        if (metadata is not BackdropFrameMetadata backdropMetadata)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            RecordFallback(
                node,
                PrismFallbackReason.MissingBackdrop,
                "The backdrop crop has no raster metadata.");
            return;
        }

        System.Numerics.Matrix3x2 transform =
            backdropMetadata.CoordinateTransform;
        float pixelScale = scope.PixelScale;
        PrismBackdropCropKernelSettings settings = new(
            (float)backdropMetadata.AlphaMode,
            new Vector3(
                transform.M11 /
                    (pixelScale * backdropMetadata.PixelWidth),
                transform.M21 /
                    (pixelScale * backdropMetadata.PixelWidth),
                transform.M31 /
                    backdropMetadata.PixelWidth),
            new Vector3(
                transform.M12 /
                    (pixelScale * backdropMetadata.PixelHeight),
                transform.M22 /
                    (pixelScale * backdropMetadata.PixelHeight),
                transform.M32 /
                    backdropMetadata.PixelHeight));
        Rectangle destination = ResolveBackdropDestination(
            scope.Bounds,
            pixelScale,
            target);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }

        RenderKernel(
            renderer,
            target,
            source,
            source,
            kernels.BackdropCrop,
            1f,
            destination: destination,
            backdropCropSettings: settings);
    }

    private void RenderColorConversion(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.BackdropMetadata is BackdropFrameMetadata backdropMetadata)
        {
            if (node.ColorProfile is not PrismColorProfile targetProfile ||
                !Enum.IsDefined(backdropMetadata.ColorProfile) ||
                !Enum.IsDefined(targetProfile))
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.InvalidColorProfile,
                    node.DiagnosticName);
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            int sourceIndex =
                FindAnyInputIndex(plan, graph, node.Id);
            if (sourceIndex < 0)
            {
                ClearSurface(
                    renderer,
                    target,
                    Microsoft.Xna.Framework.Color.Transparent);
                return;
            }

            Texture2D source =
                GetExecutionSurface(frame, sourceIndex);
            RenderKernel(
                renderer,
                target,
                source,
                source,
                kernels.BackdropColorConversion,
                1f,
                backdropColorSettings:
                    new PrismBackdropColorKernelSettings(
                        backdropMetadata.ColorProfile,
                        targetProfile));
            return;
        }

        if (node.ColorProfile is not PrismColorProfile profile ||
            !kernels.TryGetColorConversionKernel(
                profile,
                out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        RenderSingleInputKernel(
            renderer,
            plan,
            graph,
            frame,
            step,
            target,
            node,
            kernel,
            1f);
    }

    private void RenderMask(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.MaskPass is null or PrismMaskPass.Extract)
        {
            RenderMaskExtract(renderer, graph, target, node);
            return;
        }

        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.MissingKernel,
                "A mask feather pass has no scalar input.");
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        float transformScale = MathF.Max(
            MathF.Sqrt(
                (scope.EffectiveTransform.M11 *
                    scope.EffectiveTransform.M11) +
                (scope.EffectiveTransform.M12 *
                    scope.EffectiveTransform.M12)),
            MathF.Sqrt(
                (scope.EffectiveTransform.M21 *
                    scope.EffectiveTransform.M21) +
                (scope.EffectiveTransform.M22 *
                    scope.EffectiveTransform.M22)));
        float radius =
            (node.Feather ?? 0) *
            transformScale *
            scope.PixelScale;
        if (!float.IsFinite(radius))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The transformed mask feather radius is not finite.");
            return;
        }

        Vector2 featherStep =
            node.MaskPass == PrismMaskPass.FeatherHorizontal
                ? new Vector2(radius / target.Width, 0)
                : new Vector2(0, radius / target.Height);
        float density =
            node.MaskPass == PrismMaskPass.FeatherVertical
                ? node.Density ?? 1
                : 1;
        PrismMaskKernelSettings settings = new(
            Channel: 0,
            Density: density,
            Invert: 0,
            UvRowX: new Vector3(1, 0, 0),
            UvRowY: new Vector3(0, 1, 0),
            FeatherStep: featherStep);
        Texture2D source =
            GetExecutionSurface(frame, sourceIndex);
        RenderKernel(
            renderer,
            target,
            source,
            source,
            kernels.MaskFeather,
            1f,
            maskSettings: settings);
    }

    private void RenderStyle(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan executionPlan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.Style is not PrismStyleId style ||
            !kernels.TryGetStyleKernel(
                style,
                out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        int contentIndex = FindInputIndex(
            executionPlan,
            graph,
            node.Id,
            PrismGraphEdgeKind.Content);
        int sourceIndex = FindInputIndex(
            executionPlan,
            graph,
            node.Id,
            PrismGraphEdgeKind.StyleSource);
        if (contentIndex < 0 || sourceIndex < 0)
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                "A layer-style pass has no content or prepared source.");
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        PrismStylePlan stylePlan =
            PrismStylePlanner.Create(node, scope);
        Texture2D styleTexture =
            GetExecutionSurface(frame, sourceIndex);
        bool resourceAvailable = false;
        if (stylePlan.ResourceEnabled)
        {
            if (stylePlan.Resource.Value <= 0 ||
                !scope.Resources.TryGetImage(
                    stylePlan.Resource,
                    out IDrawImage image))
            {
                if (stylePlan.ResourceRequired)
                {
                    RecordFallback(
                        node,
                        PrismFallbackReason.MissingResource,
                        $"Style resource '{stylePlan.Resource}' is not available.");
                    RenderCopyInput(
                        renderer,
                        executionPlan,
                        graph,
                        frame,
                        step,
                        target,
                        node,
                        1f);
                    return;
                }
            }
            else if (image is not MonoGameImage monoGameImage ||
                monoGameImage.Texture.IsDisposed ||
                !ReferenceEquals(
                    monoGameImage.Texture.GraphicsDevice,
                    graphicsDevice))
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.UnsupportedCapability,
                    "The style resource is not a live MonoGame texture owned by this graphics device.");
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }
            else
            {
                styleTexture = monoGameImage.Texture;
                resourceAvailable = true;
            }
        }

        PrismStyleSamplingGeometry geometry =
            PrismStylePlanner.ResolveSamplingGeometry(
                stylePlan,
                scope);
        ResolveScopeUvMapping(
            scope,
            out Vector3 boundsUvRowX,
            out Vector3 boundsUvRowY);
        PrismStyleKernelSettings settings = new(
            styleTexture,
            GetExecutionSurface(frame, sourceIndex),
            ToVector4(stylePlan.PrimaryColor),
            ToVector4(stylePlan.SecondaryColor),
            new Vector4(
                geometry.Offset.X / target.Width,
                geometry.Offset.Y / target.Height,
                geometry.Size,
                geometry.Spread),
            new Vector4(
                stylePlan.Angle * (MathF.PI / 180f),
                stylePlan.Altitude * (MathF.PI / 180f),
                stylePlan.Depth,
                geometry.Soften),
            new Vector4(
                stylePlan.Opacity,
                stylePlan.SecondaryOpacity,
                stylePlan.Noise,
                stylePlan.Jitter),
            new Vector4(
                stylePlan.Scale,
                stylePlan.TextureDepth,
                stylePlan.Offset.X,
                stylePlan.Offset.Y),
            new Vector4(
                stylePlan.Kind,
                (int)stylePlan.BlendMode,
                (int)stylePlan.SecondaryBlendMode,
                (int)stylePlan.PaintKind),
            new Vector4(
                stylePlan.Contour,
                stylePlan.DetailContour,
                stylePlan.Technique,
                stylePlan.Position),
            new Vector4(
                stylePlan.Origin,
                stylePlan.Direction,
                stylePlan.GradientMethod,
                stylePlan.GradientStyle),
            new Vector4(
                stylePlan.BevelStyle,
                (int)stylePlan.Flags,
                stylePlan.Range,
                0),
            boundsUvRowX,
            boundsUvRowY,
            resourceAvailable);
        PrismScratchSurfaceLease? scratchA = null;
        PrismScratchSurfaceLease? scratchB = null;
        try
        {
            if (style == PrismStyleId.OuterGlow)
            {
                scratchA = frame.RentScratch(surfaceKeys[step]);
                scratchB = frame.RentScratch(surfaceKeys[step]);
                Texture2D preparedMask = PrepareOuterGlowMask(
                    renderer,
                    GetExecutionSurface(frame, sourceIndex),
                    scratchA.Value.Surface,
                    scratchB.Value.Surface,
                    geometry.Size,
                    geometry.Spread,
                    stylePlan.Technique);
                settings = settings with
                {
                    MaskTexture = preparedMask
                };
            }
            else if (style == PrismStyleId.Stroke)
            {
                PrismSurfaceKey distanceKey = new(
                    target.Width,
                    target.Height,
                    SurfaceFormat.Vector4,
                    0,
                    surfaceKeys[step].ColorProfile);
                scratchA = frame.RentScratch(distanceKey);
                scratchB = frame.RentScratch(distanceKey);
                Texture2D distanceField =
                    PrepareStrokeDistanceField(
                        renderer,
                        GetExecutionSurface(frame, sourceIndex),
                        scratchA.Value.Surface,
                        scratchB.Value.Surface);
                settings = settings with
                {
                    MaskTexture = distanceField
                };
            }

            RenderKernel(
                renderer,
                target,
                GetExecutionSurface(frame, contentIndex),
                GetExecutionSurface(frame, sourceIndex),
                kernel,
                1f,
                styleSettings: settings);
        }
        finally
        {
            scratchB?.Dispose();
            scratchA?.Dispose();
        }
    }

    private Texture2D PrepareOuterGlowMask(
        IPrismCommandRenderer renderer,
        Texture2D source,
        RenderTarget2D scratchA,
        RenderTarget2D scratchB,
        float size,
        float spread,
        float technique)
    {
        Texture2D prepared = source;
        if (spread >= 0.5f)
        {
            RenderStyleMaskPass(
                renderer,
                scratchA,
                prepared,
                kernels.StyleDilate,
                MathF.Ceiling(spread),
                horizontal: true);
            RenderStyleMaskPass(
                renderer,
                scratchB,
                scratchA,
                kernels.StyleDilate,
                MathF.Ceiling(spread),
                horizontal: false);
            prepared = scratchB;
        }

        float techniqueScale = technique < 0.5f
            ? 1f
            : technique < 1.5f ? 0.65f : 0.8f;
        float radius = MathF.Max(
            MathF.Ceiling(size * techniqueScale * 1.5f),
            1f);
        RenderStyleMaskPass(
            renderer,
            scratchA,
            prepared,
            kernels.StyleGaussian,
            radius,
            horizontal: true);
        RenderStyleMaskPass(
            renderer,
            scratchB,
            scratchA,
            kernels.StyleGaussian,
            radius,
            horizontal: false);
        return scratchB;
    }

    private Texture2D PrepareStrokeDistanceField(
        IPrismCommandRenderer renderer,
        Texture2D source,
        RenderTarget2D scratchA,
        RenderTarget2D scratchB)
    {
        RenderKernel(
            renderer,
            scratchA,
            source,
            source,
            kernels.StrokeDistanceSeed,
            1f);

        RenderTarget2D read = scratchA;
        RenderTarget2D write = scratchB;
        int extent = Math.Max(source.Width, source.Height);
        int jump = 1;
        while (jump < extent)
        {
            jump <<= 1;
        }
        jump >>= 1;

        while (jump >= 1)
        {
            RenderStrokeDistanceFloodPass(
                renderer,
                write,
                read,
                jump);
            (read, write) = (write, read);
            jump >>= 1;
        }

        // JFA+1 removes the common one-texel propagation errors at Voronoi boundaries.
        RenderStrokeDistanceFloodPass(
            renderer,
            write,
            read,
            1);
        return write;
    }

    private void RenderStrokeDistanceFloodPass(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Texture2D source,
        int jump)
    {
        PrismMaskKernelSettings settings = new(
            Channel: 0,
            Density: 0,
            Invert: 0,
            UvRowX: new Vector3(1, 0, 0),
            UvRowY: new Vector3(0, 1, 0),
            FeatherStep: new Vector2(
                jump / (float)source.Width,
                jump / (float)source.Height));
        RenderKernel(
            renderer,
            target,
            source,
            source,
            kernels.StrokeDistanceFlood,
            1f,
            maskSettings: settings);
    }

    private void RenderStyleMaskPass(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Texture2D source,
        PrismKernel kernel,
        float radius,
        bool horizontal)
    {
        PrismMaskKernelSettings settings = new(
            Channel: 0,
            Density: radius,
            Invert: 0,
            UvRowX: new Vector3(1, 0, 0),
            UvRowY: new Vector3(0, 1, 0),
            FeatherStep: horizontal
                ? new Vector2(1f / source.Width, 0)
                : new Vector2(0, 1f / source.Height));
        RenderKernel(
            renderer,
            target,
            source,
            source,
            kernel,
            1f,
            maskSettings: settings);
    }

    private void RenderFilter(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan executionPlan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.Filter is not PrismFilterId filter ||
            !kernels.TryGetFilterKernel(
                filter,
                out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        int sourceIndex =
            FindAnyInputIndex(
                executionPlan,
                graph,
                node.Id);
        if (sourceIndex < 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        Texture2D source =
            GetExecutionSurface(frame, sourceIndex);
        if (node.NeighborhoodPlan is PrismNeighborhoodPlan neighborhoodPlan)
        {
            if (neighborhoodPlan.Filter != filter ||
                (uint)node.NeighborhoodPassIndex >=
                    (uint)neighborhoodPlan.Passes.Length)
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.MissingKernel,
                    "The filter node has an invalid prepared neighborhood plan.");
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            if (!TryResolveFilterResource(
                    scope,
                    node,
                    source,
                    neighborhoodPlan.Resource,
                    neighborhoodPlan.ResourceRequired,
                    out Texture2D resourceTexture,
                    out bool resourceAvailable))
            {
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            PrismNeighborhoodPass pass =
                neighborhoodPlan.Passes[node.NeighborhoodPassIndex];
            PrismFilterKernelSettings settings = new(
                resourceTexture,
                new Vector4(
                    (int)neighborhoodPlan.Operation,
                    (int)scope.CompositionSettings.WorkingColorProfile,
                    (int)pass.Kind,
                    resourceAvailable ? 1 : 0),
                ToVector4(neighborhoodPlan.Options0),
                ToVector4(neighborhoodPlan.Options1),
                ToVector4(neighborhoodPlan.Options2),
                ToVector4(neighborhoodPlan.Options3),
                Vector4.Zero,
                Vector4.Zero,
                Vector4.Zero,
                Vector4.Zero,
                Vector4.Zero,
                new Vector4(
                    pass.RadiusX,
                    pass.RadiusY,
                    pass.SampleCount,
                    (int)neighborhoodPlan.BlendMode),
                new Vector2(
                    resourceTexture.Width,
                    resourceTexture.Height));
            RenderKernel(
                renderer,
                target,
                source,
                resourceTexture,
                kernel,
                Math.Clamp(node.Amount ?? 1f, 0, 1),
                filterSettings: settings);
            return;
        }

        if (node.ResamplingPlan is PrismResamplingPlan resamplingPlan)
        {
            if (resamplingPlan.Filter != filter ||
                (uint)node.ResamplingPassIndex >=
                    (uint)resamplingPlan.Passes.Length)
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.MissingKernel,
                    "The filter node has an invalid prepared resampling plan.");
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            if (!TryResolveFilterResource(
                    scope,
                    node,
                    source,
                    resamplingPlan.PrimaryResource,
                    resamplingPlan.PrimaryResourceRequired,
                    out Texture2D primaryTexture,
                    out bool primaryAvailable) ||
                !TryResolveFilterResource(
                    scope,
                    node,
                    source,
                    resamplingPlan.AuxiliaryResource,
                    resamplingPlan.AuxiliaryResourceRequired,
                    out Texture2D auxiliaryTexture,
                    out bool auxiliaryAvailable))
            {
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            PrismResamplingPass pass =
                resamplingPlan.Passes[node.ResamplingPassIndex];
            PrismFilterKernelSettings settings = new(
                primaryTexture,
                new Vector4(
                    (int)resamplingPlan.Operation,
                    (int)scope.CompositionSettings.WorkingColorProfile,
                    (int)pass.Kind,
                    primaryAvailable ? 1 : 0),
                ToVector4(resamplingPlan.Options0),
                ToVector4(resamplingPlan.Options1),
                ToVector4(resamplingPlan.Options2),
                ToVector4(resamplingPlan.Options3),
                ToVector4(resamplingPlan.Options4),
                ToVector4(resamplingPlan.Options5),
                new Vector4(
                    auxiliaryAvailable ? 1 : 0,
                    0,
                    0,
                    0),
                Vector4.Zero,
                Vector4.Zero,
                new Vector4(
                    0,
                    0,
                    0,
                    (int)resamplingPlan.BlendMode),
                new Vector2(
                    primaryTexture.Width,
                    primaryTexture.Height),
                auxiliaryTexture);
            RenderKernel(
                renderer,
                target,
                source,
                primaryTexture,
                kernel,
                Math.Clamp(node.Amount ?? 1f, 0, 1),
                filterSettings: settings);
            return;
        }

        if (node.CatalogFilterPlan is PrismCatalogFilterPlan catalogPlan)
        {
            if (catalogPlan.Filter != filter ||
                (uint)node.CatalogFilterPassIndex >=
                    (uint)catalogPlan.Passes.Length)
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.MissingKernel,
                    "The filter node has an invalid prepared catalog filter plan.");
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            if (!TryResolveFilterResource(
                    scope,
                    node,
                    source,
                    catalogPlan.PrimaryResource,
                    catalogPlan.PrimaryResourceRequired,
                    out Texture2D primaryTexture,
                    out bool primaryAvailable) ||
                !TryResolveFilterResource(
                    scope,
                    node,
                    source,
                    catalogPlan.AuxiliaryResource,
                    catalogPlan.AuxiliaryResourceRequired,
                    out Texture2D auxiliaryTexture,
                    out bool auxiliaryAvailable))
            {
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }

            PrismCatalogFilterPass pass =
                catalogPlan.Passes[node.CatalogFilterPassIndex];
            float resourceMask =
                (primaryAvailable ? 1 : 0) +
                (auxiliaryAvailable ? 2 : 0);
            float packedPass =
                (int)pass.Kind +
                (pass.Iteration * 4);
            PrismFilterKernelSettings settings = new(
                primaryTexture,
                new Vector4(
                    (int)catalogPlan.Filter,
                    (int)scope.CompositionSettings.WorkingColorProfile,
                    (int)catalogPlan.Primitive,
                    resourceMask),
                ToVector4(catalogPlan.Options0),
                ToVector4(catalogPlan.Options1),
                ToVector4(catalogPlan.Options2),
                ToVector4(catalogPlan.Options3),
                ToVector4(catalogPlan.Options4),
                ToVector4(catalogPlan.Options5),
                ToVector4(catalogPlan.Options6),
                ToVector4(catalogPlan.Options7),
                ToVector4(catalogPlan.Options8),
                new Vector4(
                    pass.RadiusX,
                    pass.RadiusY,
                    packedPass,
                    (int)catalogPlan.BlendMode),
                new Vector2(
                    primaryTexture.Width,
                    primaryTexture.Height),
                auxiliaryTexture);
            RenderKernel(
                renderer,
                target,
                source,
                primaryTexture,
                kernel,
                Math.Clamp(node.Amount ?? 1f, 0, 1),
                filterSettings: settings);
            return;
        }

        if (!PrismAdjustmentPlanner.IsSupported(filter))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        PrismAdjustmentPlan filterPlan =
            PrismAdjustmentPlanner.Create(node, scope);
        if (!TryResolveFilterResource(
                scope,
                node,
                source,
                filterPlan.Resource,
                filterPlan.ResourceRequired,
                out Texture2D adjustmentResource,
                out bool adjustmentResourceAvailable))
        {
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        PrismFilterKernelSettings adjustmentSettings = new(
            adjustmentResource,
            new Vector4(
                (int)filterPlan.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile,
                (int)filterPlan.BlendMode,
                adjustmentResourceAvailable ? 1 : 0),
            ToVector4(filterPlan.Parameters0),
            ToVector4(filterPlan.Parameters1),
            ToVector4(filterPlan.Parameters2),
            ToVector4(filterPlan.Parameters3),
            ToVector4(filterPlan.Parameters4),
            ToVector4(filterPlan.Parameters5),
            ToVector4(filterPlan.Parameters6),
            ToVector4(filterPlan.Parameters7),
            ToVector4(filterPlan.Parameters8),
            ToVector4(filterPlan.Parameters9),
            new Vector2(
                adjustmentResource.Width,
                adjustmentResource.Height));
        RenderKernel(
            renderer,
            target,
            source,
            adjustmentResource,
            kernel,
            Math.Clamp(node.Amount ?? 1f, 0, 1),
            filterSettings: adjustmentSettings);
    }

    private bool TryResolveFilterResource(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismResourceId resource,
        bool required,
        out Texture2D texture,
        out bool available)
    {
        texture = fallback;
        available = false;
        if (resource.Value <= 0)
        {
            if (!required)
            {
                return true;
            }

            RecordFallback(
                node,
                PrismFallbackReason.MissingResource,
                $"Filter resource '{resource}' is not available.");
            return false;
        }

        if (!scope.Resources.TryGetImage(
                resource,
                out IDrawImage image))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingResource,
                $"Filter resource '{resource}' is not available.");
            return false;
        }
        if (image is not MonoGameImage monoGameImage ||
            monoGameImage.Texture.IsDisposed ||
            !ReferenceEquals(
                monoGameImage.Texture.GraphicsDevice,
                graphicsDevice))
        {
            RecordFallback(
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The filter resource is not a live MonoGame texture owned by this graphics device.");
            return false;
        }

        texture = monoGameImage.Texture;
        available = true;
        return true;
    }

    private void RenderMaskExtract(
        IPrismCommandRenderer renderer,
        PrismGraph graph,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        if (node.Resource is not UI.Prism.Definitions.PrismResourceId
                resource ||
            !scope.Resources.TryGetImage(resource, out IDrawImage image))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.MissingResource,
                $"Mask resource '{node.Resource?.ToString() ?? "<none>"}' is not available.");
            return;
        }
        if (image is not MonoGameImage maskImage ||
            maskImage.Texture.IsDisposed ||
            !ReferenceEquals(
                maskImage.Texture.GraphicsDevice,
                graphicsDevice))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The mask resource is not a live MonoGame texture owned by this graphics device.");
            return;
        }

        if (!ResolveScopeUvMapping(
            scope,
            out Vector3 uvRowX,
            out Vector3 uvRowY))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The mask mapping requires non-empty bounds and an invertible transform.");
            return;
        }

        float density = (node.Feather ?? 0) > 0
            ? 1
            : node.Density ?? 1;
        PrismMaskKernelSettings settings = new(
            Channel: (float)(
                node.MaskChannel ??
                UI.Prism.Definitions.PrismMaskChannel.Alpha),
            Density: density,
            Invert: node.Invert == true ? 1 : 0,
            UvRowX: uvRowX,
            UvRowY: uvRowY,
            FeatherStep: Vector2.Zero);
        RenderKernel(
            renderer,
            target,
            maskImage.Texture,
            maskImage.Texture,
            kernels.MaskExtract,
            1f,
            maskSettings: settings);
    }

    private void RenderOpaqueMaskFallback(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismFallbackReason reason,
        string detail)
    {
        ClearSurface(
            renderer,
            target,
            Microsoft.Xna.Framework.Color.White);
        RecordFallback(node, reason, detail);
    }

    private void RenderComposite(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        int maskIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.MaskAlpha);
        if (maskIndex >= 0)
        {
            RenderTwoInputKernel(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                PrismGraphEdgeKind.Content,
                PrismGraphEdgeKind.MaskAlpha,
                kernels.MaskAlpha,
                1f);
            return;
        }

        int foregroundIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeForeground);
        if (foregroundIndex < 0)
        {
            foregroundIndex = FindInputIndex(
                plan,
                graph,
                node.Id,
                PrismGraphEdgeKind.Content);
        }
        int backgroundIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeBackground);

        if (foregroundIndex < 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }
        PrismBlendMode blendMode =
            node.BlendMode ?? PrismBlendMode.Normal;
        if (!kernels.TryGetBlendKernel(
            blendMode,
            out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderKernel(
                renderer,
                target,
                GetExecutionSurface(frame, foregroundIndex),
                GetExecutionSurface(frame, foregroundIndex),
                kernels.Copy,
                1f);
            return;
        }

        RenderKernel(
            renderer,
            target,
            GetExecutionSurface(frame, foregroundIndex),
            backgroundIndex >= 0
                ? GetExecutionSurface(frame, backgroundIndex)
                : GetExecutionSurface(frame, foregroundIndex),
            kernel,
            1f,
            node.LayerSettings,
            node.DefinitionNodeId?.Value ?? 0,
            backgroundIndex >= 0);
    }

    private void RenderCopyInput(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        float opacity)
    {
        RenderSingleInputKernel(
            renderer,
            plan,
            graph,
            frame,
            step,
            target,
            node,
            kernels.Copy,
            opacity);
    }

    private void RenderSingleInputKernel(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismKernel kernel,
        float opacity)
    {
        int sourceIndex =
            FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }

        RenderKernel(
            renderer,
            target,
            GetExecutionSurface(frame, sourceIndex),
            GetExecutionSurface(frame, sourceIndex),
            kernel,
            opacity);
    }

    private void RenderTwoInputKernel(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismGraphEdgeKind sourceKind,
        PrismGraphEdgeKind secondaryKind,
        PrismKernel kernel,
        float opacity)
    {
        int sourceIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            sourceKind);
        int secondaryIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            secondaryKind);
        if (sourceIndex < 0 || secondaryIndex < 0)
        {
            RenderCopyInput(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                opacity);
            return;
        }

        RenderKernel(
            renderer,
            target,
            GetExecutionSurface(frame, sourceIndex),
            GetExecutionSurface(frame, secondaryIndex),
            kernel,
            opacity);
    }

    private void RenderKernel(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Texture2D source,
        Texture2D secondary,
        PrismKernel kernel,
        float opacity,
        PrismGraphLayerSettings? layerSettings = null,
        int layerIdentity = 0,
        bool backgroundAvailable = true,
        PrismMaskKernelSettings? maskSettings = null,
        PrismStyleKernelSettings? styleSettings = null,
        PrismFilterKernelSettings? filterSettings = null,
        Rectangle? destination = null,
        PrismBackdropCropKernelSettings? backdropCropSettings = null,
        PrismBackdropColorKernelSettings? backdropColorSettings = null)
    {
        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(
            Microsoft.Xna.Framework.Color.Transparent);

        PrismKernelParameters parameters = new(
            secondary,
            opacity,
            new Vector2(
                1f / source.Width,
                1f / source.Height),
            FullUvScale,
            ZeroUvOffset)
        {
            BackgroundAvailable =
                backgroundAvailable ? 1 : 0
        };
        if (layerSettings is PrismGraphLayerSettings settings)
        {
            parameters = parameters with
            {
                BlendChannels = ResolveBlendChannels(
                    settings.BlendChannels),
                KnockoutMode = (float)settings.Knockout,
                BlendIfChannel =
                    (float)settings.BlendIfChannel,
                ThisLayerRange = ResolveBlendRange(
                    settings.ThisLayerRange),
                UnderlyingRange = ResolveBlendRange(
                    settings.UnderlyingRange),
                DissolveSeed =
                    PrismBlendMath.NormalizeDissolveSeed(
                        settings.DissolveSeed,
                        layerIdentity)
            };
        }
        if (maskSettings is PrismMaskKernelSettings mask)
        {
            parameters = parameters with
            {
                MaskChannel = mask.Channel,
                MaskDensity = mask.Density,
                MaskInvert = mask.Invert,
                MaskUvRowX = mask.UvRowX,
                MaskUvRowY = mask.UvRowY,
                MaskFeatherStep = mask.FeatherStep
            };
        }
        if (styleSettings is PrismStyleKernelSettings style)
        {
            parameters = parameters with
            {
                StyleTexture = style.Texture,
                StyleMaskTexture = style.MaskTexture,
                StyleColor = style.Color,
                StyleSecondaryColor =
                    style.SecondaryColor,
                StyleGeometry0 = style.Geometry0,
                StyleGeometry1 = style.Geometry1,
                StyleOptions0 = style.Options0,
                StyleOptions1 = style.Options1,
                StyleModes0 = style.Modes0,
                StyleModes1 = style.Modes1,
                StyleModes2 = style.Modes2,
                StyleModes3 = style.Modes3,
                StyleBoundsUvRowX = style.BoundsUvRowX,
                StyleBoundsUvRowY = style.BoundsUvRowY,
                StyleResourceAvailable =
                    style.ResourceAvailable ? 1 : 0
            };
        }
        if (filterSettings is PrismFilterKernelSettings filter)
        {
            parameters = parameters with
            {
                SecondaryTexture = filter.Texture,
                FilterHeader = filter.Header,
                FilterOptions0 = filter.Options0,
                FilterOptions1 = filter.Options1,
                FilterOptions2 = filter.Options2,
                FilterOptions3 = filter.Options3,
                FilterOptions4 = filter.Options4,
                FilterOptions5 = filter.Options5,
                FilterOptions6 = filter.Options6,
                FilterOptions7 = filter.Options7,
                FilterOptions8 = filter.Options8,
                FilterOptions9 = filter.Options9,
                FilterTextureSize = filter.TextureSize,
                FilterAuxiliaryTexture =
                    filter.AuxiliaryTexture
            };
        }
        if (backdropCropSettings is PrismBackdropCropKernelSettings crop)
        {
            parameters = parameters with
            {
                MaskChannel = crop.AlphaMode,
                MaskUvRowX = crop.UvRowX,
                MaskUvRowY = crop.UvRowY
            };
        }
        if (backdropColorSettings is PrismBackdropColorKernelSettings color)
        {
            parameters = parameters with
            {
                FilterHeader = new Vector4(
                    (float)color.SourceProfile,
                    (float)color.TargetProfile,
                    0,
                    0)
            };
        }
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.Opaque);
        renderer.DrawFullscreen(
            source,
            destination ??
                new Rectangle(0, 0, target.Width, target.Height));
        renderer.EndBatch();
    }

    private static BackdropFrameMetadata? FindBackdropMetadata(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId cropNodeId)
    {
        for (int edgeIndex = 0;
            edgeIndex < graph.Edges.Length;
            edgeIndex++)
        {
            PrismGraphEdge edge = graph.Edges[edgeIndex];
            if (edge.Source != cropNodeId)
            {
                continue;
            }

            PrismGraphNode target = graph.GetNode(edge.Target);
            if (target.Kind == PrismGraphNodeKind.ColorConversion &&
                FindExecutionIndex(plan, target.Id) >= 0)
            {
                return target.BackdropMetadata;
            }
        }

        return null;
    }

    private static Rectangle ResolveBackdropDestination(
        DrawRect bounds,
        float pixelScale,
        RenderTarget2D target)
    {
        float leftValue = Math.Clamp(
            MathF.Floor(bounds.X * pixelScale),
            0,
            target.Width);
        float topValue = Math.Clamp(
            MathF.Floor(bounds.Y * pixelScale),
            0,
            target.Height);
        float rightValue = Math.Clamp(
            MathF.Ceiling(bounds.Right * pixelScale),
            0,
            target.Width);
        float bottomValue = Math.Clamp(
            MathF.Ceiling(bounds.Bottom * pixelScale),
            0,
            target.Height);
        int left = (int)leftValue;
        int top = (int)topValue;
        return new Rectangle(
            left,
            top,
            Math.Max(0, (int)rightValue - left),
            Math.Max(0, (int)bottomValue - top));
    }

    private static Vector4 ResolveBlendChannels(
        UI.Prism.Runtime.PrismBlendChannels channels)
    {
        return new Vector4(
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Red) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Green) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Blue) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Alpha) != 0
                ? 1
                : 0);
    }

    private static Vector4 ToVector4(
        System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);

    private static Vector4 ResolveBlendRange(
        UI.Prism.Runtime.PrismBlendRange range)
    {
        return new Vector4(
            range.BlackStart,
            range.BlackEnd,
            range.WhiteStart,
            range.WhiteEnd);
    }

    private void ClearSurface(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Microsoft.Xna.Framework.Color color)
    {
        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(color);
    }

    private void PresentCompletedNestedScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.ParentScopeIndex is not int parentScopeIndex ||
                scope.Output is not PrismGraphNodeId output ||
                output != node.Id)
            {
                continue;
            }

            PrismGraphScope parentScope =
                FindScope(graph, parentScopeIndex);
            int parentCaptureStep =
                captureSteps[parentScopeIndex];
            if (parentCaptureStep < 0)
            {
                throw new InvalidOperationException(
                    $"Parent Prism scope {parentScopeIndex} has no control capture.");
            }

            RenderTarget2D parentTarget =
                frame.GetSurface(parentCaptureStep);
            renderer.EndBatch();
            graphicsDevice.SetRenderTarget(parentTarget);
            if (!initializedCaptures[parentScopeIndex])
            {
                graphicsDevice.Clear(
                    Microsoft.Xna.Framework.Color.Transparent);
                initializedCaptures[parentScopeIndex] = true;
            }

            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                captureCommandIndices[parentScopeIndex],
                scope.BeginCommandIndex);

            PrismKernel presentKernel =
                GetPresentKernel(scope, node);
            if (IsScopeBypassed(scope.AnalysisScopeIndex))
            {
                RenderRawRange(
                    renderer,
                    commands,
                    scope.BeginCommandIndex + 1,
                    scope.EndCommandIndex);
                renderer.EndBatch();
            }
            else
            {
                renderer.EndBatch();
                RenderTarget2D source =
                    GetExecutionSurface(frame, step);
                DrawPresentation(
                    renderer,
                    source,
                    parentTarget,
                    presentKernel,
                    new Rectangle(
                        0,
                        0,
                        parentTarget.Width,
                        parentTarget.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.NestedPresent,
                    node,
                    scope.AnalysisScopeIndex);
            }

            captureCommandIndices[parentScopeIndex] =
                scope.EndCommandIndex + 1;
        }
    }

    private void PresentCompletedRootScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node,
        Viewport hostViewport,
        ref int hostCommandIndex)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth != 0 ||
                scope.Output is not PrismGraphNodeId output ||
                output != node.Id)
            {
                continue;
            }

            renderer.EndBatch();
            renderer.RestoreHostTarget();
            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                scope.BeginCommandIndex);

            PrismKernel presentKernel =
                GetPresentKernel(scope, node);
            if (IsScopeBypassed(scope.AnalysisScopeIndex))
            {
                RenderRawRange(
                    renderer,
                    commands,
                    scope.BeginCommandIndex + 1,
                    scope.EndCommandIndex);
            }
            else
            {
                renderer.EndBatch();
                RenderTarget2D source =
                    GetExecutionSurface(frame, step);
                DrawPresentation(
                    renderer,
                    source,
                    target: null,
                    presentKernel,
                    new Rectangle(
                        hostViewport.X,
                        hostViewport.Y,
                        hostViewport.Width,
                        hostViewport.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.RootPresent,
                    node,
                    scope.AnalysisScopeIndex);
                renderer.BeginCommandBatch();
            }

            hostCommandIndex = scope.EndCommandIndex + 1;
            int nextRootBegin =
                FindNextRootBegin(graph, hostCommandIndex, commands.Count);
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                nextRootBegin);
            hostCommandIndex = nextRootBegin;
        }
    }

    private void DrawPresentation(
        IPrismCommandRenderer renderer,
        RenderTarget2D source,
        RenderTarget2D? target,
        PrismKernel kernel,
        Rectangle destination)
    {
        if (target is not null)
        {
            graphicsDevice.SetRenderTarget(target);
        }

        PrismKernelParameters parameters = new(
            source,
            1f,
            new Vector2(
                1f / source.Width,
                1f / source.Height),
            FullUvScale,
            ZeroUvOffset);
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.AlphaBlend);
        renderer.DrawFullscreen(source, destination);
        renderer.EndBatch();
    }

    private PrismKernel GetPresentKernel(
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        if (kernels.TryGetPresentKernel(
            scope.CompositionSettings.WorkingColorProfile,
            out PrismKernel kernel))
        {
            return kernel;
        }

        return RecordInvalidPresentProfile(scope, node);
    }

    private PrismKernel RecordInvalidPresentProfile(
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        RecordFallback(
            node,
            PrismFallbackReason.InvalidColorProfile,
            node.DiagnosticName);
        return kernels.Present;
    }

    private PrismFallbackAction RecordFallback(
        PrismGraphNode node,
        PrismFallbackReason reason,
        string detail)
    {
        PrismFallbackAction action = diagnostics.Record(
            node.Id,
            node.AnalysisScopeIndex,
            reason,
            detail);
        if (action == PrismFallbackAction.BypassComposition &&
            (uint)node.AnalysisScopeIndex <
                (uint)bypassedScopes.Length)
        {
            bypassedScopes[node.AnalysisScopeIndex] = true;
        }
        return action;
    }

    private static bool ResolveScopeUvMapping(
        PrismGraphScope scope,
        out Vector3 uvRowX,
        out Vector3 uvRowY)
    {
        DrawRect bounds = scope.ControlBounds;
        if (bounds.Width <= 0 ||
            bounds.Height <= 0 ||
            !System.Numerics.Matrix3x2.Invert(
                scope.EffectiveTransform,
                out System.Numerics.Matrix3x2 inverse))
        {
            uvRowX = Vector3.Zero;
            uvRowY = Vector3.Zero;
            return false;
        }

        float pixelScale = scope.PixelScale;
        uvRowX = new Vector3(
            inverse.M11 / (pixelScale * bounds.Width),
            inverse.M21 / (pixelScale * bounds.Width),
            (inverse.M31 - bounds.X) / bounds.Width);
        uvRowY = new Vector3(
            inverse.M12 / (pixelScale * bounds.Height),
            inverse.M22 / (pixelScale * bounds.Height),
            (inverse.M32 - bounds.Y) / bounds.Height);
        return true;
    }

    private bool IsScopeBypassed(int scopeIndex)
    {
        return (uint)scopeIndex < (uint)bypassedScopes.Length &&
            bypassedScopes[scopeIndex];
    }

    private RenderTarget2D GetExecutionSurface(
        PrismSurfaceFrame frame,
        int executionIndex)
    {
        PrismRetainedSurfaceLease lease =
            retainedLeases[executionIndex];
        return lease.IsActive
            ? lease.Surface
            : frame.GetSurface(executionIndex);
    }

    private static int FindInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target,
        PrismGraphEdgeKind kind)
    {
        for (int edgeIndex = 0;
            edgeIndex < graph.Edges.Length;
            edgeIndex++)
        {
            PrismGraphEdge edge = graph.Edges[edgeIndex];
            if (edge.Target == target && edge.Kind == kind)
            {
                return FindExecutionIndex(plan, edge.Source);
            }
        }

        return -1;
    }

    private static int FindAnyInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target)
    {
        for (int edgeIndex = 0;
            edgeIndex < graph.Edges.Length;
            edgeIndex++)
        {
            PrismGraphEdge edge = graph.Edges[edgeIndex];
            if (edge.Target == target)
            {
                return FindExecutionIndex(plan, edge.Source);
            }
        }

        return -1;
    }

    private static int FindExecutionIndex(
        PrismGraphExecutionPlan plan,
        PrismGraphNodeId nodeId)
    {
        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            if (plan.ExecutionOrder[index] == nodeId)
            {
                return index;
            }
        }

        throw new InvalidOperationException(
            $"Prism execution node '{nodeId}' is not in the optimized plan.");
    }

    private static PrismGraphScope FindScope(
        PrismGraph graph,
        int analysisScopeIndex)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            if (graph.Scopes[index].AnalysisScopeIndex ==
                analysisScopeIndex)
            {
                return graph.Scopes[index];
            }
        }

        throw new InvalidOperationException(
            $"Prism graph scope '{analysisScopeIndex}' does not exist.");
    }

    private static int FindNextRootBegin(
        PrismGraph graph,
        int commandIndex,
        int fallback)
    {
        int result = fallback;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth == 0 &&
                scope.BeginCommandIndex >= commandIndex)
            {
                result = Math.Min(result, scope.BeginCommandIndex);
            }
        }
        return result;
    }

    private static void RenderRawRange(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        int start,
        int end)
    {
        for (int index = start; index < end; index++)
        {
            DrawCommand command = commands[index];
            if (command.Kind is DrawCommandKind.BeginPrism or
                DrawCommandKind.EndPrism)
            {
                continue;
            }
            renderer.RenderCommand(command);
        }
    }

    private readonly record struct PrismMaskKernelSettings(
        float Channel,
        float Density,
        float Invert,
        Vector3 UvRowX,
        Vector3 UvRowY,
        Vector2 FeatherStep);

    private readonly record struct PrismBackdropCropKernelSettings(
        float AlphaMode,
        Vector3 UvRowX,
        Vector3 UvRowY);

    private readonly record struct PrismBackdropColorKernelSettings(
        PrismColorProfile SourceProfile,
        PrismColorProfile TargetProfile);

    private readonly record struct PrismStyleKernelSettings(
        Texture2D Texture,
        Texture2D MaskTexture,
        Vector4 Color,
        Vector4 SecondaryColor,
        Vector4 Geometry0,
        Vector4 Geometry1,
        Vector4 Options0,
        Vector4 Options1,
        Vector4 Modes0,
        Vector4 Modes1,
        Vector4 Modes2,
        Vector4 Modes3,
        Vector3 BoundsUvRowX,
        Vector3 BoundsUvRowY,
        bool ResourceAvailable);

    private readonly record struct PrismFilterKernelSettings(
        Texture2D Texture,
        Vector4 Header,
        Vector4 Options0,
        Vector4 Options1,
        Vector4 Options2,
        Vector4 Options3,
        Vector4 Options4,
        Vector4 Options5,
        Vector4 Options6,
        Vector4 Options7,
        Vector4 Options8,
        Vector4 Options9,
        Vector2 TextureSize,
        Texture2D? AuxiliaryTexture = null);
}
