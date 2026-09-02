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
        ArgumentNullException.ThrowIfNull(values);
        Values = new System.Collections.ObjectModel.ReadOnlyDictionary<UiProperty, ResolvedAspectValue>(
            new Dictionary<UiProperty, ResolvedAspectValue>(values, ReferenceEqualityComparer.Instance));
        MatchedRules = Array.AsReadOnly((matchedRules ?? throw new ArgumentNullException(nameof(matchedRules))).ToArray());
        RejectedDeclarations = Array.AsReadOnly((rejectedDeclarations ?? throw new ArgumentNullException(nameof(rejectedDeclarations))).ToArray());
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    public IReadOnlyDictionary<UiProperty, ResolvedAspectValue> Values { get; }

    public IReadOnlyList<AspectRuleSet> MatchedRules { get; }

    public IReadOnlyList<RejectedAspectDeclaration> RejectedDeclarations { get; }

    public AspectDependencySet Dependencies { get; }
}
