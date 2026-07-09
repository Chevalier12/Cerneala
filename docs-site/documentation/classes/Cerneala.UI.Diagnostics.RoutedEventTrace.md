# RoutedEventTrace Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RoutedEventTrace.cs`

Builds an immutable diagnostic snapshot of the element route that a routed event would follow from a target element.

```csharp
public static class RoutedEventTrace
```

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

// MouseDown Bubble: UIElement#... -> UIElement#... -> UIRoot#...
string route = trace.ToString();
```

Trace a tunneling event by using one of the preview routed events:

```csharp
RoutedEventTraceSnapshot trace = RoutedEventTrace.Trace(child, InputEvents.PreviewMouseDownEvent);

IReadOnlyList<RoutedEventTraceStep> steps = trace.Steps;
RoutingStrategy strategy = trace.RoutingStrategy;
```

## Remarks

`RoutedEventTrace` is a diagnostics helper. It does not raise the event or call handlers; it only calculates the route and packages the result into `RoutedEventTraceSnapshot`.

The route is based on the supplied `RoutedEvent.RoutingStrategy`:

| Routing strategy | Trace order |
| --- | --- |
| `Direct` | Contains only `target`. |
| `Bubble` | Starts at `target`, then walks enabled ancestors toward the root. |
| `Tunnel` | Starts at the highest enabled ancestor, then walks down to `target`. |

Ancestor traversal uses `ElementTreeWalker.Ancestors(target, role)`. The default `role` is `ElementChildRole.Visual`; pass `ElementChildRole.Logical` to trace through logical parents instead. Disabled ancestors are skipped because the trace filters ancestors where `IsEnabled` is `true`. The target itself is always included after the null checks.

Each trace step stores the element identifier as `element.ElementId?.ToString()` and the runtime element type name from `element.GetType().Name`. If an element has no `ElementId`, `RoutedEventTraceStep.ToString()` renders that element as `ElementType#unattached`.

`Trace` throws `ArgumentNullException` when `target` or `routedEvent` is `null`. It throws `InvalidOperationException` if the routed event has a routing strategy outside the supported `Direct`, `Bubble`, and `Tunnel` values.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Trace(UIElement target, RoutedEvent routedEvent, ElementChildRole role = ElementChildRole.Visual)` | `RoutedEventTraceSnapshot` | Creates a diagnostic snapshot for `routedEvent` starting from `target`, using visual ancestors by default. |

## Result Types

### RoutedEventTraceSnapshot

```csharp
public sealed record RoutedEventTraceSnapshot(
    string EventName,
    RoutingStrategy RoutingStrategy,
    string? TargetId,
    IReadOnlyList<RoutedEventTraceStep> Steps)
```

| Member | Type | Description |
| --- | --- | --- |
| `EventName` | `string` | The routed event name copied from `RoutedEvent.Name`. |
| `RoutingStrategy` | `RoutingStrategy` | The routing strategy copied from `RoutedEvent.RoutingStrategy`. |
| `TargetId` | `string?` | The target element identifier, or `null` when the target is unattached. |
| `Steps` | `IReadOnlyList<RoutedEventTraceStep>` | Ordered route entries produced for the trace. |
| `ToString()` | `string` | Returns the event name, routing strategy, and step list joined with ` -> `. |

### RoutedEventTraceStep

```csharp
public sealed record RoutedEventTraceStep(string? ElementId, string ElementType)
```

| Member | Type | Description |
| --- | --- | --- |
| `ElementId` | `string?` | The traced element identifier, or `null` when the element is unattached. |
| `ElementType` | `string` | The runtime type name of the traced element. |
| `ToString()` | `string` | Returns `ElementType#ElementId`, using `unattached` when `ElementId` is `null`. |

## Applies To

Cerneala UI routed-event diagnostics.

## See Also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
- `Cerneala.UI.Elements.ElementTreeWalker`
- `Cerneala.UI.Elements.ElementChildRole`
- Source: `UI/Diagnostics/RoutedEventTrace.cs`
