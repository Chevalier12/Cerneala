# ElementTreeWalker Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementTreeWalker.cs`

Provides static helpers for traversing a `UIElement` tree by visual or logical child role.

```csharp
public static class ElementTreeWalker
```

## Examples
The following example builds a small visual tree and enumerates it in pre-order. The root element is included in the result.

```csharp
using Cerneala.UI.Elements;

UIElement root = new();
UIElement header = new();
UIElement content = new();

root.VisualChildren.Add(header);
root.VisualChildren.Add(content);

foreach (UIElement element in ElementTreeWalker.PreOrder(root))
{
    // Visit root, then header and content.
}
```

To walk the logical tree instead of the visual tree, pass `ElementChildRole.Logical`.

```csharp
using Cerneala.UI.Elements;

UIElement root = new();
UIElement logicalChild = new();

root.LogicalChildren.Add(logicalChild);

foreach (UIElement element in ElementTreeWalker.Descendants(root, ElementChildRole.Logical))
{
    // Visit each logical descendant, excluding root.
}
```

## Remarks
`ElementTreeWalker` selects children and parents from the requested `ElementChildRole`. For `ElementChildRole.Visual`, it reads `UIElement.VisualChildren` and `UIElement.VisualParent`. For `ElementChildRole.Logical`, it reads `UIElement.LogicalChildren` and `UIElement.LogicalParent`.

All public methods validate their required `UIElement` argument immediately and throw `ArgumentNullException` when it is `null`. The returned sequences are iterator-backed; traversal work happens as the returned `IEnumerable<UIElement>` is enumerated.

`PreOrder` and `PostOrder` include the supplied root element. `Descendants` excludes the supplied root and starts at its children. `Ancestors` excludes the supplied element and starts at its parent.

The traversal is depth-first and follows the order exposed by the selected `UIElementCollection`.

## Methods
| Name | Description |
| --- | --- |
| `PreOrder(UIElement root, ElementChildRole role = ElementChildRole.Visual)` | Returns a depth-first sequence that yields `root` before its descendants. |
| `PostOrder(UIElement root, ElementChildRole role = ElementChildRole.Visual)` | Returns a depth-first sequence that yields descendants before `root`. |
| `Ancestors(UIElement element, ElementChildRole role = ElementChildRole.Visual)` | Returns the parent chain for `element`, starting with the immediate parent. |
| `Descendants(UIElement root, ElementChildRole role = ElementChildRole.Visual)` | Returns a depth-first sequence of descendants under `root`, excluding `root`. |

## Method Behavior
| Method | Includes starting element | First yielded item | Traversal source |
| --- | --- | --- | --- |
| `PreOrder` | Yes | `root` | Selected child collection |
| `PostOrder` | Yes | First deepest child, or `root` when there are no children | Selected child collection |
| `Ancestors` | No | Immediate selected parent | Selected parent property |
| `Descendants` | No | First selected child | Selected child collection |

## Exceptions
| Method | Exception | Condition |
| --- | --- | --- |
| `PreOrder` | `ArgumentNullException` | `root` is `null`. |
| `PostOrder` | `ArgumentNullException` | `root` is `null`. |
| `Ancestors` | `ArgumentNullException` | `element` is `null`. |
| `Descendants` | `ArgumentNullException` | `root` is `null`. |

## Applies to
`Cerneala.UI.Elements` tree traversal in the `Cerneala` project.

## See also
- `UI/Elements/UIElement.cs`
- `UI/Elements/UIElementCollection.cs`
- `UI/Elements/ElementChildRole.cs`
