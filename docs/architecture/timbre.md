# Timbre

> Code: `Timbre`, `UI/Timbre`, `UI/Elements/UIRoot.Timbre.cs`, `UI/Markup/GeneratedMarkupTimbre.cs`, `Cerneala.Platforms.Sdl3/Audio` · Verified at commit `0cb32300` (2026-10-10)

How to use Timbre from markup and code is in the [Timbre guide](../guides/timbre-guide.md). This document describes how the runtime works inside.

## Responsibility

Timbre plays sounds. It decodes WAV, Ogg Vorbis, Ogg Opus and MP3 into one canonical format (48,000 Hz, 2 channels, 32-bit float), mixes up to 64 playbacks on its own thread, and writes the mix to one output. It does not record audio, has no 3D positioning, and has no limiter: the mix is hard-clipped to ±1.

The UI side connects sounds to elements: a sound started by an element stops when the element is detached.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `TimbreRuntime` | `Timbre/TimbreRuntime.cs` (+ `.Loading.cs`, `.Mixer.cs`) | Admission, the `live` list, the mixer thread, the payload cache, the memory pool | `Application.TimbreRuntime` (created on first use, shared by every window) or a caller-owned runtime |
| `TimbreRuntimeOptions` | `Timbre/TimbreRuntimeOptions.cs` | `Output`, `MaxVoices`, `AutoPreloadMaxBytes`, `MaxPreloadBytes`, `MaxCacheBytes`, `StreamingMemoryLimit`, `DelayTailCap`, `BaseDirectory` | value, read once by the constructor |
| `TimbreScope` | `Timbre/TimbreScope.cs` | Groups playbacks; `Dispose` cancels them all | one per element lifecycle (`ElementTimbreOwner`), one for `Application.Timbre` |
| `TimbrePlayback` | `Timbre/TimbrePlayback.cs` | One started sound: state, controls (volume, pause, seek), terminal transitions | runtime, from `Start` until its voice is released |
| `TimbreHandle` | `Timbre/TimbreHandle.cs` | A slot with one current playback; a new `Play` on the slot cancels the old one | one per declared sound in a `TimbreAttachment` |
| `TimbreSound`, `TimbreStartOptions` | `Timbre/TimbreSound.cs`, `TimbreStartOptions.cs` | Source, loading mode (`Auto`, `Preload`, `Streaming`), modifier chain | value |
| `TimbreVoice`, `TimbreDspChain` | `Timbre/Engine/TimbreVoice.cs`, `Timbre/Dsp/TimbreDspChain.cs` | Render state on the mixer side: gain ramp, LowPass and Delay stages, echo tail | one per playback; used only by the mixer thread |
| `PreloadedFeed`, `StreamingFeed` | `Timbre/Engine/TimbreFeed.cs`, `Timbre/Engine/StreamingFeed.cs` | Supply samples to a voice from a cached payload or from a ring buffer filled by a pump task | one per playback |
| `TimbrePayloadCache` | `Timbre/Engine/TimbrePayloadCache.cs` | Decoded payloads, LRU, with a pin count per entry | one per runtime |
| `TimbreMemoryPool`, `TimbreMemoryBudget` | `Timbre/Engine/TimbreMemoryPool.cs`, `Timbre/TimbreMemoryBudget.cs` | Counter of reserved decoder and streaming bytes; one budget per open reader | pool per runtime; budget per reader |
| `TimbreDecoders`, `TimbreFormats` and the codec sources | `Timbre/Decoding/` | Detect the format from the content, open the decoder, convert to the canonical format (`CanonicalConverter`) | static; one reader per load |
| `ITimbreOutput`, `ITimbreOutputClient` | `Timbre/ITimbreOutput.cs` | Output contract: `Open`, `Submit`, `Close`, `QueuedFrames`; the client gets `NotifyCapacityAvailable` and `NotifyDeviceLost` | implemented by the platform |
| `SdlTimbreOutput` | `Cerneala.Platforms.Sdl3/Audio/SdlTimbreOutput.cs` | One SDL stream on the default playback device, F32 little-endian, 2 channels, 48,000 Hz | `SdlWindowPlatform`, only when an `ISdlAudioApi` is passed in |
| `PlatformTimbreOutput` | `UI/Timbre/PlatformTimbreOutput.cs` | Forwards to the window platform's output, looked up at each `Open` | created by `Application` for its own runtime |
| `TimbreAttachment` | `UI/Timbre/TimbreAttachment.cs` | The sounds of one `@timbre` declaration: one handle per sound, AutoPlay, the element's current attachment | a lifetime of the Aspect behavior that declared it |
| `ElementTimbreOwner` | `UI/Timbre/ElementTimbreOwner.cs` | Creates the element's scope on first use; `Detach` disposes it | one per `TimbreAttachment`, and one behind `UIElement.Timbre` |
| `TimbrePlaybackMotion` | `UI/Timbre/TimbrePlaybackMotion.cs` | Animates playback parameters on the playback clock | registered per scope |

