# InvalidationTraceEntry Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/InvalidationTrace.cs`](../../UI/Diagnostics/InvalidationTrace.cs)

Represents one retained diagnostic event recorded by `InvalidationTrace`.

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

Inheritance:
`Object` -> `InvalidationTraceEntry`

Implements:
`IEquatable<InvalidationTraceEntry>`

## Examples

The entry is usually created by `InvalidationTrace` rather than by application code directly. A request entry keeps the invalidated element, the element id captured at the time of invalidation, the requested flags, the textual reason, and the optional source property.

```csharp
UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

child.Invalidate(InvalidationFlags.Render, "render");

InvalidationTraceEntry entry = root.Trace.Entries.Single(
    candidate => candidate.Kind == InvalidationTraceEventKind.Request);

Console.WriteLine(entry.ElementId);
Console.WriteLine(entry.Flags);
Console.WriteLine(entry.Reason);
```

## Remarks

`InvalidationTraceEntry` is a sealed record used by the invalidation diagnostics pipeline. `InvalidationTrace` creates entries for request, propagation, queue, frame phase, phase summary, and clear events.

`Element` can be `null` for entries that summarize a frame phase rather than a specific element. `ElementId` is stored separately from `Element`; request entries capture the element id as a string when the entry is recorded, so the diagnostic id can still be read after the element is detached and its live `ElementId` becomes unavailable.

`Phase` is populated for phase and phase-summary events. `Flags` contains the invalidation categories involved in the event; phase-summary entries use `InvalidationFlags.None`. `SourcePropertyName` is a convenience projection over `SourceProperty.DiagnosticName` and returns `null` when the entry has no source property.

Because this type is a C# record, it also has synthesized value equality, deconstruction, `ToString`, and `GetHashCode` behavior based on its record components.

## Constructors

| Name | Description |
| --- | --- |
| `InvalidationTraceEntry(InvalidationTraceEventKind kind, UIElement? element, string? elementId, InvalidationFlags flags, FramePhase? phase, string reason, UiProperty? sourceProperty)` | Initializes a trace entry with the event kind, optional element, captured element id, invalidation flags, optional frame phase, reason text, and optional source property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `InvalidationTraceEventKind` | Gets the category of diagnostic event represented by the entry. |
| `Element` | `UIElement?` | Gets the element associated with the event, or `null` for non-element summary entries. |
| `ElementId` | `string?` | Gets the element id string captured when the entry was recorded, or `null` when no element id was available. |
| `Flags` | `InvalidationFlags` | Gets the invalidation flags associated with the event. |
| `Phase` | `FramePhase?` | Gets the frame phase associated with phase events, or `null` for events that are not tied to a frame phase. |
| `Reason` | `string` | Gets the diagnostic reason text recorded for the event. |
| `SourceProperty` | `UiProperty?` | Gets the UI property that caused the invalidation request, or `null` when the event has no source property. |
| `SourcePropertyName` | `string?` | Gets `SourceProperty.DiagnosticName`, or `null` when `SourceProperty` is `null`. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out InvalidationTraceEventKind kind, out UIElement? element, out string? elementId, out InvalidationFlags flags, out FramePhase? phase, out string reason, out UiProperty? sourceProperty)` | Deconstructs the record into its primary constructor components. |
| `Equals(InvalidationTraceEntry? other)` | Determines whether another entry has the same record component values. |
| `Equals(object? obj)` | Determines whether an object is an equivalent `InvalidationTraceEntry`. |
| `GetHashCode()` | Returns the synthesized hash code for the record component values. |
| `ToString()` | Returns the synthesized record string representation. |

## Applies to

Cerneala retained UI diagnostics.

## See also

- [`InvalidationTrace`](../../UI/Diagnostics/InvalidationTrace.cs)
- [`InvalidationTraceEventKind`](../../UI/Diagnostics/InvalidationTrace.cs)
- [`InvalidationFlags`](../../UI/Invalidation/InvalidationFlags.cs)
- [`FramePhase`](../../UI/Invalidation/FramePhase.cs)
