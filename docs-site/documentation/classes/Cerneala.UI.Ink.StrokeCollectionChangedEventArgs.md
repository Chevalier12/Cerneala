# StrokeCollectionChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Ink`

Assembly/Project: `Cerneala`

Source: `UI/Ink/StrokeCollection.cs`

Provides event data for changes raised by `StrokeCollection.Changed`.

```csharp
public sealed record StrokeCollectionChangedEventArgs(StrokeCollectionChangeKind Kind, Stroke Stroke);
```

Inheritance:
`object` -> `StrokeCollectionChangedEventArgs`

Implements:
`IEquatable<StrokeCollectionChangedEventArgs>`

## Examples
```csharp
using Cerneala.Drawing;
using Cerneala.UI.Ink;

var strokes = new StrokeCollection();

strokes.Changed += (_, args) =>
{
    if (args.Kind == StrokeCollectionChangeKind.Added)
    {
        Stroke addedStroke = args.Stroke;
        int pointCount = addedStroke.Points.Count;
    }
};

var stroke = new Stroke(new[]
{
    new DrawPoint(0f, 0f),
    new DrawPoint(12f, 8f)
});

strokes.Add(stroke);
```

## Remarks
`StrokeCollectionChangedEventArgs` is emitted by the `StrokeCollection.Changed` event when a stroke is added or removed from a `StrokeCollection`.

`StrokeCollection.Add` raises `Changed` after adding the stroke and sets `Kind` to `StrokeCollectionChangeKind.Added`. `StrokeCollection.Remove` raises `Changed` only when the stroke was present and removed, and sets `Kind` to `StrokeCollectionChangeKind.Removed`.

Because this type is a positional record, equality and hash code generation use both positional properties. The `Stroke` value is compared according to the `Stroke` type's equality behavior.

## Constructors
| Name | Description |
| --- | --- |
| `StrokeCollectionChangedEventArgs(StrokeCollectionChangeKind Kind, Stroke Stroke)` | Initializes a new event data instance with the change kind and affected stroke. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `StrokeCollectionChangeKind` | Gets whether the stroke was added or removed. |
| `Stroke` | `Stroke` | Gets the stroke affected by the collection change. |

## Methods
| Name | Description |
| --- | --- |
| `Deconstruct(out StrokeCollectionChangeKind Kind, out Stroke Stroke)` | Deconstructs the record into its positional values. |
| `Equals(StrokeCollectionChangedEventArgs? other)` | Determines whether another instance has the same record values. |
| `Equals(object? obj)` | Determines whether an object is an equivalent `StrokeCollectionChangedEventArgs` instance. |
| `GetHashCode()` | Returns a hash code generated from the record values. |
| `ToString()` | Returns the generated record string representation. |

## Applies to
`Cerneala` UI ink APIs.

## See also
- `StrokeCollection`
- `StrokeCollectionChangeKind`
- `Stroke`
