# TimbreHandle Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreHandle.cs`

A reusable slot local to one [TimbreScope](Cerneala.Timbre.TimbreScope.md), holding at most one current playback.

```csharp
public sealed class TimbreHandle
```

## Examples

```csharp
using Cerneala.Timbre;

TimbreHandle slot = sounds.CreateHandle();
TimbrePlayback old = sounds.Play(clip, handle: slot);
TimbrePlayback current = sounds.Play(clip, handle: slot); // cancels old

old.Cancel();  // no effect on current
slot.Cancel(); // cancels current
slot.Cancel(); // empty slot: no-op
```

## Remarks

A handle is distinct from a [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md) identity. `Play(..., handle: slot)` replaces only the slot's occupant; `Cancel` on an older playback reference never affects a newer occupant. `slot.Cancel()` targets whatever occupies the slot at that moment and is a no-op when the slot is empty.

`Current` returns the occupant while it is non-terminal and `null` otherwise. A handle cannot be used with another scope's `Play`. Disposing the scope cancels the occupant.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Scope` | `TimbreScope` | The owning scope. |
| `Current` | `TimbrePlayback?` | The non-terminal occupant, or `null`. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Cancel()` | `void` | Cancels the current occupant, if any. |
