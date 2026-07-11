# MarkupConditionalValue Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Describes a UI property assignment applied while a generated markup condition is active.

```csharp
public sealed class MarkupConditionalValue
```

## Examples
```csharp
var value = new MarkupConditionalValue(element, OpacityProperty, 0.5f, UiPropertyValueSource.MarkupBase);
```

## Remarks
This is infrastructure for generated markup condition controllers. Target, property, value, and source are consumed internally when the rule becomes active.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupConditionalValue(UiObject, UiProperty, object?, UiPropertyValueSource)` | Creates one conditional assignment. |

## Applies to
Source-generated conditional markup.
