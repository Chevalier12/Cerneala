# ElementAspect Class

## Definition
Namespace: `Cerneala.UI.Aspect`  
Assembly/Project: `Cerneala`  
Source: `UI/Aspect/ElementAspect.cs`

Immutable collection of default property assignments applied to an element aspect.

```csharp
public sealed class ElementAspect
```

## Examples
```csharp
var aspect = new ElementAspect([
    new ElementAspectValue(Control.OpacityProperty, 0.8f)
]);
```

## Remarks
Assignments are copied and a property may appear only once. `IsConditional` marks aspects whose values are evaluated by the aspect engine.

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspect(IReadOnlyList<ElementAspectValue>, bool)` | Creates an aspect; conditional mode defaults to `false`. |

## Properties
| Name | Description |
| --- | --- |
| `DefaultValues` | Copied property assignments. |
| `IsConditional` | Whether condition processing is required. |

## Applies to
Modern aspect application and generated markup defaults.
