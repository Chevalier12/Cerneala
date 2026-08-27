# RejectedAspectDeclaration Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/RejectedAspectDeclaration.cs`

Represents an aspect declaration that lost cascade resolution to another declaration.

```csharp
public sealed class RejectedAspectDeclaration
```

Inheritance:
`object` -> `RejectedAspectDeclaration`

## Examples

Inspect rejected declarations after resolving aspects for an element:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectEngine engine = new();
ResolvedAspect resolved = engine.Resolve(button, catalog, environment);

foreach (RejectedAspectDeclaration rejected in resolved.RejectedDeclarations)
{
    string name = rejected.Rejected.DiagnosticName ?? rejected.Rejected.Property.Name;
    string reason = rejected.Reason;
}
```

## Remarks

`RejectedAspectDeclaration` is diagnostic data produced by `AspectEngine.Resolve` when multiple matching aspect declarations target the same `UiProperty`. The instance stores both catalog rule projections, both declarations, and a short reason string.

The engine creates rejected entries when cascade keys are compared. A new declaration can reject the current winner with the reason `Higher cascade key won.`, or the current winner can reject the new declaration with the reason `Existing cascade key won.`.

`AspectEngine.Apply` includes these entries in the `ResolvedAspect.RejectedDeclarations` collection stored in diagnostics. `AspectTrace.Capture` formats rejected declarations by using the rejected declaration's `DiagnosticName` when present, falling back to the rejected property name, and appending the rejection reason.

The constructor requires non-null `rejected` and `winningDeclaration` values. A null `reason` is accepted and stored as an empty string.

## Constructors

| Name | Description |
| --- | --- |
| `RejectedAspectDeclaration(AspectRuleSet rejectedRule, AspectDeclaration rejected, AspectRuleSet winningRule, AspectDeclaration winningDeclaration, string reason)` | Initializes a rejected declaration record with losing/winning rules and declarations plus the reason. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RejectedRule` | `AspectRuleSet` | Gets the rule projection that supplied the losing declaration. |
| `Rejected` | `AspectDeclaration` | Gets the declaration that lost cascade resolution. |
| `WinningRule` | `AspectRuleSet` | Gets the rule projection that supplied the winner. |
| `WinningDeclaration` | `AspectDeclaration` | Gets the declaration that won for the same property. |
| `Reason` | `string` | Gets the reason recorded for the rejection. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | Any rule or declaration argument is `null`. |

## Applies to

Cerneala UI aspect cascade resolution diagnostics.

## See also

- `AspectDeclaration`
- `AspectEngine`
- `ResolvedAspect`
- `AspectTrace`
