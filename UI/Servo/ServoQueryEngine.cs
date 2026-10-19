using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class ServoQueryEngine
{
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
        SemanticsTree tree = root.GetServoSemanticsTree();
        return ContainsMatch(root, tree.Root, target, []);
    }

    internal IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, ServoTarget target)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(target);
        SemanticsTree tree = root.GetServoSemanticsTree();
        List<ServoResolvedElement> matches = [];
        CollectMatches(root, tree.Root, target, [], matches);
        return matches;
    }

    // One ancestor stack serves the whole walk: a node is pushed before its children are
    // visited and popped after them, so ancestors[0..Count) is always the current path.
    private static void CollectMatches(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        List<ServoSemanticEntry> ancestors,
        List<ServoResolvedElement> matches)
    {
        if (!TryResolveEntry(root, node, out ServoSemanticEntry entry))
        {
            return;
        }

        if (Matches(entry, target, ancestors, ancestors.Count))
        {
            matches.Add(new ServoResolvedElement(node, entry.Element));
        }

        ancestors.Add(entry);
        IReadOnlyList<SemanticsNode> children = node.Children;
        for (int index = 0; index < children.Count; index++)
        {
            CollectMatches(root, children[index], target, ancestors, matches);
        }

        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static bool ContainsMatch(
        UIRoot root,
        SemanticsNode node,
        ServoTarget target,
        List<ServoSemanticEntry> ancestors)
    {
        if (!TryResolveEntry(root, node, out ServoSemanticEntry entry))
        {
            return false;
        }

        if (Matches(entry, target, ancestors, ancestors.Count))
        {
            return true;
        }

        ancestors.Add(entry);
        bool found = false;
        IReadOnlyList<SemanticsNode> children = node.Children;
        for (int index = 0; index < children.Count && !found; index++)
        {
            found = ContainsMatch(root, children[index], target, ancestors);
        }

        ancestors.RemoveAt(ancestors.Count - 1);
        return found;
    }

    // The candidate's ancestors are ancestors[0..ancestorCount), nearest last.
    private static bool Matches(
        ServoSemanticEntry candidate,
        ServoTarget target,
        List<ServoSemanticEntry> ancestors,
        int ancestorCount)
    {
        if (!MatchesOwnCriteria(candidate, target))
        {
            return false;
        }

        if (target.Scope is null)
        {
            return true;
        }

        for (int index = ancestorCount - 1; index >= 0; index--)
        {
            if (Matches(ancestors[index], target.Scope, ancestors, index))
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
