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
using DrawTransformScope scope = drawing.Transform(Matrix3x2.CreateTranslation(10, 20));
drawing.FillRectangle(bounds, color);
```

## Remarks

Dispose scopes exactly once in reverse creation order. Out-of-order or copied-scope disposal throws `InvalidOperationException`; disposing the same scope again throws `ObjectDisposedException`.

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Records the matching transform pop. |

## Applies To

Cerneala drawing state recording.
