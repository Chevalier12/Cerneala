# PrismCatalogParameterInfo Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Catalog/PrismCatalog.cs`

Describes one typed parameter belonging to a Prism filter or style.

```csharp
public sealed class PrismCatalogParameterInfo
```

## Examples

```csharp
PrismCatalogParameterInfo radius = PrismCatalog.GetFilter(PrismFilterId.Blur)
    .Parameters.Single(parameter => parameter.Name == "Radius");

float minimum = (float)(radius.Minimum ?? 0);
```

## Remarks

Use this descriptor with `PrismFilterState.GetValue<T>` and `SetValue<T>`, or the corresponding `PrismStyleState` methods. Numeric bounds are exposed as nullable doubles because finite, one-sided, and unbounded domains all exist in the catalog.

`SymbolOptions` contains stable names known by the generated catalog. Required resource parameters cannot be satisfied by metadata alone.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Gets the machine-readable parameter identifier. |
| `Name` | `string` | Gets the author-facing parameter name. |
| `ValueKind` | `PrismCatalogValueKind` | Gets the public runtime value kind. |
| `IsRequired` | `bool` | Gets whether a value is required. |
| `DefaultValue` | `string?` | Gets the canonical default text, or `null` for required parameters. |
| `DomainKind` | `string` | Gets the catalog domain classification. |
| `Minimum` | `double?` | Gets the inclusive numeric minimum when present. |
| `Maximum` | `double?` | Gets the inclusive numeric maximum when present. |
| `Unit` | `string` | Gets the catalog unit. |
| `SymbolOptions` | `ImmutableArray<string>` | Gets known stable symbol names. |
| `RequiresResource` | `bool` | Gets whether this is a required resource parameter. |

## Applies to

Parameters returned by `PrismCatalog`.

