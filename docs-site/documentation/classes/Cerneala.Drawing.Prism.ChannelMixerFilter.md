# ChannelMixerFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ChannelMixer` filter.

```csharp
public sealed class ChannelMixerFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ChannelMixerFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Red` | `Vector4` | `1, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Green` | `Vector4` | `0, 1, 0` | Optional catalog parameter; unit: `unitless`. |
| `Blue` | `Vector4` | `0, 0, 1` | Optional catalog parameter; unit: `unitless`. |
| `Constant` | `Vector4` | `0, 0, 0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `ChannelMixer` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
