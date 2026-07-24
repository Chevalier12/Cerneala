# PrismCatalogValueKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Catalog/PrismCatalog.cs`

Identifies the CLR value family used by a Prism catalog parameter.

```csharp
public enum PrismCatalogValueKind
```

## Values

| Name | CLR type |
| --- | --- |
| `Boolean` | `bool` |
| `Integer` | `int` |
| `Number` | `float` |
| `Color` | `Cerneala.Drawing.Color` |
| `Vector` | `System.Numerics.Vector4` |
| `Symbol` | `string` |
| `Resource` | `PrismResourceId` |

## Examples

```csharp
if (parameter.ValueKind == PrismCatalogValueKind.Number)
{
    float value = filterState.GetValue<float>(parameter);
}
```

## Remarks

The generic type supplied to a Prism state accessor must match this mapping exactly.

## Applies to

`PrismCatalogParameterInfo`, `PrismFilterState`, and `PrismStyleState`.

