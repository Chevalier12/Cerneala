# LayoutRounding Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: [`UI/Layout/LayoutRounding.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutRounding.cs)

Represents a layout-rounding mode that either keeps layout values unchanged or snaps them to the device pixel grid of a display scale.

```csharp
public readonly record struct LayoutRounding
```

Inheritance:
`object` -> `ValueType` -> `LayoutRounding`

Implements:
`IEquatable<LayoutRounding>`

## Examples

At scale 1, positions and rectangles round to the nearest whole number, but a `LayoutSize` rounds up:

```csharp
using Cerneala.UI.Layout;

LayoutRounding rounding = LayoutRounding.Enabled;

float x = rounding.Round(1.2f);
LayoutSize size = rounding.Round(new LayoutSize(1.2f, 3.5f));
LayoutRect rect = rounding.Round(new LayoutRect(0.4f, 1.6f, 33.4f, 10.5f));

Console.WriteLine($"{x} {size.Width} {size.Height}");
Console.WriteLine($"{rect.X} {rect.Y} {rect.Width} {rect.Height}");
// 1 2 4
// 0 2 33 10
```

At scale 2, values snap to half pixels:

```csharp
using Cerneala.UI.Layout;

LayoutRounding rounding = LayoutRounding.ForScale(2);

float x = rounding.Round(1.3f);
LayoutSize size = rounding.Round(new LayoutSize(1.1f, 2.2f));

Console.WriteLine($"{x} {size.Width} {size.Height}");
// 1.5 1.5 2.5
```

Use `Disabled` when layout values must stay unchanged:

```csharp
using Cerneala.UI.Layout;

LayoutSize original = new(1.6f, 3.5f);
LayoutSize unchanged = LayoutRounding.Disabled.Round(original);

Console.WriteLine(unchanged == original);
// True
```

## Remarks

`LayoutRounding` is a small immutable value that layout contexts carry so that rounding is an explicit choice. `LayoutManager` passes `LayoutRounding.ForScale(root.Scale)` to every `Measure` and `Arrange` it starts.

When rounding is enabled, coordinates and sizes are rounded differently:

| Member | Enabled result |
| --- | --- |
| `Round(float)` | `MathF.Round(value * Scale) / Scale` (nearest device pixel). |
| `Round(LayoutPoint)` | `X` and `Y` through `Round(float)`. |
| `Round(LayoutSize)` | `Width` and `Height` rounded up: `MathF.Ceiling(value * Scale) / Scale`. |
| `Round(LayoutRect)` | `X`, `Y`, `Width`, and `Height` through `Round(float)` (nearest device pixel), as WPF rounds the arrange rectangle. |

`UIElement.Measure` rounds `DesiredSize` with `Round(LayoutSize)`. It rounds up, so a measured size is never smaller than its content. `UIElement.Arrange` rounds `ArrangedBounds` with `Round(LayoutRect)`. It rounds the size to the nearest device pixel, like the position, so a rectangle whose edges already lie on device pixels stays unchanged. A size rounded up instead could gain a device pixel from float error. For example, 57 device pixels at scale 1.75 are `32.571428` pixels, and `32.571428 * 1.75` is slightly more than `57`. Before it calls `ArrangeCore`, `UIElement.Arrange` rounds the position of the element's rectangle with `Round(float)`, so every panel places its children from its rounded origin, as WPF arranges children relative to their parent's rounded offset. A panel that divides space, such as `Grid`, also gives each part a whole number of device pixels before it arranges its children, so the rounded rectangles of neighbouring children meet exactly. For example, a centered `StackPanel` 12 pixels wide in a 13-pixel slot at scale 1 starts at x = 0.5. Its children are placed from x = 0, so four 3-pixel children end up at 0..3, 3..6, 6..9, and 9..12. They would end up at 0..3, 4..7, 6..9, and 10..13 if each child's x (0.5, 3.5, 6.5, 9.5) were rounded on its own.

`MathF.Round` uses banker's rounding, so `Round(2.5f)` at scale 1 is `2`.

When rounding is disabled, every overload returns its input unchanged.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutRounding(bool isEnabled)` | Initializes a rounding mode with `Scale` 1. |
| `LayoutRounding(bool isEnabled, float scale)` | Initializes a rounding mode for `scale`. Throws `ArgumentOutOfRangeException` when `scale` is zero, negative, `NaN`, or infinite. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Disabled` | `LayoutRounding` | Gets a rounding mode with `IsEnabled` set to `false` and `Scale` 1. |
| `Enabled` | `LayoutRounding` | Gets a rounding mode with `IsEnabled` set to `true` and `Scale` 1. |
| `IsEnabled` | `bool` | Gets whether calls to `Round` change values. |
| `Scale` | `float` | Gets the display scale whose device pixels values snap to. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `ForScale(float scale)` | `LayoutRounding` | Returns an enabled rounding mode for `scale`. Throws `ArgumentOutOfRangeException` for an invalid scale. |
| `Round(float value)` | `float` | Rounds `value` to the nearest device pixel when enabled; otherwise returns `value`. |
| `Round(LayoutPoint point)` | `LayoutPoint` | Returns a point with `X` and `Y` passed through `Round(float)`. |
| `Round(LayoutSize size)` | `LayoutSize` | Returns a size with `Width` and `Height` rounded up to device pixels. |
| `Round(LayoutRect rect)` | `LayoutRect` | Returns a rectangle with its position and size rounded to the nearest device pixel. |

## Applies to

`MeasureContext`, `ArrangeContext`, and `LayoutManager`.

## See also

- [`LayoutPoint`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutPoint.cs)
- [`LayoutSize`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutSize.cs)
- [`LayoutRect`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutRect.cs)
- [`MeasureContext`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/MeasureContext.cs)
- [`ArrangeContext`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/ArrangeContext.cs)
