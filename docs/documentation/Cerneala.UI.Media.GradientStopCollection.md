# GradientStopCollection Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Brush.cs`

Provides internal helpers for normalizing, comparing, and hashing gradient stop sequences.

```csharp
internal static class GradientStopCollection
```

Inheritance:
`Object` -> `GradientStopCollection`

## Examples

Use `CreateOrdered` from code inside the `Cerneala` assembly to validate and sort gradient stops by offset.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

IReadOnlyList<GradientStop> stops = GradientStopCollection.CreateOrdered(
[
    new GradientStop(1f, DrawColor.Black),
    new GradientStop(0f, DrawColor.White),
]);

// stops[0].Offset == 0f
// stops[1].Offset == 1f
```

## Remarks

`GradientStopCollection` is an internal static helper used by `LinearGradientBrush` and `RadialGradientBrush`.

`CreateOrdered` requires a non-null sequence with at least one `GradientStop`. It orders the stops by `GradientStop.Offset`, materializes them into an array, and returns a read-only list wrapper over that ordered array.

`SequenceEquals` compares two read-only stop lists by count and then by sequence order. `GetSequenceHashCode` builds a combined hash code by adding each stop in order, so the hash depends on both the stop values and their order.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateOrdered(IEnumerable<GradientStop> stops)` | `IReadOnlyList<GradientStop>` | Validates that `stops` is not null, sorts the sequence by offset, rejects an empty result, and returns a read-only list. |
| `SequenceEquals(IReadOnlyList<GradientStop> left, IReadOnlyList<GradientStop> right)` | `bool` | Returns `true` when both lists have the same count and equal stops in the same order. |
| `GetSequenceHashCode(IReadOnlyList<GradientStop> stops)` | `int` | Computes an order-sensitive hash code for a gradient stop list. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CreateOrdered` | `ArgumentNullException` | `stops` is `null`. |
| `CreateOrdered` | `ArgumentException` | `stops` contains no items after enumeration. |

## Applies to

- `Cerneala.UI.Media.GradientStopCollection`

## See also

- `Cerneala.UI.Media.GradientStop`
- `Cerneala.UI.Media.LinearGradientBrush`
- `Cerneala.UI.Media.RadialGradientBrush`
