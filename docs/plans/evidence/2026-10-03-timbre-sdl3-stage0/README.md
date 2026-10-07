# Timbre SDL3 backend — etapa 0: interop, ownership și matrice

Snapshot: `master` @ `b622f8cf` + plan necomis. Host: Windows 11 Pro 10.0.26200 x64, .NET 8.0.30.
SDL: `SDL-3.4.16-e912da0 (Graphix 3.4.16-graphix.6 commit e912da037a59e33affc6ecd69be326265cdc748b)`.
Pin-uri efective: `Graphix-CS 3.4.16.1` (binding managed, `lib/net8.0/SDL3-CS.dll`) și `Graphix.Native 3.4.16-graphix.6`
(payload `runtimes/<rid>/native/`, provenance per RID; win-x64 `SDL3.dll` sha256 `2958f3d3…a51f`). Nu se migrează binding-ul.

## 1. Contract formalizat (din core/index, nu redeschis)

| Subiect | Decizie |
| --- | --- |
| Device | numai `SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK` (0xFFFFFFFF); output lazy: nimic nativ până la primul `ISoundOutput.Open` făcut de mixerul Timbre când există o voce. `SoundClip`/scope/runtime nu deschid nimic. |
| Format intern | float32 LE / stereo interleaved / 48 kHz = `SDL_AUDIO_F32LE` (0x8120), `channels=2`, `freq=48000`. 1 frame = 8 bytes; `bytes = frames * 8`, `frames = bytes / 8` (numai frame-uri complete; mixerul trimite blocuri de 480 frames = 3840 bytes). Channel layout: ordinea stereo SDL L,R = ordinea Timbre. |
| Conversie | SDL `AudioStream` convertește/resamplează de la specul nostru (sursa stream-ului) la formatul device-ului; Timbre nu face a doua conversie. |
| Push/pull | **push** (contractul `ISoundOutput` deja înghețat în core): mixerul Timbre apelează `QueuedFrames`/`Submit` pe firul lui; bugetul ≤1920 frames (40 ms) este aplicat de mixer pe `SDL_GetAudioStreamQueued` (bytes de input, neconvertiți). |
| Callback | `SDL_SetAudioStreamGetCallback` prin callback-ul din `SDL_OpenAudioDeviceStream`: **numai** contoare `Interlocked` + `ISoundOutputClient.NotifyCapacityAvailable()` (semnal non-blocant). Fără Put, fără lock managed, fără decoder/fișier/DSP/Motion/UI pe firul audio. Rulează cu lock-ul stream-ului ținut, deci nu poate distruge stream-ul. |
| Ownership nativ | cale unică `SDL_OpenAudioDeviceStream` → `SDL_ResumeAudioStreamDevice` → `SDL_DestroyAudioStream` (închide și device-ul). Fără open/create/bind explicit, fără politici de close amestecate. |
| Init/quit audio | `SDL_InitSubSystem(SDL_INIT_AUDIO)` la `Open`, `SDL_QuitSubSystem(SDL_INIT_AUDIO)` după `DestroyAudioStream` la `Close`/terminare; refcount SDL echilibrat 1:1 per deschidere. Video/Events (`SdlPlatformLifetime`) rămân neschimbate; fără `MIX_Init/MIX_Quit`, fără SDL_mixer. |
| Fir init/quit | firul care deschide/închide (mixerul Timbre; terminarea platformei pe UI), serializat sub lock-ul output-ului. Docs SDL notează „should only be called on the main thread” pentru `SDL_InitSubSystem`; marshaling pe UI ar face rezultatul awaitable dependent de pump-ul UI (interzis de core) și ar putea bloca `SoundRuntime.Dispose` pe UI. Proba arată init/quit Audio echilibrat din fire worker pe WASAPI, cu Video/Events intacte. Risc rezidual pe alte OS documentat; runtime non-Windows N/A. |
| Shutdown | platforma SDL deține output-ul comun și îl **termină înainte de `SDL_Quit`**: dezabonează callback-ul (registry), `DestroyAudioStream` (așteaptă callback-ul în curs prin lock-ul stream-ului), `QuitSubSystem(AUDIO)`, apoi `SdlPlatformLifetime` face `SDL_Quit`. Necesar deoarece `WindowApplicationRuntime.DisposeCore` apelează `platform.Dispose()` înaintea `Application.CompleteExit()` (care face dispose la `SoundRuntime`). După terminare, `Open` eșuează explicit (`DeviceUnavailable`), Submit/QueuedFrames aruncă → runtime raportează device lost. |
| Device loss | `SDL_EVENT_AUDIO_DEVICE_REMOVED` pentru device-ul logic al stream-ului, observat prin event watch-ul existent al platformei (rulează pe firul care publică evenimentul, fără pump UI) → `NotifyDeviceLost`. Fără reconectare/retry; `Play` ulterior redeschide. Urmărirea default-ului de către SDL este comportamentul device-ului implicit, nu retry Timbre. |
| Granița API | tipurile SDL3-CS nu ies din `Cerneala.Platforms.Sdl3`; output-ul este `internal`, consumat prin `ISoundOutput` (core) și `IWindowPlatform.SoundOutput` (internal). `SoundClip/SoundPlayback/SoundHandle` nu expun handle-uri native. |

