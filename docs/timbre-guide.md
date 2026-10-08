# Timbre Guide

Timbre is Cerneala's sound engine. It plays short UI sounds and longer music
through one mixer, with the same runtime whether a sound is started from C# or
from `.crn` markup. This guide explains the model and the rules both paths share.
The per-type reference lives in the API pages (`Cerneala.Timbre.*`,
`Cerneala.UI.Markup.GeneratedMarkup`) in `docs-site/documentation/classes/`.

## 1. Model

| Concept | Type | Role |
| --- | --- | --- |
| Runtime | `TimbreRuntime` | One mixer, voice budget, decoder cache and output. Shared by every window of an `Application`. |
| Scope | `TimbreScope` | Owner of playbacks. Disposing a scope cancels everything it started. `UIElement.Timbre` is a scope tied to the element's attachment. |
| Sound | `TimbreSound` | Immutable definition: source, volume, loop, loading policy, typed parameters and an ordered modifier chain. Creating or referencing one plays nothing and opens no file. |
| Clip | `TimbreClipDefinition` | Named sounds (`TimbreClipSound`, with `AutoPlay`) and the parameters they share: the C# form of a markup `<TimbreClip>`, attached by an Aspect's `@timbre`. |
| Playback | `TimbrePlayback` | One running instance: `State`, `Position`, `Duration`, `Completion`, `Pause`, `Resume`, `SeekAsync`, `Cancel`, `Set`. |
| Handle | `TimbreHandle` | A slot in a scope. Playing into a handle replaces (cancels) its previous occupant. |

The runtime mixes 48 kHz stereo float PCM. Sources are WAV, MP3, Ogg Vorbis and
Ogg Opus, opened lazily when a playback first needs them.

### Loading

`TimbreLoading.Auto` (default) preloads a source when its decoded PCM size is
known and is at most `TimbreRuntimeOptions.AutoPreloadMaxBytes` (1 MiB by
default); it streams larger sources and sources whose decoded length is
unknown. The comparison uses the canonical 48 kHz stereo float PCM size:
`lengthFrames * 8` bytes, not the encoded file size. The default threshold
therefore covers about 2.7 seconds of audio. A compressed file smaller than
1 MiB can still stream if its decoded PCM exceeds that threshold.

`Preload` decodes the whole clip (up to 16 MiB of decoded PCM per clip, 64 MiB
cache by default); `Streaming` decodes incrementally without memory
proportional to duration. The runtime holds at most 64 voices, paused voices
included.

### Output and devices

Only the default device is used. The output opens lazily when a playback first
produces PCM, never at declaration. A missing device, an open failure or a
device loss fails the affected playbacks with `TimbreErrorKind.DeviceUnavailable`;
nothing reconnects or retries on its own. A later explicit play may try again.

## 2. Playing from C#

```csharp
TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
TimbreSound confirm = new(
    "audio/confirm.wav",
    volume: 0.8f,
    parameters: [cutoff],
    modifiers: [new LowPass(cutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);

TimbrePlayback playback = button.Timbre.Play(confirm, start => start.Set(cutoff, 800f));
```

Relative paths resolve against `TimbreRuntimeOptions.BaseDirectory`, which
defaults to the application base directory. `Application.TimbreRuntime` is
created lazily on first use; assign your own runtime before that if you need
other options. `UiHostOptions.TimbreRuntime` and `UIRoot.SetTimbreRuntime` attach
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

A `<TimbreClip>` is a set of named sounds with shared parameters, the audio
counterpart of a `<PrismClip>` and its layers. It is a resource: it may live in
any `Resources` collection or in `App.crn`, and it follows the normal resource
scope and shadowing rules. It does not take `TargetType`.

```xml
<UserControl.Resources>
    <TimbreClip Name="UiSounds">
        @parameter ToneCutoff: float = 1200;
        @parameter EchoMix: float = 0.15;

        @sound Confirm
        {
            Source = "audio/confirm.wav";
            Volume = 0.8;
            @modifier LowPass { Cutoff = ToneCutoff; }
            @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
        }

        @sound Music
        {
            Source = "audio/music.ogg";
            Loop = true;
            AutoPlay = true;
        }
    </TimbreClip>
</UserControl.Resources>
```

