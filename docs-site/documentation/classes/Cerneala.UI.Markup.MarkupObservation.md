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
Concrete observations subscribe and unsubscribe through internal lifecycle
hooks. Consumers read `Value`; generated infrastructure attaches the internal
change event to condition controllers.

Internally, an observation distinguishes a resolved terminal value of `null`
from a temporarily unavailable path caused by a missing intermediate owner.
Generated binding infrastructure can write only when the current terminal
endpoint is resolved and writable. These resolution and write capabilities are
not exposed as general-purpose public setters.

## Properties
| Name | Description |
| --- | --- |
| `Value` | Current observed value. |

## Applies to
Compiled reactive markup.

## See Also
- `Cerneala.UI.Markup.GeneratedMarkup`
- `Cerneala.UI.Markup.MarkupDataPathSegment`
- `docs/markup-data-bindings.md`
