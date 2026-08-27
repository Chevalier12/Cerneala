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

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?)` | Creates one generated condition rule. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?)` | Creates a rule with optional activation and deactivation callbacks. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?, Action<bool>?)` | Creates a rule with transition callbacks and an optional condition-state notifier. |

## Properties
| Name | Description |
| --- | --- |
| `Order` | Evaluation and precedence order. |

## Applies to
Source-generated conditional markup.
