# PrismMaskChannel Enum

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismMaskDefinition.cs`

Specifies how a Prism mask image is converted into mask coverage.

```csharp
public enum PrismMaskChannel
```

## Remarks

`Alpha` is the default and reads coverage from image alpha. `Luminance` derives
coverage from image brightness instead.

## Values

| Name | Description |
| --- | --- |
| `Alpha` | Uses the image alpha channel. |
| `Luminance` | Uses image luminance. |

## Applies to

`PrismMaskDefinition` and `PrismMaskState`.
