# DrawOpacityScope Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Provides stack-only lifetime management for real group opacity.

```csharp
public ref struct DrawOpacityScope
```

## Examples

```csharp
using DrawOpacityScope scope = drawing.Opacity(0.5f);
drawing.FillRectangle(first, Color.White);
drawing.FillRectangle(second, Color.White);
```

## Remarks

Children are isolated and opacity is applied once to the combined result. Dispose scopes once in LIFO order.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching opacity pop. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `ObjectDisposedException` | The same scope variable is disposed a second time. |
| `Dispose()` | `InvalidOperationException` | Another scope opened later on the same `DrawingContext` is still open, so the scopes are not disposed in last-in, first-out order. |

## Applies To

Cerneala drawing state recording.
