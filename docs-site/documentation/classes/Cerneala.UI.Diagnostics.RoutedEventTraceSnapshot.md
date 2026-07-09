# RoutedEventTraceSnapshot Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RoutedEventTrace.cs`

Represents an immutable diagnostic snapshot of the route that a routed event would take from a target element.

```csharp
public sealed record RoutedEventTraceSnapshot(
    string EventName,
    RoutingStrategy RoutingStrategy,
    string? TargetId,
    IReadOnlyList<RoutedEventTraceStep> Steps)
```

Inheritance:
`object` -> `RoutedEventTraceSnapshot`

## Examples

Trace a bubbling mouse event through the visual tree:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement parent = new();
UIElement child = new();

root.VisualChildren.Add(parent);
parent.VisualChildren.Add(child);

RoutedEventTraceSnapshot trace = RoutedEventTrace.Trace(child, InputEvents.MouseDownEvent);

RoutingStrategy strategy = trace.RoutingStrategy; // Bubble
string description = trace.ToString();            // MouseDown Bubble: UIElement#... -> UIElement#... -> UIRoot#...
```

## Remarks

`RoutedEventTraceSnapshot` is produced by `RoutedEventTrace.Trace`. The snapshot captures the routed event name, its `RoutingStrategy`, the target element id as a string, and the ordered route steps as `RoutedEventTraceStep` values.

The route order depends on the routed event strategy. `Direct` traces contain only the target element. `Bubble` traces run from the target toward the root. `Tunnel` traces run from the root toward the target. When the route walks ancestors, disabled ancestors are skipped in the same way as the input route.

`TargetId` is read from `UIElement.ElementId` and converted with `ToString()`. It is `null` when the target has no attached element id. Each step stores its element id string and runtime element type name.

`ToString()` formats the snapshot as the event name, routing strategy, and each step joined with ` -> `. Individual steps format unattached element ids as `unattached`.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedEventTraceSnapshot(string EventName, RoutingStrategy RoutingStrategy, string? TargetId, IReadOnlyList<RoutedEventTraceStep> Steps)` | Initializes a trace snapshot with the event name, routing strategy, target id, and ordered route steps. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `EventName` | `string` | Gets the routed event name captured from `RoutedEvent.Name`. |
| `RoutingStrategy` | `RoutingStrategy` | Gets the routing strategy captured from the routed event. |
| `TargetId` | `string?` | Gets the target element id string, or `null` when the target has no attached id. |
| `Steps` | `IReadOnlyList<RoutedEventTraceStep>` | Gets the ordered diagnostic route steps. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns a compact route description containing `EventName`, `RoutingStrategy`, and the formatted route steps. |

## Applies to

`Cerneala` retained UI diagnostics.

## See also

- `RoutedEventTrace`
- `RoutedEventTraceStep`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
