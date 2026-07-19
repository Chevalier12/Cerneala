# PrismBackdropDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismBackdropDefinition.cs`

Defines the immutable optional Prism plane that processes pixels physically behind an attached control.

```csharp
public sealed class PrismBackdropDefinition : PrismNodeDefinition
```

## Examples

```csharp
PrismBackdropDefinition backdrop = new(
    new PrismNodeId(20),
    "Glass",
    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
    sourceSpan: new PrismSourceSpan(72, 9, "Card.cui.xml"));
```

## Remarks

A composition may contain at most one backdrop, and it must be the last direct composition child. Backdrops have no fill, blend mode, clipping chain, or normal-stack children.

The optional inherited `SourceSpan` is immutable diagnostic metadata and does not participate in semantic equality or hashing.

## Constructors

| Name | Description |
| --- | --- |
| `PrismBackdropDefinition(PrismNodeId id, string? name, IEnumerable<PrismFilterDefinition>? filters = null, IEnumerable<PrismStyleDefinition>? styles = null, PrismMaskDefinition? mask = null, bool visible = true, float opacity = 1, PrismSourceSpan? sourceSpan = null)` | Initializes an immutable backdrop from catalog-backed defaults and optional source metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filters` | `ImmutableArray<PrismFilterDefinition>` | Gets backdrop filters in declared order. |
| `Styles` | `ImmutableArray<PrismStyleDefinition>` | Gets backdrop styles in declared order. |
| `Mask` | `PrismMaskDefinition?` | Gets the optional backdrop mask. |
| `Visible` | `bool` | Gets whether backdrop acquisition and processing participate. |
| `Opacity` | `float` | Gets complete processed backdrop opacity. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the optional authoring source location (inherited). |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismBackdropDefinition(...)` | `ArgumentException` | Both operation lists are empty or `name` is whitespace. |
| `PrismBackdropDefinition(...)` | `ArgumentOutOfRangeException` | `opacity` is non-finite or outside zero through one. |

## Applies to

Optional Prism backdrop processing.

## See also

- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphDiagnostic`
