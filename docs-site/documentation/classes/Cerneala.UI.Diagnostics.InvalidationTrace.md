# InvalidationTrace Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`  
Assembly/Project: `Cerneala`  
Source: `UI/Diagnostics/InvalidationTrace.cs`

Records retained invalidation diagnostics for requests, propagation, queueing, frame phases, phase summaries, and dirty-state clears.

```csharp
public sealed class InvalidationTrace
```

Inheritance:  
`object` -> `InvalidationTrace`

## Examples

The trace created by `UIRoot` records invalidation activity while the retained frame is processed.

```csharp
using System.Linq;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

child.Invalidate(InvalidationFlags.Render, "render");
root.ProcessFrame();

bool sawRequest = root.Trace.Entries.Any(entry =>
    entry.Kind == InvalidationTraceEventKind.Request &&
    entry.Reason == "render");

bool sawRenderPhase = root.Trace.Entries.Any(entry =>
    entry.Kind == InvalidationTraceEventKind.Phase &&
    entry.Phase == FramePhase.RenderCache);
```

Use `InvalidationTrace.Disabled` when a scheduler or diagnostic path needs a non-recording trace instance.

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

InvalidationTrace trace = InvalidationTrace.Disabled;
trace.RecordRequest(new InvalidationRequest(new UIElement(), InvalidationFlags.Render, "render"));

int retainedEntryCount = trace.Entries.Count; // 0
```

## Remarks

`InvalidationTrace` is an in-memory diagnostic collector. A normal `UIRoot` constructs an enabled trace and passes it to `UiFrameScheduler`; `UIRoot.Invalidate` records the original `InvalidationRequest`, then `DirtyPropagation` records propagation and queue events, and the scheduler records phase, phase-summary, and clear events while processing frame work.

Entries are appended in recording order and exposed through `Entries`. The collection surface is read-only, but the trace keeps retaining entries for the lifetime of the trace instance; the class does not expose a clear or capacity API.

Disabled traces keep `IsEnabled` set to `false` and ignore all record calls. `RecordRequest` still validates that its `request` argument is not null before checking `IsEnabled`.

Each entry stores both the `UIElement` reference and the element id string available at recording time. This lets diagnostics keep the recorded id even if the element is detached later. `DirtyTreeDumper` can use the trace to attach the latest dirty reason and source property name to a dirty-tree dump.

## Constructors

| Name | Description |
| --- | --- |
| `InvalidationTrace(bool isEnabled = true)` | Creates a trace. The optional `isEnabled` value controls whether record calls append entries. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Disabled` | `InvalidationTrace` | Static disabled trace instance created with `isEnabled: false`. |
| `IsEnabled` | `bool` | Gets whether record calls append entries. |
| `Entries` | `IReadOnlyList<InvalidationTraceEntry>` | Gets the retained trace entries in append order. |

## Methods

| Name | Description |
| --- | --- |
| `RecordRequest(InvalidationRequest request)` | Records a `Request` entry from an invalidation request, including target, flags, reason, and source property. Throws if `request` is null. |
| `RecordPropagation(UIElement element, InvalidationFlags flags, string reason)` | Records a `Propagation` entry for an element and invalidation flags. |
| `RecordQueue(UIElement element, InvalidationFlags flags, string reason)` | Records a `Queue` entry when work is queued for an element. |
| `RecordPhase(FramePhase phase, UIElement element, InvalidationFlags flags)` | Records a `Phase` entry for an element processed by a frame phase. The reason is the phase name. |
| `RecordPhaseSummary(FramePhase phase, int count)` | Records a `PhaseSummary` entry with no element, `InvalidationFlags.None`, the phase, and the processed count stored as `Reason`. |
| `RecordClear(UIElement element, InvalidationFlags flags)` | Records a `Clear` entry after processed dirty flags are cleared for an element. The reason is `Clear`. |

## Related Types

### InvalidationTraceEventKind

`InvalidationTraceEventKind` classifies each trace entry.

```csharp
public enum InvalidationTraceEventKind
```

| Name | Description |
| --- | --- |
| `Request` | Original invalidation request recorded by `UIRoot.Invalidate`. |
| `Propagation` | Effective flags marked on an element by dirty propagation. |
| `Queue` | Work queued for a retained processing queue. |
| `Phase` | Element processed during a specific frame phase. |
| `PhaseSummary` | Summary entry for a frame phase with the processed count stored in `Reason`. |
| `Clear` | Dirty flags cleared after phase processing. |

### InvalidationTraceEntry

`InvalidationTraceEntry` stores one retained invalidation diagnostic event.

```csharp
public sealed record InvalidationTraceEntry(
    InvalidationTraceEventKind Kind,
    UIElement? Element,
    string? ElementId,
    InvalidationFlags Flags,
    FramePhase? Phase,
    string Reason,
    UiProperty? SourceProperty)
```

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `InvalidationTraceEventKind` | Event category. |
| `Element` | `UIElement?` | Element associated with the entry, or null for phase summaries. |
| `ElementId` | `string?` | Element id string captured when the entry was recorded. |
| `Flags` | `InvalidationFlags` | Invalidation flags associated with the entry. |
| `Phase` | `FramePhase?` | Frame phase associated with the entry, when applicable. |
| `Reason` | `string` | Request reason, propagation reason, phase name, phase-summary count, or `Clear`. |
| `SourceProperty` | `UiProperty?` | Source UI property from the original invalidation request, when available. |
| `SourcePropertyName` | `string?` | Diagnostic name of `SourceProperty`, or null when no source property is present. |

## Applies to

Cerneala retained UI diagnostics and invalidation scheduling.

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.InvalidationRequest`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Diagnostics.DirtyTreeDumper`
