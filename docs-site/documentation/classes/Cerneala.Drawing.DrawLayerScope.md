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

The backend obtains an intermediate render target from a bounded pool and returns it deterministically after compositing. Dispose scopes once in LIFO order.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching layer pop. |

## Applies To

Cerneala drawing state recording.