## 2. Interop verificat (proba `probe/`, raw în `results/probe-results.json`)

Comandă: `dotnet build probe/SdlAudioProbe.csproj -c Release -o probe-bin` apoi `dotnet probe-bin/SdlAudioProbe.dll results/probe-results.json` (watchdog 45 s, exit 0).

- ABI: `SDL.AudioSpec` sequential 12 bytes, offsets format@0/channels@4/freq@8, `AudioFormat : UInt32`; `AudioStreamCallback` `[UnmanagedFunctionPointer(Cdecl)] (nint userdata, nint stream, int additional, int total)`; importurile sunt `LibraryImport` cdecl cu bool `MarshalAs(I1)` (SDL3 `bool` 1 byte). `AudioFrameSize(F32/2) = 8`.
- Rooting: delegate-ul este un câmp static readonly (un singur function pointer pe proces); starea per stream se caută după `userdata` într-un registry; `GC.Collect` repetat în timpul redării nu a întrerupt callback-urile (64/64). Callback-uri după destroy: 0; „late callbacks” (userdata absent din registry): 0.
- Init balance: două runde InitSubSystem/QuitSubSystem(Audio) pe fire worker diferite: `Video, Events` → `Audio, Video, Events` → `Video, Events`; driver `wasapi`.
- WASAPI real: device logic 37, format device F32/2/48000, **480 sample frames** per perioadă; stream src=dst F32/2/48000 (fără conversie pe acest host); deschis pauzat (callbacks 0, queued 3840 B neconsumați); după resume callback pe un fir dedicat (nu main), `additional=0`, `total=3840`; cu bugetul de 1920 frames high-water = 1920 frames, 0 cereri starved, drain 30 ms, queued 0 după drain.
- Dummy (`SDL_AUDIO_DRIVER=dummy`): device 49, F32/2/48000, 1024 sample frames, aceeași cale; un singur request inițial `additional=3840` înainte de prima umplere. Verifică numai interop/lifecycle.
- Absent (`SDL_AUDIO_DRIVER=cerneala-absent-driver`): `InitSubSystem` false, eroare `Audio target 'cerneala-absent-driver' not available`, `WasInit(Audio)=0` (fără leak de refcount).
- Conversie SDL (stream liber F32/2/48000 → S16/2/44100): 4800 → 4410 frames exact, sinus 1 kHz estimat 995 Hz, RMS 0.35357 vs 0.35355 așteptat, canale identice.

`additional_amount` este estimarea SDL („may overestimate a little”) a bytes de input lipsă; se raportează ca cereri starved aproximative, nu ca underrun DAC.

## 3. Inventar consumatori și migrare

