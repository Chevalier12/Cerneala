# SoundLoading Enum

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundLoading.cs`

Selects whether a clip's decoded PCM is preloaded into the runtime cache or streamed per playback.

```csharp
public enum SoundLoading
```

## Examples

```csharp
using Cerneala.Timbre;

var music = new SoundClip("audio/music.ogg", loop: true, loading: SoundLoading.Streaming);
```

## Remarks

Limits come from [SoundRuntimeOptions](Cerneala.Timbre.SoundRuntimeOptions.md). A source that does not fit a configured limit is refused before the excessive allocation: a playback ends `Failed` with `SoundErrorKind.ResourceLimitExceeded`, and [SoundRuntime.PrepareAsync](Cerneala.Timbre.SoundRuntime.md) throws a `SoundException` of that kind.

Preloaded payloads are immutable and shared through the runtime cache. Active playbacks pin their payload, so eviction never removes data that is playing. Streaming keeps a fixed per-playback buffer that does not grow with the duration of the source.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Auto` | 0 | Preloads when the decoded size is known and at most `AutoPreloadMaxBytes` (1 MiB by default); streams otherwise. |
| `Preload` | 1 | Decodes the whole source into the cache, up to `MaxPreloadBytes` (16 MiB by default). |
| `Streaming` | 2 | Reads incrementally while playing. |
