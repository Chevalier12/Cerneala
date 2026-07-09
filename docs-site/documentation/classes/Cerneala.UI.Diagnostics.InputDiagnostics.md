# InputDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/InputDiagnostics.cs`

Captures a compact diagnostic snapshot for an input hit target and optional routed event.

```csharp
public static class InputDiagnostics
```

Inheritance:
`Object` -> `InputDiagnostics`

## Examples

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement hitTarget = new();

InputDiagnosticsSnapshot snapshot = InputDiagnostics.Capture(
    hitTarget,
    InputEvents.MouseDownEvent);

string line = snapshot.ToString();
```

## Remarks

`InputDiagnostics` is a static helper for producing `InputDiagnosticsSnapshot` values from the current input context. `Capture` reads the target element id, target runtime type name, routed event name, and routing strategy without mutating the element or event.

The `hitTarget` and `routedEvent` arguments are optional. When no hit target is supplied, the returned snapshot has `null` target id and target type values. When no routed event is supplied, the returned snapshot has `null` routed event name and routing strategy values. `InputDiagnosticsSnapshot.ToString()` renders those missing values as `"none"`.

## Methods

| Name | Description |
| --- | --- |
| `Capture(UIElement?, RoutedEvent?)` | Creates an `InputDiagnosticsSnapshot` from the supplied hit target and optional routed event. |

## Applies to

Cerneala retained UI input diagnostics.

## See also

- `Cerneala.UI.Diagnostics.InputDiagnosticsSnapshot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.InputEvents`
- `Cerneala.UI.Input.RoutedEvent`
