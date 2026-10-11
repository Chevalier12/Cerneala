# ElementTreeChangeKind Enum

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementTreeChangeKind.cs`

Specifies whether a child was added to or removed from a `UIElementCollection`.

```csharp
public enum ElementTreeChangeKind
```

## Examples

```csharp
using Cerneala.UI.Elements;

UIElement parent = new();
UIElement child = new();
parent.VisualChildren.Changed += (_, change) =>
    Console.WriteLine($"{change.Kind} {change.Role} {change.Child == child}");

parent.VisualChildren.Add(child);
parent.VisualChildren.Remove(child);
// Added Visual True
// Removed Visual True
```

## Remarks

`UIElementCollection.Changed` passes an `ElementTreeChange` whose `Kind` is one of these values. The event is raised after the collection and the child's parent have been updated.

| Operation | Event |
| --- | --- |
| `Add`, `Insert` | `Added` |
| `Remove` of a child in the collection | `Removed` |
| `Remove` of an element that is not a child | None; `Remove` returns `false`. |
| `Move` | None. |

When a visual child is removed while a presence exit animation applies, the child leaves the collection at once and `Removed` is raised then, before the exit animation ends.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Added` | `0` | A child was added. |
| `Removed` | `1` | A child was removed. |

## Applies to

`ElementTreeChange.Kind`.

## See also

- `Cerneala.UI.Elements.ElementTreeChange`
- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Elements.ElementChildRole`
