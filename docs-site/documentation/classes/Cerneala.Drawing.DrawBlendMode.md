# DrawBlendMode Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Selects the compositing operation used by a drawing blend or isolated layer scope.

```csharp
public enum DrawBlendMode
```

## Examples

```csharp
using DrawBlendScope blend = drawing.Blend(DrawBlendMode.Multiply);
drawing.FillRectangle(bounds, color);
```

## Remarks

Drawing colors use premultiplied alpha. Blend state objects are cached by the backend rather than recreated for each command.

## Fields

| Name | Description |
| --- | --- |
| `Normal` | Source-over premultiplied-alpha compositing. |
| `Opaque` | Replaces the destination. |
| `Additive` | Adds source and destination color. |
| `Multiply` | Multiplies source and destination color while preserving source-over alpha. |
| `Screen` | Applies the screen blend operation. |

## Applies To

Cerneala drawing state and isolated layers.
