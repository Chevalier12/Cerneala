# ManipulationDelta Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Target framework: `net8.0`

Source: `UI/Input/ManipulationProcessor.cs`

Represents the translation and scale change computed for a manipulation update.

```csharp
public readonly record struct ManipulationDelta(float TranslationX, float TranslationY, float Scale);
```

Inheritance:
`ValueType` -> `ManipulationDelta`

Implements:
`IEquatable<ManipulationDelta>`

## Examples
The `ManipulationProcessor` returns a `ManipulationDelta` for each processed set of active points. The first processed frame returns the identity delta; later frames compare against the previous frame.

```csharp
using Cerneala.UI.Input;

ManipulationProcessor processor = new();

processor.Process([new ManipulationPoint(1, 0, 0)]);
ManipulationDelta delta = processor.Process([new ManipulationPoint(1, 5, 3)]);

Console.WriteLine(delta.TranslationX); // 5
Console.WriteLine(delta.TranslationY); // 3
Console.WriteLine(delta.Scale);        // 1
```

## Remarks
`ManipulationDelta` is an immutable value type used by `ManipulationProcessor` to report movement and scaling between two manipulation snapshots.

`TranslationX` and `TranslationY` are the movement of the manipulation center from the previous snapshot to the current snapshot. `Scale` is the ratio between the current and previous point distances when both distances are greater than zero. If either distance is zero, the scale delta is `1`.

For the first processed snapshot after construction or `ManipulationProcessor.Reset()`, the processor returns `new ManipulationDelta(0, 0, 1)`.

Because this type is a C# `readonly record struct`, it has value equality, deconstruction, and compiler-generated formatting members.

## Constructors
| Name | Description |
| --- | --- |
| `ManipulationDelta(float TranslationX, float TranslationY, float Scale)` | Initializes a new delta with horizontal translation, vertical translation, and scale values. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `TranslationX` | `float` | Horizontal movement of the manipulation center since the previous snapshot. |
| `TranslationY` | `float` | Vertical movement of the manipulation center since the previous snapshot. |
| `Scale` | `float` | Scale factor between snapshots; `1` means no scale change. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out float TranslationX, out float TranslationY, out float Scale)` | `void` | Deconstructs the record into its three component values. |
| `Equals(ManipulationDelta other)` | `bool` | Determines whether another delta has the same component values. |
| `Equals(object? obj)` | `bool` | Determines whether an object is a `ManipulationDelta` with the same component values. |
| `GetHashCode()` | `int` | Returns a hash code based on the component values. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Operators
| Name | Return Type | Description |
| --- | --- | --- |
| `operator ==(ManipulationDelta left, ManipulationDelta right)` | `bool` | Determines whether two deltas have the same component values. |
| `operator !=(ManipulationDelta left, ManipulationDelta right)` | `bool` | Determines whether two deltas have different component values. |

## Applies to
Cerneala UI input manipulation processing.

## See also
- `ManipulationProcessor`
- `ManipulationPoint`
- `InputEvents.ManipulationDeltaEvent`