| Statement | Meaning |
| --- | --- |
| `@parameter Name: float = value;` | A clip parameter with its default, written at the top of the clip. Every sound may use it. |
| `@sound Name { … }` | One named sound. Names are unique in the clip and written without `$`. |
| `Source = "path";` | Required in a `@sound`. A local file path, resolved at runtime like the C# path. URIs are rejected. The compiler does not open the file. |
| `Volume = 0.8;` | The sound's volume, 0–1. |
| `Loop = true;` | Whether the sound repeats, `true` or `false`. |
| `AutoPlay = true;` | Whether the sound starts every time an Aspect attaches the clip. Default `false`. |
| `@modifier LowPass { Cutoff = ...; }` | Low-pass filter, cutoff 20–20000 Hz. |
| `@modifier Delay { Time = ...; Feedback = ...; Mix = ...; }` | Echo: time 1 ms–2 s (`120ms`, `0.12s` or seconds), feedback 0–0.95, mix 0–1. |

Modifier inputs take a constant or a clip parameter declared above them. A
parameter's default must fit every modifier input it feeds, in every sound.
Modifiers run in source order. The generator lowers the resource to the C#
`TimbreClipDefinition`, `TimbreClipSound`, `TimbreSound`, `TimbreParameter<float>`,
`LowPass` and `Delay` constructors; the clip above is the same as:

```csharp
TimbreParameter<float> toneCutoff = new("ToneCutoff", 1200f);
TimbreParameter<float> echoMix = new("EchoMix", 0.15f);
TimbreClipDefinition uiSounds = new(
    "UiSounds",
    sounds:
    [
        new TimbreClipSound("Confirm", new TimbreSound(
            "audio/confirm.wav",
            volume: 0.8f,
            parameters: [toneCutoff, echoMix],
            modifiers: [new LowPass(toneCutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: echoMix)])),
        new TimbreClipSound("Music", new TimbreSound("audio/music.ogg", loop: true), autoPlay: true)
    ],
    parameters: [toneCutoff, echoMix]);

// Plain C# needs no Aspect: play one sound of the clip on an element.
button.Timbre.Play(uiSounds.Sounds["Confirm"].Sound);
```

## 4. Sounds in an Aspect

An Aspect brings its sounds with one `@timbre`, written at the top of its body
(not inside `@on`, `@when` or `@if`), like `@prism` brings a Prism effect:

- `@timbre $UiSounds;` uses a `<TimbreClip>` resource;
- `@timbre $UiSounds(ToneCutoff = 800);` sets clip parameters for this
  application;
- `@timbre { @sound Click { Source = "audio/click.wav"; } }` declares the clip
  inline, with the same body as `<TimbreClip>`.

An Aspect has at most one `@timbre`. The commands play and control its sounds by
their full path; every command ends with `;`:

```xml
<UserControl.Resources>
    <TimbreClip Name="Clicks">
        @sound Click { Source = "audio/click.wav"; }
    </TimbreClip>
</UserControl.Resources>

<Button Content="Play">
    <Button.Aspect>
        @timbre $Clicks;
        @on Click { @play $self.timbre.Click; }
    </Button.Aspect>
</Button>

<Border Name="Speaker">
    <Border.Aspect>
        @timbre
        {
            @sound Music { Source = "audio/music.ogg"; Loop = true; Volume = 0.4; AutoPlay = true; }
        }
    </Border.Aspect>
</Border>

<Button Content="Pause music">
    <Button.Aspect>
        @on Click { @pause $Speaker.timbre.Music; }
        @on MouseRightButtonUp { @resume $Speaker.timbre.Music; }
        @on MouseWheel { @seek $Speaker.timbre.Music to 30s; }
        @on LostFocus { @stop $Speaker.timbre.Music; }
    </Button.Aspect>
</Button>
```

| Statement | Effect |
| --- | --- |
| `@play $self.timbre.Click;` | Starts the sound. If it is already playing, it starts again from the beginning (the running playback is canceled). |
| `@stop $self.timbre.Click;` | Stops the sound's running playback. |
| `@pause …;` / `@resume …;` | Pause or resume the sound's running playback. |
| `@seek $self.timbre.Music to 30s;` | Requests an absolute seek (`s` or `ms`, non-negative); does not wait. |

