using System.Diagnostics.CodeAnalysis;
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
        return ResolveAll(root, target).Count > 0;
    }

    internal IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, ServoTarget target)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(target);
        SemanticsTree tree = root.GetServoSemanticsTree();
        return ResolveAll(root, tree, target);
    }

    private static IReadOnlyList<ServoResolvedElement> ResolveAll(UIRoot root, SemanticsTree tree, ServoTarget target)
    {
        HashSet<UIElement>? scopeElements = null;
        if (target.Scope is not null)
        {
            scopeElements = new HashSet<UIElement>(ReferenceEqualityComparer.Instance);
            foreach (ServoResolvedElement scopeMatch in ResolveAll(root, tree, target.Scope))
            {
                scopeElements.Add(scopeMatch.Element);
            }
        }

        List<ServoResolvedElement> matches = [];
        CollectMatches(root, tree.Root, target, scopeElements, insideScope: scopeElements is null, matches);
        return SelectPosition(matches, target.Position);
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
        IReadOnlyList<SemanticsNode> children = node.Children;
        for (int index = 0; index < children.Count; index++)
        {
            CollectMatches(root, children[index], target, scopeElements, descendantsInsideScope, matches);
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
        return (target.Id is null || string.Equals(Servo.GetId(element), target.Id, StringComparison.Ordinal)) &&
            target.MatchesName(node.Name) &&
            (target.Role is null || node.Role == target.Role) &&
            (!target.HasPredicates || target.MatchesPredicates(CreateSnapshot(new ServoResolvedElement(node, element))));
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
}

internal sealed record ServoResolvedElement(SemanticsNode Node, UIElement Element);
