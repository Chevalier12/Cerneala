# DrawTransformScope Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Provides stack-only lifetime management for a pushed transform.

```csharp
public ref struct DrawTransformScope
```

## Examples

```csharp
using DrawTransformScope scope = drawing.Transform(System.Numerics.Matrix3x2.CreateTranslation(10, 20));
drawing.FillRectangle(bounds, color);
```

## Remarks

Dispose scopes exactly once in reverse creation order. Out-of-order or copied-scope disposal throws `InvalidOperationException`; disposing the same scope again throws `ObjectDisposedException`.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching transform pop. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `ObjectDisposedException` | The same scope variable is disposed a second time. |
| `Dispose()` | `InvalidOperationException` | Another scope opened later on the same `DrawingContext` is still open, so the scopes are not disposed in last-in, first-out order. |

## Applies To

Cerneala drawing state recording.
