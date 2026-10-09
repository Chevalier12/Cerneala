# TimbrePlaybackState Enum

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbrePlaybackState.cs`

State of a [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md).

```csharp
public enum TimbrePlaybackState
```

## Remarks

| From | Event | To |
| --- | --- | --- |
| — | `Play` accepted | `Pending` |
| `Pending` | first PCM block mixed | `Playing` |
| `Pending`, `Playing` | `Pause()` | `Paused` |
| `Paused` | `Resume()` | `Playing`, or `Pending` if no PCM was produced yet |
| any non-terminal | `Cancel()`, handle replacement, scope or runtime disposal | `Canceled` |
| any non-terminal | source, data, resource, or device failure | `Failed` |
| `Playing` | end of source, end of tail, output drained | `Completed` |

`Completed`, `Canceled`, and `Failed` are terminal.

`Paused` and `Canceled` are published synchronously, before the mixer's 5 ms fade-out has necessarily finished. Source/DSP state freezes at the end of the pause fade; a canceled rendering voice is released after its fade. Already-queued PCM may still play. See [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md) for the transport contract.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Pending` | 0 | Accepted; no PCM produced yet. |
| `Playing` | 1 | Producing PCM. |
| `Paused` | 2 | Suspended; keeps reader, position, and DSP state. |
| `Completed` | 3 | Finished naturally and drained. |
| `Canceled` | 4 | Canceled or replaced. |
| `Failed` | 5 | Ended with a [TimbreException](Cerneala.Timbre.TimbreException.md). |
