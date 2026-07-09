# MotionStagger Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionStagger.cs`

Calculates deterministic per-item motion delays from a fixed non-negative time offset.

```csharp
public sealed class MotionStagger
```

Inheritance:
`object` -> `MotionStagger`

## Examples

Create a stagger with a 20 millisecond offset and calculate the delay for each item index:

```csharp
using Cerneala.UI.Motion.Core;

MotionStagger stagger = new(TimeSpan.FromMilliseconds(20));

TimeSpan firstDelay = stagger.GetDelay(0);  // TimeSpan.Zero
TimeSpan secondDelay = stagger.GetDelay(1); // 20 ms
TimeSpan thirdDelay = stagger.GetDelay(2);  // 40 ms
```

## Remarks

`MotionStagger` stores one offset and multiplies it by the zero-based item index passed to `GetDelay`. Index `0` always returns `TimeSpan.Zero`; later indexes return `Offset * index`.

The constructor rejects negative offsets with `ArgumentOutOfRangeException`. `GetDelay` rejects negative indexes with `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionStagger(TimeSpan offset)` | Initializes a stagger calculator with a non-negative offset between adjacent indexes. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Offset` | `TimeSpan` | Gets the non-negative delay added for each increment in item index. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetDelay(int index)` | `TimeSpan` | Returns `Offset * index` for a non-negative zero-based index. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionStagger(TimeSpan offset)` | `ArgumentOutOfRangeException` | `offset` is less than `TimeSpan.Zero`. |
| `GetDelay(int index)` | `ArgumentOutOfRangeException` | `index` is less than `0`. |

## Applies to

Cerneala motion core staggered timing helpers.

## See also

- `Cerneala.UI.Motion.Core.MotionGroup`
- `Cerneala.UI.Motion.Core.MotionSequence`
- `Cerneala.UI.Motion.Core.MotionHandle`
