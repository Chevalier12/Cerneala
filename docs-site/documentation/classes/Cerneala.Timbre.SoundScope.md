# SoundScope Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundScope.cs`

Owner of playbacks and handles, connected to one [SoundRuntime](Cerneala.Timbre.SoundRuntime.md).

```csharp
public sealed class SoundScope : IDisposable
```

Inheritance:
`object` -> `SoundScope`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.Timbre;

var plainSound = new SoundClip("audio/confirm.wav");
SoundPlayback first = sounds.Play(plainSound);
SoundPlayback second = sounds.Play(plainSound); // overlaps: independent instances
first.Cancel();                                 // second keeps playing

SoundHandle slot = sounds.CreateHandle();
sounds.Play(plainSound, handle: slot);
sounds.Play(plainSound, start => start.Volume = 0.5f, handle: slot); // replaces only the slot's occupant
```

## Remarks

Obtain a scope from `SoundRuntime.CreateScope()`, from `UIElement.Sounds` (a lifecycle scope disposed on detach), or from `Application.Sounds`.

`Play` returns a new [SoundPlayback](Cerneala.Timbre.SoundPlayback.md) identity immediately in the `Pending` state; it never waits for loading, the output, or the end of the sound. Without a handle, playbacks overlap. With a handle, a successful start cancels only that handle's current occupant.

`Play` runs synchronously in this order: argument and handle checks, the `configure` delegate (exactly once, on the calling thread), validation of the configured values, voice admission, and only then replacement of the handle's occupant. Any exception up to that point — including one thrown by the delegate, which propagates unchanged — creates no playback and leaves the previous occupant playing. A later asynchronous failure of the new playback does not restore the replaced one.

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `clip` is `null`. |
| `ArgumentException` | `handle` belongs to another scope, or `configure` sets a parameter not declared by `clip`. |
| `ArgumentOutOfRangeException` | A configured value is NaN, infinite, or out of range. |
| `SoundException` (`VoiceLimitExceeded`) | The runtime already has `MaxVoices` non-terminal playbacks after accounting for the replaced occupant. |
| `ObjectDisposedException` | The scope or its runtime is disposed. |

`Dispose` cancels every non-terminal playback started by the scope and empties its handles. It is idempotent and does not affect other scopes or close the shared output.

Members are thread-safe.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Runtime` | `SoundRuntime` | The runtime that mixes this scope's playbacks. |
| `IsDisposed` | `bool` | Whether the scope was disposed. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Play(SoundClip clip, Action<SoundStartOptions>? configure = null, SoundHandle? handle = null)` | `SoundPlayback` | Starts a playback. |
| `CreateHandle()` | `SoundHandle` | Creates a reusable slot local to this scope. |
| `Dispose()` | `void` | Cancels the scope's playbacks. |

## See also

- [SoundHandle](Cerneala.Timbre.SoundHandle.md)
- [SoundStartOptions](Cerneala.Timbre.SoundStartOptions.md)
