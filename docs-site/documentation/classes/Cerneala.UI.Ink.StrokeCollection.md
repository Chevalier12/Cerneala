# StrokeCollection Class

## Definition
Namespace: `Cerneala.UI.Ink`

Assembly/Project: `Cerneala`

Source: `UI/Ink/StrokeCollection.cs`

Represents a mutable collection of ink strokes that raises change notifications when strokes are added or removed.

```csharp
public sealed class StrokeCollection : IReadOnlyList<Stroke>
```

Implements:
`IReadOnlyList<Stroke>`, `IReadOnlyCollection<Stroke>`, `IEnumerable<Stroke>`, `IEnumerable`

## Examples

```csharp
using Cerneala.UI.Ink;

StrokeCollection strokes = new();
strokes.Changed += (_, args) =>
{
    StrokeCollectionChangeKind kind = args.Kind;
    Stroke stroke = args.Stroke;
};

strokes.Add(stroke);
strokes.Remove(stroke);
```

## Remarks

`StrokeCollection` stores `Stroke` instances in insertion order. `Add` appends a stroke and raises `Changed` with `StrokeCollectionChangeKind.Added`.

`Remove` returns `false` when the supplied stroke is not present. When removal succeeds, it raises `Changed` with `StrokeCollectionChangeKind.Removed`.

The collection exposes read-only list members for consumers, but mutation is available through `Add` and `Remove`.

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of strokes in the collection. |
| `this[int index]` | Gets the stroke at the specified index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(Stroke)` | Adds a stroke and raises `Changed`. |
| `Remove(Stroke)` | Removes a stroke when present and returns whether it was removed. |
| `GetEnumerator()` | Returns an enumerator over the strokes. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised after a stroke is added or removed. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator over the strokes. |

## Applies to

Cerneala ink input and rendering.

## See also

- `Cerneala.UI.Ink.Stroke`
- `Cerneala.UI.Ink.StrokeCollectionChangedEventArgs`
- `Cerneala.UI.Controls.InkCanvas`
