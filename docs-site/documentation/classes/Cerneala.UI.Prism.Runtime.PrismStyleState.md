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

Style-specific values use catalog-generated dense storage. Applications can discover parameters through `PrismCatalog` and access them without stable IDs or slot numbers.

## Methods

| Name | Description |
| --- | --- |
| `GetValue<T>(PrismCatalogParameterInfo)` | Gets a typed style parameter after validating its descriptor and generic type. |
| `SetValue<T>(PrismCatalogParameterInfo, T)` | Validates and updates a typed style parameter. Changed writes advance the owning instance value version. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetValue<T>`, `SetValue<T>` | `ArgumentException` | The descriptor belongs to another operation or `T` does not match its catalog value kind. |
| `SetValue<T>` | `ArgumentOutOfRangeException` | The value violates the catalog domain or symbol options. |
| `GetValue<T>`, `SetValue<T>` | `InvalidOperationException` | The state handle belongs to a replaced definition. |

## Applies to

Style occurrences exposed through layer, group, and backdrop state.
