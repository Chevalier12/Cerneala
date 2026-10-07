# SoundPlaybackResult Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundPlaybackResult.cs`

Final outcome of a [SoundPlayback](Cerneala.Timbre.SoundPlayback.md), produced by its `Completion` task.

```csharp
public sealed class SoundPlaybackResult
```

## Examples

```csharp
using Cerneala.Timbre;

SoundPlaybackResult result = await playback.Completion;
switch (result.State)
{
    case SoundPlaybackState.Completed when result.TailTruncated:
        break; // the delay tail reached DelayTailCap
    case SoundPlaybackState.Failed:
        Console.WriteLine($"{result.Error!.Kind}: {result.Error.Message}");
        break;
}
```

## Remarks

`State` is always terminal: `Completed`, `Canceled`, or `Failed`. `Error` is non-null only for `Failed`. `TailTruncated` is `true` only for a `Completed` playback whose tail was cut by the configured cap, which is not a natural end of the tail.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `SoundPlaybackState` | Terminal state. |
| `Error` | `SoundException?` | Failure cause. |
| `TailTruncated` | `bool` | Whether the tail cap cut the tail. |
