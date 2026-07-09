# ElementTreeDumper Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/ElementTreeDumper.cs`

Creates a text dump of a `UIElement` subtree for diagnostics.

```csharp
public sealed class ElementTreeDumper
```

Inheritance:
`Object` -> `ElementTreeDumper`

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

UIRoot root = new();
Border parent = new();
TextBlock child = new() { Text = "Hello" };

parent.Child = child;
root.VisualChildren.Add(parent);

string dump = new ElementTreeDumper().Dump(root);
```

The returned string starts with the selected tree role and then lists the root and descendants in pre-order. Each element line also includes `visibility`, `bounds`, and `dirty` values.

```csharp
bool hasRootLine = dump.Contains($"- UIRoot#{root.ElementId}", StringComparison.Ordinal);
bool hasParentLine = dump.Contains($"  - Border#{parent.ElementId}", StringComparison.Ordinal);
bool hasChildLine = dump.Contains($"    - TextBlock#{child.ElementId}", StringComparison.Ordinal);
```

## Remarks

`ElementTreeDumper` is intended for diagnostic output, tests, and debugging of Cerneala UI element trees. It does not mutate the tree.

`Dump` walks the selected child collection recursively. When `role` is `ElementChildRole.Visual`, it reads `UIElement.VisualChildren`; when `role` is `ElementChildRole.Logical`, it reads `UIElement.LogicalChildren`.

The dump uses a two-space indent per depth level. Each element line contains the runtime type name, the attached `ElementId`, or `unattached` when the element is not attached to a root. The method trims the trailing line break before returning the string.

## Constructors

| Name | Description |
| --- | --- |
| `ElementTreeDumper()` | Initializes a new `ElementTreeDumper` instance. |

## Methods

| Name | Description |
| --- | --- |
| `Dump(UIElement root, ElementChildRole role = ElementChildRole.Visual)` | Returns a diagnostic string for `root` and its descendants using the selected child role. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dump(UIElement root, ElementChildRole role = ElementChildRole.Visual)` | `ArgumentNullException` | `root` is `null`. |

## Applies to

Cerneala UI diagnostics in the `Cerneala` project.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.ElementChildRole`
