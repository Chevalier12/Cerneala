using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGraphExecutionCache
{
    private readonly PrismSurfacePool surfacePool;
    private readonly PrismRetainedSurfaceCache retainedSurfaceCache;
    private readonly bool retainedCacheEnabled;
    private readonly bool developmentDiagnosticsEnabled;
    private readonly PrismColorProfile hostColorProfile;
    private readonly long[] missCounts =
        new long[(int)PrismCacheMissReason.Disabled + 1];
    private readonly Dictionary<
        PrismCacheOwnerToken,
        PrismRetainedCacheKey> ownerFinalKeys = [];
    private bool[] requiredNodes = [];
    private bool[] requiredTransientSurfaces = [];
    private bool[] cacheResultValid = [];
    private bool[] retainedKeyAvailable = [];
    private PrismRetainedCacheKey[] retainedKeys = [];
    private PrismRetainedSurfaceLease[] retainedLeases = [];
    private int[] requiredTraversal = [];
    private int[] promotionHeads = [];
    private int[] promotionNext = [];
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

    internal PrismGraphExecutionCache(
        PrismSurfacePool surfacePool,
        PrismRetainedSurfaceCache retainedSurfaceCache,
        bool retainedCacheEnabled,
        bool developmentDiagnosticsEnabled,
        PrismColorProfile hostColorProfile)
    {
        this.surfacePool = surfacePool;
        this.retainedSurfaceCache = retainedSurfaceCache;
        this.retainedCacheEnabled = retainedCacheEnabled;
        this.developmentDiagnosticsEnabled =
            developmentDiagnosticsEnabled;
        this.hostColorProfile = hostColorProfile;
    }

    internal bool[] RequiredNodes => requiredNodes;

    internal bool[] RequiredTransientSurfaces =>
        requiredTransientSurfaces;

    internal bool[] CacheResultValid => cacheResultValid;

    internal PrismRendererDiagnostics RendererDiagnostics =>
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

    internal void Prepare(
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        Viewport hostViewport)
    {
        int nodeCount = plan.ExecutionOrder.Length;
        EnsureCacheBuffers(nodeCount);
        Array.Clear(requiredNodes, 0, nodeCount);
        Array.Clear(requiredTransientSurfaces, 0, nodeCount);
        Array.Clear(cacheResultValid, 0, nodeCount);
        Array.Clear(retainedKeyAvailable, 0, nodeCount);
        Array.Fill(promotionHeads, -1, 0, nodeCount);
        Array.Fill(promotionNext, -1, 0, nodeCount);
        if (developmentDiagnosticsEnabled)
        {
            lastDependencyChange = PrismDependencyChange.None;
        }

        PrismRetainedRasterContext rasterContext = new(
            hostViewport.Width,
            hostViewport.Height,
            hostColorProfile,
            BackdropPixelFormat.Rgba16Float,
            PrismSampling.Linear,
            analysis.RequiredCapabilities,
            PrismKernelRegistry.ShaderPackageVersion);
        EnsureRasterContext(rasterContext);
        for (int index = 0; index < nodeCount; index++)
        {
            PrismGraphNode node =
                graph.GetNode(plan.ExecutionOrder[index]);
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

    internal void Reset()
    {
        surfacePool.Reset();
        ownerFinalKeys.Clear();
        hasLastRasterContext = false;
        pendingMissReason = PrismCacheMissReason.Invalidated;
    }

    internal void Invalidate(PrismCacheInvalidation invalidation)
    {
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

    internal void InvalidateAll()
    {
        retainedSurfaceCache.Clear(
            PrismCacheEvictionReason.Invalidation);
        ownerFinalKeys.Clear();
        hasLastRasterContext = false;
        pendingMissReason = PrismCacheMissReason.Invalidated;
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

    internal void InvalidateGraphOwners(
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

    internal void AcquireRetainedHits(
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

    internal void AcquireRetainedHits(
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

    internal bool AreCacheInputsValid(
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

    internal void PromoteCompletedResults(
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

    internal void ReleaseRetainedLeases()
    {
        for (int index = 0;
            index < retainedLeases.Length;
            index++)
        {
            retainedLeases[index]?.Dispose();
        }
    }

    internal RenderTarget2D GetExecutionSurface(
        PrismSurfaceFrame frame,
        int executionIndex)
    {
        PrismRetainedSurfaceLease lease =
            retainedLeases[executionIndex];
        return lease.IsActive
            ? lease.Surface
            : frame.GetSurface(executionIndex);
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
}
