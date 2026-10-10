# TimbrePlayback Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbrePlayback.cs`

One started instance of a [TimbreSound](Cerneala.Timbre.TimbreSound.md), with a stable identity, its own values, transport, and final result.

```csharp
public sealed class TimbrePlayback
```

## Examples

```csharp
using Cerneala.Timbre;

TimbrePlayback playback = sounds.Play(filteredTimbre);
playback.Volume = 0.8f;
playback.Set(toneCutoff, 6000f);
playback.Pause();
playback.Resume();
await playback.SeekAsync(TimeSpan.FromSeconds(1));

TimbrePlaybackResult result = await playback.Completion;
if (result.State == TimbrePlaybackState.Failed)
{
    Console.WriteLine(result.Error!.Kind);
}
```

## Remarks

The identity exists as soon as `Play` returns, in the `Pending` state, before any PCM is produced. It becomes `Playing` when its first PCM block is mixed. See [TimbrePlaybackState](Cerneala.Timbre.TimbrePlaybackState.md) for the full state machine.

Control changes are published to the mixer at the next 10 ms block boundary and affect only PCM produced afterwards; PCM already queued in the output (at most 40 ms) still plays. No change on one playback affects another playback, the clip, or the shared output.

The mixer uses a linear 5 ms de-click ramp (240 frames at 48 kHz) for live Volume changes and transport transitions. Both stereo channels use the same per-frame gain. A new target ramps from the currently rendered gain; the public `Volume` value is the requested target, not the instantaneous rendered gain. Ordinary starts use their configured gain from the first frame; handle replacements fade in from zero. These ramps do not alter already-queued PCM and do not smooth modifier inputs such as Delay time or dry/wet mix.

**Values.** `Volume` (0–1) and `Set` validate like [TimbreStartOptions](Cerneala.Timbre.TimbreStartOptions.md) and throw instead of clamping. `VolumeParameter` is the shared descriptor of the intrinsic Volume (name `Volume`, default 1): `Set(TimbrePlayback.VolumeParameter, v)` is equivalent to `Volume = v`, and no clip can declare it. It lets the Volume and the declared parameters be addressed uniformly, for example by audio Motion: `playback.Motion().Animate(TimbrePlayback.VolumeParameter)` (see [TimbreMotionFacade](Cerneala.UI.Timbre.TimbreMotionFacade.md)).

**Pause/Resume.** `Pause` changes `State` to `Paused` synchronously and keeps its voice slot and reader. When the mixer applies the request, source and DSP processing continue for at most 5 ms while the gain fades to zero, then freeze. The preserved position and DSP state (including a delay tail) are those at the end of the fade, not its start. Pausing a pending playback lets preparation continue but holds back its first PCM until `Resume`. `Resume` continues from the frozen state without restarting and fades from zero to the current Volume over 5 ms. Repeated `Pause`, and `Resume` while not paused, are no-ops.

**Seek.** `SeekAsync` moves to an absolute source time. A negative target, or one beyond a known `Duration`, throws `ArgumentOutOfRangeException` synchronously; with an unknown duration, an out-of-range target faults the returned task. The old position first fades out over 5 ms while its source and DSP continue; the seek is dispatched only after that fade. A frozen or not-yet-started playback needs no fade-out. The returned task completes when the reader has reached the exact target frame, not when it is heard; the caller does not have to await it. A newer request supersedes a pending one, whose task is canceled; cancellation or any terminal state also cancels a pending request. A paused playback stays paused; an active one fades in over 5 ms once the target is ready: it is not mixed (and no underrun is counted) while the seek is in flight or until a full software queue of data from the target (40 ms), or the rest of the source, has been read. A successful seek resets the modifier state; no DSP tail is carried across the seek. A rejected asynchronous seek keeps the old DSP state and fades back in from its post-fade position. This holds for any exception the reader throws: for example, when the stream throws `IOException` during the seek, the task faults with that `IOException`, `State` stays `Playing`, and playback continues with the next source frame without a gap, so `Position` keeps counting the frames actually played. A built-in decoder that fails part-way through a seek first returns to its old position. If that return fails too, the decoder's position is unknown: the frames it had already decoded still play, then the playback fails with `TimbreErrorKind.SourceUnavailable` for an I/O error (or the kind of a [TimbreException](Cerneala.Timbre.TimbreException.md)) instead of playing from an unknown position.

**Loop.** `Loop` is the value captured at start. A looping playback repeats the whole source without completing at each end of source and keeps its DSP state across repetitions.

**Position and Duration.** `Position` is the processed source position (the seek target once a seek completes), not the time heard at the device. `Duration` is `null` while the source length is unknown; a streaming source that only learns its length at its end (for example an MP3 without a Xing/Info tag) reports it once that end has been reached, after which seeks beyond it are rejected synchronously.

**Termination.** `Cancel` is idempotent and marks the playback `Canceled` synchronously. A rendering voice continues its source and DSP through a 5 ms fade-out, then produces no further source or tail PCM and releases the reader in the background. A frozen, silent, not-yet-started, or already-ended voice needs no release fade. Handle replacement and markup stop use the same cancellation fade; a replacement fades in independently as soon as its source is ready, overlapping the old voice's fade-out when both are ready. `Completion` for cancellation does not wait for the fade or resource release. Output capacity is required to submit the fade; runtime disposal and source/device failure release without waiting for a fade. Decoding runs on a background worker and checks for cancellation between frames or packets; a read that is blocked in the source stream (for example a slow custom stream) is not interrupted, so the reader, its stream and its memory reservations are released when that read returns, while the playback is already `Canceled`. On a terminal playback, `Pause`, `Resume`, `SeekAsync`, `Volume` assignment, and `Set` throw `InvalidOperationException`; `State` and `Completion` remain observable. Replay requires a new `Play`.

**Completion.** `Completion` completes exactly once with a [TimbrePlaybackResult](Cerneala.Timbre.TimbrePlaybackResult.md) and never faults. `Completed` requires the end of the source, the end of any tail, and the output having consumed the playback's last queued frame. The task completes on a thread-pool thread, independent of the UI pump; awaiting it on a UI thread resumes through that thread's synchronization context.

Members are thread-safe.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `VolumeParameter` (static) | `TimbreParameter<float>` | Descriptor of the intrinsic Volume for `Set` and audio Motion. |
| `Sound` | `TimbreSound` | The sound captured at start. |
| `State` | `TimbrePlaybackState` | Current state. |
| `Volume` | `float` | Linear post-chain gain, 0–1. |
| `Loop` | `bool` | Loop option captured at start. |
| `Position` | `TimeSpan` | Processed source position. |
| `Duration` | `TimeSpan?` | Source duration, when known. |
| `Completion` | `Task<TimbrePlaybackResult>` | Final result. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Set<T>(TimbreParameter<T> parameter, T value)` | `void` | Changes a declared parameter for this playback. |
| `Cancel()` | `void` | Cancels this identity. |
| `Pause()` | `void` | Suspends source and effects. |
| `Resume()` | `void` | Continues a paused playback. |
| `SeekAsync(TimeSpan position)` | `Task` | Requests an absolute seek. |

## See also

- [TimbreScope](Cerneala.Timbre.TimbreScope.md)
- [TimbreHandle](Cerneala.Timbre.TimbreHandle.md)
- [TimbreMotionFacade](Cerneala.UI.Timbre.TimbreMotionFacade.md)
