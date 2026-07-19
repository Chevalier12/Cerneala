# PrismFilterDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismFilterDefinition.cs`

Defines one immutable built-in filter occurrence in a Prism scope.

```csharp
public sealed class PrismFilterDefinition : IEquatable<PrismFilterDefinition>
```

## Examples

```csharp
PrismFilterDefinition blur = new(
    PrismFilterId.Blur,
    opacity: 0.8f,
    blendMode: PrismBlendMode.Normal);
```

## Constructors

| Name | Description |
| --- | --- |
| `PrismFilterDefinition(PrismFilterId filter, bool visible = true, float opacity = 1, PrismBlendMode blendMode = Normal)` | Initializes a filter occurrence from catalog-backed defaults and optional common overrides. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filter` | `PrismFilterId` | Gets the built-in filter identifier. |
| `Visible` | `bool` | Gets whether the filter participates in evaluation. |
| `Opacity` | `float` | Gets the filtered-result mix from zero through one. |
| `BlendMode` | `PrismBlendMode` | Gets the per-filter blend mode. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(PrismFilterDefinition? other)` | `bool` | Tests structural equality. |
| `GetHashCode()` | `int` | Returns the structural hash code. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismFilterDefinition(...)` | `ArgumentException` | `blendMode` is `PassThrough`. |
| `PrismFilterDefinition(...)` | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside zero through one. |

## Applies to

Layer, group, and backdrop filter lists.
