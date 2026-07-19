# PrismBlendRange Struct

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Represents four ordered, normalized thresholds for one side of a feathered Blend If gate.

```csharp
public readonly record struct PrismBlendRange
```

## Examples

```csharp
PrismBlendRange softHighlights = new(0f, 0.1f, 0.8f, 1f);
layer.ThisLayerRange = softHighlights;
```

## Constructors

| Name | Description |
| --- | --- |
| `PrismBlendRange(float blackStart, float blackEnd, float whiteStart, float whiteEnd)` | Initializes ordered thresholds from zero through one. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BlackStart` | `float` | Gets the hard black cutoff. |
| `BlackEnd` | `float` | Gets the end of the black feather. |
| `WhiteStart` | `float` | Gets the start of the white feather. |
| `WhiteEnd` | `float` | Gets the hard white cutoff. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismBlendRange(...)` | `ArgumentOutOfRangeException` | A threshold is non-finite or outside zero through one. |
| `PrismBlendRange(...)` | `ArgumentException` | Thresholds are not ordered from black start through white end. |

## Applies to

`PrismLayerState.ThisLayerRange` and `PrismLayerState.UnderlyingRange`.
