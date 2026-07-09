# SemanticsTree Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/SemanticsTree.cs`

Represents a retained accessibility semantics tree snapshot with a single root node.

```csharp
public sealed class SemanticsTree
```

Inheritance:
`object` -> `SemanticsTree`

## Examples

Build a semantics tree from a UI root and inspect its root node.

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
root.VisualChildren.Add(new Button { Content = "Save" });

SemanticsTree tree = new SemanticsProvider().Build(root);

SemanticsNode rootNode = tree.Root;
SemanticsNode buttonNode = rootNode.Children[0];

// rootNode.Role == SemanticsRole.Root
// buttonNode.Role == SemanticsRole.Button
// buttonNode.Name == "Save"
```

## Remarks

`SemanticsTree` is the top-level container for an accessibility snapshot. It stores the `SemanticsNode` returned as `Root`; all accessible children are reached through that node's `Children` collection.

The constructor requires a non-null root node and throws `ArgumentNullException` when `root` is `null`. The class does not build or refresh semantics itself. Use `SemanticsProvider.Build(UIRoot)` to create a new tree, or `UIRoot.GetSemanticsTree()` when the caller wants the root-level cache and invalidation behavior.

`SemanticsProvider` builds the tree from visible visual children in visual order and skips hidden elements. `UIRoot.GetSemanticsTree()` can return the same `SemanticsTree` instance while the root semantics cache is still valid, and returns a new tree after relevant semantics or tree changes invalidate the cache.

## Constructors

| Name | Description |
| --- | --- |
| `SemanticsTree(SemanticsNode root)` | Initializes a semantics tree with the supplied root node; throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Description |
| --- | --- |
| `Root` | Gets the non-null root `SemanticsNode` for the semantics snapshot. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Accessibility/SemanticsTree.cs`
- `SemanticsNode`
- `SemanticsProvider`
- `UIRoot.GetSemanticsTree()`
- `IAccessibilityPlatform.Publish(SemanticsTree)`
