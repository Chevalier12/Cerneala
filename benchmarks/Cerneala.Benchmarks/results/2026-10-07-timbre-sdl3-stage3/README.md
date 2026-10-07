# Timbre SDL3 backend — etapa 3: distribuție, cost și verificare completă

Host de referință: Windows 11 Pro 10.0.26200 x64, Intel Core i5-9300H (8 logice), .NET 8.0.30, SDL `3.4.16-graphix.6`, WASAPI F32/2/48000, 480 sample frames.

## Workflow (`.github/workflows/desktop-backends.yml`, nerulat remote)

- Trigger-e noi: `UI/Application.cs`, `UI/Timbre/**`, `Properties/AssemblyInfo.cs` (restul căilor SDL/Timbre existau).
- Windows: `Native Timbre audio interop (dummy driver)` — `CERNEALA_SDL_NATIVE_TESTS=1`, filtrul `NativeSoundOutputTests` fără `ARealDevice`, exact 5 pass / 0 skip; `Native Timbre smoke (dummy audio interop)` — `SDL_AUDIO_DRIVER=dummy`, launcher publicat, `-Mode timbre`. Corpusul Timbre (`Cerneala.Tests.Timbre`) și asset/notice check pe RID-uri existau deja.
- Fără joburi audio pe Linux/macOS; Xvfb/Vulkan nu e tratat ca mediu audio. Testele legacy nu sunt dezactivate.
- Livrarea fizică cere `CERNEALA_TIMBRE_AUDIO_DEVICE=1` + native opt-in pe un host Windows cu device (`TimbreAudioDeviceFactAttribute`; altfel skip raportat), plus smoke fără `SDL_AUDIO_DRIVER`. Echivalentul local al pasului CI: `native-audio-ci-equivalent.trx` (5/5 cu `SDL_AUDIO_DRIVER=dummy` în mediu).
- Defect de configurare găsit la proba CI locală și reparat: cu `SDL_AUDIO_DRIVER` în mediu, `SDL_SetHint` normal e refuzat; smoke-ul și testele native selectează driverul cu `SDL_HINT_OVERRIDE`, iar `SDL_ResetHint` revine la valoarea din mediu.

## Native Windows și platforme

- Host cu device real: `audio-device-host.trx` 46/46 (6 native audio incluzând `ARealDevice…` pe WASAPI, fake lifecycle, parser, arhitectură). Smoke `timbre` prin launcher publicat: `smoke-timbre-wasapi.json` (18/18, driver wasapi) și `smoke-timbre-dummy.json` (18/18, driver dummy) — dummy certifică numai interop.
- Linux/macOS/win-arm64 runtime: **N/A conform politicii index §7**, nelansate.
- Host-side pentru cele șase RIDs (`six-rid-publish.json`, `Publish-SdlGpuSmoke.ps1` framework-dependent): asset-ul SDL3 al familiei (fără asset-uri străine), 4 fixture-uri Timbre, `Cerneala`/`NVorbis`/`Concentus`/`SDL3-CS`/`Cerneala.Platforms.Sdl3`/`Cerneala.Backends.SdlGpu`, `Cerneala.Timbre.THIRD-PARTY-NOTICES.txt`, zero biblioteci native de codec sau SDL_mixer. Output-ul audio nu adaugă dependențe: folosește același `SDL3` nativ deja distribuit (licență zlib, `Graphix.Native`). Cross-publish-ul nu certifică runtime pe alte sisteme.

## Cost (protocol fixat înainte: `cost-protocol.md`; script: `Invoke-TimbreSdlCost.ps1`)

32 voices (28 preload + 4 streaming MP3/Vorbis/Opus/MP3 reale), LowPass+Delay, 5 procese × (1000 warmup + 10000 blocuri măsurate).

| Campanie | P99 mixer (ms) | Alocări | Coadă max | Underrun device / voice | P95 Play→PCM (ms) | Deschideri / late | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SDL WASAPI | 0.35–0.45 (max/min 1.29) | 0 | 1920 | 0 / 0 | 10.56–10.64 | 1 / 0 per proces | **PASS** 5/5 |
| Emulator (regresie core după schimbările din etapa 2) | 0.34–0.35 (1.05) | 0 | 1920 | 0 / 0 | 16.14–16.27 | — | **PASS** 5/5 |

Praguri index §7 neschimbate: P99 < 2.5 ms, 0 alocări steady-state, ≤1920 frames, 0 underrun, P95 < 50 ms. Pilotul (`pilot-sdl.json`) nu intră în verdict. În timpul campaniei SDL a apărut tranzitoriu un `dotnet restore` străin (nepornit de această sarcină); rezultatele trec oricum toate pragurile.

## API, documentație, compatibilitate

- Nicio suprafață publică nouă: output-ul, seam-ul și binding-ul rămân `internal`. ApiCompat strict (`api-compat.proj`, baseline propriu `b622f8cf` în worktree `C:\Users\lauri\Desktop\Cerneala-baseline-sdl3-b622f8c`; `Cerneala.dll` 5,702,656 B SHA-256 `5173D8A9…41FD`, `Cerneala.Platforms.Sdl3.dll` `1CC8F6F2…EEC1`, `Cerneala.Backends.SdlGpu.dll` `C6518448…2957`), strict mode + nume de parametri, fără suppression-uri: exit 0 (`api-compat.log`).
- Documentație canonică: `Cerneala.UI.Application.md` (output-ul platformei), `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.md` (output SDL3, lifetime, erori, transport), `Cerneala.Timbre.SoundPlayback.md` și `Cerneala.Timbre.SoundLoading.md` (pornire streaming, politica de underrun). Fără pagini noi → manifest neschimbat; testul de manifest din `Cerneala.Tests.VisualStudio` trece în suita completă. `docs/sdl-desktop-backend.md`: secțiunea „Audio output (Timbre)”, modul smoke și notele CI.

## Suita completă

`dotnet build .\Cerneala.slnx -c Release` (warning-uri CS0108 preexistente în Playground generat) și `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1` și `CERNEALA_TIMBRE_AUDIO_DEVICE=1`: 12 proiecte, **6981 pass, 0 fail, 7 skip** (`full-solution/*.trx`, `full-solution.log`). Skip-uri preexistente, fără legătură: 4 cazuri `AlphaBlendRenderingTests`, `SdlWindowsNativeContractTests.NativeOwnership…` (maintainer 2026-09-21), gate-ul de cadență SceneVillage, P95 de completion din Language. Include SourceGen (625), Timbre (306), SdlGpu (1008, toate cele 6 teste native audio), `SdlArchitectureTests`, lifecycle și consumatorii direcți `NativeSdlApi`. După suită s-au modificat numai teste/smoke (selecția driverului) și au fost re-rulate ţintit.

Smoke-uri vizuale existente prin launcher publicat win-x64: `multi-window` și `prism` OK (`Window.SaveScreenshot`).

## Rămas în afara acestui plan

Validarea auditivă umană pe Windows (index §7/§10) este un gate separat al inițiativei; nu a fost efectuată și nu este dedusă din PCM sau contoare.
