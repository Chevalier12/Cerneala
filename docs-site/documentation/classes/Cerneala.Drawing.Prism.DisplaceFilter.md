# DisplaceFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Displace` filter.

```csharp
public sealed class DisplaceFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DisplaceFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Map` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `HorizontalScale` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |
| `VerticalScale` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |
| `MapFit` | `string` | `Stretch` | Optional catalog parameter; unit: `none`. |
| `UndefinedAreas` | `string` | `RepeatEdgePixels` | Optional catalog parameter; unit: `none`. |
| `ChannelX` | `string` | `Red` | Optional catalog parameter; unit: `none`. |
| `ChannelY` | `string` | `Green` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Displace` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
