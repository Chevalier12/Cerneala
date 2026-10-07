# SoundPlaybackState Enum

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundPlaybackState.cs`

State of a [SoundPlayback](Cerneala.Timbre.SoundPlayback.md).

```csharp
public enum SoundPlaybackState
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

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Pending` | 0 | Accepted; no PCM produced yet. |
| `Playing` | 1 | Producing PCM. |
| `Paused` | 2 | Suspended; keeps reader, position, and DSP state. |
| `Completed` | 3 | Finished naturally and drained. |
| `Canceled` | 4 | Canceled or replaced. |
| `Failed` | 5 | Ended with a [SoundException](Cerneala.Timbre.SoundException.md). |
