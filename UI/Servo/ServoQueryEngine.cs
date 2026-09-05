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
        IReadOnlyList<ServoResolvedElement> matches = ResolveAll(root, target);
        if (matches.Count == 0)
        {
            throw new ServoTargetNotFoundException("The Servo target did not match any element.");
        }

        if (matches.Count > 1)
        {
            throw new ServoTargetAmbiguousException(
                $"The Servo target matched {matches.Count} elements; exactly one was required.");
        }

        return matches[0];
    }

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

        if (target.Scope is null)
        {
            return true;
        }

        for (int index = ancestors.Count - 1; index >= 0; index--)
        {
            ServoSemanticEntry ancestor = ancestors[index];
            IReadOnlyList<ServoSemanticEntry> ancestorAncestors = ancestors.Take(index).ToArray();
            if (Matches(ancestor, target.Scope, ancestorAncestors))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesOwnCriteria(ServoSemanticEntry candidate, ServoTarget target)
    {
        return (target.Id is null || string.Equals(Servo.GetId(candidate.Element), target.Id, StringComparison.Ordinal)) &&
            (target.Name is null || string.Equals(candidate.Node.Name, target.Name, StringComparison.Ordinal)) &&
            (target.Role is null || candidate.Node.Role == target.Role);
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
