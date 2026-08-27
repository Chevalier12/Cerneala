# AspectConditionTrace Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionTrace.cs`

Represents the captured result and dependencies of one condition evaluation in an Aspect resolution trace.

```csharp
public sealed class AspectConditionTrace
```

## Examples

```csharp
foreach (AspectConditionTrace condition in step.Conditions)
{
    Console.WriteLine($"{condition.DiagnosticText}: {condition.Matches}");
}
```

## Remarks

The engine creates traces from the exact `AspectConditionResult` used for matching. Diagnostics do not reevaluate predicates. Dependency and child collections are copied into immutable snapshots.

## Constructors

| Name | Description |
| --- | --- |
| `AspectConditionTrace(bool matches, string diagnosticText, IReadOnlyList<AspectConditionDependency> dependencies, IReadOnlyList<AspectConditionTrace>? children = null)` | Creates a captured condition trace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Matches` | `bool` | Gets the captured match result. |
| `DiagnosticText` | `string` | Gets the condition's diagnostic description. |
| `Dependencies` | `IReadOnlyList<AspectConditionDependency>` | Gets the immutable dependency snapshot. |
| `Children` | `IReadOnlyList<AspectConditionTrace>` | Gets traces for compound child conditions. |

## Applies to

Aspect resolution diagnostics.

## See also

- `AspectConditionResult`
- `AspectResolutionStep`
- `AspectDiagnostics`
