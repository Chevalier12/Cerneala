# Thickness Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Thickness.cs`

Represents four layout edge thickness values.

```csharp
public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
```

Inheritance:
`ValueType` -> `Thickness`

## Examples

Create thickness values for layout margins, padding, or spacing calculations.

```csharp
using Cerneala.UI.Layout;

Thickness padding = new(8);
float contentOffsetX = padding.Left;
float totalHorizontal = padding.Horizontal;
```

## Remarks

`Thickness` is an immutable record struct with left, top, right, and bottom edge values.

The uniform constructor assigns the same value to all four edges. `Horizontal` returns `Left + Right`, and `Vertical` returns `Top + Bottom`.

Use `Zero` when no edge thickness is needed.

## Constructors

| Name | Description |
| --- | --- |
| `Thickness(float)` | Initializes all four edges to the same value. |
| `Thickness(float, float, float, float)` | Initializes each edge independently. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bottom` | `float` | Gets the bottom edge thickness. |
| `Horizontal` | `float` | Gets the combined left and right thickness. |
| `Left` | `float` | Gets the left edge thickness. |
| `Right` | `float` | Gets the right edge thickness. |
| `Top` | `float` | Gets the top edge thickness. |
| `Vertical` | `float` | Gets the combined top and bottom thickness. |
| `Zero` | `Thickness` | Gets a thickness whose edges are all zero. |

## Applies to

- `Cerneala.UI.Layout.Thickness`

## See also

- `Cerneala.UI.Layout.LayoutSize`
- `Cerneala.UI.Layout.LayoutRect`
