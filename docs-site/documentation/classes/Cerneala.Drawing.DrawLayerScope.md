# DrawLayerScope Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Provides stack-only lifetime management for an isolated drawing layer.

```csharp
public ref struct DrawLayerScope
```

## Examples

```csharp
using DrawLayerScope scope = drawing.Layer(new DrawLayerOptions(0.8f));
drawing.FillPath(path, brush);
```

## Remarks

The SDL_GPU backend renders the children into an intermediate render target. It keeps one target per nesting depth and reuses it in later frames while the pixel size, color format, and sample count stay the same. A different size at the same depth retires the old target and creates a new one. The target is not released after compositing. Dispose scopes once in LIFO order.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching layer pop. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `ObjectDisposedException` | The same scope variable is disposed a second time. |
| `Dispose()` | `InvalidOperationException` | Another scope opened later on the same `DrawingContext` is still open, so the scopes are not disposed in last-in, first-out order. |

## Applies To

Cerneala drawing state recording.
