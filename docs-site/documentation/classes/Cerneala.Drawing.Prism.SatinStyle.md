# SatinStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Satin` style.

```csharp
public sealed class SatinStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `SatinStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Multiply` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Angle` | `float` | `19` | Optional catalog parameter; unit: `degrees`. |
| `Distance` | `float` | `11` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `14` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Gaussian` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Invert` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Satin` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new SatinStyle())`.

Each property in the table also has a public static `MotionProperty<SatinStyle, T>` field named after it, for example `BlendModeProperty` of type `MotionProperty<SatinStyle, string>`. Motion uses these fields to animate the value.

The class inherits `StyleId` and `Visible` from `PrismStyle`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static field `VisibleProperty` for the inherited `Visible`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
