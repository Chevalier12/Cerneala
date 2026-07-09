# InputDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/InputDiagnostics.cs`

Captures a compact diagnostic snapshot of the current input hit target and optional routed event.

```csharp
public sealed record InputDiagnosticsSnapshot(
    string? HitTargetId,
    string? HitTargetType,
    string? RoutedEventName,
    RoutingStrategy? RoutingStrategy)
```

## Examples

```csharp
using Cerneala.UI.Diagnostics;

InputDiagnosticsSnapshot snapshot = InputDiagnostics.Capture(hitTarget, routedEvent);
string text = snapshot.ToString();
```

## Remarks

`InputDiagnosticsSnapshot` is returned by `InputDiagnostics.Capture`. It stores the hit target element id, hit target type name, routed event name, and routing strategy as nullable values so missing input context can be represented explicitly.

`ToString` formats the snapshot as a single diagnostic line. Missing hit target, routed event, or routing strategy values are rendered as `"none"`.

## Constructors

| Name | Description |
| --- | --- |
| `InputDiagnosticsSnapshot(string?, string?, string?, RoutingStrategy?)` | Initializes the snapshot with hit target and routed event details. |

## Properties

| Name | Description |
| --- | --- |
| `HitTargetId` | Gets the string form of the hit target element id, or `null` when no target is present. |
| `HitTargetType` | Gets the hit target runtime type name, or `null` when no target is present. |
| `RoutedEventName` | Gets the routed event name, or `null` when no event is present. |
| `RoutingStrategy` | Gets the routed event routing strategy, or `null` when no event is present. |

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns a compact diagnostic string for the captured input state. |

## Applies to

Cerneala retained UI input diagnostics.

## See also

- `Cerneala.UI.Diagnostics.InputDiagnostics`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Elements.UIElement`
