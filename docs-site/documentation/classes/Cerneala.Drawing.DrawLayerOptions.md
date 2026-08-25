# DrawLayerOptions Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Specifies how an isolated drawing layer is composited into its parent.

```csharp
public sealed record DrawLayerOptions
```

## Examples

```csharp
using DrawLayerScope layer = drawing.Layer(
    new DrawLayerOptions(0.65f, DrawBlendMode.Screen));
drawing.FillRectangle(bounds, color);
```

## Remarks

The layer renders its children into an intermediate surface, then applies `Opacity` and `BlendMode` once to the combined result. This preserves group-opacity semantics for overlapping children. Opacity must be finite and between `0` and `1` inclusive.

## Constructors

| Name | Description |
| --- | --- |
| `DrawLayerOptions(float, DrawBlendMode)` | Creates options with optional opacity and blend mode. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Opacity` | `float` | Gets the group opacity. |
| `BlendMode` | `DrawBlendMode` | Gets the layer compositing mode. |

## Applies To

Cerneala isolated drawing layers.
