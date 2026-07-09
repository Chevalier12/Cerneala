# ManipulationPoint Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ManipulationProcessor.cs`

Represents one active manipulation contact point, identified by an integer id and two `float` coordinates.

```csharp
public readonly record struct ManipulationPoint(int Id, float X, float Y);
```

Inheritance:
`Object` -> `ValueType` -> `ManipulationPoint`

## Examples

The following example feeds a single contact point through `ManipulationProcessor`. The second frame moves the same point, so the processor reports translation and keeps scale at `1`.

```csharp
using Cerneala.UI.Input;

ManipulationProcessor processor = new();

processor.Process([new ManipulationPoint(1, 0, 0)]);
ManipulationDelta delta = processor.Process([new ManipulationPoint(1, 5, 3)]);

// delta.TranslationX == 5
// delta.TranslationY == 3
// delta.Scale == 1
```

## Remarks

`ManipulationPoint` is a small immutable value type used as input to `ManipulationProcessor.Process`. The processor uses `Id` as the key for an active point and reads `X` and `Y` when building a manipulation snapshot.

When one point is processed across frames, movement changes the resulting translation. When two points are processed, the distance between the first two active points contributes to the resulting scale. The coordinate space is supplied by the caller; the type stores the values without validation or conversion.

Because this is a `readonly record struct`, it has compiler-generated value equality, deconstruction, string formatting, and hash-code behavior based on `Id`, `X`, and `Y`.

## Constructors

| Name | Description |
| --- | --- |
| `ManipulationPoint(int Id, float X, float Y)` | Initializes a manipulation point with an id and coordinates. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `int` | Identifies the active manipulation point. `ManipulationProcessor` uses this value as the point key for a frame. |
| `X` | `float` | Stores the horizontal coordinate supplied by the caller. |
| `Y` | `float` | Stores the vertical coordinate supplied by the caller. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out int Id, out float X, out float Y)` | Deconstructs the point into its id and coordinates. |
| `Equals(ManipulationPoint other)` | Returns whether another `ManipulationPoint` has the same `Id`, `X`, and `Y` values. |
| `Equals(object? obj)` | Returns whether an object is a `ManipulationPoint` with the same values. |
| `GetHashCode()` | Returns a hash code based on `Id`, `X`, and `Y`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==(ManipulationPoint left, ManipulationPoint right)` | Returns whether two points have the same values. |
| `operator !=(ManipulationPoint left, ManipulationPoint right)` | Returns whether two points have different values. |

## Applies to

`Cerneala` retained UI input manipulation processing.

## See also

- `ManipulationProcessor`
- `ManipulationDelta`
