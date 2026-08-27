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
var assignment = new ElementAspectValue(UIElement.OpacityProperty, 0.75f);
```

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspectValue(UiProperty, object?)` | Creates one property assignment and validates the value against the UI property contract. |
| `ElementAspectValue(UiProperty, AspectValue)` | Creates a deferred/token/computed assignment whose value type matches the UI property. |

## Properties
| Name | Description |
| --- | --- |
| `Property` | Target UI property. |
| `Value` | Assigned value. |
| `DynamicValue` | Optional `AspectValue` used when resolution is deferred; otherwise `null`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ElementAspectValue(UiProperty, object?)` | `ArgumentNullException` | `property` is `null`. |
| `ElementAspectValue(UiProperty, object?)` | `ArgumentException` | `value` is not valid for the supplied property. |
| `ElementAspectValue(UiProperty, AspectValue)` | `ArgumentException` | The Aspect value type does not match the UI property value type. |

## Applies to
Aspect defaults, conditions, and generated markup.
