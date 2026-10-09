# TimbreStartOptions Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreStartOptions.cs`

Initial values for one start, configured inside the delegate passed to [TimbreScope.Play](Cerneala.Timbre.TimbreScope.md).

```csharp
public sealed class TimbreStartOptions
```

## Examples

```csharp
using Cerneala.Timbre;

TimbrePlayback playback = sounds.Play(filteredTimbre, start =>
{
    start.Volume = 0.2f;
    start.Loop = true;
    start.Set(toneCutoff, 800f);
}, handle: slot);
```

## Remarks

The options start from the clip's defaults. Setters validate immediately: `Volume` outside 0–1, NaN, or infinity throws `ArgumentOutOfRangeException`; `Set` with `TimbrePlayback.VolumeParameter` is equivalent to assigning `Volume`; `Set` with any other parameter not declared by the clip throws `ArgumentException`, and an out-of-range or non-finite value throws `ArgumentOutOfRangeException`. Throwing from the delegate aborts the start without replacing the handle's occupant.

The configured values are snapshotted before `Play` returns, so they apply to the first PCM block. The delegate runs once and is never retained or re-run by a worker or audio thread. Using the options object after the delegate returns throws `InvalidOperationException`.

An ordinary start applies the configured Volume from its first frame. A handle replacement instead fades from zero to that configured gain over 5 ms, overlapping the old voice's release fade when both sources are ready.

`Loop` is fixed for the playback's lifetime; there is no loop setter on [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md).

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Volume` | `float` | Initial linear post-chain gain; defaults to `TimbreSound.Volume`. |
| `Loop` | `bool` | Whether the playback repeats; defaults to `TimbreSound.Loop`. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Set<T>(TimbreParameter<T> parameter, T value)` | `void` | Overrides a declared parameter for this playback. |
