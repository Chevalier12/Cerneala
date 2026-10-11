# DrawBlendScope Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Provides stack-only lifetime management for a drawing blend mode.

```csharp
public ref struct DrawBlendScope
```

## Examples

```csharp
using DrawBlendScope scope = drawing.Blend(DrawBlendMode.Additive);
drawing.FillEllipse(bounds, color);
```

## Remarks

The selected blend applies to drawing commands until the scope is disposed. Dispose scopes once in LIFO order.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching blend pop. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `ObjectDisposedException` | The same scope variable is disposed a second time. |
| `Dispose()` | `InvalidOperationException` | Another scope opened later on the same `DrawingContext` is still open, so the scopes are not disposed in last-in, first-out order. |

## Applies To

Cerneala drawing state recording.
