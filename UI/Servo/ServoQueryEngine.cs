using System.Diagnostics.CodeAnalysis;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class ServoQueryEngine
{
    private readonly SemanticsProvider semanticsProvider = new();

    internal ServoElement Find(UIRoot root, ServoTarget target)
    {
        return CreateSnapshot(Resolve(root, target));
    }

    internal ServoResolvedElement Resolve(UIRoot root, ServoTarget target)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(target);
        SemanticsTree tree = semanticsProvider.Build(root, SemanticsProjection.Servo);
        IReadOnlyList<ServoResolvedElement> matches = ResolveAll(root, tree, target);
        if (matches.Count == 0)
        {
            throw new ServoTargetNotFoundException(DescribeNotFound(root, tree, target));
        }

        if (matches.Count > 1)
        {
            throw CreateAmbiguousException(target, matches.Count);
        }

        return matches[0];
    }

    private static ServoTargetAmbiguousException CreateAmbiguousException(ServoTarget target, int matchCount) =>
        new($"The Servo target {target} matched {matchCount} elements; exactly one was required.");

    internal IReadOnlyList<ServoElement> FindAll(UIRoot root, ServoTarget target)
    {
        ServoElement[] snapshots = ResolveAll(root, target)
            .Select(CreateSnapshot)
            .ToArray();
        return Array.AsReadOnly(snapshots);
    }

    internal bool Exists(UIRoot root, ServoTarget target)
    {
        return ResolveAll(root, target).Count > 0;
    }

    internal IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, ServoTarget target)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(target);
        SemanticsTree tree = semanticsProvider.Build(root, SemanticsProjection.Servo);
        return ResolveAll(root, tree, target);
    }

    private static IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, SemanticsTree tree, ServoTarget target)
    {
        HashSet<UIElement>? scopeElements = ResolveScopeElements(root, tree, target);
        List<ServoResolvedElement> matches = [];
        CollectMatches(root, tree.Root, target, scopeElements, insideScope: scopeElements is null, matches);
        return SelectPosition(matches, target.Position);
    }

    private static HashSet<UIElement>? ResolveScopeElements(UIRoot root, SemanticsTree tree, ServoTarget target)
    {
        if (target.Scope is null)
        {
            return null;
        }

        HashSet<UIElement> scopeElements = new(ReferenceEqualityComparer.Instance);
        foreach (ServoResolvedElement scopeMatch in ResolveAll(root, tree, target.Scope))
        {
            scopeElements.Add(scopeMatch.Element);
        }

        return scopeElements;
    }

    private static void CollectMatches(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        HashSet<UIElement>? scopeElements,
        bool insideScope,
        List<ServoResolvedElement> matches)
    {
        if (!TryResolveEntry(root, node, out UIElement? element))
        {
            return;
        }

        if (insideScope && MatchesOwnCriteria(node, element, target))
        {
            matches.Add(new ServoResolvedElement(node, element));
        }

        bool descendantsInsideScope = insideScope || scopeElements!.Contains(element);
        foreach (SemanticsNode child in node.Children)
        {
            CollectMatches(root, child, target, scopeElements, descendantsInsideScope, matches);
        }
    }

    private static IReadOnlyList<ServoResolvedElement> SelectPosition(
        List<ServoResolvedElement> matches,
        Index? position)
    {
        if (position is not Index index)
        {
            return matches;
        }

        int offset = index.GetOffset(matches.Count);
        return offset >= 0 && offset < matches.Count ? [matches[offset]] : [];
    }

    private static bool MatchesOwnCriteria(SemanticsNode node, UIElement element, ServoTarget target)
    {
        return MatchesId(element, target) &&
            target.MatchesName(node.Name) &&
            MatchesRole(node, target) &&
            (!target.HasPredicates || target.MatchesPredicates(CreateSnapshot(new ServoResolvedElement(node, element))));
    }

    private static bool MatchesId(UIElement element, ServoTarget target) =>
        target.Id is null || string.Equals(Servo.GetId(element), target.Id, StringComparison.Ordinal);

    private static bool MatchesRole(SemanticsNode node, ServoTarget target) =>
        target.Role is null || node.Role == target.Role;

    // Runs only after a resolution found no match. A near miss is an element that fails exactly one criterion
    // of a target with two or more criteria (Id, Role, name, each Where predicate, within); a single-criterion
    // target has no near misses, because every element fails exactly its one criterion. With a position, the
    // elements that pass every criterion are near misses of the position.
    private static string DescribeNotFound(UIRoot root, SemanticsTree tree, ServoTarget target)
    {
        string message = $"The Servo target {target} did not match any element.";
        string[] criteria = target.Scope is null
            ? [.. target.DescribeOwnCriteria()]
            : [.. target.DescribeOwnCriteria(), target.DescribeScope()];
        NearMissCounts counts = new(new int[criteria.Length]);
        if (criteria.Length >= 2 || target.Position is not null)
        {
            HashSet<UIElement>? scopeElements = ResolveScopeElements(root, tree, target);
            CountNearMisses(root, tree.Root, target, scopeElements, insideScope: scopeElements is null, counts);
        }

        List<string> nearMisses = criteria.Length < 2
            ? []
            : Enumerable.Range(0, criteria.Length)
                .Where(index => counts.PerCriterion[index] > 0)
                .OrderByDescending(index => counts.PerCriterion[index])
                .ThenBy(index => index)
                .Select(index => $"{counts.PerCriterion[index]} matched every criterion except {criteria[index]}")
                .ToList();
        if (target.Position is Index position && counts.FullMatches > 0)
        {
            nearMisses.Insert(
                0,
                $"{counts.FullMatches} matched every criterion, but {ServoTarget.DescribePosition(position)} selects none of them");
        }

        return nearMisses.Count == 0
            ? message
            : $"{message} Near misses: {string.Join("; ", nearMisses)}.";
    }

    private static void CountNearMisses(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        HashSet<UIElement>? scopeElements,
        bool insideScope,
        NearMissCounts counts)
    {
        if (!TryResolveEntry(root, node, out UIElement? element))
        {
            return;
        }

        int failures = 0;
        int failed = -1;
        int criterion = 0;
        void Check(bool matches)
        {
            if (!matches)
            {
                failures++;
                failed = criterion;
            }

            criterion++;
        }

        if (target.Id is not null) Check(MatchesId(element, target));
        if (target.Role is not null) Check(MatchesRole(node, target));
        if (target.HasName) Check(target.MatchesName(node.Name));
        if (target.HasPredicates)
        {
            ServoElement snapshot = CreateSnapshot(new ServoResolvedElement(node, element));
            for (int index = 0; index < target.PredicateCount; index++)
            {
                Check(target.MatchesPredicate(index, snapshot));
            }
        }

        if (target.Scope is not null) Check(insideScope);
        if (failures == 0)
        {
            counts.FullMatches++;
        }
        else if (failures == 1)
        {
            counts.PerCriterion[failed]++;
        }

        bool descendantsInsideScope = insideScope || scopeElements!.Contains(element);
        foreach (SemanticsNode child in node.Children)
        {
            CountNearMisses(root, child, target, scopeElements, descendantsInsideScope, counts);
        }
    }

    private static bool TryResolveEntry(UIRoot root, SemanticsNode node, [NotNullWhen(true)] out UIElement? element)
    {
        if (node.ElementId is UiElementId elementId &&
            root.ElementIds.TryGetElement(elementId, out element) &&
            element is not null &&
            ReferenceEquals(element.Root, root))
        {
            return true;
        }

        element = null;
        return false;
    }

    private static ServoElement CreateSnapshot(ServoResolvedElement match)
    {
        UIElement element = match.Element;
        SemanticsNode node = match.Node;
        return new ServoElement(
            element.GetType().Name,
            Servo.GetId(element),
            node.Name,
            node.Role,
            InputCoordinateConverter.GetRootBounds(element),
            UIElementVisibility.IsEffectivelyVisible(element),
            element.IsEnabled,
            element.IsKeyboardFocused,
            node.GetProperty<string>(SemanticsProperty.Value),
            node.Properties);
    }

    // PerCriterion is indexed like ServoTarget.DescribeOwnCriteria followed by the scope.
    private sealed class NearMissCounts(int[] perCriterion)
    {
        internal int[] PerCriterion { get; } = perCriterion;

        internal int FullMatches { get; set; }
    }
}

internal sealed record ServoResolvedElement(SemanticsNode Node, UIElement Element);
