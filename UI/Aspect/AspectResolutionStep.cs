namespace Cerneala.UI.Aspect;

public sealed record AspectResolutionStep
{
    public AspectResolutionStep(
        string packageName,
        string ruleName,
        string target,
        AspectLayer layer,
        AspectSpecificity specificity,
        int declarationOrder,
        int sourceOrder,
        AspectOrigin origin,
        string scope,
        IReadOnlyList<AspectConditionTrace> conditions,
        IReadOnlyList<AspectConditionDependency> dependencies,
        string outcome)
    {
        PackageName = packageName ?? string.Empty;
        RuleName = ruleName ?? string.Empty;
        Target = target ?? string.Empty;
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        Specificity = specificity;
        DeclarationOrder = declarationOrder;
        SourceOrder = sourceOrder;
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Scope = scope ?? string.Empty;
        Conditions = Array.AsReadOnly((conditions ?? throw new ArgumentNullException(nameof(conditions))).ToArray());
        Dependencies = Array.AsReadOnly((dependencies ?? throw new ArgumentNullException(nameof(dependencies))).ToArray());
        Outcome = outcome ?? string.Empty;
    }

    public string PackageName { get; init; }

    public string RuleName { get; init; }

    public string Target { get; init; }

    public AspectLayer Layer { get; init; }

    public AspectSpecificity Specificity { get; init; }

    public int DeclarationOrder { get; init; }

    public int SourceOrder { get; init; }

    public AspectOrigin Origin { get; init; }

    public string Scope { get; init; }

    public IReadOnlyList<AspectConditionTrace> Conditions { get; init; }

    public IReadOnlyList<AspectConditionDependency> Dependencies { get; init; }

    public string Outcome { get; init; }
}
