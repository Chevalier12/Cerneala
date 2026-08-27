namespace Cerneala.UI.Aspect;

public sealed class AspectConditionTrace
{
    public AspectConditionTrace(
        bool matches,
        string diagnosticText,
        IReadOnlyList<AspectConditionDependency> dependencies,
        IReadOnlyList<AspectConditionTrace>? children = null)
    {
        Matches = matches;
        DiagnosticText = diagnosticText ?? string.Empty;
        Dependencies = Array.AsReadOnly((dependencies ?? throw new ArgumentNullException(nameof(dependencies))).ToArray());
        Children = Array.AsReadOnly((children ?? []).ToArray());
    }

    public bool Matches { get; }

    public string DiagnosticText { get; }

    public IReadOnlyList<AspectConditionDependency> Dependencies { get; }

    public IReadOnlyList<AspectConditionTrace> Children { get; }

    internal static AspectConditionTrace FromResult(AspectConditionResult result)
    {
        return new AspectConditionTrace(
            result.Matches,
            result.DiagnosticText,
            result.Dependencies,
            result.Children.Select(FromResult).ToArray());
    }
}
