# Timbre Motion — etapa 3: PCM/native, cost și documentație

Host: Windows 11 Pro 10.0.26200 x64, Intel i5-9300H (8 fire logice), .NET SDK 10.0.400, runtime 8.0.30.

## PCM: publicațiile ating numai redarea capturată

`SoundMotionPcmTests` (Cerneala.Tests.Timbre, sink determinist, aceeași programare de blocuri în fiecare rulare):

- **Liniaritate / izolare.** A = sursă streaming cu LowPass(`Cut`) + Delay(`Mix`), animată pe Volume, `Cut` și `Mix`;
  B = altă sursă preload cu același lanț, neanimată. Pe 20 de blocuri, `mix(A animat + B) = mix(B) + mix(A animat)`
  (|Δ| ≤ 1e-6 per eșantion), deci PCM-ul lui B nu este atins de animația lui A; `mix(A animat)` diferă de `mix(A static)`
  din blocul 1, iar blocul 0 (mixat înaintea primului sample) este identic — publicarea are granularitate de bloc.
- **Fără layout/render.** Root vizibil și control ascuns: 4 frame-uri cu Volume + Cut animate → `MeasureCalls`,
  `ArrangeCalls`, `RenderedElements`, `MotionRenderInvalidations`, `MotionLayoutInvalidations`, `MotionPropertyWrites` = 0,
  `MotionValuesChanged` = 8 (2 sloturi × 4 frame-uri).

## Nativ Windows (`tests/Cerneala.SdlGpuSmoke --mode timbre-motion`)

`TimbreMotionPanel.crn` declară clipuri WAV/MP3/Vorbis/Opus, un clip LowPass+Delay cu parametri și un Aspect reactiv de
transport; `TimbreMotionSmoke` acționează numai prin Servo (input rutat prin fereastra SDL reală). Tap-ul PCM din fața
output-ului SDL3 (WASAPI 0x8120/2/48000) este comparat bloc cu bloc cu același clip randat static (Volume 1) în sink-ul
determinist: Volume este gain post-lanț publicat la granița de bloc, deci câștigul least-squares al fiecărui bloc este
volumul cu care a fost mixat. Rezultat: `native/timbre-motion-diagnostics.json`, `SDL_GPU_SMOKE_OK mode=timbre-motion`.

| Scenariu | Probe |
| --- | --- |
| click fade WAV / MP3 / Vorbis / Opus | lungime identică cu randarea statică; câștig bloc 0 = 0.2000 (timpul pornește la primul PCM), monoton, ultimul = 0.8000, ~40 blocuri de rampă (400 ms), 0 underrun |
| sweep Filtered | blocul 0 bit-identic cu parametrii declarați, ≥ 150 blocuri diferite, valorile finale (200 Hz, 0.6) păstrate |
| transport reactiv streaming (MP3 lung) | Pause îngheață volumul, publicațiile și tap-ul; Resume continuă; Seek 60s nu repornește; Stop anulează mid-flight, apoi 0 publicații |
| loop + hover | fade-ul se termină o dată, wrap-urile nu îl reiau; hover pe zona de transport animă looper-ul capturat la 0.3, fără pornire nouă |
| final | `ActiveVoices`, `LiveReaders`, ținte Motion audio = 0; `MotionSamplesRejected` = 0 |

Tap-ul certifică pipeline-ul până la output, nu ce a auzit cineva; validarea auditivă umană rămâne gate separat (index §7).

## Cost (protocolul indexului §7)

Runner: `benchmarks/Cerneala.Benchmarks --timbre-motion` (`TimbreMotionBenchmarkRunner.cs`), campanie
`benchmarks/Cerneala.Benchmarks/results/2026-10-07-timbre-motion-stage3/Invoke-TimbreMotionCost.ps1`: fixture-ul core înghețat
(32 voices looping, 4 streaming, LowPass+Delay, blocuri 480, 1000 warmup + 10000 blocuri măsurate, device emulat pe ceas
de perete) cu fiecare voice animând Volume și Cutoff pe root-ul elementului, buclă UI 60 Hz (`UiHost.Update`) pe toată
fereastra, 100 frame-uri UI de warmup; 5 procese per scenariu. Pragurile sunt cele ale core-ului, nerelaxate.

| Scenariu | Proces | Mixer P50/P95/P99 ms | Alocări mixer | Underrun engine | Underrun device | UI P99 ms | Publicații |
| --- | --- | --- | --- | --- | --- | --- | --- |
| control ascuns | 1–5 | 0.31–0.32 / 0.37–0.42 / 0.42–0.59 | 0 | 0 | 0 | 1.26–1.84 | ~207 900 |
| root vizibil | 1 | 0.31 / 0.40 / 0.50 | 0 | 0 | **226** | 2.43 | 207 872 |
| root vizibil | 2 | 0.27 / 0.32 / 0.40 | 0 | 0 | 0 | 0.67 | 207 872 |
| root vizibil | 3 | 0.28 / 0.35 / 0.44 | 0 | 0 | 0 | 1.21 | 207 936 |
| root vizibil | 4 | 0.33 / 0.52 / 1.45 | 0 | 0 | **310** | 3.98 | 207 904 |
| root vizibil | 5 | 0.34 / 0.57 / 1.52 | 0 | **3712** | 0 | 4.35 | 207 931 |

