# PrismCompositionDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismCompositionDefinition.cs`

Defines an immutable, structurally comparable Prism composition.

```csharp
public sealed class PrismCompositionDefinition : IEquatable<PrismCompositionDefinition>
```

## Examples

```csharp
PrismCompositionDefinition composition = new(
    "GlassCard",
    [
        new PrismLayerDefinition(
            new PrismNodeId(1),
            "Content",
            styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)]),
        new PrismBackdropDefinition(
            new PrismNodeId(2),
            "Glass",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
    ],
    sourceSpan: new PrismSourceSpan(0, 128, "Card.cui.xml"));
```

## Remarks

Nodes are declared front-to-back and evaluated bottom-up. The optional `Backdrop` remains a separate logical plane and is excluded from `EnumerateContentBottomUp()`. Node IDs are unique across the tree; optional names are unique in their address scope.

`SourceSpan` is immutable diagnostic metadata used when a graph failure cannot be attributed to a more specific node. It is deliberately excluded from semantic equality and hashing.

## Constructors

| Name | Description |
| --- | --- |
| `PrismCompositionDefinition(string name, IEnumerable<PrismNodeDefinition> nodes, PrismColorProfile workingColorProfile = LinearSrgb, float globalLightAngle = 120, float globalLightAltitude = 30, PrismSourceSpan? sourceSpan = null)` | Initializes and validates an immutable composition with optional source metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the composition name. |
| `Nodes` | `ImmutableArray<PrismNodeDefinition>` | Gets direct nodes in declaration order. |
| `WorkingColorProfile` | `PrismColorProfile` | Gets the working color profile. |
| `GlobalLightAngle` | `float` | Gets the shared light angle in degrees. |
| `GlobalLightAltitude` | `float` | Gets the shared light altitude in degrees. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the optional authoring source location. |
| `Backdrop` | `PrismBackdropDefinition?` | Gets the last direct node when it is a backdrop. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnumerateContentBottomUp()` | `IEnumerable<PrismNodeDefinition>` | Enumerates normal content from back to front, excluding the backdrop. |
| `TryGetNamedNode(string path, out PrismNodeId nodeId)` | `bool` | Resolves a validated dot-separated node address. |
| `ToDiagnosticString()` | `string` | Serializes a deterministic human-readable snapshot. |
| `Equals(PrismCompositionDefinition? other)` | `bool` | Tests structural equality. |
| `GetHashCode()` | `int` | Returns the structural hash code. |
| `ToString()` | `string` | Returns `ToDiagnosticString()`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismCompositionDefinition(...)` | `ArgumentException` | The name or node list is empty, IDs or scoped names are duplicated, or a backdrop is duplicated, nested, or not last. |
| `PrismCompositionDefinition(...)` | `ArgumentOutOfRangeException` | A light value is non-finite. |
| `TryGetNamedNode(...)` | `ArgumentException` | `path` is null, empty, or whitespace. |

## Applies to

Shared Prism definitions used by one or more `PrismInstance` objects.

## See also

- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphDiagnostic`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuildException`
