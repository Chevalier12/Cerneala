# PrismGraphDiagnostic Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphDiagnostic.cs`

Describes the composition, optional node, optional source location, and reason for a Prism graph build failure.

```csharp
public readonly record struct PrismGraphDiagnostic(
    string CompositionName,
    PrismNodeId? NodeId,
    string? NodeName,
    PrismSourceSpan? SourceSpan,
    string Message);
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

PrismGraphDiagnostic diagnostic = new(
    "GlassCard",
    new PrismNodeId(3),
    "BlurredContent",
    new PrismSourceSpan(24, 7, "Card.cui.xml"),
    "Unsupported blend mode.");

Console.WriteLine(diagnostic);
```

## Remarks

Composition-wide failures leave `NodeId` and `NodeName` null. Node-specific failures prefer the node source span and fall back to the composition source span when available.

`ToString()` formats the same contextual message used by `PrismGraphBuildException`, including source text such as `Card.cui.xml@24+7` when a span is present.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphDiagnostic(string compositionName, PrismNodeId? nodeId, string? nodeName, PrismSourceSpan? sourceSpan, string message)` | Creates an immutable graph-build diagnostic. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CompositionName` | `string` | Gets the composition name. |
| `NodeId` | `PrismNodeId?` | Gets the failing definition node ID, if known. |
| `NodeName` | `string?` | Gets the failing node name, if present. |
| `SourceSpan` | `PrismSourceSpan?` | Gets the best available source location. |
| `Message` | `string` | Gets the underlying failure reason. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Formats a contextual graph-build failure message. |

## Applies to

Cerneala Prism graph build failures and authoring diagnostics.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphBuildException`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
