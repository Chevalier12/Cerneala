using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Graph;

public readonly record struct PrismGraphDiagnostic(
    string CompositionName,
    PrismNodeId? NodeId,
    string? NodeName,
    PrismSourceSpan? SourceSpan,
    string Message)
{
    public override string ToString()
    {
        string target = NodeId is null
            ? $"composition '{CompositionName}'"
            : $"node '{NodeName ?? "<unnamed>"}' (#{NodeId.Value.Value}) in composition '{CompositionName}'";
        string location = SourceSpan is null ? string.Empty : $" at {SourceSpan.Value}";
        return $"Cannot build Prism graph for {target}{location}: {Message}";
    }
}

public sealed class PrismGraphBuildException : InvalidOperationException
{
    public PrismGraphBuildException(
        PrismGraphDiagnostic diagnostic,
        Exception? innerException = null)
        : base(diagnostic.ToString(), innerException)
    {
        Diagnostic = diagnostic;
    }

    public PrismGraphDiagnostic Diagnostic { get; }
}
