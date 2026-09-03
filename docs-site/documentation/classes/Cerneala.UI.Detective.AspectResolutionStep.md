# AspectResolutionStep Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/AspectResolutionStep.cs`

Captures one considered rule with its immutable origin, scope, cascade coordinates, condition results, dependencies, and outcome.

```csharp
public sealed record AspectResolutionStep
```

## Examples

```csharp
AspectDiagnostics.Snapshot diagnostics = root.Detective.CaptureAspect(button);

foreach (AspectResolutionStep step in diagnostics.ResolutionSteps)
{
    Console.WriteLine(
        $"{step.Origin.Document} {step.Origin.Kind} {step.Scope} " +
        $"{step.PackageName}/{step.RuleName}: {step.Outcome}");
}
```

## Remarks

`AspectEngine.Apply` captures one step for every catalog rule. Type and slot mismatches are recorded without evaluating conditions. Structurally matching rules retain the exact `AspectConditionResult` data used for matching; diagnostics never invoke a predicate a second time.

`Layer`, `SourceOrder`, `Specificity`, and `DeclarationOrder` are the canonical cascade coordinates in comparison order. `Origin` explains the code/markup document and named/inline form but does not affect the winner. `Scope` is a deterministic label (`root`, `application`, `scope[n]`, or `element`).

Public steps are materialized lazily on the first `Detective.CaptureAspect` call after an apply.

## Constructors

| Name | Description |
| --- | --- |
| `AspectResolutionStep(string packageName, string ruleName, string target, AspectLayer layer, AspectSpecificity specificity, int declarationOrder, int sourceOrder, AspectOrigin origin, string scope, IReadOnlyList<AspectConditionTrace> conditions, IReadOnlyList<AspectConditionDependency> dependencies, string outcome)` | Creates a complete immutable resolution step. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PackageName` | `string` | Gets the contributing package name. |
| `RuleName` | `string` | Gets the considered rule name. |
| `Target` | `string` | Gets the target type/slot description. |
| `Layer` | `AspectLayer` | Gets the first cascade coordinate. |
| `Specificity` | `AspectSpecificity` | Gets the third cascade coordinate. |
| `DeclarationOrder` | `int` | Gets the final cascade coordinate. |
| `SourceOrder` | `int` | Gets the root/application/scope/element source rank. |
| `Origin` | `AspectOrigin` | Gets immutable authoring metadata. |
| `Scope` | `string` | Gets the deterministic runtime scope label. |
| `Conditions` | `IReadOnlyList<AspectConditionTrace>` | Gets captured top-level condition traces. |
| `Dependencies` | `IReadOnlyList<AspectConditionDependency>` | Gets the condition dependencies captured for this rule. |
| `Outcome` | `string` | Gets `matched` or a deterministic structural/condition rejection reason. |

## Applies to

Canonical Aspect diagnostics for code-first and generated markup rules.

## See also

- `AspectOrigin`
- `AspectConditionTrace`
- `AspectDiagnostics`
- `AspectTrace`
