# PrismStyleState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable common state for one built-in layer-style occurrence.

```csharp
public sealed class PrismStyleState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Style` | `PrismStyleId` | Gets the built-in style identifier. |
| `Visible` | `bool` | Gets or sets whether the style runs. |

## Remarks

Style-specific values use catalog-generated typed keys and dense per-type storage. Changed writes increment the owning instance value version; identical writes do not.

## Applies to

Style occurrences exposed through layer, group, and backdrop state.
