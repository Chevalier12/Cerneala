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
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        DiagnosticText = diagnosticText ?? string.Empty;
        Children = children ?? [];
    }

    public bool Matches { get; }

    public IReadOnlyList<AspectConditionDependency> Dependencies { get; }

    public string DiagnosticText { get; }

    public IReadOnlyList<AspectConditionResult> Children { get; }
}