## Data Flow

### A button click plays a sound

Markup:

```text
<Button Content="Play">
  <Button.Aspect>@timbre $Sounds; @on Click { @play $self.timbre.Tone; }</Button.Aspect>
</Button>
```

1. **Input.** The user releases the left mouse button over the button. `ButtonBase` raises the routed `Click` event.
2. **Generated handler.** The source generator subscribed a handler to `Click`. It calls `GeneratedMarkup.PlayTimbre(button, "Tone")`.
3. **Attachment.** `TimbreAttachment.Require` finds the element's current attachment. It throws `InvalidOperationException` when the element is not attached, has no current `@timbre`, or the clip does not declare `"Tone"`. Then `TimbreAttachment.Play("Tone")` asks `ElementTimbreOwner.GetScope()` for the scope. `GetScope` calls `root.Relay.VerifyAccess()`, needs `root.TimbreRuntime`, and creates the scope on first use. The play goes to `TimbreScope.Play` with the handle for `"Tone"`.
4. **Admission.** `TimbreRuntime.Start` checks the voice limit under the `Sync` lock. It builds the `TimbrePlayback` (voice and DSP chain) outside the lock, because a Delay stage can allocate a large buffer. Under the lock again it checks the limit again, adds the playback to `live` and to the scope, and makes it the handle's occupant. A previous occupant is canceled.
5. **Feed.** If the payload is in the cache and the sound is not `Streaming`, the playback pins the entry and gets a `PreloadedFeed` at once. Otherwise a thread-pool task runs `LoadAsync`: it opens the reader, detects the format, then either decodes the whole payload into the cache or creates a `StreamingFeed`.
6. **Mixer.** The playback is put on the `adopted` list, the mixer thread ("Timbre mixer", started on the first play) is woken, and `Play` returns. The playback state is `Pending`.
7. **Mixing.** The mixer moves `adopted` into its own voice list under `Sync`. It opens the output on the first voice. It mixes one block of 480 frames (10 ms) only while the output queue holds at most 1,440 frames (1,920 − 480), so at most 40 ms are queued. It reads each voice's feed, runs the DSP chain and the gain ramp, sums, clips to ±1, and calls `ITimbreOutput.Submit`.
8. **Device.** The SDL audio thread asks for data. `SdlTimbreOutput.OnRequest` only counts and calls `NotifyCapacityAvailable`, which wakes the mixer. It never touches PCM.
9. **End.** When the device has consumed every frame of the playback, the mixer moves it to `Completed`, removes it from `live` and from the scope, and releases its voice. Releasing a `PreloadedFeed` drops its cache pin.

Each click starts a new playback on the same handle, so a second click cancels the first sound and starts it again. `TimbreAspectIntegrationTests.EachUserClickRestartsTheSound` counts 2 started playbacks after 2 clicks.

### Preload or streaming

`ShouldPreload` decides per load:

- `Preload` always preloads; `Streaming` always streams.
- `Auto` (the default) preloads when the length is known and `frames × 8 bytes ≤ AutoPreloadMaxBytes` (default 1 MiB). 1 MiB is 131,072 frames, about 2.7 seconds of canonical audio. A longer sound, or a sound of unknown length, streams.

A preload reserves its bytes in the cache before it allocates:

- A payload larger than `MaxPreloadBytes` (default 16 MiB) fails with `ResourceLimitExceeded`.
- The cache (`MaxCacheBytes`, default 64 MiB) evicts unpinned entries from the least recently used end until the payload fits. If pinned entries still fill it, the load fails with `ResourceLimitExceeded`.

A streaming playback reserves 64 KiB (an 8,192-frame ring) in the memory pool, opens its own reader, and starts only after 1,920 frames are buffered or the source ends.

## Lifecycle And Ownership

