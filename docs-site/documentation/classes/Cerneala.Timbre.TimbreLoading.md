# TimbreLoading Enum

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreLoading.cs`

Selects whether a clip's decoded PCM is preloaded into the runtime cache or streamed per playback.

```csharp
public enum TimbreLoading
```

## Examples

```csharp
using Cerneala.Timbre;

var music = new TimbreSound("audio/music.ogg", loop: true, loading: TimbreLoading.Streaming);
```

## Remarks

Limits come from [TimbreRuntimeOptions](Cerneala.Timbre.TimbreRuntimeOptions.md). A source that does not fit a configured limit is refused before the excessive allocation: a playback ends `Failed` with `TimbreErrorKind.ResourceLimitExceeded`, and [TimbreRuntime.PrepareAsync](Cerneala.Timbre.TimbreRuntime.md) throws a `TimbreException` of that kind.

A preloaded clip is held in one managed array, so one clip holds at most 8,589,934,360 bytes of decoded PCM (1,073,741,795 frames, about 6.2 hours at 48 kHz). The effective per-clip preload limit is the smaller of `MaxPreloadBytes` and that size. Example: with `MaxPreloadBytes = MaxCacheBytes = 64 GiB`, a `Preload` source that declares 2,147,483,648 frames (16 GiB) is refused with `ResourceLimitExceeded` before anything is allocated or read.

A reader that reports a negative `LengthFrames` violates the [TimbreReader](Cerneala.Timbre.TimbreReader.md) contract in every loading mode: the playback fails, and `PrepareAsync` throws, with `TimbreErrorKind.InvalidData`.

Preloaded payloads are immutable and shared through the runtime cache. Active playbacks pin their payload, so eviction never removes data that is playing. Streaming keeps a fixed per-playback buffer that does not grow with the duration of the source. A streaming playback stays `Pending` until a full software queue of decoded audio (40 ms), or the whole source, is buffered, so its start is never padded with silence. If decoding later falls behind, mixing waits while the output still holds queued audio; only when the output would otherwise run dry is silence padded, counted as an underrun and without skipping source content.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Auto` | 0 | Preloads when the decoded size is known and at most `AutoPreloadMaxBytes` (1 MiB by default); streams otherwise. |
| `Preload` | 1 | Decodes the whole source into the cache, up to `MaxPreloadBytes` (16 MiB by default) and at most 8,589,934,360 bytes per clip. |
| `Streaming` | 2 | Reads incrementally while playing. |
