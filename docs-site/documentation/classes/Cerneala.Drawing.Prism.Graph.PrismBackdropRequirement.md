# PrismBackdropRequirement Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismBackdropRequirement.cs`

Identifies the analyzed Prism scopes that require backdrop input for the current frame.

```csharp
public sealed class PrismBackdropRequirement
```

Inheritance:
`object` -> `PrismBackdropRequirement`

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static void PrintBackdropScopes(PrismFrameAnalysis analysis)
{
    PrismBackdropRequirement? requirement = analysis.BackdropRequirement;
    if (requirement is null)
    {
        return;
    }

    foreach (int scopeIndex in requirement.ScopeIndices)
    {
        Console.WriteLine(scopeIndex);
    }
}
```

## Remarks

Instances are created by `PrismFrameAnalyzer`; the constructor is not public. `ScopeIndices` is immutable and follows analyzed scope encounter order.

A requirement is produced only for non-empty scopes whose runtime backdrop state is visible and has positive opacity. Multiple qualifying scopes are represented by one frame-level requirement.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ScopeIndices` | `System.Collections.Immutable.ImmutableArray<int>` | Gets the analyzed scope indices that need backdrop input. |
| `ScopeCount` | `int` | Gets the number of scope indices in the requirement. |

## Applies to

Cerneala backdrop-aware Prism frame hosting and composition.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameLease`
- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
