# MarkupConditionalContent Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Describes lazily-created visual children controlled by a generated markup condition.

```csharp
public sealed class MarkupConditionalContent
```

## Examples
```csharp
var content = new MarkupConditionalContent(10, () => [new TextBlock { Text = "Visible" }]);
```

## Remarks
Children are created at most once and cached. Optional activation and deactivation callbacks let generated code update lifecycle state without exposing the controller.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionalContent(int, Func<IReadOnlyList<UIElement>>)` | Creates content without callbacks. |
| `MarkupConditionalContent(int, Func<IReadOnlyList<UIElement>>, Action?, Action?)` | Creates content with activation callbacks. |

## Properties
| Name | Description |
| --- | --- |
| `Order` | Stable source-order key used by the controller. |

## Applies to
Source-generated conditional markup.
