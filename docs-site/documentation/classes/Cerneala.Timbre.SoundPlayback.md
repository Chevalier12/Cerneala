# SoundPlayback Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundPlayback.cs`

One started instance of a [SoundClip](Cerneala.Timbre.SoundClip.md), with a stable identity, its own values, transport, and final result.

```csharp
public sealed class SoundPlayback
```

## Examples

```csharp
using Cerneala.Timbre;

SoundPlayback playback = sounds.Play(filteredSound);
playback.Volume = 0.8f;
playback.Set(toneCutoff, 6000f);
playback.Pause();
playback.Resume();
await playback.SeekAsync(TimeSpan.FromSeconds(1));

SoundPlaybackResult result = await playback.Completion;
if (result.State == SoundPlaybackState.Failed)
{
    Console.WriteLine(result.Error!.Kind);
}
```

## Remarks

The identity exists as soon as `Play` returns, in the `Pending` state, before any PCM is produced. It becomes `Playing` when its first PCM block is mixed. See [SoundPlaybackState](Cerneala.Timbre.SoundPlaybackState.md) for the full state machine.

Control changes are published to the mixer at the next 10 ms block boundary and affect only PCM produced afterwards; PCM already queued in the output (at most 40 ms) still plays. No change on one playback affects another playback, the clip, or the shared output.

**Values.** `Volume` (0–1) and `Set` validate like [SoundStartOptions](Cerneala.Timbre.SoundStartOptions.md) and throw instead of clamping.

**Pause/Resume.** `Pause` keeps the reader, position, values, and DSP state, including a delay tail, and the playback keeps its voice slot. Pausing a pending playback lets preparation continue but holds back its first PCM until `Resume`. `Resume` continues without restarting. Repeated `Pause`, and `Resume` while not paused, are no-ops.

**Seek.** `SeekAsync` moves to an absolute source time. A negative target, or one beyond a known `Duration`, throws `ArgumentOutOfRangeException` synchronously; with an unknown duration, an out-of-range target faults the returned task. The returned task completes when the reader has reached the exact target frame; the caller does not have to await it. A newer request supersedes a pending one, whose task is canceled; cancellation or any terminal state also cancels a pending request. A paused playback stays paused; an active one continues once the target is ready: it is not mixed (and no underrun is counted) until a full software queue of data from the target (40 ms), or the rest of the source, has been read. A successful seek resets the modifier state; a seek never produces a tail.

**Loop.** `Loop` is the value captured at start. A looping playback repeats the whole source without completing at each end of source and keeps its DSP state across repetitions.

**Position and Duration.** `Position` is the processed source position (the seek target once a seek completes), not the time heard at the device. `Duration` is `null` while the source length is unknown; a streaming source that only learns its length at its end (for example an MP3 without a Xing/Info tag) reports it once that end has been reached, after which seeks beyond it are rejected synchronously.

**Termination.** `Cancel` is idempotent: it stops producing source and tail PCM immediately, ends the playback as `Canceled`, and releases the reader in the background. Decoding runs on a background worker and checks for cancellation between frames or packets; a read that is blocked in the source stream (for example a slow custom stream) is not interrupted, so the reader, its stream and its memory reservations are released when that read returns, while the playback is already `Canceled`. On a terminal playback, `Pause`, `Resume`, `SeekAsync`, `Volume` assignment, and `Set` throw `InvalidOperationException`; `State` and `Completion` remain observable. Replay requires a new `Play`.

**Completion.** `Completion` completes exactly once with a [SoundPlaybackResult](Cerneala.Timbre.SoundPlaybackResult.md) and never faults. `Completed` requires the end of the source, the end of any tail, and the output having consumed the playback's last queued frame. The task completes on a thread-pool thread, independent of the UI pump; awaiting it on a UI thread resumes through that thread's synchronization context.

Members are thread-safe.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clip` | `SoundClip` | The definition captured at start. |
| `State` | `SoundPlaybackState` | Current state. |
| `Volume` | `float` | Linear post-chain gain, 0–1. |
| `Loop` | `bool` | Loop option captured at start. |
| `Position` | `TimeSpan` | Processed source position. |
| `Duration` | `TimeSpan?` | Source duration, when known. |
| `Completion` | `Task<SoundPlaybackResult>` | Final result. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Set<T>(SoundParameter<T> parameter, T value)` | `void` | Changes a declared parameter for this playback. |
| `Cancel()` | `void` | Cancels this identity. |
| `Pause()` | `void` | Suspends source and effects. |
| `Resume()` | `void` | Continues a paused playback. |
| `SeekAsync(TimeSpan position)` | `Task` | Requests an absolute seek. |

## See also

- [SoundScope](Cerneala.Timbre.SoundScope.md)
- [SoundHandle](Cerneala.Timbre.SoundHandle.md)
