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
        List<ServoResolvedElement> matches = [];
        CollectMatches(root, tree.Root, target, [], matches);
        if (matches.Count == 0)
        {
            throw new ServoTargetNotFoundException(DescribeNotFound(root, tree.Root, target));
        }

        if (matches.Count > 1)
        {
            throw CreateAmbiguousException(target, matches.Count);
        }

        return matches[0];
    }

    internal static ServoTargetAmbiguousException CreateAmbiguousException(ServoTarget target, int matchCount) =>
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
        SemanticsTree tree = semanticsProvider.Build(root, SemanticsProjection.Servo);
        return ContainsMatch(root, tree.Root, target, []);
    }

    internal IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, ServoTarget target)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(target);
        SemanticsTree tree = semanticsProvider.Build(root, SemanticsProjection.Servo);
        List<ServoResolvedElement> matches = [];
        CollectMatches(root, tree.Root, target, [], matches);
        return matches;
    }

    private static void CollectMatches(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        IReadOnlyList<ServoSemanticEntry> ancestors,
        List<ServoResolvedElement> matches)
    {
        if (!TryResolveEntry(root, node, out ServoSemanticEntry entry))
        {
            return;
        }

        if (Matches(entry, target, ancestors))
        {
            matches.Add(new ServoResolvedElement(node, entry.Element));
        }

        List<ServoSemanticEntry> descendantsAncestors = new(ancestors.Count + 1);
        descendantsAncestors.AddRange(ancestors);
        descendantsAncestors.Add(entry);
        foreach (SemanticsNode child in node.Children)
        {
            CollectMatches(root, child, target, descendantsAncestors, matches);
        }
    }

    private static bool ContainsMatch(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        IReadOnlyList<ServoSemanticEntry> ancestors)
    {
        if (!TryResolveEntry(root, node, out ServoSemanticEntry entry))
        {
            return false;
        }

        if (Matches(entry, target, ancestors))
        {
            return true;
        }

        List<ServoSemanticEntry> descendantsAncestors = new(ancestors.Count + 1);
        descendantsAncestors.AddRange(ancestors);
        descendantsAncestors.Add(entry);
        foreach (SemanticsNode child in node.Children)
        {
            if (ContainsMatch(root, child, target, descendantsAncestors))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(
        ServoSemanticEntry candidate,
        ServoTarget target,
        IReadOnlyList<ServoSemanticEntry> ancestors)
    {
        if (!MatchesOwnCriteria(candidate, target))
        {
            return false;
        }

        return target.Scope is null || HasAncestorMatching(target.Scope, ancestors);
    }

    private static bool HasAncestorMatching(
        ServoTarget scope,
        IReadOnlyList<ServoSemanticEntry> ancestors)
    {
        for (int index = ancestors.Count - 1; index >= 0; index--)
        {
            ServoSemanticEntry ancestor = ancestors[index];
            IReadOnlyList<ServoSemanticEntry> ancestorAncestors = ancestors.Take(index).ToArray();
            if (Matches(ancestor, scope, ancestorAncestors))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesOwnCriteria(ServoSemanticEntry candidate, ServoTarget target)
    {
        return MatchesId(candidate, target) && MatchesName(candidate, target) && MatchesRole(candidate, target);
    }

    private static bool MatchesId(ServoSemanticEntry candidate, ServoTarget target) =>
        target.Id is null || string.Equals(Servo.GetId(candidate.Element), target.Id, StringComparison.Ordinal);

    private static bool MatchesName(ServoSemanticEntry candidate, ServoTarget target) =>
        target.Name is null || string.Equals(candidate.Node.Name, target.Name, StringComparison.Ordinal);

    private static bool MatchesRole(ServoSemanticEntry candidate, ServoTarget target) =>
        target.Role is null || candidate.Node.Role == target.Role;

    // Runs only after a resolution found no match. A near miss is an element that fails exactly one criterion
    // of a target with two or more criteria; a single-criterion target has no near misses, because every
    // element fails exactly its one criterion.
    private static string DescribeNotFound(UIRoot root, SemanticsNode treeRoot, ServoTarget target)
    {
        string message = $"The Servo target {target} did not match any element.";
        string[] criteria = target.Scope is null
            ? [.. target.DescribeOwnCriteria()]
            : [.. target.DescribeOwnCriteria(), target.DescribeScope()];
        if (criteria.Length < 2)
        {
            return message;
        }

        int[] nearMissCounts = new int[criteria.Length];
        CountNearMisses(root, treeRoot, target, [], nearMissCounts);
        string[] nearMisses = Enumerable.Range(0, criteria.Length)
            .Where(index => nearMissCounts[index] > 0)
            .OrderByDescending(index => nearMissCounts[index])
            .ThenBy(index => index)
            .Select(index => $"{nearMissCounts[index]} matched every criterion except {criteria[index]}")
            .ToArray();
        return nearMisses.Length == 0
            ? message
            : $"{message} Near misses: {string.Join("; ", nearMisses)}.";
    }

    private static void CountNearMisses(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        IReadOnlyList<ServoSemanticEntry> ancestors,
        int[] nearMissCounts)
    {
        if (!TryResolveEntry(root, node, out ServoSemanticEntry entry))
        {
            return;
        }

        if (GetOnlyFailedCriterion(entry, target, ancestors) is int failed)
        {
            nearMissCounts[failed]++;
        }

        List<ServoSemanticEntry> descendantsAncestors = new(ancestors.Count + 1);
        descendantsAncestors.AddRange(ancestors);
        descendantsAncestors.Add(entry);
        foreach (SemanticsNode child in node.Children)
        {
            CountNearMisses(root, child, target, descendantsAncestors, nearMissCounts);
        }
    }

    // The index of the one failed criterion, in the order of ServoTarget.DescribeOwnCriteria followed by the
    // scope, or null when no criterion or more than one criterion fails.
    private static int? GetOnlyFailedCriterion(
        ServoSemanticEntry candidate,
        ServoTarget target,
        IReadOnlyList<ServoSemanticEntry> ancestors)
    {
        int index = 0;
        int failures = 0;
        int failed = -1;
        void Check(bool matches)
        {
            if (!matches)
            {
                failures++;
                failed = index;
            }

            index++;
        }

        if (target.Id is not null) Check(MatchesId(candidate, target));
        if (target.Role is not null) Check(MatchesRole(candidate, target));
        if (target.Name is not null) Check(MatchesName(candidate, target));
        if (target.Scope is not null) Check(HasAncestorMatching(target.Scope, ancestors));
        return failures == 1 ? failed : null;
    }

    private static bool TryResolveEntry(UIRoot root, SemanticsNode node, out ServoSemanticEntry entry)
    {
        if (node.ElementId is UiElementId elementId &&
            root.ElementIds.TryGetElement(elementId, out UIElement? element) &&
            element is not null &&
            ReferenceEquals(element.Root, root))
        {
            entry = new ServoSemanticEntry(node, element);
            return true;
        }

        entry = default;
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

    private readonly record struct ServoSemanticEntry(SemanticsNode Node, UIElement Element);
}

internal sealed record ServoResolvedElement(SemanticsNode Node, UIElement Element);
