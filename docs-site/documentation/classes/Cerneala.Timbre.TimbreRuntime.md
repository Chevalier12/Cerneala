# TimbreRuntime Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreRuntime.cs`

Owns the active playbacks, their DSP, the final mix, the preload cache, and the connection to one output.

```csharp
public sealed class TimbreRuntime : IDisposable
```

Inheritance:
`object` -> `TimbreRuntime`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.Timbre;

using var runtime = new TimbreRuntime(new TimbreRuntimeOptions { Output = output });
using TimbreScope sounds = runtime.CreateScope();

var confirm = new TimbreClip("audio/confirm.wav");
await runtime.PrepareAsync(confirm);
TimbrePlayback playback = sounds.Play(confirm);
TimbrePlaybackResult result = await playback.Completion;
```

`output` is an [ITimbreOutput](Cerneala.Timbre.ITimbreOutput.md) supplied by a platform backend or a test sink.

## Remarks

There is no global runtime. Standalone code creates one explicitly; a UI application uses `Application.TimbreRuntime`, and window roots share it. Scopes created from one runtime mix into the same output.

The runtime processes audio on its own mixer thread in blocks of 480 frames (10 ms) of interleaved stereo float32 at 48 kHz, and keeps at most 40 ms of PCM queued in the output. The thread and the output start lazily with the first accepted playback; constructing a runtime, creating scopes, or declaring clips opens nothing. Playback continues independently of UI redraw, the UI pump, and hidden windows.

All voices are summed after their own chains and volumes. The sum is hard-clipped to ±1 at the output; there is no normalization, compressor, or limiter.

When no output is configured, the output fails to open, or the device is lost, the affected playbacks end `Failed` with `TimbreErrorKind.DeviceUnavailable`. The runtime does not retry; a later `Play` tries to open the output again.

`PrepareAsync` decodes a preloadable clip into the cache ahead of time without playing it. For a streaming clip it verifies that the source opens: a decoded file or stream reads its headers (and, for Ogg, its final page) and decodes none of its audio. The token is observed between decoded frames or packets. It throws a [TimbreException](Cerneala.Timbre.TimbreException.md) for source, format, or resource-limit failures.

`Dispose` cancels every playback of every scope, stops the mixer thread, closes the output, and releases the cache. It is idempotent. Creating scopes or preparing clips afterwards throws `ObjectDisposedException`.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreRuntime(TimbreRuntimeOptions? options = null)` | Creates a runtime; the options are copied and validated. |

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `SampleRate` | `48000` | Processing and output sample rate in Hz. |
| `ChannelCount` | `2` | Interleaved channels per frame. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDisposed` | `bool` | Whether `Dispose` was called. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `CreateScope()` | `TimbreScope` | Creates an owner of playbacks and handles. |
| `PrepareAsync(TimbreClip clip, CancellationToken cancellationToken = default)` | `Task` | Preloads or validates a clip's source without playing it. |
| `Dispose()` | `void` | Cancels playbacks and releases the runtime. |

## See also

- [TimbreRuntimeOptions](Cerneala.Timbre.TimbreRuntimeOptions.md)
- [TimbreScope](Cerneala.Timbre.TimbreScope.md)
- [ITimbreOutput](Cerneala.Timbre.ITimbreOutput.md)
