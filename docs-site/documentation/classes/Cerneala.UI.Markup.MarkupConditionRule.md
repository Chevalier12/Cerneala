# MarkupConditionRule Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Combines a predicate with conditional property values and optional visual content.

```csharp
public sealed class MarkupConditionRule
```

## Examples
```csharp
var rule = new MarkupConditionRule(0, () => viewModel.IsReady);
```

## Remarks
Rules are evaluated in ascending `Order`. Values default to an empty list; content is optional. A `null` predicate is invalid.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionRule(int, Func<bool>, IReadOnlyList<MarkupConditionalValue>?, MarkupConditionalContent?)` | Creates one generated condition rule. |

## Properties
| Name | Description |
| --- | --- |
| `Order` | Evaluation and precedence order. |

## Applies to
Source-generated conditional markup.
