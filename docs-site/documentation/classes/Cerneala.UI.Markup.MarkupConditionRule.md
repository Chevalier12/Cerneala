# MarkupConditionRule Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Combines a predicate with conditional property values, optional visual content,
and optional branch-transition callbacks.

```csharp
public sealed class MarkupConditionRule
```

## Examples
```csharp
var rule = new MarkupConditionRule(0, () => viewModel.IsReady);
```

## Remarks
Rules are evaluated in ascending `Order`. Values default to an empty list;
content and transition callbacks are optional. The activation callback runs only
when an attached owner enters the rule, not on unchanged reevaluation. The
deactivation callback runs when that active rule exits or its owner detaches. The optional condition-state callback receives `true` and `false` transitions independently of render-gated Motion activation, allowing generated Aspect rules to update `AspectConditionKey` state without writing styled properties. A
`null` predicate is invalid.

The optional sound-activation callback is the audio sidecar of generated Timbre
actions. It runs once per true interval of an attached owner, independently of
renderability: an initially true rule under a hidden or collapsed owner or
ancestor starts its sound, a true-to-false change does not stop audio and only
re-arms the rule, and hiding then showing an owner with an unchanged true rule
does not run it again. The visual activation keeps its renderability gating.
When both are due in the same evaluation the controller passes the visual
activation to the sound callback, which invokes it at the Motion position so a
mixed Timbre command/Motion body runs in source order; otherwise it receives `null`.
The initial activation after attach is deferred through the root Relay for both
parts, and detach clears the audio state so reattaching activates again.

```csharp
var rule = new MarkupConditionRule(
    0,
    () => viewModel.HasError,
    values: null,
    content: null,
    activated: null,
    deactivated: null,
    conditionStateChanged: null,
    timbreActivated: visual =>
    {
        GeneratedMarkup.PlayTimbre(border, "Error");
        visual?.Invoke();
    });
```

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?)` | Creates one generated condition rule. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?)` | Creates a rule with optional activation and deactivation callbacks. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?, Action<bool>?)` | Creates a rule with transition callbacks and an optional condition-state notifier. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?, Action<bool>?, Action<Action?>?)` | Also takes the renderability-independent sound activation, which receives the visual activation when it is due in the same evaluation. |

## Properties
| Name | Description |
| --- | --- |
| `Order` | Evaluation and precedence order. |

## Applies to
Source-generated conditional markup.
