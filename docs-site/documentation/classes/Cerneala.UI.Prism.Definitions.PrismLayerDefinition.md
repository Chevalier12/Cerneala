# PrismLayerDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLayerDefinition.cs`

Defines an immutable leaf in the normal Prism layer stack.

```csharp
public sealed class PrismLayerDefinition : PrismNodeDefinition
```

## Examples

```csharp
PrismLayerDefinition layer = new(
    new PrismNodeId(1),
    "SoftGlow",
    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
    styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)],
    sourceSpan: new PrismSourceSpan(24, 7, "Card.cui.xml"));
```

## Remarks

A layer is always a leaf. Filters and styles are separate ordered collections, and `Mask` represents the optional single mask. The layer must contain at least one filter or style.

The optional inherited `SourceSpan` is immutable diagnostic metadata and does not participate in semantic equality or hashing.

## Constructors

| Name | Description |
| --- | --- |
| `PrismLayerDefinition(PrismNodeId id, string? name, IEnumerable<PrismFilterDefinition>? filters = null, IEnumerable<PrismStyleDefinition>? styles = null, PrismMaskDefinition? mask = null, bool visible = true, float opacity = 1, float fill = 1, PrismBlendMode blendMode = Normal, bool clipToBelow = false, PrismSourceSpan? sourceSpan = null)` | Initializes an immutable layer using catalog-backed defaults and optional source metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filters` | `ImmutableArray<PrismFilterDefinition>` | Gets filters in declared order. |
| `Styles` | `ImmutableArray<PrismStyleDefinition>` | Gets styles in declared order. |
| `Mask` | `PrismMaskDefinition?` | Gets the optional mask. |
| `Visible` | `bool` | Gets whether the complete layer participates. |
| `Opacity` | `float` | Gets opacity for content and styles together. |
| `Fill` | `float` | Gets filtered-content opacity before styles. |
| `BlendMode` | `PrismBlendMode` | Gets the layer blend mode. |
| `ClipToBelow` | `bool` | Gets whether the layer joins the clipping chain below it. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the optional authoring source location (inherited). |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismLayerDefinition(...)` | `ArgumentException` | Both operation lists are empty, `name` is whitespace, or `blendMode` is `PassThrough`. |
| `PrismLayerDefinition(...)` | `ArgumentOutOfRangeException` | `opacity` or `fill` is non-finite or outside zero through one. |

## Applies to

Normal Prism content stacks.

## See also

- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
- `Cerneala.UI.Prism.Runtime.PrismLayerState`
