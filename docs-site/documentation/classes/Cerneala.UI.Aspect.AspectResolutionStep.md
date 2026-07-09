# AspectResolutionStep Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectResolutionStep.cs`

Represents one diagnostic step recorded while aspect resolution explains matched rules or rejected declarations.

```csharp
public sealed record AspectResolutionStep(
    string PackageName,
    string RuleName,
    string Target,
    AspectLayer Layer,
    AspectSpecificity Specificity,
    int DeclarationOrder,
    string Outcome);
```

Inheritance:
`object` -> `AspectResolutionStep`

## Examples

Print the recorded aspect resolution steps for an element:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectEngine engine = new();
Button button = new();

engine.Apply(
    button,
    new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog(),
    DefaultAspectPackage.CreateEnvironment());

AspectDiagnostics.Snapshot diagnostics = engine.GetDiagnostics(button);

foreach (AspectResolutionStep step in diagnostics.ResolutionSteps)
{
    Console.WriteLine(
        $"{step.PackageName} {step.RuleName} {step.Target} " +
        $"{step.Layer} {step.Specificity} order={step.DeclarationOrder} {step.Outcome}");
}
```

## Remarks

`AspectResolutionStep` is a diagnostics record, not the resolved aspect value itself. `AspectEngine.Apply` stores these steps in `AspectDiagnostics.Snapshot.ResolutionSteps` after applying a `ResolvedAspect` to an element.

For matched rules, `AspectEngine` records the package name, rule name, target text, layer, target specificity, declaration order, and the outcome string `matched`. For rejected declarations, it records the rejected declaration name or property name, the target property's diagnostic name, `AspectLayer.Reset`, default `AspectSpecificity`, declaration order `0`, and an outcome string prefixed with `rejected:`.

`AspectTrace.Capture` consumes these records to produce human-readable trace lines. The record is immutable after construction and uses normal C# record value equality.

## Constructors

| Name | Description |
| --- | --- |
| `AspectResolutionStep(string PackageName, string RuleName, string Target, AspectLayer Layer, AspectSpecificity Specificity, int DeclarationOrder, string Outcome)` | Initializes a diagnostic resolution step with package, rule, target, cascade metadata, declaration order, and outcome text. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PackageName` | `string` | Gets the package name associated with the matched rule, or an empty string for rejected declaration steps. |
| `RuleName` | `string` | Gets the matched rule name or rejected declaration diagnostic name. |
| `Target` | `string` | Gets the target description recorded for the matched rule or rejected property. |
| `Layer` | `AspectLayer` | Gets the aspect layer used by the matched rule; rejected declaration steps use `AspectLayer.Reset`. |
| `Specificity` | `AspectSpecificity` | Gets the target specificity for the matched rule; rejected declaration steps use the default specificity value. |
| `DeclarationOrder` | `int` | Gets the declaration order recorded for cascade diagnostics. |
| `Outcome` | `string` | Gets the outcome text, such as `matched` or a rejection reason. |

## Applies to

Cerneala UI aspect diagnostics produced by `AspectEngine.Apply` and rendered by `AspectTrace`.

## See also

- `AspectDiagnostics`
- `AspectEngine`
- `AspectTrace`
- `AspectLayer`
- `AspectSpecificity`
