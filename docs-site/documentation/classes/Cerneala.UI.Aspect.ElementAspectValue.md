# ElementAspectValue Class

## Definition
Namespace: `Cerneala.UI.Aspect`  
Assembly/Project: `Cerneala`  
Source: `UI/Aspect/ElementAspect.cs`

Pairs a UI property with the value assigned by an `ElementAspect`.

```csharp
public sealed class ElementAspectValue
```

## Examples
```csharp
var assignment = new ElementAspectValue(Control.OpacityProperty, 0.75f);
```

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspectValue(UiProperty, object?)` | Creates one property assignment. |

## Properties
| Name | Description |
| --- | --- |
| `Property` | Target UI property. |
| `Value` | Assigned value. |

## Applies to
Aspect defaults, conditions, and generated markup.
