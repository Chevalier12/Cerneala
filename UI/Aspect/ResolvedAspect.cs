using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class ResolvedAspect
{
    public ResolvedAspect(
        IReadOnlyDictionary<UiProperty, ResolvedAspectValue> values,
        IReadOnlyList<AspectRuleSet> matchedRules,
        IReadOnlyList<RejectedAspectDeclaration> rejectedDeclarations,
        AspectDependencySet dependencies)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        MatchedRules = matchedRules ?? throw new ArgumentNullException(nameof(matchedRules));
        RejectedDeclarations = rejectedDeclarations ?? throw new ArgumentNullException(nameof(rejectedDeclarations));
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    public IReadOnlyDictionary<UiProperty, ResolvedAspectValue> Values { get; }

    public IReadOnlyList<AspectRuleSet> MatchedRules { get; }

    public IReadOnlyList<RejectedAspectDeclaration> RejectedDeclarations { get; }

    public AspectDependencySet Dependencies { get; }
}
