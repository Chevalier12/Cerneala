# PrismGroupDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismGroupDefinition.cs`

Defines an immutable Prism container for layers and nested groups.

```csharp
public sealed class PrismGroupDefinition : PrismNodeDefinition
```

## Examples

```csharp
PrismGroupDefinition group = new(
    new PrismNodeId(10),
    "Effects",
    [
        new PrismLayerDefinition(
            new PrismNodeId(11),
            "Content",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
    ],
    sourceSpan: new PrismSourceSpan(40, 12, "Card.cui.xml"));
```

## Remarks

Only groups may contain normal-stack children. `Children` preserves declaration order; `EnumerateChildrenBottomUp()` traverses it in evaluation order without moving or duplicating nodes. The default blend mode is `PassThrough`.

The optional inherited `SourceSpan` is immutable diagnostic metadata and does not participate in semantic equality or hashing.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGroupDefinition(PrismNodeId id, string? name, IEnumerable<PrismNodeDefinition> children, IEnumerable<PrismFilterDefinition>? filters = null, IEnumerable<PrismStyleDefinition>? styles = null, PrismMaskDefinition? mask = null, bool visible = true, float opacity = 1, PrismBlendMode blendMode = PassThrough, PrismSourceSpan? sourceSpan = null)` | Initializes a nonempty immutable group with optional source metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `ImmutableArray<PrismNodeDefinition>` | Gets layer and nested-group children in declaration order. |
| `Filters` | `ImmutableArray<PrismFilterDefinition>` | Gets filters applied to the prepared group. |
| `Styles` | `ImmutableArray<PrismStyleDefinition>` | Gets styles applied to the prepared group. |
| `Mask` | `PrismMaskDefinition?` | Gets the optional group mask. |
| `Visible` | `bool` | Gets whether the group and its subtree participate. |
| `Opacity` | `float` | Gets complete group opacity. |
| `BlendMode` | `PrismBlendMode` | Gets pass-through or isolated group blending. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the optional authoring source location (inherited). |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnumerateChildrenBottomUp()` | `IEnumerable<PrismNodeDefinition>` | Enumerates the last declared child first. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismGroupDefinition(...)` | `ArgumentException` | `children` is empty, contains a backdrop or unsupported node, or `name` is whitespace. |
| `PrismGroupDefinition(...)` | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside zero through one. |

## Applies to

Nested normal Prism layer stacks.

## See also

- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
- `Cerneala.UI.Prism.Runtime.PrismGroupState`
