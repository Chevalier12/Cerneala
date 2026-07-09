# MotionTraceEvent Struct

## Definition
Namespace: `Cerneala.UI.Motion.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Diagnostics/MotionTraceEvent.cs`

Represents one immutable motion diagnostic event retained by `MotionTrace`.

```csharp
public readonly record struct MotionTraceEvent(MotionTraceEventKind Kind, string? DebugName = null)
```

Inheritance:
`ValueType` -> `MotionTraceEvent`

Implements:
`IEquatable<MotionTraceEvent>`

## Examples

Record a named event through `MotionDiagnostics` and inspect the retained trace:

```csharp
using Cerneala.UI.Motion.Diagnostics;

MotionDiagnostics diagnostics = new() { IsEnabled = true };

diagnostics.Record(MotionTraceEventKind.MotionStarted, "fade-in");

MotionTraceEvent traceEvent = diagnostics.Trace.Events[0];
Console.WriteLine(traceEvent.Kind);
Console.WriteLine(traceEvent.DebugName);
```

Create a trace event directly when working with diagnostic data:

```csharp
using Cerneala.UI.Motion.Diagnostics;

MotionTraceEvent traceEvent = new(
    MotionTraceEventKind.MotionSkippedReducedMotion,
    "press-feedback");

bool isReducedMotionSkip =
    traceEvent.Kind == MotionTraceEventKind.MotionSkippedReducedMotion;
```

## Remarks

`MotionTraceEvent` is the value stored in `MotionTrace.Events`. `MotionDiagnostics.Record` creates these values only when `MotionDiagnostics.IsEnabled` is `true`; disabled diagnostics leave the trace unchanged.

`Kind` identifies the diagnostic category, such as motion start, sampling, completion, property writes, render or layout invalidation, cancellation, retargeting, and reduced-motion skips. `DebugName` carries the optional diagnostic name supplied by the recording caller and defaults to `null`.

Because this type is a readonly C# record struct, it has synthesized value equality, deconstruction, `ToString`, and `GetHashCode` behavior based on `Kind` and `DebugName`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTraceEvent(MotionTraceEventKind kind, string? debugName = null)` | Initializes a trace event with the event kind and optional diagnostic name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `MotionTraceEventKind` | Gets the category of motion diagnostic event. |
| `DebugName` | `string?` | Gets the optional diagnostic name associated with the event, or `null` when no name was supplied. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out MotionTraceEventKind kind, out string? debugName)` | `void` | Deconstructs the record into its primary constructor components. |
| `Equals(MotionTraceEvent other)` | `bool` | Determines whether another trace event has the same `Kind` and `DebugName`. |
| `Equals(object? obj)` | `bool` | Determines whether an object is an equivalent `MotionTraceEvent`. |
| `GetHashCode()` | `int` | Returns the synthesized hash code for the record component values. |
| `ToString()` | `string` | Returns the synthesized record string representation. |

## Applies to

Cerneala UI motion diagnostics.

## See also

- `Cerneala.UI.Motion.Diagnostics.MotionDiagnostics`
- `Cerneala.UI.Motion.Diagnostics.MotionTrace`
- `Cerneala.UI.Motion.Diagnostics.MotionTraceEventKind`
- Source: `UI/Motion/Diagnostics/MotionTraceEvent.cs`
