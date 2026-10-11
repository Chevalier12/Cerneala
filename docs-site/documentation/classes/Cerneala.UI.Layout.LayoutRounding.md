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

At scale 1, positions round to the nearest whole number, but sizes round up:

```csharp
using Cerneala.UI.Layout;

LayoutRounding rounding = LayoutRounding.Enabled;

float x = rounding.Round(1.2f);
LayoutSize size = rounding.Round(new LayoutSize(1.2f, 3.5f));

Console.WriteLine($"{x} {size.Width} {size.Height}");
// 1 2 4
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
| `Round(LayoutRect)` | `X` and `Y` through `Round(float)`; `Width` and `Height` rounded up like `Round(LayoutSize)`. |

Sizes round up so that content is never clipped by a fraction of a pixel. `MathF.Round` uses banker's rounding, so `Round(2.5f)` at scale 1 is `2`.

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
| `Round(LayoutRect rect)` | `LayoutRect` | Returns a rectangle with a rounded position and a rounded-up size. |

## Applies to

`MeasureContext`, `ArrangeContext`, and `LayoutManager`.

## See also

- [`LayoutPoint`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutPoint.cs)
- [`LayoutSize`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutSize.cs)
- [`LayoutRect`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/LayoutRect.cs)
- [`MeasureContext`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/MeasureContext.cs)
- [`ArrangeContext`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Layout/ArrangeContext.cs)