Toate 10: 0 measure/arrange/render, 0 invalidări Motion, 0 sample-uri respinse, coadă maximă 1920. Alocări UI:
~1672 B/frame. Probe de atribuire (teste temporare, șterse): un `SoundPlaybackMotionTarget.Tick` alocă 0 B după corecția de
mai jos; 32 ținte audio adaugă ~792 B/frame constant față de UI inactiv (fazele frame-ului Motion root, independente de
numărul de ținte) și 0 B peste un frame vizual deja activ. **Corecție găsită de măsurare:** inițial ținta aloca 112 B/frame
(display-class-uri de closure create la fiecare iterație a buclelor din `Tick`/`Flush`, deși lambda nu rula); închiderile au
fost înlocuite cu metode explicite (`CancelQuietly`, `Reject`, `Synchronize`).

**Verdict conform protocolului: FAIL** pentru „root vizibil” (procesele 1, 4, 5 depășesc pragul de 0 underrun; protocolul
interzice reluări care să înlocuiască un proces eșuat). „Control ascuns”: PASS 5/5.

**Atribuire (nu schimbă verdictul).** Campanie intercalată `Invoke-TimbreMotionAttribution.ps1` (`attribution/`), 5 runde ×
{audio Motion, același încărcat + o animație vizuală, fără Motion}, gazda neutilizată altfel:

| Rulare | Mixer P99 / max ms | Underrun engine | Underrun device | UI P99 / max ms |
| --- | --- | --- | --- | --- |
| audio 1, 2, 4, 5 | 0.50–1.24 / 2.7–15.9 | 0 | 0 | 1.8–3.5 |
| audio 3 | 16.6 / **3043** | 392 448 | 2 004 273 | 87 / **3352** |
| vizual 1–5 | 0.53–2.09 / 2.5–16.8 | 0 | 0 | 1.0–3.6 |
| fără Motion 1–5 | 0.47–**2.62** / 1.6–22.4 | 0 | 0 / **2584** (runda 5) | 0.6–1.6 |

Audio 3 conține o oprire de ~3 s a întregului proces (blocul maxim al mixerului și frame-ul UI maxim se suprapun; 25 colectări
gen1 față de ~4 în celelalte rulări). Controlul fără Motion depășește și el pragurile (P99 2.62 ms > 2.5; 2584 frame-uri
underrun device). Concluzie susținută în condițiile testate: pe acest host, fixture-ul core + buclă UI nu satisface stabil
pragurile nici fără animații audio, deci eșecurile nu disting Motion audio; cauza opririlor (planificare/memorie pe host) nu
este determinată. Rămâne decizie pentru utilizator: gate-ul de cost al planului este nesatisfăcut.

**Campania 2 (cerută de utilizator: VS Code și Chrome închise, procesor la 100%).** Campanie nouă și completă, nu reluare
de procese: același runner și script, aceleași praguri, 5 procese × 2 scenarii, director `campaign-2/`
(`campaign-2.log`). Prima campanie rămâne raportată mai sus ca FAIL.

| Scenariu | Procese | Mixer P50 / P95 / P99 / max ms | Alocări mixer | Underrun engine / device | UI P99 ms | Publicații |
| --- | --- | --- | --- | --- | --- | --- |
| control ascuns | 1–5 | 0.24–0.25 / 0.30–0.31 / 0.35–0.37 / 0.67–0.86 | 0 | 0 / 0 | 0.34–0.40 | ~207 950 |
| root vizibil | 1–5 | 0.24–0.25 / 0.30–0.33 / 0.35–0.39 / 0.56–1.12 | 0 | 0 / 0 | 0.32–0.48 | ~207 960 |

Toate 10: coadă maximă 1920, 0 measure/arrange/render, 0 invalidări Motion, 0 sample-uri respinse, ~1672 B/frame UI.
Variația max/min a P99 mixer: 1.11 (control ascuns), 1.11 (root vizibil). **Verdict: PASS.** Rezultatul este în acord cu
atribuirea de mai sus: eșecurile campaniei 1 și ale controalelor fără Motion țineau de încărcarea host-ului, nu de Motion audio.

## Documentație