- **Runtime.** `Application.TimbreRuntime` creates the runtime on first use, with a `PlatformTimbreOutput`, and owns it. A runtime assigned before first use stays owned by the caller. `TimbreRuntime.Dispose` cancels every live playback, disposes every scope, wakes the mixer and waits for it to end (unless it is called on the mixer thread), then clears the cache.
- **Roots.** `WindowApplicationRuntime` gives every window root `application.TimbreRuntime` with `UIRoot.SetTimbreRuntime`, and passes `null` when the window closes. `SetTimbreRuntime` retires every element owner registered with the root, which disposes their scopes.
- **Elements.** `UIElement.DetachFromRoot` disposes the Aspect behavior first. That disposes its `TimbreAttachment`, which retires its `ElementTimbreOwner`, which disposes the scope, which cancels every playback in it. A re-attached element gets a new scope on its next play; canceled playbacks are not restarted.
- **Renderability.** Hiding an element does not stop its sounds.
- **Output.** The mixer opens the output when it has a voice and the output is closed. After a device loss it fails every live playback with `DeviceUnavailable`, closes the output, and reopens it on the next `Play`. `SdlWindowPlatform.Dispose` calls `SdlTimbreOutput.Terminate()` before SDL quits. `Terminate` closes the stream, reports the device as lost, and refuses every later `Open`.
- **Memory.** A reader's budget is closed after the reader is disposed. A cache pin lives from feed attachment until the voice is released, also for a canceled playback whose fade is still rendering.

## Threads

| Thread | Work |
|---|---|
| UI thread (any caller of `Play`, `Cancel`, `Pause`, `Seek`) | Admission under `Sync`; element access is checked by `VerifyAccess` |
| Thread pool | `LoadAsync` (open, detect, preload or create the stream); the streaming pump; decoding is synchronous on these threads |
| "Timbre mixer" (background, `AboveNormal`) | The only thread that mixes and submits PCM |
| "Timbre pump dispatcher" | Deferred actions, such as stopping a stream, outside its lock |
| SDL audio thread | Counts requests and signals the mixer; never blocks |

The mixer takes `Sync` once at the start of each iteration to adopt new playbacks and copy their controls. Reading, DSP and mixing run outside the lock, on buffers owned by the mixer thread.

## Frame Integration

Timbre has no `FramePhase` and no queue in the UI scheduler. The mixer runs on its own thread, paced by the output queue, not by UI frames. The UI side only starts and controls playbacks from event handlers and Aspect triggers. Playback parameter animation goes through Motion, with its own reduced-motion policy (see [motion.md](motion.md)).

## Invariants

