# AspectConditionKey Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`
Source: `UI/Aspect/AspectConditionKey.cs`

Identifies per-element condition state used by generated Aspect rules.

```csharp
public sealed class AspectConditionKey
```

## Examples
```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button card = new();
AspectConditionKey key = new("card.hover");
AspectCondition condition = AspectCondition.Signal(key);

key.SetActive(card, true);
bool matches = condition.Evaluate(new AspectMatchContext(card)).Matches;
```

## Remarks
State is stored independently for each `UIElement` through weak ownership. `SetActive` invalidates the element's Aspect work only when the Boolean state changes. `AspectCondition.Signal` reads the state during normal engine resolution, so generated observations update dependency state instead of writing styled properties.

Setting the state to `false` during behavior disposal removes the conditional rule on the next Aspect phase. Dead elements are not retained by the key.

## Constructors
| Name | Description |
| --- | --- |
| `AspectConditionKey(string name)` | Creates a condition key with a non-empty diagnostic name. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the diagnostic identity of the condition state. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `IsActive(UIElement element)` | `bool` | Returns the current state for `element`; missing state is `false`. |
| `SetActive(UIElement element, bool active)` | `bool` | Changes state, invalidates Aspect, and returns `true`; returns `false` for an equal state. |
| `ToString()` | `string` | Returns `Name`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentException` | `name` is null, empty, or whitespace. |
| `IsActive`, `SetActive` | `ArgumentNullException` | `element` is `null`. |

## Applies to
Generated Aspect conditions and engine dependency-state signaling.