- Ghid: `docs/timbre-guide.md` §5 „Animating sounds with Motion” (target schema, contexte, captură, ceas
  pending/pause/seek/loop, hidden/detach/Window.Hide, publicare, respingeri, API C#), secțiunile următoare renumerotate;
  `docs/CernealaMarkupGuide.md` §14 bullet `.sound.`. Exemplele xml/csharp sunt compilate de `DocumentedSoundExamplesCompile`.
- API canonic: `Cerneala.UI.Timbre.SoundMotionFacade`, `Cerneala.UI.Timbre.SoundMotionAnimationBuilder` (noi, în manifest),
  `SoundPlayback`, `SoundStartOptions`, `SoundClip`, `MotionExtensions`, `GeneratedMarkup`,
  `Cerneala.UI.Detective.SoundDiagnosticsSnapshot` (`MotionSamplesRejected`). Exemplele paginilor noi sunt compilate în
  `tests/Fixtures/TimbreConsumer/DocumentationExamples.cs`.

## Compatibilitate API (strict ApiCompat)

`api-compat.proj`, baseline binar propriu `26d8f96a` (worktree `C:\Users\lauri\Desktop\Cerneala-baseline-motion-26d8f96`),
strict + nume de parametri, fără suppression-uri. Compatibil: exit 0 (`api-compat-compatible.log`). Strict: exit 1 cu exact 9
adăugiri (`api-compat-strict.log`): tipurile `SoundMotionFacade`, `SoundMotionAnimationBuilder` (CP0001) și membrii
`SoundPlayback.VolumeParameter`, `GeneratedMarkup.StartSoundMotion`, `GeneratedMarkup.StartSoundMotionProperty`,
`MotionExtensions.Motion(SoundPlayback)`, `MotionExtensions.Motion(SoundPlayback, UIRoot)`,
`SoundDiagnosticsSnapshot.MotionSamplesRejected` get/init (CP0002). Nicio eliminare sau modificare.

## Verificare finală

`dotnet build .\Cerneala.slnx -c Release -m:1` → 0 erori (`full-solution-build.log`). `dotnet test .\Cerneala.slnx -c Release
--no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1` și `CERNEALA_TIMBRE_AUDIO_DEVICE=1` (`full-solution.log`,
`full-solution/*.trx`):

| Proiect | Rezultat |
| --- | --- |
| Cerneala.Tests | 4328 passed |
| Cerneala.Tests.SdlGpu | 1009 passed, 5 skipped (preexistente) |
| Cerneala.Tests.SourceGen | 640 passed, 1 failed → corectat, rerulat integral: **641 passed** (`sourcegen-rerun.trx`) |
| Cerneala.Tests.Language | 371 passed, 1 skipped (preexistent) |
| Cerneala.Tests.Timbre | 368 passed |
| Cerneala.Tests.Scene2DImporters / Scene2DPackages | 173 / 104 passed |
| Cerneala.Tests.VisualStudio (incl. manifest API) | 48 passed |
| Cerneala.Tests.LanguageServer / PreviewHost | 45 / 22 passed |
| Cerneala.Tests.SceneVillage | 43 passed, 1 skipped (preexistent) |
| Cerneala.Tetris.Tests | 31 passed |

Eșecul din SourceGen era exemplul nou din `docs/timbre-guide.md` §5 în `DocumentedSoundExamplesCompile`: sursa sintetizată de
test nu avea `using System;` (proiectele reale îl primesc prin implicit usings) → `TimeSpan` nerezolvat. Harness-ul primește
`using System;` și usings-urile Motion; ghidul a rămas neschimbat. Codul de producție nu s-a schimbat după rulare.

Matrice nativă Windows după ultima modificare de cod (`native/<mod>/run.log`): `timbre-motion` → `SDL_GPU_SMOKE_OK
scenarios=8 opens=1` (streaming 65536 B în timpul fade-ului); regresii `timbre-markup` → `scenarios=10 opens=1`,
`timbre` (C#) → `scenarios=18 opens=2`; toate exit 0. Rularea `native/timbre-motion-diagnostics.json` din rădăcină este
rularea anterioară corecției de alocare și a probei streaming, păstrată ca istoric. Runtime-ul non-Windows este N/A conform
indexului §7.

Publish `tests/Cerneala.SdlGpuSmoke` pentru win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64: exit 0 pe toate,
fixture-urile `timbre/*` copiate, tipul generat `TimbreMotionPanel` prezent în asamblare (`six-rid-publish.log`).

Review: `git diff --check` fără erori; diff-ul de producție nu conține cod de debug, probe sau `NotImplementedException`;
probe-urile temporare de alocare/depanare au fost șterse. Worktree-ul `Cerneala-baseline-motion-26d8f96` rămâne ca intrare
reproductibilă pentru ApiCompat, ca baseline-urile planurilor anterioare.

## Rămas în afara planului

- Validarea auditivă umană a inițiativei (index §7) nu este parte din acest plan și nu este pretinsă.
