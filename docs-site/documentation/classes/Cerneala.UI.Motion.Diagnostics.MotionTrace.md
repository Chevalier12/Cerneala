# MotionTrace Class

## Definition
Namespace: `Cerneala.UI.Motion.Diagnostics`  
Assembly/Project: `Cerneala`  
Source: `UI/Motion/Diagnostics/MotionTrace.cs`

Stores the ordered motion diagnostic events recorded by `MotionDiagnostics`.

```csharp
public sealed class MotionTrace
```

Inheritance:  
`object` -> `MotionTrace`

## Examples

Enable motion diagnostics on a root, run motion work, and inspect the retained trace events.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();
root.Motion.Diagnostics.IsEnabled = true;

MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
value.AnimateTo(1f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));

root.Motion.Tick();

foreach (MotionTraceEvent traceEvent in root.Motion.Diagnostics.Trace.Events)
{
    Console.WriteLine(traceEvent.Kind);
}
```

Clear retained trace events when the same diagnostics object is reused for a fresh inspection.

```csharp
using Cerneala.UI.Motion.Diagnostics;

MotionDiagnostics diagnostics = new() { IsEnabled = true };
diagnostics.Record(MotionTraceEventKind.MotionStarted, "fade-in");

diagnostics.Trace.Clear();

int eventCount = diagnostics.Trace.Events.Count; // 0
```

## Remarks

`MotionTrace` is an in-memory diagnostic collection used by `MotionDiagnostics.Trace`. Public callers can read the appended `MotionTraceEvent` values through `Events` and can reset the collection with `Clear`.

Recording is performed through `MotionDiagnostics.Record`, which appends only when `MotionDiagnostics.IsEnabled` is `true`. The trace itself does not expose a public record method, filtering flag, capacity limit, or snapshot copy API. `Events` is a read-only list view over the retained event storage, so callers should treat it as live diagnostic state.

Motion events currently include lifecycle, property-write, invalidation, and reduced-motion skip categories represented by `MotionTraceEventKind`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTrace()` | Creates an empty motion trace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Events` | `IReadOnlyList<MotionTraceEvent>` | Gets the retained trace events in append order. |

## Methods

| Name | Description |
| --- | --- |
| `Clear()` | Removes all retained trace events. |

## Applies to

Cerneala UI motion diagnostics.

## See also

- `Cerneala.UI.Motion.Diagnostics.MotionDiagnostics`
- `Cerneala.UI.Motion.Diagnostics.MotionTraceEvent`
- `Cerneala.UI.Motion.Diagnostics.MotionTraceEventKind`
- Source: `UI/Motion/Diagnostics/MotionTrace.cs`
