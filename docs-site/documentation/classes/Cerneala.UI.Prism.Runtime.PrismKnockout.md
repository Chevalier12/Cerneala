# PrismKnockout Enum

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Specifies Photoshop-style knockout behavior for a Prism layer.

```csharp
public enum PrismKnockout
```

## Remarks

Knockout is a layer-only setting and defaults to `None`. The corresponding
runtime state setter rejects values outside this enum.

For `Shallow` and `Deep`, Prism uses a clean-room implementation of the PDF 1.7
knockout-group recurrence. The compositor keeps the current backdrop, the
original backdrop selected by the knockout scope, the source shape before
layer opacity, and the source alpha as separate values. The layer blend mode is
evaluated against the original backdrop; knockout does not silently replace the
selected blend mode with `Normal`.

The original backdrop that the SDL_GPU renderer uses is:

| Mode | Layer position | Original backdrop |
| --- | --- | --- |
| `Shallow` | Inside a `PassThrough` group | The background of that group. |
| `Shallow` | At the top level, or inside a group that is not `PassThrough` | None: transparent black. |
| `Deep` | Any, when the Prism scope has a host backdrop | The host backdrop. |
| `Deep` | Any, when the Prism scope has no host backdrop | None: transparent black. |

A Prism scope gets a host backdrop only when its blend modes or styles need
backdrop input. A `Deep` knockout does not request one by itself, so a scope in
which every layer uses `Normal` blending and no style reads the backdrop draws
its `Deep` layers against transparent black. With a transparent original
backdrop, the layer blend mode has no effect on the knocked-out pixels.
Disabled blend channels are restored from the current backdrop after the
recurrence.

The GPU implementation is in
`Drawing/Prism/Shaders/Hlsl/Blends/AdvancedBlending.hlsl`. The CPU reference
`PrismBlendMath.CompositeKnockout` uses the same shape/alpha recurrence on
premultiplied color, but it passes the layer's own backdrop as the original
backdrop, and no renderer calls it.

## Examples

```csharp
PrismLayerState layer = instance.GetLayerState(layerId);
layer.BlendMode = PrismBlendMode.Multiply;
layer.Knockout = PrismKnockout.Deep;
```

## Values

| Name | Description |
| --- | --- |
| `None` | Performs no knockout. |
| `Shallow` | Knocks out earlier content down to the background of the enclosing `PassThrough` group, or to transparent black elsewhere (see Remarks). |
| `Deep` | Knocks out earlier content down to the host backdrop of the Prism scope, or to transparent black when the scope has none (see Remarks). |

## Applies to

`PrismLayerState.Knockout`.
