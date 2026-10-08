# SoundDiagnosticsSnapshot Record

## Definition

Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/SoundDiagnosticsSnapshot.cs`

Read-only copy of a root's [SoundRuntime](Cerneala.Timbre.SoundRuntime.md) counters, returned by `Detective.CaptureSound()`.

```csharp
public sealed record SoundDiagnosticsSnapshot(
    bool OutputOpen,
    int ActivePlaybacks,
    long PlaybacksFailed,
    long UnderrunFrames,
    long ClippedSamples,
    long CacheBytes,
    long StreamingBufferBytes,
    long DspStateBytes)
{
    public long MotionSamplesRejected { get; init; }
}
```

## Examples

```csharp
using Cerneala.UI.Detective;

SoundDiagnosticsSnapshot? sound = root.Detective.CaptureSound();
if (sound is { OutputOpen: false, ActivePlaybacks: > 0 })
{
    // Playbacks are waiting for an output that is not open.
}
```

## Remarks

The snapshot answers the usual questions about silent, starved, or distorted audio: whether the output is open, how many playbacks are non-terminal, how many failed, how many silence frames were padded because a streaming source was late, and how many output samples were clipped to ±1. The memory fields separate the preload cache, streaming buffers, and modifier state (delay lines).

`MotionSamplesRejected` counts audio Motion samples that were not finite or fell outside their parameter's range; each one ended only the animation of that parameter, which kept its last valid value (see [SoundMotionAnimationBuilder](Cerneala.UI.Timbre.SoundMotionAnimationBuilder.md)).

Values are copied at capture time and do not update. Counters belong to the runtime, which all window roots of an application share. Capturing never opens an output, starts a playback, or changes audio state.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OutputOpen` | `bool` | Whether the runtime's output is currently open. |
| `ActivePlaybacks` | `int` | Non-terminal playbacks, including pending and paused ones. |
| `PlaybacksFailed` | `long` | Playbacks that ended `Failed` since the runtime was created. |
| `UnderrunFrames` | `long` | Silence frames padded because a playing source had no data yet. |
| `ClippedSamples` | `long` | Output samples hard-clipped to ±1. |
| `CacheBytes` | `long` | Decoded preload payload bytes in the cache. |
| `StreamingBufferBytes` | `long` | Bytes of live streaming ring buffers. |
| `DspStateBytes` | `long` | Bytes of live modifier state. |
| `MotionSamplesRejected` | `long` | Audio Motion samples rejected as non-finite or out of range since the runtime was created. |

## See also

- [Detective](Cerneala.UI.Detective.Detective.md)
- [SoundRuntime](Cerneala.Timbre.SoundRuntime.md)
