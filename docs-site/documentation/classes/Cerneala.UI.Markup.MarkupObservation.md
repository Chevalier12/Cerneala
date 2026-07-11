# MarkupObservation Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Base class for generated markup observations that publish value changes.

```csharp
public abstract class MarkupObservation
```

## Examples
```csharp
MarkupObservation observation = GeneratedMarkup.ObserveObject(() => viewModel.IsReady);
object? current = observation.Value;
```

## Remarks
Concrete observations subscribe and unsubscribe through internal lifecycle hooks. Consumers read `Value`; generated infrastructure attaches the internal change event to condition controllers.

## Properties
| Name | Description |
| --- | --- |
| `Value` | Current observed value. |

## Applies to
Compiled reactive markup.