| Consumator | Tratament |
| --- | --- |
| `SdlGpuApplicationBackend.CreatePlatform` (bootstrap real, folosit și de `WindowApplicationRuntime.CreateDefault` și `DesignPreviewSession` prin registry) | singurul care furnizează `NativeSdlAudioApi` platformei. Preview-ul nu redă până la acțiuni audio (planul markup deține politica preview). |
| `SdlWindowPlatform` | parametru opțional nou pentru API-ul audio (implicit `null` = fără output); termină output-ul primul în `Dispose`; rutează `AudioDeviceRemoved` din event watch. |
| `SdlWindowPlatformFactory` (nefolosit de bootstrap) | neschimbat; nu primește audio. |
| `SdlPlatformLifetime`, `ISdlApi.InitializeVideo/Quit`, `FakeSdlApi` | **neschimbate**; seam audio separat `ISdlAudioApi`, nu se umple `ISdlApi`. `NativeSdlApi.ConvertEvent` adaugă doar maparea `AudioDeviceRemoved`. |
| Direct `InitializeVideo/Quit`: `SdlGpuShaderArtifactTests`, `tests/Shared/SdlDrawingFixture`, `PrismBackdropHostingMigrationTests` | neschimbate; nicio inițializare audio implicită. |
| `new SdlWindowPlatform(api, graphics[, scale])`: benchmarks (TileMapStage4, RenderSurface3DProbe, PrismSdlGpu), `SdlWindowsNativeContractTests`, `SdlWindowPlatformTests`, `SdlWindowMigrationContractTests`, `NativeSdlLifetimeTests` | semnătură compatibilă (parametru opțional); fără audio. |
| `IWindowPlatform` fakes (`WindowRuntimeTests`, `ApplicationSoundsIntegrationTests`, `ApplicationRuntimeTests`, `ApplicationBackendRegistrationTests`) | membru default `SoundOutput => null`; neschimbate. |
| `Application.SoundRuntime` (runtime deținut) | leagă output-ul platformei instalate la primul `Open` (amânat, deci valid și dacă runtime-ul se creează înainte de `Install`); runtime-urile atribuite rămân ale apelantului. Fără platformă cu output → `DeviceUnavailable` explicit, ca înainte. |

## 4. Baseline GREEN (TRX în `results/`)

Comanda planului §5 (filtru SdlPlatformLifetime|NativeSdlLifetime|SdlArchitecture|SdlGpuShaderArtifact):
- fără opt-in: 13 passed, 4 skipped (cele 4 native — skip raportat separat, nu pass) — `baseline-default.trx`;
- `CERNEALA_SDL_NATIVE_TESTS=1` (job-scoped, restaurat): 17 passed, 0 skipped — `baseline-native.trx`.
Înghețat: platforma face exact 1 `InitializeVideo` + 1 `Quit`, ambele pe firul UI; shutdown din alt fir respins.

## 5. Matrice platforme

- Windows x64 (acest host): output real WASAPI disponibil (format mai sus); dummy disponibil separat. Required: real + dummy.
- Linux/macOS/win-arm64 runtime/native audio: **N/A conform politicii index §7**, nelansate. Cele șase RIDs (win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64) rămân obligatorii pentru restore/build/publish/assets/notices host-side în etapa 3.
- Dacă device-ul Windows lipsește la gate, rezultatul este blocked, nu N/A.

## 6. Observatori definiți

Implementați în `SdlSoundOutput` (diagnostice interne) înaintea gate-ului care îi folosește:
streams/devices deschise acum și cumulativ, subsystem init/quit, ordine operații (fake log), callbacks totale, callbacks per fir (fake), bytes cerute, cereri starved + bytes starved (estimare SDL), frames submitted, queue high-water (frames), Put failures, late callbacks (userdata absent din registry), format device/stream negociat și sample frames, device lost, terminare. Contoarele core rămân sursa pentru frames consumed/underrun-urile sursei (`SoundRuntimeDiagnostics`).

## 7. Nivelul observabil al progresului

Observăm PCM produs (mixer), queued (`SDL_GetAudioStreamQueued`, bytes de input) și dequeued (scăderea queued + callback-urile de get). Nimic din acestea nu certifică momentul auzit la DAC: buffer-ul device-ului (480 frames pe WASAPI) și latența OS/hardware sunt în afara observației. Completion = EOF + tail + drain software observat (core). Bugetul aplicat este ≤40 ms software queued; nu există prag DAC aprobat. Validarea auditivă umană pe Windows rămâne gate final separat, nedeductibil din contoare.
