using System.Globalization;
using System.Numerics;
using System.Text;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal readonly record struct PrismExecutionDiagnostic(
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

internal readonly record struct PrismExecutionPass(
    PrismExecutionPassKind Kind,
    PrismGraphNodeId NodeId,
    int ScopeIndex);

internal readonly record struct PrismExecutionCounters(
    int PassCount,
    int CaptureCount,
    long CreatedSurfaceCount,
    long ReusedSurfaceCount,
    int PeakLiveSurfaceCount,
    int FallbackCount,
    TimeSpan CpuSubmitTime);

internal sealed class PrismExecutionDiagnostics
{
    private PrismExecutionDiagnostic[] entries =
        new PrismExecutionDiagnostic[16];
    private PrismExecutionPass[] passes =
        new PrismExecutionPass[32];
    private PrismFrameAnalysis? frameAnalysis;
    private PrismGraphExecutionPlan? executionPlan;
    private int passCount;
    private int captureCount;
    private int peakLiveSurfaceCount;
    private long createdSurfaceCount;
    private long reusedSurfaceCount;
    private TimeSpan cpuSubmitTime;

    public int Count { get; private set; }

    public PrismExecutionCounters Counters =>
        new(
            passCount,
            captureCount,
            createdSurfaceCount,
            reusedSurfaceCount,
            peakLiveSurfaceCount,
            Count,
            cpuSubmitTime);

    public void BeginFrame()
    {
        Count = 0;
        passCount = 0;
        captureCount = 0;
        peakLiveSurfaceCount = 0;
        createdSurfaceCount = 0;
        reusedSurfaceCount = 0;
        cpuSubmitTime = TimeSpan.Zero;
        frameAnalysis = null;
        executionPlan = null;
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
        frameAnalysis = analysis;
        executionPlan = plan;
        if (passes.Length < expectedPassCount)
        {
            Array.Resize(ref passes, expectedPassCount);
        }
    }

    public void RecordGraphPass(PrismGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        RecordPass(
            PrismExecutionPassKind.GraphNode,
            node.Id,
            node.AnalysisScopeIndex);
        if (node.Kind == PrismGraphNodeKind.ControlCapture)
        {
            captureCount++;
        }
    }

    public void RecordPresentation(
        PrismExecutionPassKind kind,
        PrismGraphNodeId nodeId,
        int scopeIndex)
    {
        if (kind is not PrismExecutionPassKind.NestedPresent and
            not PrismExecutionPassKind.RootPresent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "A presentation pass must be nested or root.");
        }

        RecordPass(kind, nodeId, scopeIndex);
    }

    public void ObserveLiveSurfaces(int liveSurfaceCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(liveSurfaceCount);
        peakLiveSurfaceCount = Math.Max(
            peakLiveSurfaceCount,
            liveSurfaceCount);
    }

    public void CompleteExecution(
        long createdSurfaces,
        long reusedSurfaces,
        TimeSpan submitTime)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(createdSurfaces);
        ArgumentOutOfRangeException.ThrowIfNegative(reusedSurfaces);
        if (submitTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(submitTime));
        }

        createdSurfaceCount = createdSurfaces;
        reusedSurfaceCount = reusedSurfaces;
        cpuSubmitTime = submitTime;
    }

    public PrismExecutionDiagnostic Get(int index)
    {
        if ((uint)index >= (uint)Count)
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
        if (Count == entries.Length)
        {
            Array.Resize(ref entries, checked(entries.Length * 2));
        }

        PrismFallbackAction action =
            PrismFallbackPolicy.Resolve(reason);
        entries[Count++] = new PrismExecutionDiagnostic(
            nodeId,
            scopeIndex,
            reason,
            action,
            detail);
        return action;
    }

    public string DumpExecutedGraph()
    {
        PrismFrameAnalysis analysis = frameAnalysis ??
            throw new InvalidOperationException(
                "No correlated Prism execution is available.");
        PrismGraphExecutionPlan plan = executionPlan ??
            throw new InvalidOperationException(
                "No correlated Prism execution plan is available.");
        analysis.EnsureCurrent();

        PrismGraph graph = plan.OptimizedGraph;
        StringBuilder builder = new();
        builder.AppendLine("prism-execution v1");
        builder.Append("analysis version=")
            .Append(analysis.CommandListVersion)
            .Append(" scopes=")
            .Append(analysis.Scopes.Length)
            .Append(" required-surfaces=")
            .Append(analysis.RequiredSurfaceCount)
            .AppendLine();

        for (int index = 0; index < analysis.Scopes.Length; index++)
        {
            PrismAnalyzedScope analyzedScope = analysis.Scopes[index];
            PrismGraphScope graphScope = FindScope(
                graph,
                analyzedScope.ScopeIndex);
            ValidateCorrelation(analyzedScope, graphScope);

            builder.Append("scope ")
                .Append(analyzedScope.ScopeIndex)
                .Append(" commands=")
                .Append(analyzedScope.BeginCommandIndex)
                .Append(':')
                .Append(analyzedScope.EndCommandIndex)
                .Append(" depth=")
                .Append(analyzedScope.Depth)
                .Append(" parent=");
            AppendNullableInt(
                builder,
                analyzedScope.ParentScopeIndex);
            builder.Append(" owner=")
                .Append(graphScope.CacheOwnerToken.Value)
                .Append(" bounds=");
            AppendRect(builder, graphScope.Bounds);
            builder.Append(" transform=");
            AppendMatrix(builder, graphScope.EffectiveTransform);
            builder.Append(" output=")
                .Append(graphScope.Output?.ToString() ?? "<none>")
                .AppendLine();
        }

        for (int index = 0; index < passCount; index++)
        {
            PrismExecutionPass pass = passes[index];
            PrismGraphNode node = graph.GetNode(pass.NodeId);
            builder.Append("pass ")
                .Append(index)
                .Append(' ')
                .Append(pass.Kind)
                .Append(" scope=")
                .Append(pass.ScopeIndex)
                .Append(" node=")
                .Append(pass.NodeId)
                .Append(" kind=")
                .Append(node.Kind)
                .Append(" name=");
            AppendEscaped(builder, node.DiagnosticName);
            builder.AppendLine();
        }

        for (int index = 0; index < Count; index++)
        {
            PrismExecutionDiagnostic diagnostic = entries[index];
            builder.Append("fallback ")
                .Append(index)
                .Append(" scope=")
                .Append(diagnostic.ScopeIndex)
                .Append(" node=")
                .Append(diagnostic.NodeId?.ToString() ?? "<none>")
                .Append(" reason=")
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
        PrismGraphNodeId nodeId,
        int scopeIndex)
    {
        if (passCount == passes.Length)
        {
            Array.Resize(
                ref passes,
                checked(Math.Max(1, passes.Length * 2)));
        }

        passes[passCount++] =
            new PrismExecutionPass(kind, nodeId, scopeIndex);
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
            builder.Append(
                character switch
                {
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\\' => "\\\\",
                    _ => character.ToString()
                });
        }
    }
}
