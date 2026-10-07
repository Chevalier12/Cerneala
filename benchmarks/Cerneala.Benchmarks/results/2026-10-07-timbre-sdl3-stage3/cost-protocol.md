# Timbre SDL3 backend cost gate: protocol fixat înaintea măsurării

Data: 2026-10-07. Scris înaintea campaniei; nu se modifică după rezultate.

## Fixture

- Runner: `benchmarks/Cerneala.Benchmarks` mod `--timbre-core <report.json>` (`TimbreCoreBenchmarkRunner.cs`), Release, procese separate, același fixture ca gate-ul core: 32 voices looping (28 preload + 4 streaming), fiecare cu `LowPass(2000 Hz)` + `Delay(0.25 s, 0.4, 0.3)`, Volume 0.5, blocuri de 480 frame-uri, coada software 40 ms, 1000 blocuri warmup + 10000 măsurate, apoi 200 de porniri ale unui clip plain pregătit cu cele 32 voices active.
- Streaming: `TIMBRE_BENCH_DECODED_CORPUS=tests/Cerneala.Tests.Timbre/Corpus` — cele 4 voices streaming decodează MP3 lung 22.05 kHz, Vorbis lung, Opus lung și MP3 CBR128 44.1 kHz pe pump-urile lor (codecuri reale + conversie).
- Consumator, campania **SDL** (`TIMBRE_BENCH_OUTPUT=sdl`): `SdlSoundOutput` peste `NativeSdlAudioApi`, device implicit real (WASAPI pe hostul de referință), cu `SdlPlatformLifetime` pe firul principal. Underrun-ul de device în fereastra măsurată este estimarea SDL (`additional_amount` cumulat / 8 bytes per frame) dintre warmup și final; coada maximă este high-water-ul output-ului după fiecare `Put`.
- Consumator, campania **emulator** (regresie core după schimbarea politicii de underrun/priming din etapa 2): emulatorul de device existent, neschimbat.
- Instrumentare: `ISoundBlockObserver` pe firul mixer (selecție, render, mix, clipping, `Submit` — inclusiv `SDL_PutAudioStreamData`); alocări prin `GC.GetAllocatedBytesForCurrentThread()` pe același interval.
- Host de referință: acest Windows 11 x64 (procesor raportat în JSON), .NET 8 Release, fără alte sarcini pornite intenționat.

## Praguri (index §7, aprobate) — per proces

| Metrică | Prag |
| --- | --- |
| P99 cost mixer per bloc | < 2.5 ms (25% din 10 ms) |
| Alocări managed pe firul mixer în fereastra măsurată | 0 bytes |
| Coada software maximă | ≤ 1920 frame-uri (40 ms) |
| Underrun device în fereastra măsurată | 0 frame-uri |
| Underrun voice (padding de sursă) în fereastra măsurată | 0 frame-uri |
| P95 `Play` clip pregătit → primul PCM queued | < 50 ms |

Suplimentar pentru SDL (churn de resurse): exact 1 deschidere de output pe proces (fără reopen/retry), 0 late callbacks.

## Regula de decizie și variație

- PASS numai dacă fiecare din cele 5 procese ale unei campanii satisface toate pragurile. Fără reluări care să înlocuiască un proces eșuat; un eșec este FAIL.
- Raportat: max/min între procese pentru P99 mixer. O campanie neexecutată complet (crash, timeout, device indisponibil) este **inconclusive/blocked**, nu PASS.
- Pilotul (`TIMBRE_BENCH_PILOT=1`) validează doar fixture-ul și câmpurile; nu intră în verdict și nu ajustează pragurile.
- Fără gate GPU și fără prag DAC fizic.
