# InvalidationTraceEntry Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: [`UI/Detective/InvalidationTrace.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/InvalidationTrace.cs)

Represents one retained diagnostic event recorded by `InvalidationTrace`.

```csharp
public sealed record InvalidationTraceEntry(
    InvalidationTraceEventKind Kind,
    UIElement? Element,
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

The entry is usually created by `InvalidationTrace` rather than by application code directly. The root needs an enabled trace; adding the child already records two `Request` entries with the reason `Visual child added`, so the example takes the last one. A request entry keeps the invalidated element, the requested flags, the textual reason, and the optional source property.

```csharp
InvalidationTrace trace = new();
UIRoot root = new(trace);
UIElement child = new();
root.VisualChildren.Add(child);

child.Invalidate(InvalidationFlags.Render, "render");

InvalidationTraceEntry entry = trace.Entries.Last(
    candidate => candidate.Kind == InvalidationTraceEventKind.Request);

Console.WriteLine(ReferenceEquals(entry.Element, child)); // True
Console.WriteLine(entry.Flags);                           // Render
Console.WriteLine(entry.Reason);                          // render
```

## Remarks

`InvalidationTraceEntry` is a sealed record used by the invalidation diagnostics pipeline. `InvalidationTrace` creates entries for request, propagation, queue, frame phase, phase summary, and clear events.

`Element` can be `null` for entries that summarize a frame phase rather than a specific element.
`Phase` is populated for phase and phase-summary events. `Flags` contains the invalidation categories involved in the event; phase-summary entries use `InvalidationFlags.None`. `SourcePropertyName` is a convenience projection over `SourceProperty.DiagnosticName` and returns `null` when the entry has no source property.

Because this type is a C# record, it also has synthesized value equality, deconstruction, `ToString`, and `GetHashCode` behavior based on its record components.

## Constructors

| Name | Description |
| --- | --- |
| `InvalidationTraceEntry(InvalidationTraceEventKind Kind, UIElement? Element, InvalidationFlags Flags, FramePhase? Phase, string Reason, UiProperty? SourceProperty)` | Initializes a trace entry with the event kind, optional element, invalidation flags, optional frame phase, reason text, and optional source property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `InvalidationTraceEventKind` | Gets the category of diagnostic event represented by the entry. |
| `Element` | `UIElement?` | Gets the element associated with the event, or `null` for non-element summary entries. |
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
| `operator ==(InvalidationTraceEntry?, InvalidationTraceEntry?)` | Returns whether both entries are equal by value, or both are `null`. |
| `operator !=(InvalidationTraceEntry?, InvalidationTraceEntry?)` | Returns the negation of `==`. |

## Applies to

Cerneala retained UI diagnostics.

## See also

- [`InvalidationTrace`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/InvalidationTrace.cs)
- [`InvalidationTraceEventKind`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/InvalidationTrace.cs)
- [`InvalidationFlags`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Invalidation/InvalidationFlags.cs)
- [`FramePhase`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Invalidation/FramePhase.cs)
