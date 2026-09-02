namespace Cerneala.UI.Aspect;

public sealed class AspectConditionResult
{
    public AspectConditionResult(
        bool matches,
        IReadOnlyList<AspectConditionDependency> dependencies,
        string diagnosticText,
        IReadOnlyList<AspectConditionResult>? children = null)
    {
        Matches = matches;
        Dependencies = Array.AsReadOnly((dependencies ?? throw new ArgumentNullException(nameof(dependencies))).ToArray());
        DiagnosticText = diagnosticText ?? string.Empty;
        Children = Array.AsReadOnly((children ?? []).ToArray());
    }

    public bool Matches { get; }

    public IReadOnlyList<AspectConditionDependency> Dependencies { get; }

    public string DiagnosticText { get; }

    public IReadOnlyList<AspectConditionResult> Children { get; }
}
