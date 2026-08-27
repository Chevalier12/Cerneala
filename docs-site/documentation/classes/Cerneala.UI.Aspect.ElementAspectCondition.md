# ElementAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`
Source: `UI/Aspect/ElementAspect.cs`

Groups one condition signal with the element-local declarations enabled by that signal.

```csharp
public sealed class ElementAspectCondition
```

## Examples
```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

Brush hoverBrush = new SolidColorBrush(Color.White);
AspectConditionKey hover = new("button.hover");
ElementAspectCondition condition = new(
    hover,
    [new ElementAspectValue(Control.BackgroundProperty, hoverBrush)],
    order: 0);
```

## Remarks
`ElementAspect` projects each condition into an `AspectRuleSet` on the runtime layer using `AspectCondition.Signal(Key)`. Values are immutable snapshots and duplicate properties within one condition are rejected. Higher `Order` participates in declaration ordering after the local source and target specificity coordinates.

The class stores declarations only. Observation, event, Motion, and detach behavior remains in generated lifecycle sidecars.

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspectCondition(AspectConditionKey key, IReadOnlyList<ElementAspectValue> values, int order)` | Creates an immutable conditional declaration group. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Key` | `AspectConditionKey` | Signal evaluated by the engine. |
| `Values` | `IReadOnlyList<ElementAspectValue>` | Immutable declarations enabled while `Key` is active. |
| `Order` | `int` | Local conditional declaration order. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `key` or `values` is `null`. |
| Constructor | `ArgumentException` | A value is `null` or the same UI property appears more than once. |

## Applies to
Named and inline generated `ElementAspect` rules.
