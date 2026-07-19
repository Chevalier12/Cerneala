# PrismGraphParameter Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Stores one immutable, typed filter or style parameter snapshot.

```csharp
public readonly record struct PrismGraphParameter
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static void PrintNumbers(PrismGraphNode filterNode)
{
    foreach (PrismGraphParameter parameter in filterNode.Parameters)
    {
        if (parameter.Kind == PrismGraphParameterValueKind.Number)
        {
            Console.WriteLine(parameter.NumberValue);
        }
    }
}
```

## Remarks

Meaningful snapshots are emitted by `PrismGraphBuilder`; the value-populating constructor is internal. `Index` is the generated catalog property position. `Kind` identifies the one matching typed value property; all other value properties remain at their default value.

Symbols use `IntegerValue`. Resource parameters use `ResourceValue` and also contribute a `Resource` graph dependency when the resource ID is positive.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Index` | `int` | Gets the zero-based property index in the generated catalog entry. |
| `Kind` | `PrismGraphParameterValueKind` | Gets the populated value kind. |
| `BooleanValue` | `bool` | Gets a Boolean value when `Kind` is `Boolean`. |
| `IntegerValue` | `int` | Gets an integer or symbol value when selected by `Kind`. |
| `NumberValue` | `float` | Gets a numeric value when `Kind` is `Number`. |
| `ColorValue` | `Color` | Gets a color value when `Kind` is `Color`. |
| `VectorValue` | `System.Numerics.Vector4` | Gets a vector value when `Kind` is `Vector`. |
| `ResourceValue` | `PrismResourceId` | Gets a resource identifier when `Kind` is `Resource`. |

## Applies to

Cerneala retained Prism filter and style operation snapshots.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphParameterValueKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
