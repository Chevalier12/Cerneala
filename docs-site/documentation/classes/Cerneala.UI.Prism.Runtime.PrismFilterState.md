# PrismFilterState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable common state for one built-in filter occurrence.

```csharp
public sealed class PrismFilterState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filter` | `PrismFilterId` | Gets the built-in filter identifier. |
| `Visible` | `bool` | Gets or sets whether the filter runs. |
| `Opacity` | `float` | Gets or sets the filtered-result mix from zero through one. |
| `BlendMode` | `PrismBlendMode` | Gets or sets the per-filter blend mode. |

## Remarks

Catalog-specific filter parameters are stored in dense typed slots. Applications use `PrismCatalogParameterInfo` with the generic accessors, while generated markup continues to use allocation-free generated keys internally.

## Methods

| Name | Description |
| --- | --- |
| `GetValue<T>(PrismCatalogParameterInfo)` | Gets a typed catalog parameter after validating the descriptor and generic type. |
| `SetValue<T>(PrismCatalogParameterInfo, T)` | Validates and updates a typed catalog parameter. Changed writes advance the owning instance value version. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through one. |
| `BlendMode` | `ArgumentException` | The assigned value is `PassThrough`. |
| `GetValue<T>`, `SetValue<T>` | `ArgumentException` | The descriptor belongs to another operation or `T` does not match its catalog value kind. |
| `SetValue<T>` | `ArgumentOutOfRangeException` | The value violates the catalog domain or symbol options. |
| `GetValue<T>`, `SetValue<T>` | `InvalidOperationException` | The state handle belongs to a replaced definition. |

## Applies to

Filter occurrences exposed through layer, group, and backdrop state.
