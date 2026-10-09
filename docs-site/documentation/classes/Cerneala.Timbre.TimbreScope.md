# TimbreScope Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreScope.cs`

Owner of playbacks and handles, connected to one [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md).

```csharp
public sealed class TimbreScope : IDisposable
```

Inheritance:
`object` -> `TimbreScope`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.Timbre;

var plainTimbre = new TimbreSound("audio/confirm.wav");
TimbrePlayback first = sounds.Play(plainTimbre);
TimbrePlayback second = sounds.Play(plainTimbre); // overlaps: independent instances
first.Cancel();                                 // second keeps playing

TimbreHandle slot = sounds.CreateHandle();
sounds.Play(plainTimbre, handle: slot);
sounds.Play(plainTimbre, start => start.Volume = 0.5f, handle: slot); // replaces only the slot's occupant
```

## Remarks

Obtain a scope from `TimbreRuntime.CreateScope()`, from `UIElement.Timbre` (a lifecycle scope disposed on detach), or from `Application.Timbre`.

`Play` returns a new [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md) identity immediately in the `Pending` state; it never waits for loading, the output, or the end of the sound. Without a handle, playbacks overlap. With a handle, a successful start cancels only that handle's current occupant.

`Play` runs synchronously in this order: argument and handle checks, the `configure` delegate (exactly once, on the calling thread), validation of the configured values, voice admission, and only then replacement of the handle's occupant. Any exception up to that point — including one thrown by the delegate, which propagates unchanged — creates no playback and leaves the previous occupant playing. A later asynchronous failure of the new playback does not restore the replaced one.

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `clip` is `null`. |
| `ArgumentException` | `handle` belongs to another scope, or `configure` sets a parameter not declared by `clip`. |
| `ArgumentOutOfRangeException` | A configured value is NaN, infinite, or out of range. |
| `TimbreException` (`VoiceLimitExceeded`) | The runtime already has `MaxVoices` non-terminal playbacks after accounting for the replaced occupant. |
| `ObjectDisposedException` | The scope or its runtime is disposed. |

`Dispose` cancels every non-terminal playback started by the scope and empties its handles. It is idempotent and does not affect other scopes or close the shared output.

Cancellation and replacement mark the old identity terminal synchronously, while its rendering voice fades out over 5 ms before releasing. A handle replacement fades in over 5 ms as soon as its source is ready and may briefly overlap the old voice. Scope disposal uses the cancellation fade; runtime disposal closes the output without waiting for fades. Already-queued PCM is not altered. See [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md).

Members are thread-safe.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Runtime` | `TimbreRuntime` | The runtime that mixes this scope's playbacks. |
| `IsDisposed` | `bool` | Whether the scope was disposed. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Play(TimbreSound sound, Action<TimbreStartOptions>? configure = null, TimbreHandle? handle = null)` | `TimbrePlayback` | Starts a playback. |
| `CreateHandle()` | `TimbreHandle` | Creates a reusable slot local to this scope. |
| `Dispose()` | `void` | Cancels the scope's playbacks. |

## See also

- [TimbreHandle](Cerneala.Timbre.TimbreHandle.md)
- [TimbreStartOptions](Cerneala.Timbre.TimbreStartOptions.md)
