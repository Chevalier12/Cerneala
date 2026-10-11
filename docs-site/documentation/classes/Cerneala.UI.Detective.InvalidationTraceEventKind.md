# InvalidationTraceEventKind Enum

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/InvalidationTrace.cs`

Identifies which step of the invalidation pipeline an `InvalidationTraceEntry` records.

```csharp
public enum InvalidationTraceEventKind
```

## Examples

Trace one render invalidation through a frame. The trace must be enabled and passed to the `UIRoot` constructor; the default `UIRoot` uses the disabled `InvalidationTrace.Disabled`.

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

InvalidationTrace trace = new();
UIRoot root = new(trace);
UIElement box = new();
root.VisualChildren.Add(box);
root.ProcessFrame();
int start = trace.Entries.Count;

root.Invalidate(new InvalidationRequest(box, InvalidationFlags.Render, "color changed"));
root.ProcessFrame();

foreach (InvalidationTraceEntry entry in trace.Entries.Skip(start))
{
    if (entry.Kind != InvalidationTraceEventKind.PhaseSummary)
    {
        Console.WriteLine($"{entry.Kind} {entry.Phase} {entry.Reason}");
    }
}

Console.WriteLine(trace.Entries.Skip(start).Count(e => e.Kind == InvalidationTraceEventKind.PhaseSummary));

// Request  color changed
// Propagation  color changed
// Queue  color changed
// Phase RenderCache RenderCache
// Clear  Clear
// 8
```

The 8 `PhaseSummary` entries are one per phase of the frame: `InheritedProperties` (twice), `CommandState`, `Aspect`, `Measure`, `Arrange`, `RenderCache`, and `HitTest`.

## Remarks

The framework writes these entries in this order of the pipeline:

| Kind | Written when | Entry members |
| --- | --- | --- |
| `Request` | `UIRoot.Invalidate` is called, once per call. | `Element` is the request target. `Flags`, `Reason`, and `SourceProperty` come from the request. |
| `Propagation` | Dirty flags are marked on an element. | `Flags` are the flags marked on that element. `Reason` is the request reason. |
| `Queue` | The element is added to one phase queue. A request with several flags writes one entry per queue. | `Flags` holds one flag, such as `Render`. |
| `Phase` | A frame phase finished processing the element without an exception. | `Phase` is the phase, and `Reason` is its name. `Flags` is the phase flag, or `None` for `CommandState`. |
| `PhaseSummary` | A frame phase ends. A frame with work writes one per phase. A frame with no work and no active motion writes one `Idle` entry. | `Element` and `ElementId` are `null`. `Phase` is the phase. `Reason` is the number of processed elements as text, for example `"1"`. |
| `Clear` | Dirty flags are removed from the element: after it is processed and not queued again, or when layout skips it. | `Flags` are the removed flags, which can include related flags such as `Text` and `Image`. `Reason` is `"Clear"`. |

All six kinds are written only when the trace's `IsEnabled` is `true`. Code outside the framework can also write entries through the public `Record...` methods of `InvalidationTrace`.

`DirtyTreeDumper` uses the newest `Request`, `Propagation`, or `Queue` entry of a dirty element as its reason, and looks back to the element's last `Request` for the source property name.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Request` | `0` | An invalidation request reached the root. |
| `Propagation` | `1` | Dirty flags were marked on an element. |
| `Queue` | `2` | An element was added to a phase queue. |
| `Phase` | `3` | A frame phase processed an element. |
| `PhaseSummary` | `4` | A frame phase ended. |
| `Clear` | `5` | Dirty flags were removed from an element. |

## Applies to

`InvalidationTrace`, `InvalidationTraceEntry`, and `DirtyTreeDumper`.

## See also

- `Cerneala.UI.Detective.InvalidationTrace`
- `Cerneala.UI.Detective.InvalidationTraceEntry`
- `Cerneala.UI.Detective.DirtyTreeDumper`
- `Cerneala.UI.Invalidation.InvalidationRequest`
