# PrismStyleDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismStyleDefinition.cs`

Defines one immutable built-in layer-style occurrence in a Prism scope.

```csharp
public sealed class PrismStyleDefinition : IEquatable<PrismStyleDefinition>
```

## Remarks

The definition stores the stable built-in style identifier and its initial
visibility. Typed catalog parameters are held by the corresponding per-instance
style state and are exposed to generated markup accessors.

## Constructors

| Name | Description |
| --- | --- |
| `PrismStyleDefinition(PrismStyleId style, bool visible = true)` | Initializes a style occurrence from the catalog-backed visibility default. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Style` | `PrismStyleId` | Gets the built-in style identifier. |
| `Visible` | `bool` | Gets whether the style participates in evaluation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(PrismStyleDefinition? other)` | `bool` | Tests structural equality. |
| `GetHashCode()` | `int` | Returns the structural hash code. |

## Applies to

Layer, group, and backdrop style lists.
