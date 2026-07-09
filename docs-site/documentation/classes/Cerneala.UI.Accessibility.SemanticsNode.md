# SemanticsNode Class

## Definition
Namespace: `Cerneala.UI.Accessibility`
Assembly/Project: `Cerneala`
Source: `UI/Accessibility/SemanticsNode.cs`

Represents a node in the retained accessibility semantics tree.

```csharp
public sealed class SemanticsNode
```

## Examples

```csharp
using Cerneala.UI.Accessibility;

var node = new SemanticsNode(
    elementId: null,
    role: SemanticsRole.Text,
    name: "Status");
```

## Remarks

`SemanticsNode` stores the semantic role, optional accessible name, optional element id, custom semantic properties, and child semantic nodes for an accessibility tree snapshot.

The constructor normalizes blank names to `null`, copies the supplied property dictionary into a read-only dictionary, and copies the supplied child list into a read-only collection. `GetProperty<T>` returns a typed semantic property value when the stored value matches `T`; otherwise it returns `default`.

## Constructors

| Name | Description |
| --- | --- |
| `SemanticsNode(UiElementId?, SemanticsRole, string?, IReadOnlyDictionary<SemanticsProperty, object?>?, IReadOnlyList<SemanticsNode>?)` | Initializes a semantics node with an element id, role, optional name, optional properties, and optional children. |

## Properties

| Name | Description |
| --- | --- |
| `ElementId` | Gets the associated UI element id, when one exists. |
| `Role` | Gets the semantic role represented by the node. |
| `Name` | Gets the normalized accessible name, or `null` when none is supplied. |
| `Properties` | Gets the read-only semantic property map. |
| `Children` | Gets the read-only child node collection. |

## Methods

| Name | Description |
| --- | --- |
| `GetProperty<T>(SemanticsProperty)` | Returns a typed semantic property value when present and assignable to `T`; otherwise returns `default`. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Accessibility/SemanticsNode.cs`
- `SemanticsTree`
- `SemanticsRole`
- `SemanticsProperty`
