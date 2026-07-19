# PrismNodeDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismNodeDefinition.cs`

Defines the immutable base contract for a Prism layer, group, or backdrop node.

```csharp
public abstract class PrismNodeDefinition : IEquatable<PrismNodeDefinition>
```

Inheritance:
`object` -> `PrismNodeDefinition`

Derived types:
`PrismLayerDefinition`, `PrismGroupDefinition`, `PrismBackdropDefinition`

## Examples

```csharp
PrismLayerDefinition layer = new(
    new PrismNodeId(1),
    "Content",
    styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)],
    sourceSpan: new PrismSourceSpan(24, 7, "Card.cui.xml"));
```

## Remarks

`Id` is the runtime identity used by dense instance state. `Name` is an optional Motion and diagnostics address; it never becomes an arbitrary image source.

`SourceSpan` is immutable diagnostic metadata supplied to a concrete node constructor. It is deliberately excluded from semantic equality and hashing, so the same Prism definition authored at different source locations remains structurally equal.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `PrismNodeId` | Gets the stable numeric node identifier. |
| `Name` | `string?` | Gets the optional addressable node name. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the optional authoring source location. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(PrismNodeDefinition? other)` | `bool` | Tests structural equality with another node definition. |
| `Equals(object? obj)` | `bool` | Tests structural equality with an object. |
| `GetHashCode()` | `int` | Returns the structural hash code. |

## Applies to

Immutable Prism composition trees.

## See also

- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
- `Cerneala.UI.Prism.Definitions.PrismCompositionDefinition`
