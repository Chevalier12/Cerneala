# ElementAspect Class

## Definition
Namespace: `Cerneala.UI.Aspect`  
Assembly/Project: `Cerneala`  
Source: `UI/Aspect/ElementAspect.cs`

Collection of default property assignments applied to an element aspect, with incremental updates for existing assignments.

```csharp
public sealed class ElementAspect
```

## Examples
```csharp
var aspect = new ElementAspect([
    new ElementAspectValue(Control.OpacityProperty, 0.8f)
]);

button.Aspect = aspect;
aspect.SetValue(Control.OpacityProperty, 0.6f);
```

## Remarks
Assignments are copied and exposed through a read-only list, and a property may appear only once. `SetValue` adds or replaces one assignment in place and immediately propagates only that UI property to every element currently using the aspect. Setting an equal value is a no-op.

`IsConditional` marks aspects whose values are evaluated by the aspect engine.

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspect(IReadOnlyList<ElementAspectValue>, bool)` | Creates an aspect; conditional mode defaults to `false`. |

## Properties
| Name | Description |
| --- | --- |
| `DefaultValues` | Copied property assignments. |
| `IsConditional` | Whether condition processing is required. |

## Methods
| Name | Description |
| --- | --- |
| `SetValue(UiProperty, object?)` | Adds or replaces one assignment and returns `true`; returns `false` when the value is unchanged. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `SetValue` | `ArgumentNullException` | `property` is `null`. |
| `SetValue` | `ArgumentException` | `value` is invalid for the UI property. |

## Applies to
Modern aspect application and generated markup defaults.
