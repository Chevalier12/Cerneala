# DirtyTreeDumper Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/DirtyTreeDumper.cs`

Creates a text snapshot of dirty elements in a retained UI element tree.

```csharp
public sealed class DirtyTreeDumper
```

Inheritance:
`object` -> `DirtyTreeDumper`

## Examples

The following example invalidates a child element and dumps the dirty visual tree with trace details from the root.

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);
root.ProcessFrame();

child.Invalidate(InvalidationFlags.Render, "manual render");

string dump = new DirtyTreeDumper().Dump(root, root.Trace);
```

The returned text starts with `Dirty tree` and includes one line for each dirty element, for example:

```text
Dirty tree
- UIElement#2 flags=Render version=1 reason=manual render
```

## Remarks

`DirtyTreeDumper` walks the element tree in preorder using `ElementTreeWalker.PreOrder`. By default it walks visual children because `Dump` uses `ElementChildRole.Visual` unless another role is supplied.

Only elements whose `DirtyState.IsDirty` value is `true` are written to the dump. Each dirty element line includes the runtime type name, element id, invalidation flags, and dirty-state version. If an `InvalidationTrace` is supplied, the dumper also appends the latest dirty-cause reason for the same element. Dirty causes are trace entries with kind `Request`, `Propagation`, or `Queue`; `Clear` entries are not reported as reasons.

When the latest relevant trace entry can be associated with a source property, the line also includes `source=...`. The dumper searches backward through trace entries for the same element until it finds a source property name or reaches a request entry.

If no dirty elements are found, the dump contains:

```text
Dirty tree
- none
```

## Constructors

| Name | Description |
| --- | --- |
| `DirtyTreeDumper()` | Initializes a new instance of the `DirtyTreeDumper` class. |

## Methods

| Name | Description |
| --- | --- |
| `Dump(UIElement root, InvalidationTrace? trace = null, ElementChildRole role = ElementChildRole.Visual)` | Returns a text dump of dirty elements reachable from `root` for the selected child role. |

## Dump Method

```csharp
public string Dump(
    UIElement root,
    InvalidationTrace? trace = null,
    ElementChildRole role = ElementChildRole.Visual)
```

### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `root` | `UIElement` | The root element where traversal starts. |
| `trace` | `InvalidationTrace?` | Optional trace used to annotate dirty elements with the latest recorded reason and source property. |
| `role` | `ElementChildRole` | The child relationship used for traversal. The default is `ElementChildRole.Visual`. |

### Returns

`string`

A trimmed multi-line text dump. The first line is always `Dirty tree`.

### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `root` is `null`. |

## Applies to

Cerneala retained UI diagnostics.

## See also

- `Cerneala.UI.Diagnostics.InvalidationTrace`
- `Cerneala.UI.Elements.ElementTreeWalker`
- `Cerneala.UI.Elements.UIElement`
