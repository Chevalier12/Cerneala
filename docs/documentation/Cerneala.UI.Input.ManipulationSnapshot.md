# ManipulationSnapshot Struct

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/ManipulationProcessor.cs`

Captures the center point and two-point distance used by `ManipulationProcessor` to compute manipulation translation and scale deltas.

```csharp
internal readonly record struct ManipulationSnapshot(float CenterX, float CenterY, float Distance)
```

Inheritance:  
`ValueType` -> `ManipulationSnapshot`

Accessibility: `internal`

## Examples
The type is internal to the `Cerneala` assembly. The following example shows the calculation it performs inside the input pipeline:

```csharp
ManipulationSnapshot previous = ManipulationSnapshot.From(
[
    new ManipulationPoint(1, 0, 0),
    new ManipulationPoint(2, 10, 0),
]);

ManipulationSnapshot current = ManipulationSnapshot.From(
[
    new ManipulationPoint(1, -5, 0),
    new ManipulationPoint(2, 15, 0),
]);

ManipulationDelta delta = current.CreateDelta(previous);
// delta.TranslationX == 0
// delta.TranslationY == 0
// delta.Scale == 2
```

## Remarks
`ManipulationSnapshot` is the compact state object used by `ManipulationProcessor` between calls to `Process`. A snapshot stores the average center of the active manipulation points and, when at least two points are present, the distance between the first two points in the current enumeration order.

When no points are supplied, `From` returns a snapshot with `CenterX`, `CenterY`, and `Distance` set to `0`. When one point is supplied, the center is that point and `Distance` is `0`.

`CreateDelta` compares the current snapshot with a previous snapshot. Translation is the difference between centers. Scale is `Distance / previous.Distance` only when both distances are greater than `0`; otherwise scale is `1`.

## Constructors
| Name | Description |
| --- | --- |
| `ManipulationSnapshot(float CenterX, float CenterY, float Distance)` | Initializes a snapshot with a center position and a stored two-point distance. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `CenterX` | `float` | Gets the average X coordinate of the active manipulation points. |
| `CenterY` | `float` | Gets the average Y coordinate of the active manipulation points. |
| `Distance` | `float` | Gets the distance between the first two active manipulation points, or `0` when fewer than two points are present. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `From(IEnumerable<ManipulationPoint> points)` | `ManipulationSnapshot` | Creates a snapshot from the supplied manipulation points by averaging their coordinates and measuring the first two points when available. |
| `CreateDelta(ManipulationSnapshot previous)` | `ManipulationDelta` | Computes translation and scale from `previous` to the current snapshot. |

## Record-Generated Members
| Name | Description |
| --- | --- |
| `Deconstruct(out float CenterX, out float CenterY, out float Distance)` | Deconstructs the snapshot into its positional values. |
| `Equals(...)`, `GetHashCode()`, `ToString()` | Provide value equality, hash code generation, and record-style string formatting. |
| `==`, `!=` | Compare two snapshots using record struct value equality. |

## Applies to
Internal manipulation processing in the `Cerneala` project.

## See also
- `ManipulationProcessor`
- `ManipulationPoint`
- `ManipulationDelta`