The path names the element and the sound:

- `$self.timbre.Sound` — a sound of this Aspect's own `@timbre`;
- `$Speaker.timbre.Sound` — a sound of the element named `Speaker`, resolved in
  the namescope where the Aspect is written, as for every `$Name` (an Aspect in
  `App.crn` can use only `$self` and `$owner`);
- `$owner.timbre.Sound` — a sound of the template owner.

There is no short form (`@play Click;` or `@play $Click;` are errors), and
`@pause $Speaker;` is an error too: `Speaker` is an element, not a sound. The
compiler checks that the sound exists in the Aspect the target has in markup.
At runtime the command reaches the sounds of the Aspect the target has *now*:
if Speaker's Aspect is replaced by another that also has a `Music` sound, the
command keeps working; if the target is detached, or its current Aspect has no
such sound, the command throws `InvalidOperationException`.

A sound that is not playing makes `@stop`, `@pause`, `@resume` and `@seek`
no-ops; they never start it. Commands cannot appear inside `@parallel` or
`@sequence`. In a body that mixes sound and Motion, statements run in source
order: a `@play` binds its playback before the following Motion starts, and
nothing waits for the source to load. Several Motion executions in one body
still need explicit composition.

### Scope and lifetime

Each application of an Aspect attaches its own sounds — one per element,
template part or item occurrence. Two buttons sharing a named Aspect do not
share playbacks. The `@timbre` resource is looked up when the Aspect is
applied: replacing the resource in C# reaches the next application, not the
running one. Every sound with `AutoPlay = true` starts at each application.
Detaching the element, replacing its Aspect or retiring a template occurrence
stops every sound that application started.

The new Aspect brings its own sounds: after `button.Aspect = otherAspect;` in
C#, the sounds of the old Aspect stop, the `AutoPlay` sounds of `otherAspect`
start, the next click runs the `@on Click` of `otherAspect`, and a `@when`
condition of `otherAspect` that is already true plays once, as on a first
attach. See *Applying and replacing an aspect* in the
[Cerneala Markup Guide](CernealaMarkupGuide.md).

### Events

`@on Event` subscribes while the element is attached. Routed input (clicks,
hover, wheel, keys) reaches the Aspect exactly like a C# handler.

### Reactive conditions

`@when` and `@if` bodies run their commands when their condition becomes true:

- initially true when the element attaches: runs once;
- re-evaluation that stays true: does not repeat;
- true → false: does not stop anything (there is no deactivation);
- false → true: runs again.

Audio does not depend on rendering. A hidden or collapsed element (or ancestor)
keeps its sounds and transport, condition changes while hidden still act, and
hide → show with a condition that stayed true does not replay. A window hidden
with `Window.Hide` may stop pumping its UI, which delays its condition
processing; that is the window's lifecycle, not an audio rule.

### Errors

A synchronous error (a clip resource that cannot be resolved at runtime, a
command to a target without that sound) throws and stops the rest of the body.
Asynchronous failures — a missing file, a decode error, an unavailable device —
end that playback as `Failed` and do not undo actions that already started.

## 5. Animating sounds with Motion

Motion animates the Volume and the parameters of a sound's running playback.
The target path is `$self.timbre.Sound.Property` (or `$Name`/`$owner` instead of
`$self`):

```xml
<UserControl.Resources>
    <TimbreClip Name="Chimes">
        @parameter Brightness: float = 800;
        @sound Chime
        {
            Source = "audio/chime.wav";
            Volume = 0.2;
            @modifier LowPass { Cutoff = Brightness; }
        }
    </TimbreClip>
</UserControl.Resources>

<Button Content="Chime">
    <Button.Aspect>
        @timbre $Chimes;
        @on Click
        {
            @play $self.timbre.Chime;
            @animate with Tween(300ms, EaseOut)
            {
                @to
                {
                    $self.timbre.Chime.Volume = 0.8;
                    $self.timbre.Chime.Brightness = 6000;
                }
            }
        }
    </Button.Aspect>
</Button>
```

