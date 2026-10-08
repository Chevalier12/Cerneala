# SoundStartOptions Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundStartOptions.cs`

Initial values for one start, configured inside the delegate passed to [SoundScope.Play](Cerneala.Timbre.SoundScope.md).

```csharp
public sealed class SoundStartOptions
```

## Examples

```csharp
using Cerneala.Timbre;

SoundPlayback playback = sounds.Play(filteredSound, start =>
{
    start.Volume = 0.2f;
    start.Loop = true;
    start.Set(toneCutoff, 800f);
}, handle: slot);
```

## Remarks

The options start from the clip's defaults. Setters validate immediately: `Volume` outside 0–1, NaN, or infinity throws `ArgumentOutOfRangeException`; `Set` with `SoundPlayback.VolumeParameter` is equivalent to assigning `Volume`; `Set` with any other parameter not declared by the clip throws `ArgumentException`, and an out-of-range or non-finite value throws `ArgumentOutOfRangeException`. Throwing from the delegate aborts the start without replacing the handle's occupant.

The configured values are snapshotted before `Play` returns, so they apply to the first PCM block. The delegate runs once and is never retained or re-run by a worker or audio thread. Using the options object after the delegate returns throws `InvalidOperationException`.

`Loop` is fixed for the playback's lifetime; there is no loop setter on [SoundPlayback](Cerneala.Timbre.SoundPlayback.md).

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Volume` | `float` | Initial linear post-chain gain; defaults to `SoundClip.Volume`. |
| `Loop` | `bool` | Whether the playback repeats; defaults to `SoundClip.Loop`. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Set<T>(SoundParameter<T> parameter, T value)` | `void` | Overrides a declared parameter for this playback. |
