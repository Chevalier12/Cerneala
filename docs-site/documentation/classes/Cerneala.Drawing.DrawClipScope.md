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

## Applies To

Cerneala drawing state recording.
