# StrokeCollectionChangeKind Enum

## Definition
Namespace: `Cerneala.UI.Ink`

Assembly/Project: `Cerneala`

Source: `UI/Ink/StrokeCollectionChangeKind.cs`

Specifies whether a stroke was added to or removed from a `StrokeCollection`.

```csharp
public enum StrokeCollectionChangeKind
```

## Examples

```csharp
using Cerneala.UI.Ink;

StrokeCollection strokes = new();
strokes.Changed += (_, e) => Console.WriteLine($"{e.Kind} {strokes.Count}");

Stroke stroke = new();
strokes.Add(stroke);
Console.WriteLine(strokes.Remove(stroke));
Console.WriteLine(strokes.Remove(stroke));
// Added 1
// Removed 0
// True
// False
```

The second `Remove` finds nothing, so it returns `false` and raises no event.

## Remarks

`StrokeCollectionChangedEventArgs.Kind` carries this value, and `StrokeCollectionChangedEventArgs.Stroke` carries the stroke. The event is raised after the collection has changed.

`InkCanvas` listens to its own `Strokes.Changed`. For both kinds it invalidates its measure and render. When the canvas is attached to a root and the change runs on another thread, the handler throws `InvalidOperationException`; the stroke has already been added or removed at that point.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Added` | `0` | A stroke was added by `StrokeCollection.Add`. |
| `Removed` | `1` | A stroke was removed by `StrokeCollection.Remove`. |

## Applies to

`StrokeCollectionChangedEventArgs.Kind`.

## See also

- `Cerneala.UI.Ink.StrokeCollection`
- `Cerneala.UI.Ink.StrokeCollectionChangedEventArgs`
- `Cerneala.UI.Controls.InkCanvas`
