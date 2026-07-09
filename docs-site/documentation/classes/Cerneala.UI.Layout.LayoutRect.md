# LayoutRect Struct

## Definition
Namespace: `Cerneala.UI.Layout`  
Assembly/Project: `Cerneala`  
Source: `UI/Layout/LayoutRect.cs`

Represents a rectangular layout area using single-precision `X`, `Y`, `Width`, and `Height` values.

```csharp
public readonly record struct LayoutRect(float X, float Y, float Width, float Height)
```

Inheritance:  
`ValueType` -> `LayoutRect`

## Examples

```csharp
using Cerneala.UI.Layout;

LayoutRect bounds = new(10, 20, 300, 120);

LayoutPoint origin = bounds.Location;
LayoutSize size = bounds.Size;

LayoutRect empty = LayoutRect.Empty;
```

## Remarks

`LayoutRect` is a readonly value type used to carry layout bounds through the UI layout system. Its positional values are stored as `float` values and are not validated, normalized, rounded, or clamped by the type itself.

The `Location` property creates a `LayoutPoint` from `X` and `Y`. The `Size` property creates a `LayoutSize` from `Width` and `Height`. `Empty` is the predefined zero rectangle: `(0, 0, 0, 0)`.

Because `LayoutRect` is a record struct, it has value-based equality and supports standard record-generated members such as deconstruction and `with` expressions.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutRect(float X, float Y, float Width, float Height)` | Initializes a rectangle from its position and size components. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | The horizontal coordinate of the rectangle. |
| `Y` | `float` | The vertical coordinate of the rectangle. |
| `Width` | `float` | The width component of the rectangle. |
| `Height` | `float` | The height component of the rectangle. |
| `Empty` | `LayoutRect` | Gets a rectangle whose `X`, `Y`, `Width`, and `Height` values are all `0`. |
| `Location` | `LayoutPoint` | Gets a `LayoutPoint` built from `X` and `Y`. |
| `Size` | `LayoutSize` | Gets a `LayoutSize` built from `Width` and `Height`. |

## Applies To

`Cerneala.UI.Layout` in the `Cerneala` project.

## See Also

- `LayoutPoint`
- `LayoutSize`
