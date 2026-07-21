using System.Globalization;
using System.Numerics;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal enum PrismExecutionDiagnosticStage
{
    CapabilityCheck,
    KernelLookup,
    BackdropAcquisition,
    ResourceResolution,
    ColorProfile,
    SurfaceBudget,
    ShaderLoad
}

internal readonly record struct PrismExecutionDiagnostic(
    string Code,
    PrismExecutionDiagnosticStage Stage,
    PrismGraphNodeId? NodeId,
    int ScopeIndex,
    PrismFallbackReason Reason,
    PrismFallbackAction Action,
    string Detail);

internal enum PrismExecutionPassKind
{
    GraphNode,
    NestedPresent,
    RootPresent
}

internal readonly record struct PrismExecutionCounters(
    int ActiveCompositionCount,
    int PlannedPassCount,
    int PassCount,
    int CaptureCount,
    int ActiveSurfaceCount,
    long CreatedSurfaceCount,
    long ReusedSurfaceCount,
    int PeakLiveSurfaceCount,
    long SurfaceByteCount,
    long PeakSurfaceByteCount,
    int FallbackCount,
    TimeSpan CpuSubmitTime);

internal sealed class PrismExecutionDiagnostics
{
    private readonly bool detailedDiagnosticsEnabled;
    private PrismExecutionDiagnostic[] entries = [];
    private PrismExecutionPass[] passes = [];
    private PrismExecutionScope[] scopes = [];
    private PrismGraphNodeId[] graphNodeIds = [];
    private int detailedEntryCount;
    private int detailedPassCount;
    private int scopeCount;
    private int graphNodeCount;
    private int activeCompositionCount;
    private int plannedPassCount;
    private int passCount;
    private int captureCount;
    private int activeSurfaceCount;
    private int peakLiveSurfaceCount;
    private int fallbackCount;
    private long createdSurfaceCount;
    private long reusedSurfaceCount;
    private long surfaceByteCount;
    private long peakSurfaceByteCount;
    private long commandListVersion;
    private int requiredSurfaceCount;
    private TimeSpan cpuSubmitTime;
    private bool hasCapturedExecution;
    private PrismExecutionDiagnostic? lastFallback;

    public PrismExecutionDiagnostics(
        bool detailedDiagnosticsEnabled = true)
    {
        this.detailedDiagnosticsEnabled =
            detailedDiagnosticsEnabled;
    }

    public bool DetailedDiagnosticsEnabled =>
        detailedDiagnosticsEnabled;

    public bool HasCapturedExecution =>
        hasCapturedExecution;

    public int Count => fallbackCount;

    public int DetailedCount => detailedEntryCount;

    public PrismExecutionDiagnostic? LastFallback =>
        lastFallback;

    public PrismExecutionCounters Counters =>
        new(
            activeCompositionCount,
            plannedPassCount,
            passCount,
            captureCount,
            activeSurfaceCount,
            createdSurfaceCount,
            reusedSurfaceCount,
            peakLiveSurfaceCount,
            surfaceByteCount,
            peakSurfaceByteCount,
            fallbackCount,
            cpuSubmitTime);

    public void BeginFrame()
    {
        if (detailedEntryCount > 0)
        {
            Array.Clear(entries, 0, detailedEntryCount);
        }
        if (detailedPassCount > 0)
        {
            Array.Clear(passes, 0, detailedPassCount);
        }
        if (scopeCount > 0)
        {
            Array.Clear(scopes, 0, scopeCount);
        }

        detailedEntryCount = 0;
        detailedPassCount = 0;
        scopeCount = 0;
        graphNodeCount = 0;
        activeCompositionCount = 0;
        plannedPassCount = 0;
        passCount = 0;
        captureCount = 0;
        activeSurfaceCount = 0;
        peakLiveSurfaceCount = 0;
        fallbackCount = 0;
        createdSurfaceCount = 0;
        reusedSurfaceCount = 0;
        surfaceByteCount = 0;
        peakSurfaceByteCount = 0;
        commandListVersion = 0;
        requiredSurfaceCount = 0;
        cpuSubmitTime = TimeSpan.Zero;
        hasCapturedExecution = false;
    }

