# Timbre Sound Guide

Timbre is Cerneala's sound engine. It plays short UI sounds and longer music
through one mixer, with the same runtime whether a sound is started from C# or
from `.crn` markup. This guide explains the model and the rules both paths share.
The per-type reference lives in the API pages (`Cerneala.Timbre.*`,
`Cerneala.UI.Markup.GeneratedMarkup`) in `docs-site/documentation/classes/`.

## 1. Model

| Concept | Type | Role |
| --- | --- | --- |
| Runtime | `SoundRuntime` | One mixer, voice budget, decoder cache and output. Shared by every window of an `Application`. |
| Scope | `SoundScope` | Owner of playbacks. Disposing a scope cancels everything it started. `UIElement.Sounds` is a scope tied to the element's attachment. |
| Clip | `SoundClip` | Immutable definition: source, volume, loop, loading policy, typed parameters and an ordered modifier chain. Creating or referencing one plays nothing and opens no file. |
| Playback | `SoundPlayback` | One running instance: `State`, `Position`, `Duration`, `Completion`, `Pause`, `Resume`, `SeekAsync`, `Cancel`, `Set`. |
| Handle | `SoundHandle` | A slot in a scope. Playing into a handle replaces (cancels) its previous occupant. |

The runtime mixes 48 kHz stereo float PCM. Sources are WAV, MP3, Ogg Vorbis and
Ogg Opus, opened lazily when a playback first needs them.

### Loading

`SoundLoading.Auto` (default) preloads files up to 1 MiB and streams larger
ones; `Preload` decodes the whole clip (up to 16 MiB per clip, 64 MiB cache);
`Streaming` decodes incrementally without memory proportional to duration. The
runtime holds at most 64 voices, paused voices included.

### Output and devices

Only the default device is used. The output opens lazily when a playback first
produces PCM, never at declaration. A missing device, an open failure or a
device loss fails the affected playbacks with `SoundErrorKind.DeviceUnavailable`;
nothing reconnects or retries on its own. A later explicit play may try again.

## 2. Playing from C#

```csharp
SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
SoundClip confirm = new(
    "audio/confirm.wav",
    volume: 0.8f,
    parameters: [cutoff],
    modifiers: [new LowPass(cutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);

SoundPlayback playback = button.Sounds.Play(confirm, start => start.Set(cutoff, 800f));
```

Relative paths resolve against `SoundRuntimeOptions.BaseDirectory`, which
defaults to the application base directory. `Application.SoundRuntime` is
created lazily on first use; assign your own runtime before that if you need
other options. `UiHostOptions.SoundRuntime` and `UIRoot.SetSoundRuntime` attach
a runtime to hosts without an `Application`.

### Overlap, replacement and cancellation

- `scope.Play(clip)` without a handle always starts a new, overlapping playback.
- `scope.Play(clip, handle: slot)` cancels the slot's current occupant first.
- `Cancel` stops only that playback. PCM already queued for the output (at most
  40 ms) may still be heard; the shared output is never flushed.
- Disposing the scope, detaching the element or closing its window cancels every
  playback the scope owns.

### Pause, resume, seek and loop

- `Pause` holds the position. Pausing a pending playback (source still loading)
  guarantees no PCM is produced until `Resume`. Paused playbacks keep their
  voice.
- `Resume` continues; DSP state (filter memory, echoes) is preserved.
- `SeekAsync(position)` is absolute in the source's timeline and never blocks
  the caller. The latest request wins: an earlier pending seek completes as
  canceled. A successful seek resets DSP state. `Cancel` also cancels a pending
  seek.
- `Loop = true` repeats the whole source until cancel, replacement, detach or
  failure. A loop keeps DSP state across repetitions and never reports
  `Completed`. Loop is fixed when the playback starts.
- A terminal playback (`Completed`, `Canceled`, `Failed`) rejects pause, resume,
  seek and parameter changes; replaying needs a new `Play`.

`Completion` finishes after the source ends, the modifier tail decays (capped
at 30 s) and the output has drained the playback's PCM. It reports what the
engine delivered to the output, not what a listener heard.

## 3. Declaring sounds in markup

A `SoundClip` is a resource. It may live in any `Resources` collection or in
`App.crn`, and it follows the normal resource scope and shadowing rules. It
does not take `TargetType`.

```xml
<UserControl.Resources>
    <SoundClip Name="ConfirmSound">
        Source = "audio/confirm.wav";
        Volume = 0.8;
        @parameter ToneCutoff: float = 1200;
        @parameter EchoMix: float = 0.15;
        @modifier LowPass { Cutoff = ToneCutoff; }
        @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
    </SoundClip>
    <SoundClip Name="Music">
        Source = "audio/music.ogg";
        Loop = true;
    </SoundClip>
</UserControl.Resources>
```

