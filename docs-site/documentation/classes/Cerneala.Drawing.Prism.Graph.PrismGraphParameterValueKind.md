# PrismGraphParameterValueKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Identifies which typed value is populated in a `PrismGraphParameter` snapshot.

```csharp
public enum PrismGraphParameterValueKind
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static float? ReadNumber(PrismGraphParameter parameter)
{
    return parameter.Kind == PrismGraphParameterValueKind.Number
        ? parameter.NumberValue
        : null;
}
```

## Remarks

The kind comes from the generated Prism catalog property descriptor. Consumers must read the matching value property; unrelated value properties contain their default value.

## Fields

| Name | Matching property |
| --- | --- |
| `Boolean` | `BooleanValue` |
| `Integer` | `IntegerValue` |
| `Number` | `NumberValue` |
| `Color` | `ColorValue` |
| `Vector` | `VectorValue` |
| `Symbol` | `IntegerValue` |
| `Resource` | `ResourceValue` |

## Applies to

Cerneala Prism filter and style parameter snapshots.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphParameter`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
