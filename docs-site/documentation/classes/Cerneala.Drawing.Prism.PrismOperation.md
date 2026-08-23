# PrismOperation Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismOperation.cs`

Provides the common catalog metadata and change version for a typed Prism filter or style.

```csharp
public abstract class PrismOperation
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CatalogInfo` | `PrismCatalogOperationInfo` | Gets the catalog entry that defines the operation and its parameters. |
| `Version` | `long` | Gets the operation change version. |

## Remarks

Concrete operation classes are generated from `prism-catalog.json`. Filter names use the `{CatalogSymbol}Filter` form, such as `BlurFilter`; style names use `{CatalogSymbol}Style`, such as `OuterGlowStyle`. Their public properties are strongly typed and validated against the catalog domain when assigned.

## See Also

- `PrismFilter`
- `PrismStyle`
- `PrismCatalog`
- `PrismPipeline`