| Statement | Meaning |
| --- | --- |
| `Source = "path";` | Required. A local file path, resolved at runtime like the C# path. URIs are rejected. The compiler does not open the file. |
| `Volume = 0.8;` | Default volume, 0–1. |
| `Loop = true;` | Default loop, `true` or `false`. |
| `@parameter Name: float = value;` | A typed parameter descriptor with its default. |
| `@modifier LowPass { Cutoff = ...; }` | Low-pass filter, cutoff 20–20000 Hz. |
| `@modifier Delay { Time = ...; Feedback = ...; Mix = ...; }` | Echo: time 1 ms–2 s (`120ms`, `0.12s` or seconds), feedback 0–0.95, mix 0–1. |

Modifier inputs take a constant or a declared parameter. A parameter's default
must fit every modifier input it feeds. Modifiers run in source order. The
generator lowers the resource to the same `SoundClip`, `SoundParameter<float>`,
`LowPass` and `Delay` constructors shown in the C# example.

## 4. Sound actions in an Aspect

Sound actions are statements inside an `@on`, `@when` or `@if` body of any
`<Aspect>` or `{Control}.Aspect`, including Scene2D aspects. Every statement ends
with `;`.

```xml
<Button Content="Play">
    <Button.Aspect>
        @handle Playback;
        @on Click { @sound $ConfirmSound(Volume = 0.5, ToneCutoff = 800) as Playback; }
        @on MouseRightButtonUp { @pause Playback; }
        @on MouseLeave { @resume Playback; }
        @on MouseWheel { @seek Playback to 30s; }
        @on LostFocus { @cancel Playback; }
    </Button.Aspect>
</Button>
```

| Statement | Effect |
| --- | --- |
| `@sound $Clip;` | Plays the clip in the Aspect's scope. Without `as`, playbacks overlap. |
| `@sound $Clip(args);` | Overrides `Volume`, `Loop` and the clip's parameters for this start only. |
| `@sound $Clip as Handle;` | Plays into the handle slot, replacing its occupant. |
| `@handle Handle;` | Declares a slot, at the top of the Aspect before use. |
| `@cancel Handle;` | Cancels the slot's current occupant only, not other playbacks of the clip. |
| `@pause Handle;` / `@resume Handle;` | Pause/resume the occupant captured when the action runs. |
| `@seek Handle to 30s;` | Requests an absolute seek (`s` or `ms`, non-negative); does not wait. |

An empty slot makes `@cancel`, `@pause`, `@resume` and `@seek` no-ops; they never
start a sound. A handle may receive different clips over time. The same name
cannot be both a sound handle and a Motion handle in one Aspect.

Sound actions cannot appear inside `@parallel` or `@sequence`. In a body that
mixes sound and Motion, statements run in source order: a `@sound` binds its
playback and overrides before the following Motion starts, and nothing waits
for the source to load. Several Motion executions in one body still need
explicit composition.

### Scope and lifetime

Each concrete Aspect application owns its own sound scope and handle slots —
one per element, template part or item occurrence. Two buttons sharing a named
Aspect do not share handles. Detaching the element, replacing its Aspect or
retiring a template occurrence cancels the playbacks of that scope only, and a
callback from a retired scope never touches a new occupant. Playbacks without a
handle are owned by the scope too.

### Events

`@on Event` subscribes while the element is attached. Routed input (clicks,
hover, wheel, keys) reaches the Aspect exactly like a C# handler.

### Reactive conditions

`@when` and `@if` bodies play when their condition becomes true:

- initially true when the element attaches: plays once;
- re-evaluation that stays true: does not repeat;
- true → false: does not stop anything (there is no deactivation);
- false → true: plays again.

Audio activations do not depend on rendering. A hidden or collapsed element (or
ancestor) keeps its sounds and transport, condition changes while hidden still
act, and hide → show with a condition that stayed true does not replay. A
window hidden with `Window.Hide` may stop pumping its UI, which delays its
condition processing; that is the window's lifecycle, not an audio rule.

### Errors

A synchronous error (for example a clip resource that cannot be resolved at
runtime) throws from the action and stops the rest of the body. Asynchronous
failures — a missing file, a decode error, an unavailable device — end that
playback as `Failed` and do not undo actions that already started.

## 5. Tooling

The language server, the Visual Studio extension and the generator use one
binding of the sound syntax:

- diagnostics `CERNEALAUI030` (syntax), `031` (references: unknown clip,
  parameter, handle, modifier, duplicates), `032` (values: ranges, types, seek
  durations, `Source`) and `033` (context: actions outside `@on`/`@when`/`@if`,
  inside `@parallel`/`@sequence`), identical for a loose `.crn` file and a
  project document;
- completion for clip properties, `@parameter`/`@modifier`, modifier inputs,
  `$Clip` references and arguments, handles, `to` and seek durations, plus
  signature help for `$Clip(` and go-to-definition for clips, handles and
  parameters.

## 6. Live Preview

Preview audio is **off by default**. A previewed document that starts a sound
gets an explicit failure (`DeviceUnavailable`, "Live Preview audio is
disabled…"), never a silent success, and the preview status shows
`audio off (N blocked)`. The **Audio** button in the Live Preview bar enables
playback for that preview; changing it recreates the preview. A recompile
retires the previous preview and every sound it owned; nothing is restored, and
the new document's reactive rules apply from scratch. Editing a `SoundClip` body
or an Aspect body always recompiles instead of patching the running preview.
