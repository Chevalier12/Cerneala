# TimbreRuntimeOptions Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreRuntimeOptions.cs`

Configuration copied by the [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md) constructor.

```csharp
public sealed class TimbreRuntimeOptions
```

## Examples

```csharp
using Cerneala.Timbre;

var runtime = new TimbreRuntime(new TimbreRuntimeOptions
{
    Output = output,
    MaxVoices = 32,
    MaxCacheBytes = 32L * 1024 * 1024
});
```

## Remarks

Changing the options object after the runtime is constructed has no effect. The constructor throws `ArgumentOutOfRangeException` for non-positive counts and byte limits or a negative tail cap.

`MaxVoices` counts every non-terminal playback, including pending, paused, and draining ones. A start beyond it is rejected synchronously with `TimbreErrorKind.VoiceLimitExceeded`; existing playbacks are never stolen.

`StreamingMemoryLimit` is an explicit cooperative allowance shared by the streaming buffers and by every reservation made through a [TimbreMemoryBudget](Cerneala.Timbre.TimbreMemoryBudget.md): file read buffers, decoder state, and budget-aware source factories. Exceeding it refuses the source before the allocation, with `TimbreErrorKind.ResourceLimitExceeded`. Its default, `long.MaxValue`, is accounting headroom only: nothing of that size is allocated, and no numeric cap applies unless one is configured.

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Output` | `ITimbreOutput?` | `null` | Output for the final mix; `null` fails playbacks with `DeviceUnavailable`. |
| `MaxVoices` | `int` | 64 | Maximum non-terminal playbacks. |
| `AutoPreloadMaxBytes` | `long` | 1 MiB | Largest decoded size preloaded by `TimbreLoading.Auto`. |
| `MaxPreloadBytes` | `long` | 16 MiB | Largest decoded payload per clip. |
| `MaxCacheBytes` | `long` | 64 MiB | Total preload cache. |
| `StreamingMemoryLimit` | `long` | `long.MaxValue` | Explicit allowance for streaming buffers and reader/decoder reservations. |
| `DelayTailCap` | `TimeSpan` | 30 s | Longest tail produced after the end of a source. |
| `BaseDirectory` | `string?` | `null` | Base for relative file sources; `null` uses `AppContext.BaseDirectory`. |
