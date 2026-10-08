# TimbrePlaybackResult Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbrePlaybackResult.cs`

Final outcome of a [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md), produced by its `Completion` task.

```csharp
public sealed class TimbrePlaybackResult
```

## Examples

```csharp
using Cerneala.Timbre;

TimbrePlaybackResult result = await playback.Completion;
switch (result.State)
{
    case TimbrePlaybackState.Completed when result.TailTruncated:
        break; // the delay tail reached DelayTailCap
    case TimbrePlaybackState.Failed:
        Console.WriteLine($"{result.Error!.Kind}: {result.Error.Message}");
        break;
}
```

## Remarks

`State` is always terminal: `Completed`, `Canceled`, or `Failed`. `Error` is non-null only for `Failed`. `TailTruncated` is `true` only for a `Completed` playback whose tail was cut by the configured cap, which is not a natural end of the tail.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `TimbrePlaybackState` | Terminal state. |
| `Error` | `TimbreException?` | Failure cause. |
| `TailTruncated` | `bool` | Whether the tail cap cut the tail. |