- Replacing a handle's occupant cancels the old playback, and the new one becomes `Current`. `tests/Cerneala.Tests.Timbre/Engine/HandleAndScopeTests.cs:ModifiedClipStartsAndReplacesThroughTheSamePathAsAPlainClip`.
- Paused playbacks count against `MaxVoices`, and a rejected start throws `VoiceLimitExceeded` without changing the other playbacks. `HandleAndScopeTests.cs:VoiceLimitCountsPausedPlaybacksAndRejectsWithoutStealing`.
- With no output, a playback fails with `DeviceUnavailable`. `tests/Cerneala.Tests.Timbre/Engine/LifecycleTests.cs:MissingOutputFailsPlaybacksWithDeviceUnavailable`.
- A failed output open is not retried until the next `Play`; the next `Play` opens it again (open count 2). `LifecycleTests.cs:OutputOpenFailureIsNotRetriedUntilTheNextPlay`.
- A device loss fails the active playbacks, closes the output once, and the next `Play` reopens it. `LifecycleTests.cs:DeviceLossFailsActivePlaybacksClosesTheOutputAndPlayReopens`.
- `Auto` preloads at the threshold and streams above it; a payload over the preload limit fails with `ResourceLimitExceeded`; each streaming playback opens its own reader. `LifecycleTests.cs:LoadingPolicyRespectsAutoThresholdAndPreloadLimits`.
- The cache evicts only unpinned payloads, least recently used first, and refuses a load when pinned payloads fill it. `LifecycleTests.cs:CacheEvictsOnlyUnpinnedPayloadsAndRefusesWhenPinnedDataFillsIt`.
- A long Delay echo is truncated at 30 seconds (`TailTruncated`), and its DSP state bytes return to 0 after release. `tests/Cerneala.Tests.Timbre/Dsp/DspChainEngineTests.cs:DefaultTailCapTruncatesALongEchoAtThirtySeconds`.
- Detaching an element cancels the playbacks its Aspects started; re-attaching starts AutoPlay and triggers again with new playbacks. `tests/Cerneala.Tests.Timbre/Markup/TimbreAspectIntegrationTests.cs:DetachCancelsOwnedPlaybacksAndReattachActivatesAgain`.
- 100 detach and attach cycles leave 1 live scope and 0 active voices after the last detach. `tests/Cerneala.Tests.Timbre/Markup/TimbreAspectLifecycleTests.cs:HundredAttachDetachCyclesStartOncePerAttachAndReleaseEveryScope`.
- Detaching cancels a playback that is still loading and releases its reader. `TimbreAspectLifecycleTests.cs:DetachCancelsAPendingPlaybackAndReleasesItsReader`.
- The element scope lives for one attachment; after detach it is disposed, and a root-level playback keeps going. `tests/Cerneala.Tests.Timbre/Hosting/OwnerAccessTests.cs:ElementScopeIsPerLifecycleAndDetachCancelsItsPlaybacks`.
- Hiding an element or an ancestor does not cancel its audio. `OwnerAccessTests.cs:HidingAnElementOrAncestorDoesNotCancelItsAudio`.
- The root's reduced-motion preference does not slow audio animations: with `Reduce`, a volume animation still reaches about 0.5 halfway, while a visual opacity animation jumps to its end. `tests/Cerneala.Tests.Timbre/Motion/TimbreMotionContractTests.cs:TimbreMotionIgnoresTheVisualReducedMotionPreference`.
- SDL requests of any size, from any thread, only notify the client and never write PCM. `tests/Cerneala.Tests.SdlGpu/SdlTimbreOutputTests.cs:RequestsOfAnySizeFromAnyThreadOnlyNotifyTheClient`.
- `Close` waits for a running SDL request, and nothing reaches the client after it. `SdlTimbreOutputTests.cs:CloseWaitsForARunningRequestWithoutDeadlockAndNothingRunsAfterIt`.
- `Terminate` releases the device, reports it lost once, and refuses later opens. `SdlTimbreOutputTests.cs:TerminationReleasesTheDeviceReportsItLostAndRejectsLaterOpens`.
- Through native SDL with the `dummy` audio driver (opt-in with `CERNEALA_SDL_NATIVE_TESTS=1`), a 14,400-frame tone completes and the output queue never holds more than 1,920 frames. `tests/Cerneala.Tests.SdlGpu/NativeTimbreOutputTests.cs:TheDummyDriverPlaysToCompletionAndReleasesEverythingBeforeSdlQuits`.

## Diagnostic

`root.Detective.CaptureTimbre()` returns a `TimbreDiagnosticsSnapshot` of the root's runtime, or `null` when the root has no runtime. The runtime counts started, completed, canceled and failed playbacks, mixed blocks, submitted and consumed frames, underrun frames, clipped samples, live readers, output opens, cache bytes and entries, streaming bytes, DSP state bytes, live scopes and pending loads. See [detective.md](detective.md).

## Known Limitations

- `MaxVoices` counts `live` playbacks, including paused ones. A canceled playback leaves `live` at once, even while the mixer still renders its fade.
- The default `StreamingMemoryLimit` is `long.MaxValue`: the memory pool counts, but does not limit. DSP state is counted, not reserved; an animatable Delay always allocates a 2-second kernel (768,000 bytes).
- A play-started preload runs with `CancellationToken.None`. A playback canceled during decoding is dropped only after the decode ends. `PrepareAsync` honors its token.
- A refused cache reservation has already evicted the unpinned entries it walked past; they are not restored.
- Cache keys are the resolved file path (case-insensitive on Windows) or the source factory delegate. Two sources with the same content do not share an entry.
- Decoding is synchronous on thread-pool threads.
- MP3 seeking walks frame headers from the start (no index), so its cost grows with the target position. Every seek recreates the resampler.
- Refused inputs: MP3 Layer I and II, more than 2 channels, sample rates outside 8,000–192,000 Hz, WAV RIFX/RF64/BW64, WAV float other than 32-bit, Opus channel mapping families other than 0.
- Clipping is a hard clamp to ±1, with a counter. There is no limiter.
- A window platform has an audio output only when an `ISdlAudioApi` is passed to `SdlWindowPlatform`. Without one, `PlatformTimbreOutput.Open` fails with `DeviceUnavailable`. The preview host disables audio by default.
- The 40 ms queue and the 1,920-frame streaming start window are constants, not options. Start latency on a device: nemăsurat.