    public void BeginExecution(
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        int expectedPassCount)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedPassCount);

        BeginFrame();
        activeCompositionCount = analysis.Scopes.Length;
        plannedPassCount = expectedPassCount;
        commandListVersion = analysis.CommandListVersion;
        requiredSurfaceCount = analysis.RequiredSurfaceCount;
        if (!detailedDiagnosticsEnabled)
        {
            return;
        }

        PrismGraph graph = plan.OptimizedGraph;
        EnsureGraphNodeCapacity(graph.Nodes.Length);
        for (int index = 0; index < graph.Nodes.Length; index++)
        {
            graphNodeIds[index] = graph.Nodes[index].Id;
        }
        graphNodeCount = graph.Nodes.Length;
        EnsureScopeCapacity(analysis.Scopes.Length);
        EnsurePassCapacity(expectedPassCount);
        for (int index = 0; index < analysis.Scopes.Length; index++)
        {
            PrismAnalyzedScope analyzedScope = analysis.Scopes[index];
            PrismGraphScope graphScope = FindScope(
                graph,
                analyzedScope.ScopeIndex);
            ValidateCorrelation(analyzedScope, graphScope);
            scopes[index] = new PrismExecutionScope(
                analyzedScope.ScopeIndex,
                analyzedScope.BeginCommandIndex,
                analyzedScope.EndCommandIndex,
                analyzedScope.Depth,
                analyzedScope.ParentScopeIndex,
                analyzedScope.Scope.Definition.Name,
                analyzedScope.RequiredCapabilities,
                graphScope.Bounds,
                graphScope.EffectiveTransform,
                graphScope.Output is PrismGraphNodeId output
                    ? FindCapturedNodeIndex(output)
                    : null);
        }

        scopeCount = analysis.Scopes.Length;
        hasCapturedExecution = true;
    }

    public void RecordGraphPass(PrismGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        RecordPass(
            PrismExecutionPassKind.GraphNode,
            node,
            node.AnalysisScopeIndex);
        if (node.Kind == PrismGraphNodeKind.ControlCapture)
        {
            captureCount++;
        }
    }

    public void RecordPresentation(
        PrismExecutionPassKind kind,
        PrismGraphNode node,
        int scopeIndex)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (kind is not PrismExecutionPassKind.NestedPresent and
            not PrismExecutionPassKind.RootPresent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "A presentation pass must be nested or root.");
        }

        RecordPass(kind, node, scopeIndex);
    }

    public void ObserveLiveSurfaces(int liveSurfaceCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(liveSurfaceCount);
        activeSurfaceCount = liveSurfaceCount;
        peakLiveSurfaceCount = Math.Max(
            peakLiveSurfaceCount,
            liveSurfaceCount);
    }

    public void CompleteExecution(
        long createdSurfaces,
        long reusedSurfaces,
        int activeSurfaces,
        long surfaceBytes,
        long peakSurfaceBytes,
        TimeSpan submitTime)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(createdSurfaces);
        ArgumentOutOfRangeException.ThrowIfNegative(reusedSurfaces);
        ArgumentOutOfRangeException.ThrowIfNegative(activeSurfaces);
        ArgumentOutOfRangeException.ThrowIfNegative(surfaceBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(peakSurfaceBytes);
        if (peakSurfaceBytes < surfaceBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(peakSurfaceBytes));
        }
        if (submitTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(submitTime));
        }

        createdSurfaceCount = createdSurfaces;
        reusedSurfaceCount = reusedSurfaces;
        activeSurfaceCount = activeSurfaces;
        surfaceByteCount = surfaceBytes;
        peakSurfaceByteCount = peakSurfaceBytes;
        cpuSubmitTime = submitTime;
    }

    public PrismExecutionDiagnostic Get(int index)
    {
        if ((uint)index >= (uint)detailedEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return entries[index];
    }

    public PrismFallbackAction Record(
        PrismGraphNodeId? nodeId,
        int scopeIndex,
        PrismFallbackReason reason,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        PrismFallbackAction action =
            PrismFallbackPolicy.Resolve(reason);
        fallbackCount++;
        if (!detailedDiagnosticsEnabled)
        {
            return action;
        }

        PrismExecutionDiagnostic diagnostic = new(
            CodeFor(reason),
            StageFor(reason),
            nodeId,
            scopeIndex,
            reason,
            action,
            RedactUnstableGpuIdentifiers(detail));
        EnsureEntryCapacity(detailedEntryCount + 1);
        entries[detailedEntryCount++] = diagnostic;
        lastFallback = diagnostic;
        return action;
    }

    public string DumpExecutedGraph()
    {
        if (!detailedDiagnosticsEnabled)
        {
            throw new InvalidOperationException(
                "Prism development diagnostics are disabled.");
        }
        if (!hasCapturedExecution)
        {
            throw new InvalidOperationException(
                "No correlated Prism execution is available.");
        }

        StringBuilder builder = new();
        builder.AppendLine("prism-execution v2 runtime-identifiers=redacted");
        builder.Append("analysis version=")
            .Append(commandListVersion)
            .Append(" scopes=")
            .Append(scopeCount)
            .Append(" required-surfaces=")
            .Append(requiredSurfaceCount)
            .Append(" planned-passes=")
            .Append(plannedPassCount)
            .AppendLine();

        for (int index = 0; index < scopeCount; index++)
        {
            PrismExecutionScope scope = scopes[index];
            builder.Append("scope ")
                .Append(scope.ScopeIndex)
                .Append(" commands=")
                .Append(scope.BeginCommandIndex)
                .Append(':')
                .Append(scope.EndCommandIndex)
                .Append(" depth=")
                .Append(scope.Depth)
                .Append(" parent=");
            AppendNullableInt(builder, scope.ParentScopeIndex);
            builder.Append(" owner=scope-")
                .Append(scope.ScopeIndex)
                .Append(" composition=");
            AppendEscaped(builder, scope.CompositionName);
            builder.Append(" capabilities=")
                .Append(scope.RequiredCapabilities)
                .Append(" bounds=");
            AppendRect(builder, scope.Bounds);
            builder.Append(" transform=");
            AppendMatrix(builder, scope.EffectiveTransform);
            builder.Append(" output=")
                .Append(
                    scope.OutputNodeIndex is int outputNodeIndex
                        ? $"node-{outputNodeIndex}"
                        : "<none>")
                .AppendLine();
        }

        for (int index = 0; index < detailedPassCount; index++)
        {
            PrismExecutionPass pass = passes[index];
            builder.Append("pass ")
                .Append(index)
                .Append(' ')
                .Append(pass.Kind)
                .Append(" scope=")
                .Append(pass.ScopeIndex)
                .Append(" node=")
                .Append("node-")
                .Append(pass.NodeIndex)
                .Append(" kind=")
                .Append(pass.NodeKind)
                .Append(" name=");
            AppendEscaped(builder, pass.DiagnosticName);
            builder.AppendLine();
        }

        for (int index = 0; index < detailedEntryCount; index++)
        {
            PrismExecutionDiagnostic diagnostic = entries[index];
            builder.Append("fallback ")
                .Append(index)
                .Append(" code=")
                .Append(diagnostic.Code)
                .Append(" stage=")
                .Append(diagnostic.Stage)
                .Append(" scope=")
                .Append(diagnostic.ScopeIndex)
                .Append(" node=");
            AppendDiagnosticNode(builder, diagnostic.NodeId);
            builder.Append(" reason=")
                .Append(diagnostic.Reason)
                .Append(" action=")
                .Append(diagnostic.Action)
                .Append(" detail=");
            AppendEscaped(builder, diagnostic.Detail);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private void RecordPass(
        PrismExecutionPassKind kind,
        PrismGraphNode node,
        int scopeIndex)
    {
        passCount++;
        if (!detailedDiagnosticsEnabled)
        {
            return;
        }

        EnsurePassCapacity(detailedPassCount + 1);
        passes[detailedPassCount++] = new PrismExecutionPass(
            kind,
            FindCapturedNodeIndex(node.Id),
            scopeIndex,
            node.Kind,
            node.DiagnosticName);
    }

    private void EnsureEntryCapacity(int requiredCapacity)
    {
        if (entries.Length >= requiredCapacity)
        {
            return;
        }

        int capacity = Math.Max(
            requiredCapacity,
            Math.Max(16, checked(entries.Length * 2)));
        Array.Resize(ref entries, capacity);
    }

    private void EnsurePassCapacity(int requiredCapacity)
    {
        if (passes.Length >= requiredCapacity)
        {
            return;
        }

        int capacity = Math.Max(
            requiredCapacity,
            Math.Max(32, checked(passes.Length * 2)));
        Array.Resize(ref passes, capacity);
    }

    private void EnsureScopeCapacity(int requiredCapacity)
    {
        if (scopes.Length < requiredCapacity)
        {
            Array.Resize(ref scopes, requiredCapacity);
        }
    }

    private void EnsureGraphNodeCapacity(int requiredCapacity)
    {
        if (graphNodeIds.Length < requiredCapacity)
        {
            Array.Resize(ref graphNodeIds, requiredCapacity);
        }
    }

    private int FindCapturedNodeIndex(PrismGraphNodeId nodeId)
    {
        for (int index = 0; index < graphNodeCount; index++)
        {
            if (graphNodeIds[index] == nodeId)
            {
                return index;
            }
        }

        throw new InvalidOperationException(
            $"Executed Prism node '{nodeId}' is not part of the captured graph.");
    }

    private void AppendDiagnosticNode(
        StringBuilder builder,
        PrismGraphNodeId? nodeId)
    {
        if (nodeId is null)
        {
            builder.Append("<none>");
            return;
        }

        for (int index = 0; index < graphNodeCount; index++)
        {
            if (graphNodeIds[index] == nodeId.Value)
            {
                builder.Append("node-").Append(index);
                return;
            }
        }

        builder.Append("<runtime-node-redacted>");
    }

    private static PrismGraphScope FindScope(
        PrismGraph graph,
        int scopeIndex)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            if (graph.Scopes[index].AnalysisScopeIndex == scopeIndex)
            {
                return graph.Scopes[index];
            }
        }

        throw new InvalidOperationException(
            $"Executed Prism graph has no scope {scopeIndex}.");
    }

    private static void ValidateCorrelation(
        PrismAnalyzedScope analysis,
        PrismGraphScope graph)
    {
        if (analysis.BeginCommandIndex != graph.BeginCommandIndex ||
            analysis.EndCommandIndex != graph.EndCommandIndex ||
            analysis.Depth != graph.Depth ||
            analysis.ParentScopeIndex != graph.ParentScopeIndex ||
            analysis.DependencyStamp.CacheOwnerToken !=
                graph.CacheOwnerToken)
        {
            throw new InvalidOperationException(
                $"Prism graph scope {analysis.ScopeIndex} does not correlate with its frame analysis.");
        }
    }

    private static string CodeFor(PrismFallbackReason reason) =>
        reason switch
        {
            PrismFallbackReason.UnsupportedCapability => "PRISM7001",
            PrismFallbackReason.MissingKernel => "PRISM7002",
            PrismFallbackReason.MissingBackdrop => "PRISM7003",
            PrismFallbackReason.MissingResource => "PRISM7004",
            PrismFallbackReason.InvalidColorProfile => "PRISM7005",
            PrismFallbackReason.SurfaceAllocationFailed => "PRISM7006",
            PrismFallbackReason.ShaderUnavailable => "PRISM7007",
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unknown Prism fallback reason.")
        };

    private static PrismExecutionDiagnosticStage StageFor(
        PrismFallbackReason reason) =>
        reason switch
        {
            PrismFallbackReason.UnsupportedCapability =>
                PrismExecutionDiagnosticStage.CapabilityCheck,
            PrismFallbackReason.MissingKernel =>
                PrismExecutionDiagnosticStage.KernelLookup,
            PrismFallbackReason.MissingBackdrop =>
                PrismExecutionDiagnosticStage.BackdropAcquisition,
            PrismFallbackReason.MissingResource =>
                PrismExecutionDiagnosticStage.ResourceResolution,
            PrismFallbackReason.InvalidColorProfile =>
                PrismExecutionDiagnosticStage.ColorProfile,
            PrismFallbackReason.SurfaceAllocationFailed =>
                PrismExecutionDiagnosticStage.SurfaceBudget,
            PrismFallbackReason.ShaderUnavailable =>
                PrismExecutionDiagnosticStage.ShaderLoad,
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unknown Prism fallback reason.")
        };

    private static void AppendNullableInt(
        StringBuilder builder,
        int? value)
    {
        if (value is int integer)
        {
            builder.Append(integer);
        }
        else
        {
            builder.Append("<none>");
        }
    }

    private static void AppendRect(
        StringBuilder builder,
        DrawRect rect)
    {
        builder.Append('[');
        AppendFloat(builder, rect.X);
        builder.Append(',');
        AppendFloat(builder, rect.Y);
        builder.Append(',');
        AppendFloat(builder, rect.Width);
        builder.Append(',');
        AppendFloat(builder, rect.Height);
        builder.Append(']');
    }

    private static void AppendMatrix(
        StringBuilder builder,
        Matrix3x2 matrix)
    {
        builder.Append('[');
        AppendFloat(builder, matrix.M11);
        builder.Append(',');
        AppendFloat(builder, matrix.M12);
        builder.Append(',');
        AppendFloat(builder, matrix.M21);
        builder.Append(',');
        AppendFloat(builder, matrix.M22);
        builder.Append(',');
        AppendFloat(builder, matrix.M31);
        builder.Append(',');
        AppendFloat(builder, matrix.M32);
        builder.Append(']');
    }

    private static void AppendFloat(
        StringBuilder builder,
        float value)
    {
        builder.Append(
            value.ToString(
                "R",
                CultureInfo.InvariantCulture));
    }

    private static void AppendEscaped(
        StringBuilder builder,
        string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            switch (character)
            {
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
    }

    private static string RedactUnstableGpuIdentifiers(string value)
    {
        int match = FindHexIdentifier(value, 0, out int matchEnd);
        if (match < 0)
        {
            return value;
        }

        StringBuilder builder = new(value.Length);
        int copiedThrough = 0;
        while (match >= 0)
        {
            builder.Append(value, copiedThrough, match - copiedThrough);
            builder.Append("<gpu-id>");
            copiedThrough = matchEnd;
            match = FindHexIdentifier(
                value,
                copiedThrough,
                out matchEnd);
        }
        builder.Append(value, copiedThrough, value.Length - copiedThrough);
        return builder.ToString();
    }

    private static int FindHexIdentifier(
        string value,
        int startIndex,
        out int endIndex)
    {
        for (int index = startIndex;
            index + 2 < value.Length;
            index++)
        {
            if (value[index] != '0' ||
                value[index + 1] is not ('x' or 'X'))
            {
                continue;
            }

            int current = index + 2;
            while (current < value.Length &&
                Uri.IsHexDigit(value[current]))
            {
                current++;
            }
            if (current - index >= 6)
            {
                endIndex = current;
                return index;
            }
        }

        endIndex = -1;
        return -1;
    }

    private readonly record struct PrismExecutionPass(
        PrismExecutionPassKind Kind,
        int NodeIndex,
        int ScopeIndex,
        PrismGraphNodeKind NodeKind,
        string DiagnosticName);

    private readonly record struct PrismExecutionScope(
        int ScopeIndex,
        int BeginCommandIndex,
        int EndCommandIndex,
        int Depth,
        int? ParentScopeIndex,
        string CompositionName,
        PrismGraphCapabilities RequiredCapabilities,
        DrawRect Bounds,
        Matrix3x2 EffectiveTransform,
        int? OutputNodeIndex);
}
