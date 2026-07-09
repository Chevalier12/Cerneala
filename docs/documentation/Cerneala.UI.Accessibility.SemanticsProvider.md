# SemanticsProvider Class

## Definition
Namespace: `Cerneala.UI.Accessibility`

Assembly/Project: `Cerneala`

Source: `UI/Accessibility/SemanticsProvider.cs`

Builds a `SemanticsTree` snapshot from a `UIRoot` visual tree.

```csharp
public sealed class SemanticsProvider
```

Inheritance:
`object` -> `SemanticsProvider`

## Examples

Build a semantics tree for a root that contains an accessible button.

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new() { Content = "Save" };
AccessibleName.SetName(button, "Save drawing");
root.VisualChildren.Add(button);

SemanticsTree tree = new SemanticsProvider().Build(root);
SemanticsNode buttonNode = tree.Root.Children[0];

// buttonNode.Role == SemanticsRole.Button
// buttonNode.Name == "Save drawing"
```

## Remarks

`SemanticsProvider` walks the `UIRoot` and its `VisualChildren` recursively, then creates one `SemanticsNode` per included element through `AutomationPeer.Create(element).CreateNode(children)`. Roles, names, and property values therefore come from the matching automation peer for each element.

Child elements are included only when `UIElementVisibility.ParticipatesInRendering(child)` returns `true`, which requires the child to be visible and to have `Visibility.Visible`. If a child is skipped, its descendants are not traversed by this provider.

The provider creates a new `SemanticsTree` for each `Build` call. `UIRoot.GetSemanticsTree()` owns the root-level semantics cache and invalidation behavior.

## Constructors

| Name | Description |
| --- | --- |
| `SemanticsProvider()` | Initializes a new `SemanticsProvider` instance. |

## Methods

| Name | Description |
| --- | --- |
| `Build(UIRoot root)` | Builds a `SemanticsTree` whose root node represents `root`; throws `ArgumentNullException` when `root` is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `SemanticsTree`
- `SemanticsNode`
- `AutomationPeer`
- `UIRoot.GetSemanticsTree()`
