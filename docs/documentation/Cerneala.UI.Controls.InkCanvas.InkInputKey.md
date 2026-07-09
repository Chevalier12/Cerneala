# InkCanvas.InkInputKey Record Struct

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/InkCanvas.cs`

Identifies one active ink input stream by input kind and pointer ID while `InkCanvas` is building strokes.

```csharp
private readonly record struct InkInputKey(InkInputKind Kind, int Id);
```

Containing type:
`InkCanvas`

Inheritance:
`object` -> `System.ValueType` -> `InkCanvas.InkInputKey`

## Examples
`InkInputKey` is private to `InkCanvas`, so callers do not create it directly. Its observable behavior is that active touch strokes with different IDs are tracked independently.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

InkCanvas canvas = new();

canvas.ApplyTouch(new TouchInputPoint(1, 0, 0, TouchInputAction.Down));
canvas.ApplyTouch(new TouchInputPoint(2, 10, 10, TouchInputAction.Down));
canvas.ApplyTouch(new TouchInputPoint(1, 1, 1, TouchInputAction.Move));
canvas.ApplyTouch(new TouchInputPoint(2, 11, 11, TouchInputAction.Move));
canvas.ApplyTouch(new TouchInputPoint(1, 2, 2, TouchInputAction.Up));
canvas.ApplyTouch(new TouchInputPoint(2, 12, 12, TouchInputAction.Up));

int strokeCount = canvas.Strokes.Count;
```

## Remarks
`InkInputKey` is a private nested implementation detail used as the key in `InkCanvas`'s active stroke dictionary. `InkCanvas.ApplyPoint` creates a key from an internal `InkInputKind` value and the incoming pointer ID, then uses that key to create, extend, or finish the matching active `Stroke`.

The key includes both `Kind` and `Id`. This keeps stylus and touch input spaces separate even when they reuse the same numeric ID. It also lets concurrent touch points with different IDs build separate strokes.

Because the type is a `readonly record struct`, equality and hash-code behavior are value-based and generated from the positional fields. `InkCanvas` relies on that value equality when it stores and looks up active strokes in `Dictionary<InkInputKey, Stroke>`.

## Constructors
| Name | Description |
| --- | --- |
| `InkInputKey(InkInputKind Kind, int Id)` | Creates a key for an active input stream using the input kind and pointer ID. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `InkInputKind` | Gets the internal input category, either stylus or touch. |
| `Id` | `int` | Gets the pointer ID from the stylus or touch input point. |

## Methods
| Name | Description |
| --- | --- |
| `Deconstruct(out InkInputKind Kind, out int Id)` | Deconstructs the key into its positional values. |
| `Equals(InkInputKey other)` | Compares two keys by `Kind` and `Id`. |
| `Equals(object? obj)` | Returns whether `obj` is an `InkInputKey` with the same positional values. |
| `GetHashCode()` | Returns the generated value hash code for `Kind` and `Id`. |
| `ToString()` | Returns the generated record string representation. |

## Applies to
Project: `Cerneala`

## See also
- Source: `UI/Controls/InkCanvas.cs`
- `InkCanvas`
- `Cerneala.UI.Ink.Stroke`
- `Cerneala.UI.Ink.StrokeCollection`
- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.TouchInputPoint`