- `Volume` is always animatable (0–1). Another name must be a clip parameter the
  sound uses (one of its modifiers reads it); its range is the intersection of
  the inputs it feeds. `Source`, `Loop`, the position, modifier names and their
  inputs are not targets; seeking stays an explicit `@seek`.
- Values are numbers within the range, or `current`. `@animate` (with `@from`
  and `@to`), `@keyframes`, and `@parallel`/`@sequence` of such executions are
  supported; `@set`, `@scroll`, `@stagger`, `MotionClip` bodies and bindings as
  values are not. One execution cannot mix sound targets with element or Prism
  targets; animate them in separate `@on` blocks.
- The animation captures the sound's running playback when it starts (for
  example after a `@play` earlier in the same body, or a playback started by
  `AutoPlay`). It never moves to a later playback: restarting or stopping the
  sound ends its animations, and the new playback starts from its own values.
  Animating a sound that is not playing does nothing and plays nothing.
- Animation time starts with the playback's first PCM, not while it loads. It
  holds while the playback is paused (also when paused before starting) and
  while a seek is pending, then continues without restarting; a looping source
  does not restart it. Visual Motion keeps its own clock.
- Timbre animations belong to the Aspect application that starts them: hiding
  or collapsing the element or an ancestor does not stop them, and detaching
  the element, replacing its Aspect or retiring a template occurrence cancels
  them. A window hidden with `Window.Hide` is not pumped, so its sound
  animations are not sampled until it is shown again (the playback itself
  keeps playing); the first frame afterwards uses the usual Motion maximum
  delta of 100 ms.
- Samples are published to the playback once per UI frame and affect only PCM
  mixed afterwards (block granularity); they cause no layout or render work.
  The visual Reduced Motion preference does not disable sound animations.
- A sample that is not finite or leaves the range (for example a bouncy spring
  overshooting Volume 1) ends only that parameter's animation on its last valid
  value; the sound continues and `Detective.CaptureTimbre().MotionSamplesRejected`
  counts it. Values are never clamped.

The same animations are available from C# on any playback whose scope belongs
to an element (`element.Timbre`):

```csharp
TimbreSound chime = new("audio/chime.wav");
TimbrePlayback playback = button.Timbre.Play(chime, start => start.Volume = 0.2f);
MotionHandle fade = playback.Motion()
    .Animate(TimbrePlayback.VolumeParameter)
    .To(0.8f)
    .With(new TweenSpec<float>(TimeSpan.FromMilliseconds(300), Easings.EaseOut));
```

`Animate` takes `TimbrePlayback.VolumeParameter` or a `TimbreParameter<float>` of
the playback's sound; `From` is optional and `With` returns a cancelable
`MotionHandle`. Assigning `Volume` or calling `Set` cancels only that
parameter's animation. Playbacks of `Application.Timbre` or a standalone
runtime name the sampling root explicitly: `playback.Motion(root)`.

## 6. Tooling

The language server, the Visual Studio extension and the generator use one
binding of the sound syntax:

- diagnostics `CERNEALAUI030` (syntax: clip shape, `@sound`, command paths),
  `031` (references: unknown clip, parameter, sound, element, modifier,
  duplicates), `032` (values: ranges, types, seek durations, `Source`) and `033`
  (context: `@timbre` outside the top of an Aspect, commands outside
  `@on`/`@when`/`@if` or inside `@parallel`/`@sequence`), identical for a loose
  `.crn` file and a project document;
- completion for `@parameter`/`@sound`, sound properties, `@modifier` and its
  inputs, `@timbre $Clip` references and arguments, sound paths, `to` and seek
  durations, plus signature help for `$Clip(` and go-to-definition for clips,
  sounds and parameters.

## 7. Live Preview

Preview audio is **off by default**. A previewed document that starts a sound
gets an explicit failure (`DeviceUnavailable`, "Live Preview audio is
disabled…"), never a silent success, and the preview status shows
`audio off (N blocked)`. The **Audio** button in the Live Preview bar enables
playback for that preview; changing it recreates the preview. A recompile
retires the previous preview and every sound it owned; nothing is restored, and
the new document's reactive rules apply from scratch. Editing a `TimbreClip` body
or an Aspect body always recompiles instead of patching the running preview.
