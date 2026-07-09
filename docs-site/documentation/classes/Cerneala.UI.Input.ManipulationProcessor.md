# ManipulationProcessor Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ManipulationProcessor.cs`

Tracks successive active manipulation points and computes translation and scale deltas between frames.

```csharp
public sealed class ManipulationProcessor
```

Inheritance:
`object` -> `ManipulationProcessor`

## Examples

The first call establishes the initial snapshot. Later calls return movement relative to the previous snapshot.

```csharp
using Cerneala.UI.Input;

var processor = new ManipulationProcessor();

processor.Process(new[]
{
    new ManipulationPoint(1, 0, 0),
});

ManipulationDelta delta = processor.Process(new[]
{
    new ManipulationPoint(1, 5, 3),
});

// delta.TranslationX == 5
// delta.TranslationY == 3
// delta.Scale == 1
```

Two active points can produce a scale delta.

```csharp
using Cerneala.UI.Input;

var processor = new ManipulationProcessor();

processor.Process(new[]
{
    new ManipulationPoint(1, 0, 0),
    new ManipulationPoint(2, 10, 0),
});

ManipulationDelta delta = processor.Process(new[]
{
    new ManipulationPoint(1, -5, 0),
    new ManipulationPoint(2, 15, 0),
});

// delta.Scale == 2
```

## Remarks

`ManipulationProcessor` is a small stateful helper for gesture-style input. Each call to `Process` converts the supplied active points into a snapshot, compares it with the previous snapshot, stores the current snapshot, and returns a `ManipulationDelta`.

The first `Process` call after construction or `Reset` returns a neutral delta: `TranslationX` is `0`, `TranslationY` is `0`, and `Scale` is `1`.

Translation is computed from the movement of the point center. The center is the average `X` and `Y` of all active points in the current snapshot. Scale is computed from the distance between the first two active points in each snapshot. If either snapshot has fewer than two points, or either distance is zero, scale is reported as `1`.

Call `Reset` when a manipulation sequence ends so the next sequence starts from a fresh neutral baseline.

## Constructors

| Name | Description |
| --- | --- |
| `ManipulationProcessor()` | Initializes a new processor with no previous manipulation snapshot. |

## Methods

| Name | Description |
| --- | --- |
| `Process(IReadOnlyList<ManipulationPoint> activePoints)` | Computes a `ManipulationDelta` from the supplied active points compared with the previous call, then stores the current snapshot. Throws `ArgumentNullException` when `activePoints` is `null`. |
| `Reset()` | Clears active point state and removes the previous snapshot. |

## Related Value Types

| Name | Description |
| --- | --- |
| `ManipulationPoint` | Public readonly record struct with `Id`, `X`, and `Y` values used as `Process` input. |
| `ManipulationDelta` | Public readonly record struct with `TranslationX`, `TranslationY`, and `Scale` values returned by `Process`. |

## Applies to

`Cerneala.UI.Input` in the `Cerneala` project.

## See also

- `UI/Input/ManipulationProcessor.cs`
- `tests/Cerneala.Tests/Input/ManipulationProcessorTests.cs`
