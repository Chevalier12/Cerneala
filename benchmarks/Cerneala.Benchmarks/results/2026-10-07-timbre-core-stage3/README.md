# Timbre core stage 3: concurrency, bugete și API/docs

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-core-runtime.md`, etapa 3.

## Contoare și Detective

- Contoare interne noi înaintea stress-ului: `LiveScopes` și `PendingLoads` (lângă readers/pumps/buffers streaming/cache/stare DSP/publicații de parametri din etapele precedente).
- Defect găsit la adăugarea `LiveScopes`: scope-urile dispuse rămâneau în lista runtime-ului, deci fiecare ciclu attach/detach al unui element creștea lista nelimitat. Ownerul (runtime-ul) le scoate acum la dispose; stress-ul verifică `LiveScopes = 0` la final.
- Expunere Detective justificată: `Detective.CaptureSound()` → `SoundDiagnosticsSnapshot` (output deschis, redări active, eșecuri, underrun, clipping, memorie cache/streaming/DSP), `null` fără runtime. Read-only; Detective nu deține și nu pornește audio. Contoarele de transport detaliate rămân interne (folosite de teste și benchmark).

## Calea realtime: alocări și cod străin pe firul mixer

Pilotul fixture-ului de cost (care validează doar fixture-ul) a arătat 128 B alocați pe 490/500 de blocuri pe firul mixer. RED permanent: `RealtimeAllocationTests` (`stage3-allocation-red.trx`): preload 0 B, streaming ~124 B/bloc.

1. **Ipoteză confirmată:** `SemaphoreSlim.Release()` din mixer completa waiter-ul `WaitAsync(CancellationToken)` al pump-ului, iar continuarea era pusă în coadă printr-un work item alocat. Înlocuit cu `AsyncAutoResetSignal` (single-waiter `IValueTaskSource`, continuarea este chiar state machine-ul pump-ului) → GREEN.
2. **Sub încărcarea paralelă a suitei:** rar 64 B / 4352 B (segment al cozii ThreadPool) — orice punere în coada thread pool-ului din mixer poate aloca, iar `feed.Stop()` rula callback-urile `CancellationTokenSource.Cancel()` (cod al readerului) sincron pe mixer. Ownerul corect: un `PumpDispatcher` per runtime. Mixer-ul setează numai flag-uri per feed și un eveniment; dispatcher-ul face wake/cancel. Defect secundar găsit de stress și corectat: o cerere de stop sosită după ultima trecere a dispatcher-ului la dispose s-ar fi pierdut (pump necanceled) — cererea se execută acum inline dacă dispatcher-ul este dispus, cu flag-ul setat înaintea verificării.

## Stress concurent (`ConcurrencyStressTests`)

Două seed-uri înghețate (20261007, 17), 4 fire dedicate × 2000 operații (Play preload/streaming/lanț/loop/handle, Pause, Resume, Seek, Cancel, Volume, Set, slot cancel, dispose/recreare scope, sursă care eșuează), sink care consumă liber. Verificări: nicio excepție în afara respingerilor contractate (terminal/dispus, `ArgumentOutOfRange` la seek peste durată, `VoiceLimitExceeded`); fiecare redare terminală cu rezultatul egal cu starea și eroare numai la `Failed`; toate cererile de seek terminate; după shutdown voices/readers/pumps/încărcări/buffere/stare DSP/scope-uri/cache = 0, output închis, `Started = Completed + Canceled + Failed`; coada ≤ 1920. Firele de lucru sunt dedicate (nu thread pool) ca să nu înfometeze loaderii și pump-urile.

Corpusul complet (124 teste, execuție paralelă xunit) rulat de 50 de ori consecutiv după corecții: 0 rulări eșuate.

## Consumer extern

`tests/Fixtures/TimbreConsumer` (fără `InternalsVisibleTo`, fără markup/Aspect/SDL): `StandaloneUsage` compilează exemplul din §2.1; `ConsumerScenarios` execută overlap/cancel per identitate, Loop la start, Pause/SeekAsync/Position/Duration/Resume, slot vs instanță (replacement, cancel pe referință veche, cancel pe slot), rezultat final, owner access pe `UIElement`/`Scene2D` cu detach/reattach și dispose; `DocumentationExamples` compilează exemplele paginilor canonice. Rulat prin `ExternalConsumerTests`.

## Cost

Protocolul și regula de decizie: `cost-protocol.md` (scrise înaintea măsurării).

Host: Intel Core i5-9300H (8 logici), Windows 10.0.26200, .NET 8.0.30 x64, Release. Rapoarte brute: `campaign/process-1..5.json`, agregat `campaign/summary.json`, script `Invoke-TimbreCoreCost.ps1`. Pilotul (`pilot.json`, înainte de corecția alocărilor; `pilot-2.json`, după) a validat numai fixture-ul.

| Proces | P50 ms | P95 ms | P99 ms | Max ms | Alocări B | Coadă max | Underrun device | Underrun voice | Play→PCM P95 ms |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 0.28 | 0.35 | 0.39 | 2.15 | 0 | 1920 | 0 | 0 | 16.26 |
| 2 | 0.27 | 0.33 | 0.38 | 0.89 | 0 | 1920 | 0 | 0 | 16.17 |
| 3 | 0.26 | 0.30 | 0.33 | 0.47 | 0 | 1920 | 0 | 0 | 15.83 |
| 4 | 0.25 | 0.29 | 0.32 | 0.78 | 0 | 1920 | 0 | 0 | 16.04 |
| 5 | 0.26 | 0.29 | 0.32 | 0.71 | 0 | 1920 | 0 | 0 | 16.02 |

Variație max/min între procese: P99 1.22×, P95 1.18×, latență P95 1.03×. **Verdict: PASS** — fiecare proces trece toate pragurile pre-înregistrate (P99 < 2.5 ms, 0 B pe firul mixer în 10000 blocuri măsurate, coadă ≤ 1920, 0 underrun, P95 < 50 ms). Latența Play→primul PCM queued este dominată de granularitatea emulatorului de device (~15.6 ms, tick-ul timer-ului Windows), nu de mixer. Memorie la final: cache 3,110,400 B, buffere streaming 4 × 64 KiB, stare DSP 32 × 96,000 B; 1 colecție gen0 per proces (pregătirea, în afara ferestrei realtime).

## Verificare finală

- `stage3-timbre.trx`: `Cerneala.Tests.Timbre` 125/125 (stare de cod finală).
- `api-compat.md` / `api-compat.log`: strict ApiCompat pe baseline-ul etapei 0, exit 0.
- `full-solution.log`: `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` după build-ul complet al soluției — 0 eșecuri în toate proiectele: Tests 4195 (2 skip), Timbre 125, SourceGen 625, SdlGpu 563 (235 skip: teste native opt-in `CERNEALA_SDL_NATIVE_TESTS` neactivat), Language 259 (1 skip), LanguageServer 40, PreviewHost 17, Scene2DImporters 173, Scene2DPackages 104, SceneVillage 36 (8 skip), VisualStudio 47 (include validarea manifestului API), Tetris 31. Skip-urile sunt preexistente și independente de Timbre.
- Nu se pretinde output fizic, codec-uri sau validare auditivă umană: acestea aparțin planurilor decoding/SDL3 și gate-ului auditiv din index §7.
