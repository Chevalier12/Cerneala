# DrawClipScope Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Provides stack-only lifetime management for a rectangular or geometric clip.

```csharp
public ref struct DrawClipScope
```

## Examples

```csharp
using DrawClipScope scope = drawing.Clip(path, DrawFillRule.EvenOdd);
drawing.FillRectangle(bounds, color);
```

## Remarks

Axis-aligned rectangular clips use the scissor fast path. Path clips and transformed non-axis-aligned rectangles use geometric clipping. Dispose scopes once in LIFO order.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching clip pop. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `ObjectDisposedException` | The same scope variable is disposed a second time. |
| `Dispose()` | `InvalidOperationException` | Another scope opened later on the same `DrawingContext` is still open, so the scopes are not disposed in last-in, first-out order. |

## Applies To

Cerneala drawing state recording.
