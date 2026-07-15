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
deactivation callback runs when that active rule exits or its owner detaches. A
`null` predicate is invalid.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?)` | Creates one generated condition rule. |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?, Action?, Action?)` | Creates a rule with optional activation and deactivation callbacks. |

## Properties
| Name | Description |
| --- | --- |
| `Order` | Evaluation and precedence order. |

## Applies to
Source-generated conditional markup.
