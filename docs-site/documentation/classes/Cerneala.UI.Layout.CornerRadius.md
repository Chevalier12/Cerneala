# CornerRadius Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/CornerRadius.cs`

Represents the radius of each of the four corners of a rectangle.

```csharp
public readonly record struct CornerRadius(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
```

Inheritance:
`ValueType` -> `CornerRadius`

## Examples

Round all four corners of a `Border` by 8 pixels, or only its left corners:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Border rounded = new() { CornerRadius = new CornerRadius(8) };
Border leftRounded = new() { CornerRadius = new CornerRadius(8, 0, 0, 8) };
```

In markup, one value sets all four corners, and four comma-separated values set top-left, top-right, bottom-right, and bottom-left:

```xml
<Border CornerRadius="8" />
<Border CornerRadius="8,0,0,8" />
```

## Remarks

`CornerRadius` is an immutable record struct. The uniform constructor assigns the same radius to all four corners. The struct itself does not validate its values; `Border.CornerRadius` rejects a negative, `NaN`, or infinite corner.

A radius of `0` is a square corner. Use `Zero` when no corner is rounded.

## Constructors

| Name | Description |
| --- | --- |
| `CornerRadius(float)` | Initializes all four corners to the same radius. |
| `CornerRadius(float, float, float, float)` | Initializes the top-left, top-right, bottom-right, and bottom-left radius independently. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BottomLeft` | `float` | Gets the radius of the bottom-left corner. |
| `BottomRight` | `float` | Gets the radius of the bottom-right corner. |
| `TopLeft` | `float` | Gets the radius of the top-left corner. |
| `TopRight` | `float` | Gets the radius of the top-right corner. |
| `Zero` | `CornerRadius` | Gets a corner radius whose corners are all zero. |

## Applies to

- `Cerneala.UI.Controls.Border`

## See also

- `Cerneala.UI.Controls.Border`
- `Cerneala.UI.Layout.Thickness`
- `Cerneala.Drawing.DrawCornerRadius`
