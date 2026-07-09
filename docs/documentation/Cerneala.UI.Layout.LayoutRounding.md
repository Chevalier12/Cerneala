# LayoutRounding Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: [`UI/Layout/LayoutRounding.cs`](../../UI/Layout/LayoutRounding.cs)

Represents an explicit layout-rounding mode that can either preserve layout values or round them to whole-number coordinates and sizes.

```csharp
public readonly record struct LayoutRounding(bool IsEnabled)
```

Inheritance:
`object` -> `ValueType` -> `LayoutRounding`

Implements:
`IEquatable<LayoutRounding>`

## Examples

```csharp
using Cerneala.UI.Layout;

LayoutRounding rounding = LayoutRounding.Enabled;

float width = rounding.Round(1.6f);
LayoutSize size = rounding.Round(new LayoutSize(1.6f, 3.5f));

// width == 2
// size == new LayoutSize(2, 4)
```

Use `Disabled` when layout values must stay unchanged.

```csharp
using Cerneala.UI.Layout;

LayoutSize original = new(1.6f, 3.5f);
LayoutSize unchanged = LayoutRounding.Disabled.Round(original);

// unchanged == original
```

## Remarks

`LayoutRounding` is a small immutable value used by layout contexts to make rounding an explicit choice. `Enabled` stores `IsEnabled` as `true`; `Disabled` stores it as `false`.

When rounding is enabled, `Round(float)` delegates to `MathF.Round(float)`. The point, size, and rectangle overloads round each component independently:

| Overload | Rounded components |
| --- | --- |
| `Round(LayoutPoint)` | `X`, `Y` |
| `Round(LayoutSize)` | `Width`, `Height` |
| `Round(LayoutRect)` | `X`, `Y`, `Width`, `Height` |

When rounding is disabled, `Round(float)` returns the original value. The layout primitive overloads return new values whose components are unchanged.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutRounding(bool IsEnabled)` | Initializes a rounding mode with rounding enabled when `IsEnabled` is `true`, or disabled when it is `false`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Disabled` | `LayoutRounding` | Gets a rounding mode with `IsEnabled` set to `false`. |
| `Enabled` | `LayoutRounding` | Gets a rounding mode with `IsEnabled` set to `true`. |
| `IsEnabled` | `bool` | Gets whether calls to `Round` round values instead of preserving them. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Round(float value)` | `float` | Returns `MathF.Round(value)` when enabled; otherwise returns `value`. |
| `Round(LayoutPoint point)` | `LayoutPoint` | Returns a point with `X` and `Y` passed through `Round(float)`. |
| `Round(LayoutSize size)` | `LayoutSize` | Returns a size with `Width` and `Height` passed through `Round(float)`. |
| `Round(LayoutRect rect)` | `LayoutRect` | Returns a rectangle with `X`, `Y`, `Width`, and `Height` passed through `Round(float)`. |

## Applies to

`Cerneala` UI layout primitives.

## See also

- [`LayoutPoint`](../../UI/Layout/LayoutPoint.cs)
- [`LayoutSize`](../../UI/Layout/LayoutSize.cs)
- [`LayoutRect`](../../UI/Layout/LayoutRect.cs)
- [`MeasureContext`](../../UI/Layout/MeasureContext.cs)
- [`ArrangeContext`](../../UI/Layout/ArrangeContext.cs)
