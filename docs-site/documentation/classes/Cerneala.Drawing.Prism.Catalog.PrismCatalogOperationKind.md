# PrismCatalogOperationKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Catalog/PrismCatalog.cs`

Identifies the family of a discoverable Prism operation.

```csharp
public enum PrismCatalogOperationKind
```

## Values

| Name | Description |
| --- | --- |
| `Filter` | The operation transforms pixels flowing through a Prism scope. |
| `Style` | The operation decorates a prepared Prism scope. |

## Examples

```csharp
bool isFilter = PrismCatalog.GetFilter(PrismFilterId.Color).Kind ==
    PrismCatalogOperationKind.Filter;
```

## Remarks

The value controls which state type and definition enum are valid for an operation.

## Applies to

`PrismCatalogOperationInfo`.

