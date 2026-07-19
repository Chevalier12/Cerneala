# PrismGraphBuildException Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphDiagnostic.cs`

Represents a contextual failure to build a retained Prism composition graph.

```csharp
public sealed class PrismGraphBuildException : InvalidOperationException
```

Inheritance:
`object` -> `Exception` -> `SystemException` -> `InvalidOperationException` -> `PrismGraphBuildException`

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static PrismGraph? TryBuild(PrismFrameAnalysis analysis)
{
    try
    {
        return new PrismGraphBuilder().Build(analysis);
    }
    catch (PrismGraphBuildException exception)
    {
        Console.WriteLine(exception.Diagnostic.SourceSpan);
        return null;
    }
}
```

## Remarks

The exception message is initialized from `Diagnostic.ToString()`. A stale frame analysis is wrapped as a composition-level diagnostic; invalid node state is reported with the definition node identity and the best available node or composition source span.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphBuildException(PrismGraphDiagnostic diagnostic, Exception? innerException = null)` | Initializes the exception from a contextual diagnostic and optional underlying exception. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Diagnostic` | `PrismGraphDiagnostic` | Gets the structured graph-build diagnostic. |

## Applies to

Cerneala retained Prism graph construction and authoring diagnostics.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphDiagnostic`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.UI.Prism.Definitions.PrismSourceSpan`
