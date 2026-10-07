# Timbre SDL3 backend — etapa 1: output și shutdown

Host: Windows 11 Pro 10.0.26200 x64, .NET 8.0.30, SDL `3.4.16-graphix.6` (WASAPI real + dummy). Native opt-in `CERNEALA_SDL_NATIVE_TESTS=1` setat și restaurat numai pentru comenzile native.

## Implementare

- `Cerneala.Platforms.Sdl3/Audio/ISdlAudioApi.cs` — seam audio separat de `ISdlApi`: init/quit subsystem, `OpenDefaultPlaybackStream` (deschis pauzat), device/format, resume, put (bool), queued bytes, flush, destroy.
- `Audio/NativeSdlAudioApi.cs` — un singur delegate static rooted pe proces; starea per stream prin `userdata` id într-un registry; callback fără handler → `LateCallbacks`; excepțiile managed nu trec în firul audio SDL (`HandlerFailures`).
- `Audio/SdlSoundOutput.cs` — `ISoundOutput` comun platformei: `Open` = init → open → resume (unwind exact la eșec), `Close` = destroy → quit, `Terminate` (platformă) = close + reject later opens + `NotifyDeviceLost`, `HandleDeviceRemoved` pentru device-ul logic propriu. Callback-ul SDL doar numără și semnalează mixerul. Diagnostice: open/close, device + format negociat + sample frames, requests/bytes, starved requests/bytes, frames submitted, queue high-water, put failures, flushes, devices lost.
- `SdlWindowPlatform` deține output-ul (parametru opțional `ISdlAudioApi`, implicit fără audio), îl termină **primul** în `Dispose` (înainte de `SDL_Quit`), rutează `SDL_EVENT_AUDIO_DEVICE_REMOVED` din event watch fără pump UI. `NativeSdlApi.ConvertEvent` mapează evenimentul. `SdlGpuApplicationBackend` furnizează `NativeSdlAudioApi`.
- `IWindowPlatform.SoundOutput` (internal, default null), `WindowApplicationRuntime.SoundOutput`; `Application.SoundRuntime` deținut folosește `UI/Timbre/PlatformSoundOutput` care rezolvă output-ul platformei instalate la primul `Open`.

## Defect găsit și reparat în etapă: tail reținut de resampler

Observație: proba nativă cu device fixat la S16/2/44100 (alt device logic deschis întâi pe dummy) a expirat — redarea de 9600 frames nu s-a încheiat în 15 s (`green-native-format.trx`, prima rulare).
Ipoteză: un `SDL_AudioStream` care resamplează păstrează ultimele frame-uri de input pentru filtrul resamplerului până la mai mult input sau `SDL_FlushAudioStream`; `SDL_GetAudioStreamQueued` nu ajunge la 0, drain-ul cerut de completion nu se observă, iar ultimele frame-uri nu ajung la device.
Experiment: fake cu `HeldBackBytes = 64 frames` → `AResamplingDeviceReceivesTheHeldBackTailAndThePlaybackCompletes` RED („device drive did not converge”, `red-resampling-tail.trx`).
Fix (owner: output-ul SDL): mixerul citește `QueuedFrames`; dacă device-ul a raportat lipsă (`additional_amount > 0`) și mixerul n-a trimis nimic de la verificarea anterioară, output-ul face `FlushStream` pe firul mixerului (callback-ul rămâne count + semnal). Playback continuu nu este niciodată flush-uit (testul cere exact 1 flush la final pentru 9600 frames cu cereri neregulate). GREEN în fake și nativ (44.1 kHz S16 complet, high-water ≤1920).

Ipoteză falsificată: hint-urile `SDL_AUDIO_FREQUENCY`/`SDL_AUDIO_FORMAT` pe dummy nu schimbă formatul device-ului deschis cu spec explicit (rămâne F32/48000); experimentul a fost înlocuit cu device logic suplimentar.

## RED → GREEN

- RED (suprafață staged, motiv = comportament lipsă): `red.trx` (28/29 fail: NotImplemented / playback Failed fără output), `red-platform.trx` (output null), `red-resampling-tail.trx`.
- Fixture corectat înainte de GREEN: testele de aplicație async continuau pe alt fir după `await` și încălcau afinitatea UI a platformei SDL; au devenit sincrone (bariere `SyncAsync().Wait`). Două așteptări de test greșite corectate cu dovadă: mixerul trimite blocuri complete de 480 frames (ultimul bloc parțial e completat cu zero) și poate pune în coadă până la 1920 frames dintr-o voce înainte de primul request.
- GREEN final (stare finală a codului): `final-default.trx` 44 pass / 9 native skip; `final-native.trx` 54/54; `final-core-hosting.trx` 108/108. Compatibilitate: `sdlgpu-full-default.trx` 593 pass / 240 skip (native fără opt-in); `sdlgpu-lifetime-native.trx` 57 pass / 1 skip preexistent (maintainer skip 2026-09-21, foreground focus).

## Acoperire per item

| Item | Dovadă |
| --- | --- |
| frame ordering / lifecycle exact / callback concurent + drain | `SdlSoundOutputTests` (ordine init/open/resume/destroy/quit, unwind per pas, request-uri din 5 fire, request după close, close în timpul callback-ului fără deadlock), `SdlSoundOutputRuntimeTests.MixedFramesReachTheDeviceInOrder…` |
| variable byte requests, additional 0/≠0, Put/queued failure, dispose repetat, formate negociate/resampling | `RequestsOfAnySize…`, `ARequestWithNothingMissing…`, `APutFailure…` (bool, fără contabilizare), `AQueuedQueryFailure…`, `DiagnosticsReportTheNegotiatedDeviceFormat`, nativ `SdlConvertsTheMixFormat…` |
| transport cu coadă observabilă | `CompletionWaitsForTheDeviceToDrain…`, `CancelingOneOfTwoMixedVoices…` (PCM queued păstrat, după cancel numai vocea rămasă), `TransportAndParameterChanges…` (pause/resume/seek/volume/loop/replacement, vocea stabilă neîntreruptă, fără close/clear), `OnlyTheLatestSeekPublishesPcmAfterResume`, `AFailingPut…` |
| cancel/dispose în callback, callback după shutdown, două ferestre | `CloseWaitsForARunningRequest…`, `Termination…`, `DisposingThePlatformReleasesAudioBeforeSdlQuits` (audio quit la `SDL.QuitCount == 0`), `ApplicationSoundsUseThePlatformOutputSharedByTwoWindows` |
| fără invalidări/frame-uri GPU | `PlayingAndUpdatingSoundsRequestsNoFrames` (20 iterații play/volume/pull: `PumpOnce` false, `PresentCount` neschimbat) |
| fereastră ascunsă/închisă fără redraw | nativ `AudioKeepsFlowingWhileOneWindowIsHidden…` (≥20 requests și ≥9600 frames noi fără `PumpOnce`, output deschis o singură dată) |
| late callbacks / memorie eliberată | registry + `LateCallbacks`/`RegisteredHandlers` neschimbate după dispose nativ; `HandlerFailures == 0` |
| GPU init/quit compatibil | baseline lifetime/arhitectură/shader nativ verde; `AudioStaysBehindItsOwnInternalSeamAndOutOfTheGpuApi` |
