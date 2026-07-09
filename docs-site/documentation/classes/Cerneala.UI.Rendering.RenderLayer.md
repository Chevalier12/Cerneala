# RenderLayer Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RenderLayer.cs`

Represents layer-level rendering state for opacity and visibility.

```csharp
public readonly record struct RenderLayer(float Opacity = 1)
```

Inheritance:
`ValueType` -> `RenderLayer`

## Examples

Use the default layer when rendering without a custom opacity:

```csharp
using Cerneala.UI.Rendering;

RenderLayer layer = RenderLayer.Default;

if (layer.IsVisible)
{
    float opacity = layer.Opacity;
}
```

Create a transparent layer:

```csharp
using Cerneala.UI.Rendering;

RenderLayer hidden = new(Opacity: 0);
bool visible = hidden.IsVisible;
```

## Remarks

`RenderLayer` is an immutable value used by rendering contexts to carry layer opacity. `Default` is equivalent to a layer with opacity `1`.

`IsVisible` returns `true` when `Opacity` is greater than `0`. The type does not clamp or validate opacity values.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Opacity` | `float` | Gets the opacity value carried by the layer. |
| `Default` | `RenderLayer` | Gets the default layer with opacity `1`. |
| `IsVisible` | `bool` | Gets whether the layer opacity is greater than `0`. |

## Applies To

Cerneala retained UI rendering APIs.

## See Also

- `Cerneala.UI.Rendering.RenderContext`
- `Cerneala.UI.Rendering.ElementRenderCache`
