# PrismCatalogOperationInfo Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Catalog/PrismCatalog.cs`

Describes one built-in Prism filter or style.

```csharp
public sealed class PrismCatalogOperationInfo
```

## Examples

```csharp
PrismCatalogOperationInfo operation = PrismCatalog.GetStyle(PrismStyleId.DropShadow);
bool needsExternalInput = operation.RequiresResource;
```

## Remarks

Instances are immutable and owned by `PrismCatalog`. `StableId` matches the numeric value of the corresponding `PrismFilterId` or `PrismStyleId`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `StableId` | `int` | Gets the stable numeric catalog identifier. |
| `Id` | `string` | Gets the machine-readable catalog identifier. |
| `Symbol` | `string` | Gets the author-facing operation name. |
| `Kind` | `PrismCatalogOperationKind` | Gets whether the operation is a filter or style. |
| `Category` | `string` | Gets the catalog category. |
| `Parameters` | `ImmutableArray<PrismCatalogParameterInfo>` | Gets parameters in catalog order. |
| `RequiresResource` | `bool` | Gets whether at least one required parameter needs a Prism resource. |

## Applies to

Built-in Prism filters and styles.

